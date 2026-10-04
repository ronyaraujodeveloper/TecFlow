using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TecFlow.Business.Dto;
using TecFlow.Business.Integrations.Telegram;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Security;
using TecFlow.Business.Service.Security;
using TecFlow.Business.Service.Telegram;
using TecFlow.Core.Entities;
using TecFlow.Database;

namespace TecFlow.Infrastructure.Services.WhatsApp;

public sealed class TelegramIntegrationService : ITelegramIntegrationService
{
    private readonly AppDbContext _context;
    private readonly ITelegramApiService _telegramApi;
    private readonly TelegramBotOptions _telegramOptions;
    private readonly WebhookSecurityOptions _webhookOptions;
    private readonly ILogger<TelegramIntegrationService> _logger;

    public TelegramIntegrationService(
        AppDbContext context,
        ITelegramApiService telegramApi,
        IOptions<TelegramBotOptions> telegramOptions,
        IOptions<WebhookSecurityOptions> webhookOptions,
        ILogger<TelegramIntegrationService> logger)
    {
        _context = context;
        _telegramApi = telegramApi;
        _telegramOptions = telegramOptions.Value;
        _webhookOptions = webhookOptions.Value;
        _logger = logger;
    }

    public async Task<TelegramIntegrationResponseDto> GetMineAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var row = await EnsureRowAsync(userId, cancellationToken);
        return Ok(Map(row));
    }

    public async Task<TelegramIntegrationResponseDto> SaveAsync(
        int userId,
        SaveTelegramIntegrationDto request,
        CancellationToken cancellationToken = default)
    {
        request ??= new SaveTelegramIntegrationDto();
        var row = await ApplyFieldsAsync(userId, request, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(Map(row), "Integração Telegram salva.");
    }

    public async Task<TelegramIntegrationResponseDto> ConnectAsync(
        int userId,
        SaveTelegramIntegrationDto request,
        CancellationToken cancellationToken = default)
    {
        request ??= new SaveTelegramIntegrationDto();
        if (TelegramBotRules.IsPlaceholderToken(request.Token))
        {
            return Fail(TelegramBotRules.MissingTokenMessage);
        }

        var row = await ApplyFieldsAsync(userId, request, cancellationToken);
        if (string.IsNullOrWhiteSpace(row.Token) || TelegramBotRules.IsPlaceholderToken(row.Token))
        {
            return Fail(TelegramBotRules.MissingTokenMessage);
        }

        if (string.IsNullOrWhiteSpace(row.ChatId))
        {
            return Fail("Informe o ChatId do canal ou grupo.");
        }

        TelegramBotIdentityDto? identity;
        try
        {
            identity = await _telegramApi.ValidateBotTokenAsync(row.Token, cancellationToken);
        }
        catch
        {
            return Fail(TelegramBotRules.InvalidTokenMessage);
        }

        if (identity is null)
        {
            return Fail(TelegramBotRules.InvalidTokenMessage);
        }

        var webhookUrl = _telegramOptions.BuildUserWebhookUrl(userId);
        var webhookRegistrado = false;
        try
        {
            webhookRegistrado = await _telegramApi.SetWebhookAsync(
                row.Token,
                webhookUrl,
                _webhookOptions.Secret,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Não foi possível registrar o webhook do Telegram (provavelmente ambiente localhost). Prosseguindo com fallback de Chat ID manual.");
        }

        row.Token = row.Token!.Trim();
        row.BotUsername = string.IsNullOrWhiteSpace(identity.Username)
            ? row.BotUsername
            : identity.Username.Trim().TrimStart('@');
        if (!string.IsNullOrWhiteSpace(_webhookOptions.Secret))
        {
            row.ApiKey = _webhookOptions.Secret;
        }

        row.IsActive = true;
        row.SessionData = System.Text.Json.JsonSerializer.Serialize(new
        {
            botId = identity.Id,
            bot = row.BotUsername,
            chatId = row.ChatId,
            webhookUrl,
            webhookRegistered = webhookRegistrado,
            status = webhookRegistrado ? TelegramBotRules.ConnectedLabel : TelegramBotRules.DispatchModeLabel,
            mode = webhookRegistrado ? "listen" : "dispatch",
            connectedAt = DateTime.UtcNow
        });
        row.Touch();
        await _context.SaveChangesAsync(cancellationToken);

        var mapped = Map(row);
        if (!webhookRegistrado)
        {
            return Ok(mapped, TelegramBotRules.DispatchModeSavedMessage);
        }

        return Ok(mapped, TelegramBotRules.ConnectedWebhookMessage);
    }

    private async Task<TelegramIntegration> ApplyFieldsAsync(
        int userId,
        SaveTelegramIntegrationDto request,
        CancellationToken cancellationToken)
    {
        var row = await EnsureRowAsync(userId, cancellationToken);
        if (!IsMaskedOrEmpty(request.Token))
        {
            row.Token = request.Token!.Trim();
        }

        if (!IsMaskedOrEmpty(request.ApiKey))
        {
            row.ApiKey = request.ApiKey!.Trim();
        }

        row.ChatId = string.IsNullOrWhiteSpace(request.ChatId) ? row.ChatId : request.ChatId.Trim();
        row.BotUsername = string.IsNullOrWhiteSpace(request.BotUsername)
            ? row.BotUsername
            : request.BotUsername.Trim().TrimStart('@');
        row.IsActive = true;
        row.Touch();
        return row;
    }

    private async Task<TelegramIntegration> EnsureRowAsync(int userId, CancellationToken cancellationToken)
    {
        var row = await _context.TelegramIntegrations
            .OrderByDescending(item => item.IsActive)
            .ThenByDescending(item => item.Id)
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);

        if (row is not null)
        {
            IntegrationOwnershipGuard.EnsureOwner(row.UserId, userId);
            return row;
        }

        row = new TelegramIntegration
        {
            UserId = userId,
            IsActive = true
        };
        await _context.TelegramIntegrations.AddAsync(row, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        IntegrationOwnershipGuard.EnsureOwner(row.UserId, userId);
        return row;
    }

    private static bool IsMaskedOrEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) || value == SecretMasking.MaskedValue;

    private static TelegramIntegrationDto Map(TelegramIntegration row)
    {
        IntegrationOwnershipGuard.EnsureOwner(row.UserId, row.UserId);
        var webhookRegistered = TelegramBotRules.IsWebhookRegistered(row.SessionData);
        var hasToken = !string.IsNullOrEmpty(row.Token);
        var hasUsername = !string.IsNullOrWhiteSpace(row.BotUsername);
        var hasChatId = !string.IsNullOrWhiteSpace(row.ChatId);
        var status = TelegramBotRules.ResolveUiStatus(row.IsActive, hasToken, hasUsername, hasChatId, webhookRegistered);
        var connected = status is TelegramBotRules.ConnectedLabel or TelegramBotRules.DispatchModeLabel;
        return new TelegramIntegrationDto
        {
            Id = row.Id,
            UserId = row.UserId,
            ChatId = row.ChatId,
            BotUsername = row.BotUsername,
            IsActive = row.IsActive,
            HasToken = hasToken,
            TokenMasked = SecretMasking.Mask(row.Token),
            HasApiKey = !string.IsNullOrEmpty(row.ApiKey),
            ApiKeyMasked = SecretMasking.Mask(row.ApiKey),
            HasSessionData = !string.IsNullOrEmpty(row.SessionData),
            SessionDataMasked = SecretMasking.Mask(row.SessionData),
            IsConnected = connected,
            WebhookRegistered = webhookRegistered,
            UiStatusLabel = status
        };
    }

    private static TelegramIntegrationResponseDto Ok(TelegramIntegrationDto data, string message = "OK") =>
        new()
        {
            Status = true,
            Descricao = message,
            Data = data
        };

    private static TelegramIntegrationResponseDto Fail(string message) =>
        new()
        {
            Status = false,
            Descricao = message
        };
}
