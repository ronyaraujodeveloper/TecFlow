using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TecFlow.Business.Dto;
using TecFlow.Business.Integrations.WhatsApp;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Security;
using TecFlow.Business.Service.WhatsApp;
using TecFlow.Core.Entities;
using TecFlow.Database;

namespace TecFlow.Infrastructure.Services.WhatsApp;

public sealed class WhatsAppSessionService : IWhatsAppSessionService
{
    private readonly AppDbContext _context;
    private readonly IEvolutionApiService _evolution;
    private readonly EvolutionApiOptions _evolutionOptions;
    private readonly ILogger<WhatsAppSessionService> _logger;

    public WhatsAppSessionService(
        AppDbContext context,
        IEvolutionApiService evolution,
        IOptions<EvolutionApiOptions> evolutionOptions,
        ILogger<WhatsAppSessionService> logger)
    {
        _context = context;
        _evolution = evolution;
        _evolutionOptions = evolutionOptions.Value;
        _logger = logger;
    }

    public async Task<WhatsAppIntegrationResponseDto> GetMineAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var row = await EnsureRowAsync(userId, cancellationToken);
        return Ok(Map(row));
    }

    public async Task<WhatsAppIntegrationResponseDto> ConnectAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var row = await EnsureRowAsync(userId, cancellationToken);
            var created = await _evolution.CreateInstanceAsync(row.InstanceName, cancellationToken);
            if (!created)
            {
                return Fail(
                    _evolutionOptions.IsConfigured
                        ? WhatsAppSessionRules.EvolutionUnreachableMessage
                        : "Não foi possível registrar a instância na Evolution API. Verifique EvolutionApi:BaseUrl e ApiKey.");
            }

            var qr = await _evolution.FetchQrCodeAsync(row.InstanceName, cancellationToken);
            row.ConnectionStatus = WhatsAppConnectionStatuses.AwaitingQr;
            row.IsActive = true;
            row.ApiKey = string.IsNullOrWhiteSpace(_evolutionOptions.ApiKey) ? row.ApiKey : _evolutionOptions.ApiKey;
            row.Token = row.InstanceName;
            PersistSessionSnapshot(row);
            row.Touch();
            await _context.SaveChangesAsync(cancellationToken);

            var dto = Map(row);
            dto.QrCodeDataUrl = WhatsAppSessionRules.NormalizeQrDataUrl(qr);
            if (string.IsNullOrWhiteSpace(dto.QrCodeDataUrl))
            {
                return Fail("Instância criada, mas o QR Code ainda não está disponível. Tente novamente em instantes.");
            }

            return Ok(dto, "Leia o QR Code no WhatsApp do celular.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao gerar conexão WhatsApp. UserId={UserId}", userId);
            return Fail(WhatsAppSessionRules.EvolutionUnreachableMessage);
        }
    }

    public async Task<WhatsAppIntegrationResponseDto> RefreshStatusAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var row = await EnsureRowAsync(userId, cancellationToken);
        try
        {
            var state = await _evolution.GetConnectionStateAsync(row.InstanceName, cancellationToken);
            row.ConnectionStatus = WhatsAppSessionRules.MapFromEvolutionState(state.State);
            if (row.ConnectionStatus == WhatsAppConnectionStatuses.Connected)
            {
                row.PhoneNumber = NormalizePhone(state.PhoneNumber) ?? row.PhoneNumber;
                row.ProfileName = string.IsNullOrWhiteSpace(state.ProfileName) ? row.ProfileName : state.ProfileName.Trim();
                if (!string.IsNullOrWhiteSpace(state.ProfilePictureUrl))
                {
                    row.ProfilePictureUrl = state.ProfilePictureUrl.Trim();
                }
                row.LastConnectedAt = DateTime.UtcNow;
                row.IsActive = true;
            }

            PersistSessionSnapshot(row);
            row.Touch();
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao consultar estado Evolution. UserId={UserId}", userId);
        }

        return Ok(Map(row));
    }

    public async Task<WhatsAppIntegrationResponseDto> UpdateBotPreferencesAsync(
        int userId,
        WhatsAppBotPreferencesDto preferences,
        CancellationToken cancellationToken = default)
    {
        preferences ??= new WhatsAppBotPreferencesDto();
        var row = await EnsureRowAsync(userId, cancellationToken);
        row.EnableAutoConvertBot = preferences.EnableAutoConvertBot;
        row.ReplyToPrivateMessages = preferences.ReplyToPrivateMessages;
        row.ReplyToGroupMessages = preferences.ReplyToGroupMessages;
        row.Touch();
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(Map(row), "Preferências do bot salvas.");
    }

    private async Task<WhatsAppIntegration> EnsureRowAsync(int userId, CancellationToken cancellationToken)
    {
        var instanceName = WhatsAppSessionRules.BuildInstanceName(userId);
        var row = await _context.WhatsAppIntegrations
            .OrderByDescending(item => item.IsActive)
            .ThenByDescending(item => item.Id)
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);

        if (row is not null)
        {
            IntegrationOwnershipGuard.EnsureOwner(row.UserId, userId);
            if (!string.Equals(row.InstanceName, instanceName, StringComparison.OrdinalIgnoreCase))
            {
                row.InstanceName = instanceName;
                row.Touch();
                await _context.SaveChangesAsync(cancellationToken);
            }

            return row;
        }

        row = new WhatsAppIntegration
        {
            UserId = userId,
            InstanceName = instanceName,
            ConnectionStatus = WhatsAppConnectionStatuses.Disconnected,
            IsActive = true,
            EnableAutoConvertBot = true,
            ReplyToPrivateMessages = true,
            ReplyToGroupMessages = false
        };
        await _context.WhatsAppIntegrations.AddAsync(row, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        IntegrationOwnershipGuard.EnsureOwner(row.UserId, userId);
        return row;
    }

    private static void PersistSessionSnapshot(WhatsAppIntegration row) =>
        row.SessionData = System.Text.Json.JsonSerializer.Serialize(new
        {
            instance = row.InstanceName,
            status = row.ConnectionStatus,
            phone = row.PhoneNumber,
            lastConnectedAt = row.LastConnectedAt
        });

    private static WhatsAppIntegrationDto Map(WhatsAppIntegration row)
    {
        IntegrationOwnershipGuard.EnsureOwner(row.UserId, row.UserId);
        return new WhatsAppIntegrationDto
        {
            Id = row.Id,
            UserId = row.UserId,
            InstanceName = row.InstanceName,
            ConnectionStatus = row.ConnectionStatus,
            UiStatusLabel = WhatsAppSessionRules.ToUiLabel(row.ConnectionStatus),
            PhoneNumber = row.PhoneNumber,
            ProfileName = row.ProfileName,
            ProfilePictureUrl = row.ProfilePictureUrl,
            CreatedAt = row.CreatedAt,
            LastConnectedAt = row.LastConnectedAt,
            IsActive = row.IsActive,
            IsConnected = row.ConnectionStatus == WhatsAppConnectionStatuses.Connected,
            EnableAutoConvertBot = row.EnableAutoConvertBot,
            ReplyToPrivateMessages = row.ReplyToPrivateMessages,
            ReplyToGroupMessages = row.ReplyToGroupMessages,
            HasToken = !string.IsNullOrEmpty(row.Token),
            TokenMasked = SecretMasking.Mask(row.Token),
            HasApiKey = !string.IsNullOrEmpty(row.ApiKey),
            ApiKeyMasked = SecretMasking.Mask(row.ApiKey),
            HasSessionData = !string.IsNullOrEmpty(row.SessionData),
            SessionDataMasked = SecretMasking.Mask(row.SessionData)
        };
    }

    private static string? NormalizePhone(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var digits = new string(raw.Where(char.IsDigit).ToArray());
        return string.IsNullOrWhiteSpace(digits) ? raw.Trim() : digits;
    }

    private static WhatsAppIntegrationResponseDto Ok(WhatsAppIntegrationDto data, string message = "OK") =>
        new()
        {
            Status = true,
            Descricao = message,
            Data = data
        };

    private static WhatsAppIntegrationResponseDto Fail(string message) =>
        new()
        {
            Status = false,
            Descricao = message
        };
}
