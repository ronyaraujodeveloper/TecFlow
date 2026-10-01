using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Security;
using TecFlow.Business.Service.WhatsApp;
using TecFlow.Core.Entities;
using TecFlow.Database;

namespace TecFlow.Infrastructure.Services.WhatsApp;

public sealed class WhatsAppBroadcastService : IWhatsAppBroadcastService
{
    private readonly AppDbContext _context;
    private readonly IEvolutionApiService _evolution;
    private readonly ILogger<WhatsAppBroadcastService> _logger;

    public WhatsAppBroadcastService(
        AppDbContext context,
        IEvolutionApiService evolution,
        ILogger<WhatsAppBroadcastService> logger)
    {
        _context = context;
        _evolution = evolution;
        _logger = logger;
    }

    public async Task<WhatsAppBroadcastResponseDto> ListGroupsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var groups = await _context.WhatsAppGroups
            .Where(group => group.UserId == userId && group.IsActive)
            .OrderByDescending(group => group.IsAdmin)
            .ThenBy(group => group.Name)
            .ToListAsync(cancellationToken);

        var integration = await _context.WhatsAppIntegrations
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

        return Ok(groups: groups.Select(MapGroup).ToList());
    }

    public async Task<WhatsAppBroadcastResponseDto> SyncGroupsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var instanceName = WhatsAppSessionRules.BuildInstanceName(userId);
        var integration = await _context.WhatsAppIntegrations
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (integration is null)
        {
            return Fail("Conecte o WhatsApp antes de sincronizar grupos.");
        }

        IntegrationOwnershipGuard.EnsureOwner(integration.UserId, userId);
        var remote = await _evolution.FetchUserGroupsAsync(instanceName, cancellationToken);
        if (remote.Count == 0)
        {
            return await ListGroupsAsync(userId, cancellationToken);
        }

        var existing = await _context.WhatsAppGroups
            .Where(group => group.UserId == userId)
            .ToListAsync(cancellationToken);

        foreach (var item in remote)
        {
            var row = existing.FirstOrDefault(group =>
                string.Equals(group.Jid, item.Jid, StringComparison.OrdinalIgnoreCase));
            if (row is null)
            {
                row = new WhatsAppGroup { UserId = userId, Jid = item.Jid };
                await _context.WhatsAppGroups.AddAsync(row, cancellationToken);
                existing.Add(row);
            }

            row.Name = string.IsNullOrWhiteSpace(item.Name) ? item.Jid : item.Name.Trim();
            row.ParticipantCount = item.ParticipantCount;
            row.IsAdmin = item.IsAdmin;
            row.IsActive = true;
            row.Touch();
        }

        await _context.SaveChangesAsync(cancellationToken);
        return await ListGroupsAsync(userId, cancellationToken);
    }

    public async Task<WhatsAppBroadcastResponseDto> ListCampaignsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var campaigns = await _context.WhatsAppBroadcastCampaigns
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.ScheduledAt)
            .ThenByDescending(item => item.Id)
            .Take(50)
            .ToListAsync(cancellationToken);

        return Ok(campaigns: campaigns.Select(MapCampaign).ToList());
    }

    public async Task<WhatsAppBroadcastResponseDto> ScheduleAsync(
        int userId,
        WhatsAppScheduleCampaignDto request,
        CancellationToken cancellationToken = default)
    {
        request ??= new WhatsAppScheduleCampaignDto();
        var title = (request.Title ?? string.Empty).Trim();
        var message = (request.MessageText ?? string.Empty).Trim();
        var jids = WhatsAppBroadcastRules.DeserializeJids(
            WhatsAppBroadcastRules.SerializeJids(request.TargetGroupJids));
        if (title.Length is < 3 or > 128)
        {
            return Fail("Informe um título entre 3 e 128 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            return Fail("Informe o texto da oferta.");
        }

        if (jids.Count == 0)
        {
            return Fail("Selecione ao menos um grupo.");
        }

        var scheduledAt = request.ScheduledAt?.ToUniversalTime() ?? DateTime.UtcNow;
        if (scheduledAt < DateTime.UtcNow.AddMinutes(-2))
        {
            scheduledAt = DateTime.UtcNow;
        }

        var campaign = new WhatsAppBroadcastCampaign
        {
            UserId = userId,
            Title = title,
            MessageText = message,
            ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim(),
            TargetGroupJidsJson = WhatsAppBroadcastRules.SerializeJids(jids),
            ScheduledAt = scheduledAt,
            IntervalSeconds = WhatsAppBroadcastRules.ClampIntervalSeconds(request.IntervalSeconds),
            Status = WhatsAppBroadcastStatuses.Pending
        };
        await _context.WhatsAppBroadcastCampaigns.AddAsync(campaign, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var listed = await ListCampaignsAsync(userId, cancellationToken);
        listed.Campaign = MapCampaign(campaign);
        listed.Descricao = "Campanha agendada.";
        return listed;
    }

    public async Task ProcessDueCampaignsAsync(CancellationToken cancellationToken = default)
    {
        var due = await _context.WhatsAppBroadcastCampaigns
            .Where(item =>
                item.Status == WhatsAppBroadcastStatuses.Pending
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

    private async Task ProcessOneAsync(WhatsAppBroadcastCampaign campaign, CancellationToken cancellationToken)
    {
        campaign.Status = WhatsAppBroadcastStatuses.Processing;
        campaign.Touch();
        await _context.SaveChangesAsync(cancellationToken);

        var jids = WhatsAppBroadcastRules.DeserializeJids(campaign.TargetGroupJidsJson);
        if (jids.Count == 0)
        {
            campaign.Status = WhatsAppBroadcastStatuses.Failed;
            campaign.Touch();
            await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        var instanceName = WhatsAppSessionRules.BuildInstanceName(campaign.UserId);
        var integration = await _context.WhatsAppIntegrations
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.UserId == campaign.UserId, cancellationToken);
        if (integration is null)
        {
            campaign.Status = WhatsAppBroadcastStatuses.Failed;
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
        var text = WhatsAppBroadcastRules.ApplyCommissionTag(campaign.MessageText, commissionLink);
        var interval = TimeSpan.FromSeconds(WhatsAppBroadcastRules.ClampIntervalSeconds(campaign.IntervalSeconds));
        var sent = 0;

        for (var index = 0; index < jids.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var jid = jids[index];
            try
            {
                var ok = string.IsNullOrWhiteSpace(campaign.ImageUrl)
                    ? await _evolution.SendTextMessageAsync(instanceName, jid, text, cancellationToken)
                    : await _evolution.SendMediaMessageAsync(
                        instanceName,
                        jid,
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
                _logger.LogWarning(
                    ex,
                    "Falha no disparo WhatsApp. CampaignId={CampaignId} Jid={Jid}",
                    campaign.Id,
                    jid);
            }

            if (index < jids.Count - 1)
            {
                await Task.Delay(interval, cancellationToken);
            }
        }

        campaign.Status = sent > 0 ? WhatsAppBroadcastStatuses.Completed : WhatsAppBroadcastStatuses.Failed;
        campaign.Touch();
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static WhatsAppGroupDto MapGroup(WhatsAppGroup group) =>
        new()
        {
            Id = group.Id,
            Jid = group.Jid,
            Name = group.Name,
            ParticipantCount = group.ParticipantCount,
            IsAdmin = group.IsAdmin,
            IsActive = group.IsActive
        };

    private static WhatsAppBroadcastCampaignDto MapCampaign(WhatsAppBroadcastCampaign campaign) =>
        new()
        {
            Id = campaign.Id,
            Title = campaign.Title,
            MessageText = campaign.MessageText,
            ImageUrl = campaign.ImageUrl,
            TargetGroupJids = WhatsAppBroadcastRules.DeserializeJids(campaign.TargetGroupJidsJson).ToList(),
            ScheduledAt = campaign.ScheduledAt,
            IntervalSeconds = campaign.IntervalSeconds,
            Status = campaign.Status,
            UiStatusLabel = WhatsAppBroadcastRules.ToUiStatus(campaign.Status),
            CreatedAt = campaign.CreatedAt
        };

    private static WhatsAppBroadcastResponseDto Ok(
        List<WhatsAppGroupDto>? groups = null,
        List<WhatsAppBroadcastCampaignDto>? campaigns = null) =>
        new()
        {
            Status = true,
            Descricao = "OK",
            Groups = groups ?? [],
            Campaigns = campaigns ?? []
        };

    private static WhatsAppBroadcastResponseDto Fail(string message) =>
        new()
        {
            Status = false,
            Descricao = message
        };
}
