using Microsoft.EntityFrameworkCore;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Master;
using TecFlow.Core.Entities;
using TecFlow.Database;

namespace TecFlow.Infrastructure.Services.Master;

/// <summary>Avisos globais, notificações segmentadas e changelog do produto.</summary>
public sealed class SystemCommunicationService : ISystemCommunicationService
{
    private static readonly string[] Severities = ["info", "warning", "maintenance"];

    private readonly AppDbContext _context;
    private readonly IMasterAuditService _audit;

    public SystemCommunicationService(AppDbContext context, IMasterAuditService audit)
    {
        _context = context;
        _audit = audit;
    }

    public async Task<IReadOnlyList<SystemBannerDto>> ListBannersAsync(bool onlyActive, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var query = _context.SystemBanners.AsNoTracking();
        if (onlyActive)
        {
            query = query.Where(item => item.IsActive && item.StartsAt <= now && (item.EndsAt == null || item.EndsAt > now));
        }

        var items = await query.OrderByDescending(item => item.CreatedAt).Take(100).ToListAsync(cancellationToken);
        return items.Select(MapBanner).ToList();
    }

    public async Task<SystemBannerDto> SaveBannerAsync(MasterActor actor, SystemBannerDto request, CancellationToken cancellationToken = default)
    {
        var banner = request.Id > 0
            ? await _context.SystemBanners.FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken)
            : null;
        if (banner is null)
        {
            banner = new SystemBanner { CreatedBy = actor.Email };
            _context.SystemBanners.Add(banner);
        }

        banner.Title = Clip(request.Title, 128);
        banner.Message = Clip(request.Message, 1000);
        banner.Severity = NormalizeSeverity(request.Severity);
        banner.StartsAt = request.StartsAt ?? DateTime.UtcNow;
        banner.EndsAt = request.EndsAt;
        banner.IsActive = request.IsActive;
        banner.Touch();
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(actor, MasterAuditActions.BannerSaved, "SystemBanner", banner.Id.ToString(),
            $"[{banner.Severity}] {banner.Title} (ativo: {(banner.IsActive ? "sim" : "não")}).", cancellationToken);
        return MapBanner(banner);
    }

    public async Task<bool> DeleteBannerAsync(MasterActor actor, int id, CancellationToken cancellationToken = default)
    {
        var banner = await _context.SystemBanners.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (banner is null)
        {
            return false;
        }

        _context.SystemBanners.Remove(banner);
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(actor, MasterAuditActions.BannerDeleted, "SystemBanner", id.ToString(), banner.Title, cancellationToken);
        return true;
    }

    public async Task<int> NotifySegmentAsync(MasterActor actor, SegmentNotificationRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Message))
        {
            return 0;
        }

        var segment = string.IsNullOrWhiteSpace(request.Segment) ? "todos" : request.Segment.Trim();
        var tenantIds = await ResolveSegmentAsync(segment, cancellationToken);
        foreach (var tenantId in tenantIds)
        {
            _context.TenantNotifications.Add(new TenantNotification
            {
                TenantId = tenantId,
                Title = Clip(request.Title, 128),
                Message = Clip(request.Message, 2000),
                Segment = Clip(segment, 64)
            });
        }

        if (tenantIds.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        await _audit.LogAsync(actor, MasterAuditActions.NotificationSent, "Segmento", segment,
            $"\"{request.Title.Trim()}\" enviada para {tenantIds.Count} inquilino(s).", cancellationToken);
        return tenantIds.Count;
    }

    public async Task<IReadOnlyList<TenantNotificationDto>> ListNotificationsAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        await _context.TenantNotifications
            .AsNoTracking()
            .Where(item => item.TenantId == tenantId)
            .OrderByDescending(item => item.CreatedAt)
            .Take(30)
            .Select(item => new TenantNotificationDto
            {
                Id = item.Id,
                Title = item.Title,
                Message = item.Message,
                Segment = item.Segment,
                CreatedAt = item.CreatedAt,
                ReadAt = item.ReadAt
            })
            .ToListAsync(cancellationToken);

    public async Task<bool> MarkNotificationReadAsync(Guid tenantId, int id, CancellationToken cancellationToken = default)
    {
        var item = await _context.TenantNotifications
            .FirstOrDefaultAsync(row => row.Id == id && row.TenantId == tenantId, cancellationToken);
        if (item is null)
        {
            return false;
        }

        item.ReadAt ??= DateTime.UtcNow;
        item.Touch();
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<ProductChangelogDto>> ListChangelogAsync(bool onlyPublished, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var query = _context.ProductChangelogEntries.AsNoTracking();
        if (onlyPublished)
        {
            query = query.Where(item => item.IsPublished && item.PublishedAt <= now);
        }

        return await query
            .OrderByDescending(item => item.PublishedAt)
            .Take(100)
            .Select(item => new ProductChangelogDto
            {
                Id = item.Id,
                Version = item.Version,
                Title = item.Title,
                Body = item.Body,
                PublishedAt = item.PublishedAt,
                IsPublished = item.IsPublished
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductChangelogDto> SaveChangelogAsync(MasterActor actor, ProductChangelogDto request, CancellationToken cancellationToken = default)
    {
        var entry = request.Id > 0
            ? await _context.ProductChangelogEntries.FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken)
            : null;
        if (entry is null)
        {
            entry = new ProductChangelogEntry();
            _context.ProductChangelogEntries.Add(entry);
        }

        entry.Version = Clip(request.Version, 32);
        entry.Title = Clip(request.Title, 160);
        entry.Body = request.Body?.Trim() ?? string.Empty;
        entry.PublishedAt = request.PublishedAt == default ? DateTime.UtcNow : request.PublishedAt;
        entry.IsPublished = request.IsPublished;
        entry.Touch();
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(actor, MasterAuditActions.ChangelogSaved, "Changelog", entry.Id.ToString(),
            $"{entry.Version} — {entry.Title}", cancellationToken);
        request.Id = entry.Id;
        request.PublishedAt = entry.PublishedAt;
        return request;
    }

    public async Task<bool> DeleteChangelogAsync(MasterActor actor, int id, CancellationToken cancellationToken = default)
    {
        var entry = await _context.ProductChangelogEntries.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (entry is null)
        {
            return false;
        }

        _context.ProductChangelogEntries.Remove(entry);
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(actor, MasterAuditActions.ChangelogDeleted, "Changelog", id.ToString(), entry.Title, cancellationToken);
        return true;
    }

    private async Task<List<Guid>> ResolveSegmentAsync(string segment, CancellationToken cancellationToken)
    {
        var separator = segment.IndexOf(':');
        var kind = separator < 0 ? segment.ToLowerInvariant() : segment[..separator].Trim().ToLowerInvariant();
        var value = separator < 0 ? string.Empty : segment[(separator + 1)..].Trim();
        var tenants = _context.Tenants.AsNoTracking();
        switch (kind)
        {
            case "status":
                var status = TenantStatuses.Normalize(value);
                tenants = tenants.Where(item => item.Status == status);
                break;
            case "plano":
                var planTenantIds = await _context.UserAccounts
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(item => item.Plan == value)
                    .Select(item => item.TenantId)
                    .Distinct()
                    .ToListAsync(cancellationToken);
                tenants = tenants.Where(item => item.PlanCode == value || (item.PlanCode == null && planTenantIds.Contains(item.Id)));
                break;
            case "tenant":
                if (!Guid.TryParse(value, out var tenantId))
                {
                    return [];
                }

                tenants = tenants.Where(item => item.Id == tenantId);
                break;
        }

        return await tenants.Select(item => item.Id).ToListAsync(cancellationToken);
    }

    private static string NormalizeSeverity(string? severity) =>
        Severities.FirstOrDefault(item => string.Equals(item, severity?.Trim(), StringComparison.OrdinalIgnoreCase)) ?? "info";

    private static string Clip(string? value, int max)
    {
        var text = value?.Trim() ?? string.Empty;
        return text.Length <= max ? text : text[..max];
    }

    private static SystemBannerDto MapBanner(SystemBanner item) =>
        new()
        {
            Id = item.Id,
            Title = item.Title,
            Message = item.Message,
            Severity = item.Severity,
            StartsAt = item.StartsAt,
            EndsAt = item.EndsAt,
            IsActive = item.IsActive,
            CreatedAt = item.CreatedAt
        };
}
