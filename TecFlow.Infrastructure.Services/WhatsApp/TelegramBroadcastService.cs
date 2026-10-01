using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Security;
using TecFlow.Business.Service.Telegram;
using TecFlow.Core.Entities;
using TecFlow.Database;

namespace TecFlow.Infrastructure.Services.WhatsApp;

public sealed class TelegramBroadcastService : ITelegramBroadcastService
{
    private readonly AppDbContext _context;
    private readonly ITelegramApiService _telegramApi;
    private readonly ILogger<TelegramBroadcastService> _logger;

    public TelegramBroadcastService(
        AppDbContext context,
        ITelegramApiService telegramApi,
        ILogger<TelegramBroadcastService> logger)
    {
        _context = context;
        _telegramApi = telegramApi;
        _logger = logger;
    }

    public async Task<TelegramBroadcastResponseDto> ListCampaignsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var integration = await LoadOwnedIntegrationAsync(userId, cancellationToken);
        var campaigns = await _context.TelegramBroadcastCampaigns
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.ScheduledAt)
            .ThenByDescending(item => item.Id)
            .Take(50)
            .ToListAsync(cancellationToken);

        return Ok(
            campaigns.Select(MapCampaign).ToList(),
            defaultChatId: integration?.ChatId);
    }

    public async Task<TelegramBroadcastResponseDto> ScheduleAsync(
        int userId,
        TelegramScheduleCampaignDto request,
        CancellationToken cancellationToken = default)
    {
        request ??= new TelegramScheduleCampaignDto();
        var integration = await LoadOwnedIntegrationAsync(userId, cancellationToken);
        if (integration is null || string.IsNullOrWhiteSpace(integration.Token))
        {
            return Fail("Conecte o bot do Telegram antes de agendar.");
        }

        var title = (request.Title ?? string.Empty).Trim();
        var message = (request.MessageText ?? string.Empty).Trim();
        var chatId = string.IsNullOrWhiteSpace(request.TargetChatId)
            ? integration.ChatId
            : request.TargetChatId.Trim();
        if (title.Length is < 3 or > 128)
        {
            return Fail("Informe um título entre 3 e 128 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            return Fail("Informe o texto da oferta.");
        }

        if (string.IsNullOrWhiteSpace(chatId))
        {
            return Fail("Informe o ChatId do canal ou grupo.");
        }

        var scheduledAt = request.ScheduledAt?.ToUniversalTime() ?? DateTime.UtcNow;
        if (scheduledAt < DateTime.UtcNow.AddMinutes(-2))
        {
            scheduledAt = DateTime.UtcNow;
        }

        var campaign = new TelegramBroadcastCampaign
        {
            UserId = userId,
            Title = title,
            MessageText = message,
            ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim(),
            TargetChatId = chatId,
            ScheduledAt = scheduledAt,
            Status = TelegramBroadcastStatuses.Pending
        };
        await _context.TelegramBroadcastCampaigns.AddAsync(campaign, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var listed = await ListCampaignsAsync(userId, cancellationToken);
        listed.Campaign = MapCampaign(campaign);
        listed.Descricao = "Campanha agendada.";
        return listed;
    }

    public async Task ProcessDueCampaignsAsync(CancellationToken cancellationToken = default)
    {
        var due = await _context.TelegramBroadcastCampaigns
            .Where(item =>
                item.Status == TelegramBroadcastStatuses.Pending
                && item.ScheduledAt <= DateTime.UtcNow)
            .OrderBy(item => item.ScheduledAt)
            .Take(5)
            .ToListAsync(cancellationToken);

        foreach (var campaign in due)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ProcessOneAsync(campaign, cancellationToken);
        }
    }

    private async Task ProcessOneAsync(TelegramBroadcastCampaign campaign, CancellationToken cancellationToken)
    {
        campaign.Status = TelegramBroadcastStatuses.Processing;
        campaign.Touch();
        await _context.SaveChangesAsync(cancellationToken);

        var integration = await _context.TelegramIntegrations
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.UserId == campaign.UserId && item.IsActive, cancellationToken);
        if (integration is null || string.IsNullOrWhiteSpace(integration.Token))
        {
            campaign.Status = TelegramBroadcastStatuses.Failed;
            campaign.Touch();
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        if (integration.UserId != campaign.UserId)
        {
            throw new UnauthorizedAccessException();
        }

        IntegrationOwnershipGuard.EnsureOwner(integration.UserId, campaign.UserId);

        var commissionLink = await _context.ShortAffiliateLinks
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(link => link.UserId == campaign.UserId && link.IsActive)
            .OrderByDescending(link => link.CreatedAt)
            .Select(link => link.AffiliateUrl)
            .FirstOrDefaultAsync(cancellationToken);
        var text = TelegramBroadcastRules.ApplyCommissionTag(campaign.MessageText, commissionLink);

        var ok = false;
        try
        {
            ok = string.IsNullOrWhiteSpace(campaign.ImageUrl)
                ? await _telegramApi.SendTextMessageAsync(
                    integration.Token,
                    campaign.TargetChatId,
                    text,
                    cancellationToken)
                : await _telegramApi.SendPhotoMessageAsync(
                    integration.Token,
                    campaign.TargetChatId,
                    campaign.ImageUrl,
                    text,
                    cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha no disparo Telegram. CampaignId={CampaignId}", campaign.Id);
        }

        campaign.Status = ok ? TelegramBroadcastStatuses.Completed : TelegramBroadcastStatuses.Failed;
        campaign.Touch();
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<TelegramIntegration?> LoadOwnedIntegrationAsync(int userId, CancellationToken cancellationToken)
    {
        var integration = await _context.TelegramIntegrations
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (integration is not null)
        {
            if (integration.UserId != userId)
            {
                throw new UnauthorizedAccessException();
            }

            IntegrationOwnershipGuard.EnsureOwner(integration.UserId, userId);
        }

        return integration;
    }

    private static TelegramBroadcastCampaignDto MapCampaign(TelegramBroadcastCampaign campaign) =>
        new()
        {
            Id = campaign.Id,
            Title = campaign.Title,
            MessageText = campaign.MessageText,
            ImageUrl = campaign.ImageUrl,
            TargetChatId = campaign.TargetChatId,
            ScheduledAt = campaign.ScheduledAt,
            Status = campaign.Status,
            UiStatusLabel = TelegramBroadcastRules.ToUiStatus(campaign.Status),
            CreatedAt = campaign.CreatedAt
        };

    private static TelegramBroadcastResponseDto Ok(
        List<TelegramBroadcastCampaignDto> campaigns,
        string? defaultChatId = null) =>
        new()
        {
            Status = true,
            Descricao = "OK",
            Campaigns = campaigns,
            DefaultChatId = defaultChatId
        };

    private static TelegramBroadcastResponseDto Fail(string message) =>
        new()
        {
            Status = false,
            Descricao = message
        };
}
