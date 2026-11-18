using Microsoft.Extensions.Caching.Memory;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Business.Service.Master;

namespace Ofertio.Infrastructure.Services.Master;

public sealed class LinkAvailabilityService : ILinkAvailabilityService
{
    private const string GraceCacheKey = "ofertio:link-grace-days";
    private static readonly TimeSpan GraceCacheDuration = TimeSpan.FromSeconds(60);

    private readonly ITenantAccessReader _access;
    private readonly ISystemSettingsService _settings;
    private readonly IMemoryCache _cache;

    public LinkAvailabilityService(ITenantAccessReader access, ISystemSettingsService settings, IMemoryCache cache)
    {
        _access = access;
        _settings = settings;
        _cache = cache;
    }

    public async Task<int> GetGraceDaysAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(GraceCacheKey, out int cached))
        {
            return cached;
        }

        var raw = await _settings.GetValueAsync(SystemSettingCatalog.LinkGraceDays, cancellationToken);
        var days = LinkAvailabilityRules.ParseGraceDays(raw);
        _cache.Set(GraceCacheKey, days, GraceCacheDuration);
        return days;
    }

    public async Task<LinkAvailability> CheckAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var info = await _access.GetAccessAsync(tenantId, cancellationToken);
        if (info is null || !Ofertio.Business.Service.Master.TenantLifecycleRules.IsAccessBlocked(info.Status))
        {
            return new LinkAvailability(true, null);
        }

        var days = await GetGraceDaysAsync(cancellationToken);
        var until = LinkAvailabilityRules.ServingUntil(info.Status, info.StatusChangedAt, info.CanceledAt, days);
        var serving = LinkAvailabilityRules.IsServing(info.Status, info.StatusChangedAt, info.CanceledAt, DateTime.UtcNow, days);
        return new LinkAvailability(serving, until);
    }
}