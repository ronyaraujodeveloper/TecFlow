using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Database;

namespace Ofertio.Infrastructure.Services.Master;

public sealed class TenantAccessReader : ITenantAccessReader
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMemoryCache _cache;

    public TenantAccessReader(IServiceScopeFactory scopeFactory, IMemoryCache cache)
    {
        _scopeFactory = scopeFactory;
        _cache = cache;
    }

    public async Task<string?> GetStatusAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        (await GetAccessAsync(tenantId, cancellationToken))?.Status;

    public async Task<TenantAccessInfo?> GetAccessAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            return null;
        }

        var key = CacheKey(tenantId);
        if (_cache.TryGetValue(key, out TenantAccessInfo? cached))
        {
            return cached;
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var info = await context.Tenants
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(item => item.Id == tenantId)
            .Select(item => new TenantAccessInfo(item.Status, item.StatusChangedAt, item.CanceledAt))
            .FirstOrDefaultAsync(cancellationToken);
        _cache.Set(key, info, CacheDuration);
        return info;
    }

    public void Invalidate(Guid tenantId) => _cache.Remove(CacheKey(tenantId));

    private static string CacheKey(Guid tenantId) => "ofertio:tenant-access:" + tenantId.ToString("N");
}
