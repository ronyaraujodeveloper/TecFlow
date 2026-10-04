using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Groups;
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
        var names = await LoadChannelNamesAsync(userId, cancellationToken);
        var groups = await _context.TelegramGroups
            .Where(item => item.UserId == userId && item.IsActive)
            .OrderByDescending(item => item.IsAdmin)
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);
        var campaigns = await _context.TelegramBroadcastCampaigns
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.ScheduledAt)
            .ThenByDescending(item => item.Id)
            .Take(50)
            .ToListAsync(cancellationToken);

        return Ok(
            campaigns.Select(item => MapCampaign(item, names)).ToList(),
            groups.Select(MapGroup).ToList(),
            defaultChatId: integration?.ChatId);
    }

    public async Task<TelegramBroadcastResponseDto> SyncChannelsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var integration = await LoadOwnedIntegrationAsync(userId, cancellationToken);
        if (integration is null || string.IsNullOrWhiteSpace(integration.Token))
        {
            return Fail("Conecte o bot do Telegram antes de sincronizar canais.");
        }

        var seeds = new List<string>();
        if (!string.IsNullOrWhiteSpace(integration.ChatId))
        {
            seeds.Add(integration.ChatId);
        }

        var existing = await _context.TelegramGroups
            .Where(item => item.UserId == userId)
            .ToListAsync(cancellationToken);
        seeds.AddRange(existing.Select(item => item.ChatId));

        var captured = await _context.GroupCapturedMessages
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.Channel == GroupOfferCaptureRules.TelegramChannel)
            .Select(item => item.GroupKey)
            .ToListAsync(cancellationToken);
        foreach (var key in captured)
        {
            var chatId = key.Contains(':', StringComparison.Ordinal)
                ? key[(key.IndexOf(':') + 1)..]
                : key;
            if (!string.IsNullOrWhiteSpace(chatId))
            {
                seeds.Add(chatId.Trim());
            }
        }

        var remote = await _telegramApi.FetchAdminChatsAsync(integration.Token, seeds, cancellationToken);
        foreach (var item in remote)
        {
            var row = existing.FirstOrDefault(group =>
                string.Equals(group.ChatId, item.ChatId, StringComparison.OrdinalIgnoreCase));
            if (row is null)
            {
                row = new TelegramGroup { UserId = userId, ChatId = item.ChatId };
                await _context.TelegramGroups.AddAsync(row, cancellationToken);
                existing.Add(row);
            }

            row.Name = TelegramBroadcastRules.ResolveFriendlyName(
                string.IsNullOrWhiteSpace(item.GroupName) ? item.Name : item.GroupName,
                item.ChatId);
            row.ParticipantCount = item.ParticipantCount;
            row.IsAdmin = item.IsAdmin;
            row.IsActive = true;
            row.Touch();
        }

        if (remote.Count == 0 && !string.IsNullOrWhiteSpace(integration.ChatId))
        {
            var chatId = integration.ChatId.Trim();
            var row = existing.FirstOrDefault(group =>
                string.Equals(group.ChatId, chatId, StringComparison.OrdinalIgnoreCase));
            if (row is null)
            {
                row = new TelegramGroup
                {
                    UserId = userId,
                    ChatId = chatId,
                    Name = TelegramBroadcastRules.UntitledGroupName,
                    IsAdmin = true,
                    IsActive = true
                };
                await _context.TelegramGroups.AddAsync(row, cancellationToken);
            }
            else
            {
                row.IsActive = true;
                row.Touch();
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        var listed = await ListCampaignsAsync(userId, cancellationToken);
        listed.Descricao = listed.Groups.Count == 0
            ? "Nenhum canal encontrado. Envie uma mensagem no canal ou informe o Chat ID na conexão."
            : $"{listed.Groups.Count} canal(is) sincronizado(s).";
        return listed;
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
        var selected = request.SelectedChatIds.Count > 0 ? request.SelectedChatIds : request.TargetChatIds;
        var chatIds = TelegramBroadcastRules.ResolveChatIds(
            TelegramBroadcastRules.SerializeChatIds(selected),
            request.TargetChatId ?? integration.ChatId);
        if (title.Length is < 3 or > 128)
        {
            return Fail("Informe um título entre 3 e 128 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            return Fail("Informe o texto da oferta.");
        }

        if (chatIds.Count == 0)
        {
            return Fail("Selecione ao menos um canal ou grupo.");
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
            TargetChatId = chatIds[0],
            TargetChatIdsJson = TelegramBroadcastRules.SerializeChatIds(chatIds),
            ScheduledAt = scheduledAt,
            IntervalSeconds = TelegramBroadcastRules.ClampIntervalSeconds(request.IntervalSeconds),
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
        var chatIds = TelegramBroadcastRules.ResolveChatIds(campaign.TargetChatIdsJson, campaign.TargetChatId);
        var interval = TimeSpan.FromSeconds(TelegramBroadcastRules.ClampIntervalSeconds(campaign.IntervalSeconds));
        var sent = 0;

        for (var index = 0; index < chatIds.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var chatId = chatIds[index];
            try
            {
                var ok = string.IsNullOrWhiteSpace(campaign.ImageUrl)
                    ? await _telegramApi.SendTextMessageAsync(
                        integration.Token,
                        chatId,
                        text,
                        cancellationToken)
                    : await _telegramApi.SendPhotoMessageAsync(
                        integration.Token,
                        chatId,
                        campaign.ImageUrl,
                        text,
                        cancellationToken);
                if (ok)
                {
                    sent++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha no disparo Telegram. CampaignId={CampaignId} ChatId={ChatId}", campaign.Id, chatId);
            }

            if (index < chatIds.Count - 1)
            {
                await Task.Delay(interval, cancellationToken);
            }
        }

        campaign.Status = sent > 0 ? TelegramBroadcastStatuses.Completed : TelegramBroadcastStatuses.Failed;
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

    private async Task<Dictionary<string, string>> LoadChannelNamesAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var groups = await _context.TelegramGroups
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .Select(item => new { item.ChatId, item.Name })
            .ToListAsync(cancellationToken);

        return groups
            .GroupBy(item => item.ChatId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.First().Name,
                StringComparer.OrdinalIgnoreCase);
    }

    private static TelegramGroupDto MapGroup(TelegramGroup group) =>
        new()
        {
            Id = group.Id,
            ChatId = group.ChatId,
            Name = group.Name,
            GroupName = string.IsNullOrWhiteSpace(group.Name)
                ? TelegramBroadcastRules.UntitledGroupName
                : group.Name,
            ParticipantCount = group.ParticipantCount,
            IsAdmin = group.IsAdmin,
            IsActive = group.IsActive
        };

    private static TelegramBroadcastCampaignDto MapCampaign(
        TelegramBroadcastCampaign campaign,
        IReadOnlyDictionary<string, string>? names = null)
    {
        var chatIds = TelegramBroadcastRules.ResolveChatIds(campaign.TargetChatIdsJson, campaign.TargetChatId);
        return new TelegramBroadcastCampaignDto
        {
            Id = campaign.Id,
            Title = campaign.Title,
            MessageText = campaign.MessageText,
            ImageUrl = campaign.ImageUrl,
            TargetChatId = chatIds.Count > 0 ? chatIds[0] : campaign.TargetChatId,
            TargetChatIds = chatIds.ToList(),
            TargetChatNames = chatIds
                .Select(id =>
                    names is not null
                    && names.TryGetValue(id, out var name)
                    && !string.IsNullOrWhiteSpace(name)
                        ? name
                        : id)
                .ToList(),
            ScheduledAt = campaign.ScheduledAt,
            IntervalSeconds = campaign.IntervalSeconds,
            Status = campaign.Status,
            UiStatusLabel = TelegramBroadcastRules.ToUiStatus(campaign.Status),
            CreatedAt = campaign.CreatedAt
        };
    }

    private static TelegramBroadcastResponseDto Ok(
        List<TelegramBroadcastCampaignDto> campaigns,
        List<TelegramGroupDto>? groups = null,
        string? defaultChatId = null) =>
        new()
        {
            Status = true,
            Descricao = "OK",
            Campaigns = campaigns,
            Groups = groups ?? [],
            DefaultChatId = defaultChatId
        };

    private static TelegramBroadcastResponseDto Fail(string message) =>
        new()
        {
            Status = false,
            Descricao = message
        };
}
