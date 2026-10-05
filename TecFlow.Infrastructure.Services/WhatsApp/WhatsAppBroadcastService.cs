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
    private readonly IWhatsAppBroadcastJobCoordinator _jobs;
    private readonly IPreFlightService _preFlight;
    private readonly ILogger<WhatsAppBroadcastService> _logger;

    public WhatsAppBroadcastService(
        AppDbContext context,
        IEvolutionApiService evolution,
        IWhatsAppBroadcastJobCoordinator jobs,
        IPreFlightService preFlight,
        ILogger<WhatsAppBroadcastService> logger)
    {
        _context = context;
        _evolution = evolution;
        _jobs = jobs;
        _preFlight = preFlight;
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
        var remote = await _evolution.FetchUserGroupsAsync(
            instanceName,
            integration.PhoneNumber,
            cancellationToken);
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

        var remoteJids = remote
            .Select(item => item.Jid)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var row in existing)
        {
            if (!remoteJids.Contains(row.Jid))
            {
                row.IsAdmin = false;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return await ListGroupsAsync(userId, cancellationToken);
    }

    public async Task<WhatsAppBroadcastResponseDto> ListCampaignsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var names = await LoadGroupNamesAsync(userId, cancellationToken);
        var campaigns = await _context.WhatsAppBroadcastCampaigns
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.ScheduledAt)
            .ThenByDescending(item => item.Id)
            .Take(500)
            .ToListAsync(cancellationToken);

        return Ok(campaigns: campaigns.Select(item => MapCampaign(item, names)).ToList());
    }

    public async Task<WhatsAppBroadcastResponseDto> ScheduleAsync(
        int userId,
        WhatsAppScheduleCampaignDto request,
        CancellationToken cancellationToken = default)
    {
        request ??= new WhatsAppScheduleCampaignDto();
        var title = (request.Title ?? string.Empty).Trim();
        var message = (request.MessageText ?? string.Empty).Trim();
        var commissionLink = (request.CommissionLinkUrl ?? string.Empty).Trim();
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

        if (WhatsAppBroadcastRules.ContainsHttpUrl(message))
        {
            return Fail("A mensagem deve conter apenas o texto da oferta. Informe a URL no campo Link de Comissão.");
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

        if (request.Id > 0)
        {
            return await UpdateAsync(
                userId,
                new UpdateAgendamentoCommand
                {
                    Id = request.Id,
                    Title = title,
                    MessageText = message,
                    CommissionLinkUrl = commissionLink,
                    ImageUrl = request.ImageUrl,
                    TargetGroupJids = jids.ToList(),
                    ScheduledAt = scheduledAt,
                    IntervalSeconds = request.IntervalSeconds
                },
                cancellationToken);
        }

        var campaign = new WhatsAppBroadcastCampaign { UserId = userId };
        ApplyCampaignPayload(campaign, title, message, commissionLink, request.ImageUrl, jids, scheduledAt, request.IntervalSeconds);
        await _context.WhatsAppBroadcastCampaigns.AddAsync(campaign, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var listed = await ListCampaignsAsync(userId, cancellationToken);
        listed.Campaign = listed.Campaigns.FirstOrDefault(item => item.Id == campaign.Id);
        listed.Descricao = "Campanha agendada.";
        return listed;
    }

    public async Task<WhatsAppBroadcastResponseDto> UpdateAsync(
        int userId,
        UpdateAgendamentoCommand command,
        CancellationToken cancellationToken = default)
    {
        command ??= new UpdateAgendamentoCommand();
        var title = (command.Title ?? string.Empty).Trim();
        var message = (command.MessageText ?? string.Empty).Trim();
        var commissionLink = (command.CommissionLinkUrl ?? string.Empty).Trim();
        var jids = WhatsAppBroadcastRules.DeserializeJids(
            WhatsAppBroadcastRules.SerializeJids(command.TargetGroupJids));
        if (title.Length is < 3 or > 128)
        {
            return Fail("Informe um título entre 3 e 128 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            return Fail("Informe o texto da oferta.");
        }

        if (WhatsAppBroadcastRules.ContainsHttpUrl(message))
        {
            return Fail("A mensagem deve conter apenas o texto da oferta. Informe a URL no campo Link de Comissão.");
        }

        if (jids.Count == 0)
        {
            return Fail("Selecione ao menos um grupo.");
        }

        var campaign = await _context.WhatsAppBroadcastCampaigns
            .FirstOrDefaultAsync(item => item.Id == command.Id && item.UserId == userId, cancellationToken);
        if (campaign is null)
        {
            return Fail("Agendamento não encontrado.");
        }

        if (campaign.Status != WhatsAppBroadcastStatuses.Pending)
        {
            return Fail("Somente agendamentos pendentes podem ser editados.");
        }

        var scheduledAt = command.ScheduledAt?.ToUniversalTime() ?? DateTime.UtcNow;
        if (scheduledAt < DateTime.UtcNow.AddMinutes(-2))
        {
            scheduledAt = DateTime.UtcNow;
        }

        _jobs.Cancel(campaign.Id);
        ApplyCampaignPayload(campaign, title, message, commissionLink, command.ImageUrl, jids, scheduledAt, command.IntervalSeconds);
        await _context.SaveChangesAsync(cancellationToken);

        var listed = await ListCampaignsAsync(userId, cancellationToken);
        listed.Campaign = listed.Campaigns.FirstOrDefault(item => item.Id == campaign.Id);
        listed.Descricao = "Agendamento atualizado.";
        return listed;
    }

    public async Task<WhatsAppBroadcastResponseDto> DeleteCampaignAsync(
        int userId,
        int campaignId,
        CancellationToken cancellationToken = default)
    {
        var campaign = await _context.WhatsAppBroadcastCampaigns
            .FirstOrDefaultAsync(item => item.Id == campaignId && item.UserId == userId, cancellationToken);
        if (campaign is null)
        {
            return Fail("Agendamento não encontrado.");
        }

        if (campaign.Status == WhatsAppBroadcastStatuses.Completed)
        {
            return Fail("Não é possível excluir um disparo já concluído.");
        }

        _jobs.Cancel(campaignId);
        campaign.Status = WhatsAppBroadcastStatuses.Cancelled;
        campaign.Touch();
        _context.WhatsAppBroadcastCampaigns.Remove(campaign);
        await _context.SaveChangesAsync(cancellationToken);

        var listed = await ListCampaignsAsync(userId, cancellationToken);
        listed.Descricao = "Agendamento excluído.";
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
            if (!await _preFlight.EnsureReadyAsync("WhatsApp", campaign.Id, cancellationToken))
            {
                continue;
            }

            await ProcessOneAsync(campaign, cancellationToken);
        }
    }

    private async Task ProcessOneAsync(WhatsAppBroadcastCampaign campaign, CancellationToken cancellationToken)
    {
        var jobToken = _jobs.Register(campaign.Id, cancellationToken);
        try
        {
            campaign.Status = WhatsAppBroadcastStatuses.Processing;
            campaign.Touch();
            await SaveIfExistsAsync(campaign, jobToken);

            var jids = WhatsAppBroadcastRules.DeserializeJids(campaign.TargetGroupJidsJson);
            if (jids.Count == 0)
            {
                campaign.Status = WhatsAppBroadcastStatuses.Failed;
                campaign.Touch();
                await SaveIfExistsAsync(campaign, jobToken);
                return;
            }

            var instanceName = WhatsAppSessionRules.BuildInstanceName(campaign.UserId);
            var integration = await _context.WhatsAppIntegrations
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.UserId == campaign.UserId, jobToken);
            if (integration is null)
            {
                campaign.Status = WhatsAppBroadcastStatuses.Failed;
                campaign.Touch();
                await SaveIfExistsAsync(campaign, jobToken);
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
                .FirstOrDefaultAsync(jobToken);
            var text = campaign.MessageText ?? string.Empty;
            if (text.Contains(WhatsAppBroadcastRules.CommissionTag, StringComparison.OrdinalIgnoreCase))
            {
                text = WhatsAppBroadcastRules.ApplyCommissionTag(text, commissionLink);
            }
            else if (!WhatsAppBroadcastRules.ContainsHttpUrl(text))
            {
                text = WhatsAppBroadcastRules.ComposeDispatchMessage(text, commissionLink);
            }

            var interval = TimeSpan.FromSeconds(WhatsAppBroadcastRules.ClampIntervalSeconds(campaign.IntervalSeconds));
            var sent = 0;

            for (var index = 0; index < jids.Count; index++)
            {
                jobToken.ThrowIfCancellationRequested();
                if (!await CampaignExistsAsync(campaign.Id, jobToken))
                {
                    return;
                }

                var jid = jids[index];
                var stamped = TecFlow.Business.Service.Radar.OfferAttributionRules.StampMessage(text, "WhatsApp", jid);
                try
                {
                    var ok = string.IsNullOrWhiteSpace(campaign.ImageUrl)
                        ? await _evolution.SendTextMessageAsync(instanceName, jid, stamped, jobToken)
                        : await _evolution.SendMediaMessageAsync(
                            instanceName,
                            jid,
                            campaign.ImageUrl,
                            stamped,
                            jobToken);
                    if (ok)
                    {
                        sent++;
                    }
                }
                catch (OperationCanceledException) when (jobToken.IsCancellationRequested)
                {
                    throw;
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
                    await Task.Delay(interval, jobToken);
                }
            }

            campaign.Status = sent > 0 ? WhatsAppBroadcastStatuses.Completed : WhatsAppBroadcastStatuses.Failed;
            campaign.Touch();
            await SaveIfExistsAsync(campaign, CancellationToken.None);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            if (await CampaignExistsAsync(campaign.Id, CancellationToken.None))
            {
                campaign.Status = WhatsAppBroadcastStatuses.Cancelled;
                campaign.Touch();
                await SaveIfExistsAsync(campaign, CancellationToken.None);
            }
        }
        finally
        {
            _jobs.Unregister(campaign.Id);
        }
    }

    private async Task<bool> CampaignExistsAsync(int campaignId, CancellationToken cancellationToken) =>
        await _context.WhatsAppBroadcastCampaigns.AnyAsync(item => item.Id == campaignId, cancellationToken);

    private async Task SaveIfExistsAsync(WhatsAppBroadcastCampaign campaign, CancellationToken cancellationToken)
    {
        if (!await CampaignExistsAsync(campaign.Id, cancellationToken) && campaign.Id > 0)
        {
            _context.Entry(campaign).State = EntityState.Detached;
            return;
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _context.Entry(campaign).State = EntityState.Detached;
        }
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

    private async Task<Dictionary<string, string>> LoadGroupNamesAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var groups = await _context.WhatsAppGroups
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .Select(item => new { item.Jid, item.Name })
            .ToListAsync(cancellationToken);

        return groups
            .GroupBy(item => item.Jid, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.First().Name,
                StringComparer.OrdinalIgnoreCase);
    }

    private static WhatsAppBroadcastCampaignDto MapCampaign(
        WhatsAppBroadcastCampaign campaign,
        IReadOnlyDictionary<string, string>? groupNames = null)
    {
        var jids = WhatsAppBroadcastRules.DeserializeJids(campaign.TargetGroupJidsJson).ToList();
        var (_, link) = WhatsAppBroadcastRules.SplitDispatchMessage(campaign.MessageText);
        return new WhatsAppBroadcastCampaignDto
        {
            Id = campaign.Id,
            Title = campaign.Title,
            MessageText = campaign.MessageText,
            CommissionLinkUrl = string.IsNullOrWhiteSpace(link) ? null : link,
            ImageUrl = campaign.ImageUrl,
            TargetGroupJids = jids,
            TargetGroupNames = jids
                .Select(jid =>
                    groupNames is not null
                    && groupNames.TryGetValue(jid, out var name)
                    && !string.IsNullOrWhiteSpace(name)
                        ? name
                        : jid)
                .ToList(),
            ScheduledAt = campaign.ScheduledAt,
            IntervalSeconds = campaign.IntervalSeconds,
            Status = campaign.Status,
            UiStatusLabel = WhatsAppBroadcastRules.ToUiStatus(campaign.Status),
            CanEdit = campaign.Status is WhatsAppBroadcastStatuses.Pending or WhatsAppBroadcastStatuses.Paused,
            CanDelete = campaign.Status is WhatsAppBroadcastStatuses.Pending
                or WhatsAppBroadcastStatuses.Failed
                or WhatsAppBroadcastStatuses.Processing,
            CreatedAt = campaign.CreatedAt
        };
    }

    private static void ApplyCampaignPayload(
        WhatsAppBroadcastCampaign campaign,
        string title,
        string message,
        string commissionLink,
        string? imageUrl,
        IReadOnlyList<string> jids,
        DateTime scheduledAt,
        int intervalSeconds)
    {
        campaign.Title = title;
        campaign.MessageText = WhatsAppBroadcastRules.ComposeDispatchMessage(message, commissionLink);
        campaign.ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
        campaign.TargetGroupJidsJson = WhatsAppBroadcastRules.SerializeJids(jids);
        campaign.ScheduledAt = scheduledAt;
        campaign.IntervalSeconds = WhatsAppBroadcastRules.ClampIntervalSeconds(intervalSeconds);
        campaign.Status = WhatsAppBroadcastStatuses.Pending;
        campaign.Touch();
    }

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
