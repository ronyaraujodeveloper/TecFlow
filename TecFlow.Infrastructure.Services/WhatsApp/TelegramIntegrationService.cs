using Microsoft.EntityFrameworkCore;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Security;
using TecFlow.Core.Entities;
using TecFlow.Database;

namespace TecFlow.Infrastructure.Services.WhatsApp;

public sealed class TelegramIntegrationService : ITelegramIntegrationService
{
    private readonly AppDbContext _context;

    public TelegramIntegrationService(AppDbContext context)
    {
        _context = context;
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
        row.SessionData = System.Text.Json.JsonSerializer.Serialize(new
        {
            chatId = row.ChatId,
            bot = row.BotUsername,
            updatedAt = DateTime.UtcNow
        });
        row.Touch();
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(Map(row), "Integração Telegram salva.");
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
        return new TelegramIntegrationDto
        {
            Id = row.Id,
            UserId = row.UserId,
            ChatId = row.ChatId,
            BotUsername = row.BotUsername,
            IsActive = row.IsActive,
            HasToken = !string.IsNullOrEmpty(row.Token),
            TokenMasked = SecretMasking.Mask(row.Token),
            HasApiKey = !string.IsNullOrEmpty(row.ApiKey),
            ApiKeyMasked = SecretMasking.Mask(row.ApiKey),
            HasSessionData = !string.IsNullOrEmpty(row.SessionData),
            SessionDataMasked = SecretMasking.Mask(row.SessionData)
        };
    }

    private static TelegramIntegrationResponseDto Ok(TelegramIntegrationDto data, string message = "OK") =>
        new()
        {
            Status = true,
            Descricao = message,
            Data = data
        };
}
