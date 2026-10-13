using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Database;

namespace TecFlow.Infrastructure.Services.Master;

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

    public async Task<string?> GetStatusAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            return null;
        }

        var key = CacheKey(tenantId);
        if (_cache.TryGetValue(key, out string? cached))
        {
            return cached;
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var status = await context.Tenants
            .AsNoTracking()
            .Where(item => item.Id == tenantId)
            .Select(item => item.Status)
            .FirstOrDefaultAsync(cancellationToken);
        _cache.Set(key, status, CacheDuration);
        return status;
    }

    public void Invalidate(Guid tenantId) => _cache.Remove(CacheKey(tenantId));

    private static string CacheKey(Guid tenantId) => "tecflow:tenant-status:" + tenantId.ToString("N");
}
