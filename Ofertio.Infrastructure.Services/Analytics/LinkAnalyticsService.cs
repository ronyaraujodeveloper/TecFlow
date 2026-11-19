using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ofertio.Business.Configuration;
using Ofertio.Business.Dto;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Attribution;
using Ofertio.Database;
using Ofertio.Database.Entity;

namespace Ofertio.Infrastructure.Services.Analytics;

public sealed class LinkAnalyticsService : ILinkAnalyticsService
{
    /// <summary>Teto de linhas por consulta. Acima disso a tabela de totais por dia deve assumir (fora desta entrega).</summary>
    private const int MaxRows = 300_000;

    private readonly AppDbContext _context;
    private readonly ShortLinkOptions _shortLinks;
    private readonly ILogger<LinkAnalyticsService> _logger;

    public LinkAnalyticsService(
        AppDbContext context,
        IOptions<ShortLinkOptions> shortLinks,
        ILogger<LinkAnalyticsService> logger)
    {
        _context = context;
        _shortLinks = shortLinks.Value;
        _logger = logger;
    }

    public async Task<(AnalyticsOverviewDto? Data, string? Error)> GetOverviewAsync(
        AnalyticsRequest request,
        CancellationToken cancellationToken = default)
    {
        var error = Validate(request);
        if (error is not null)
        {
            return (null, error);
        }

        var rows = await LoadRowsAsync(request, null, cancellationToken);
        var (fromUtc, toUtc) = AnalyticsPeriodRules.ToUtcRange(request.From, request.To);
        var shopFilter = string.IsNullOrWhiteSpace(request.ShopId) ? null : request.ShopId.Trim();
        var generated = await _context.LinkClickLogs
            .IgnoreQueryFilters()
            .AsNoTracking()
            .CountAsync(log => log.TenantId == request.TenantId
                && log.EventKind == LinkClickLog.EventKindGeneration
                && log.ClickedAt >= fromUtc
                && log.ClickedAt < toUtc
                && (shopFilter == null || log.ShopId == shopFilter),
                cancellationToken);
        var hasAnyLink = await _context.ShortAffiliateLinks
            .IgnoreQueryFilters()
            .AsNoTracking()
            .AnyAsync(link => link.TenantId == request.TenantId, cancellationToken);
        var (byKey, byLegacy) = await LoadGroupNamesAsync(request.UserId, cancellationToken);

        var publicEvents = await LoadPublicEventsAsync(request, cancellationToken);
        var overview = LinkAnalyticsCalculator.BuildOverview(
            rows, request.From, request.To, request.Grain, generated, hasAnyLink, byKey, byLegacy, publicEvents);
        return (overview, null);
    }

    public async Task<(AnalyticsProductDetailDto? Data, string? Error)> GetProductAsync(
        AnalyticsRequest request,
        Guid linkGroupId,
        CancellationToken cancellationToken = default)
    {
        var error = Validate(request);
        if (error is not null)
        {
            return (null, error);
        }

        if (linkGroupId == Guid.Empty)
        {
            return (null, "Produto inválido.");
        }

        var rows = await LoadRowsAsync(request, linkGroupId, cancellationToken);
        var (byKey, byLegacy) = await LoadGroupNamesAsync(request.UserId, cancellationToken);
        var detail = LinkAnalyticsCalculator.BuildProductDetail(rows, request.From, request.To, request.Grain, byKey, byLegacy);
        if (detail.Product.LinkGroupId == Guid.Empty)
        {
            // Sem cliques no período: ainda mostra o produto, se ele existir.
            var link = await _context.ShortAffiliateLinks
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(item => item.TenantId == request.TenantId && item.LinkGroupId == linkGroupId)
                .Select(item => new { item.ProductName, item.ProductImageUrl, item.PlatformType, item.OriginalUrl })
                .FirstOrDefaultAsync(cancellationToken);
            if (link is null)
            {
                return (null, "Produto não encontrado.");
            }

            detail.Product = new AnalyticsProductDto
            {
                LinkGroupId = linkGroupId,
                Name = string.IsNullOrWhiteSpace(link.ProductName) ? link.OriginalUrl : link.ProductName!,
                ImageUrl = link.ProductImageUrl,
                Platform = LinkClickLog.PlatformName(link.PlatformType)
            };
        }

        return (detail, null);
    }

    private static string? Validate(AnalyticsRequest request)
    {
        if (request.TenantId == Guid.Empty || request.UserId <= 0)
        {
            return "Usuário não identificado.";
        }

        return AnalyticsPeriodRules.Validate(request.From, request.To, request.Grain);
    }

    private async Task<List<AnalyticsClickRow>> LoadRowsAsync(
        AnalyticsRequest request,
        Guid? linkGroupId,
        CancellationToken cancellationToken)
    {
        var (previousFrom, _) = AnalyticsPeriodRules.Previous(request.From, request.To);
        var fromUtc = AnalyticsPeriodRules.ToUtcRange(previousFrom, request.To).FromUtc;
        var toUtc = AnalyticsPeriodRules.ToUtcRange(request.From, request.To).ToUtcExclusive;

        var query =
            from log in _context.LinkClickLogs.IgnoreQueryFilters().AsNoTracking()
            join link in _context.ShortAffiliateLinks.IgnoreQueryFilters().AsNoTracking()
                on log.AffiliateLinkId equals link.AffiliateLinkId
            where log.TenantId == request.TenantId
                && log.EventKind == LinkClickLog.EventKindClick
                && log.ClickedAt >= fromUtc
                && log.ClickedAt < toUtc
            select new { log, link };

        if (!string.IsNullOrWhiteSpace(request.ShopId))
        {
            var shopId = request.ShopId.Trim();
            query = query.Where(item => item.log.ShopId == shopId);
        }

        if (linkGroupId is { } groupId)
        {
            query = query.Where(item => item.link.LinkGroupId == groupId);
        }

        var data = await query
            .OrderByDescending(item => item.log.ClickedAt)
            .Take(MaxRows)
            .Select(item => new
            {
                item.log.ClickedAt,
                item.log.IsBot,
                item.log.Channel,
                item.log.GroupKey,
                item.log.SourceGroup,
                item.log.DeviceType,
                item.log.IpAddress,
                item.log.UserAgent,
                item.link.LinkGroupId,
                item.link.ProductName,
                item.link.ProductImageUrl,
                item.log.Platform,
                item.log.ShopId,
                item.link.OriginalUrl,
                item.log.Country,
                item.log.Region,
                item.log.City
            })
            .ToListAsync(cancellationToken);

        if (data.Count >= MaxRows)
        {
            _logger.LogWarning(
                "Análise de cliques atingiu o teto de {Max} linhas. Tenant={TenantId}; os números mais antigos podem estar incompletos.",
                MaxRows,
                request.TenantId);
        }

        return data
            .Select(row => new AnalyticsClickRow(
                DateTime.SpecifyKind(row.ClickedAt, DateTimeKind.Utc),
                row.IsBot,
                row.Channel,
                row.GroupKey,
                GroupKeyRules.IsKey(row.SourceGroup) ? null : row.SourceGroup,
                row.DeviceType,
                row.IpAddress,
                row.UserAgent,
                row.LinkGroupId,
                row.ProductName,
                row.ProductImageUrl,
                row.Platform,
                row.ShopId,
                row.OriginalUrl,
                row.Country,
                row.Region,
                row.City))
            .ToList();
    }

    /// <summary>Visitas, conversões e entradas em grupo da página pública do inquilino, no período e no anterior.</summary>
    private async Task<List<PublicEventRow>> LoadPublicEventsAsync(AnalyticsRequest request, CancellationToken cancellationToken)
    {
        var (previousFrom, _) = AnalyticsPeriodRules.Previous(request.From, request.To);
        var fromUtc = AnalyticsPeriodRules.ToUtcRange(previousFrom, request.To).FromUtc;
        var toUtc = AnalyticsPeriodRules.ToUtcRange(request.From, request.To).ToUtcExclusive;

        var events = await _context.PublicPageEvents
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(item => item.TenantId == request.TenantId && item.CreatedAt >= fromUtc && item.CreatedAt < toUtc)
            .OrderByDescending(item => item.CreatedAt)
            .Take(MaxRows)
            .Select(item => new { item.CreatedAt, item.Kind, item.Channel, item.IsBot, item.IpMasked, item.UserAgent })
            .ToListAsync(cancellationToken);
        return events
            .Select(item => new PublicEventRow(
                DateTime.SpecifyKind(item.CreatedAt, DateTimeKind.Utc), item.Kind, item.Channel, item.IsBot, item.IpMasked, item.UserAgent))
            .ToList();
    }

    /// <summary>Recalcula a chave de cada grupo do usuário para resolver o nome (a chave não guarda o JID/ChatId).</summary>
    private async Task<(Dictionary<string, AnalyticsGroupName> ByKey, Dictionary<string, AnalyticsGroupName> ByLegacy)> LoadGroupNamesAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var byKey = new Dictionary<string, AnalyticsGroupName>(StringComparer.Ordinal);
        var byLegacy = new Dictionary<string, AnalyticsGroupName>(StringComparer.Ordinal);

        var whatsApp = await _context.WhatsAppGroups
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(group => group.UserId == userId)
            .Select(group => new { group.Jid, group.Name })
            .ToListAsync(cancellationToken);
        foreach (var group in whatsApp)
        {
            var name = new AnalyticsGroupName(group.Name, "WhatsApp");
            byKey[GroupKeyRules.Compute(_shortLinks.GroupKeySecret, userId, "WhatsApp", group.Jid)] = name;
            byLegacy[group.Jid] = name;
        }

        var telegram = await _context.TelegramGroups
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(group => group.UserId == userId)
            .Select(group => new { group.ChatId, group.Name })
            .ToListAsync(cancellationToken);
        foreach (var group in telegram)
        {
            var name = new AnalyticsGroupName(group.Name, "Telegram");
            byKey[GroupKeyRules.Compute(_shortLinks.GroupKeySecret, userId, "Telegram", group.ChatId)] = name;
            byLegacy[group.ChatId] = name;
        }

        return (byKey, byLegacy);
    }
}
