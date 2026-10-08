using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Radar;
using TecFlow.Core.Entities;
using TecFlow.Database;
using TecFlow.Database.MultiTenancy;

namespace TecFlow.Infrastructure.Services.Radar;

public sealed class GlobalTrendingDealsEngine : IGlobalTrendingDealsEngine
{
    private readonly AppDbContext _context;
    private readonly IDealCreditsService _credits;
    private readonly ICurrentTenantService _currentTenant;
    private readonly ILogger<GlobalTrendingDealsEngine> _logger;

    public GlobalTrendingDealsEngine(
        AppDbContext context,
        IDealCreditsService credits,
        ICurrentTenantService currentTenant,
        ILogger<GlobalTrendingDealsEngine> logger)
    {
        _context = context;
        _credits = credits;
        _currentTenant = currentTenant;
        _logger = logger;
    }

    public async Task RunCycleAsync(CancellationToken cancellationToken = default)
    {
        var previous = _currentTenant.BypassTenantFilters;
        _currentTenant.BypassTenantFilters = true;
        try
        {
            await _credits.GrantDailyQuotasAsync(cancellationToken);
            await ScanTrendingAsync(cancellationToken);
        }
        finally
        {
            _currentTenant.BypassTenantFilters = previous;
        }
    }

    private async Task ScanTrendingAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var since = now.AddHours(-48);
        var messages = await _context.GroupCapturedMessages
            .AsNoTracking()
            .Where(item => item.ReceivedAt >= since && item.IsAvailable && !item.IsIgnored)
            .Select(item => new
            {
                item.OriginalUrl,
                item.PrimaryProductUrl,
                item.PlatformType,
                item.ProductName,
                item.ProductImageUrl,
                Price = item.ValidatedPrice ?? item.ExtractedPrice,
                item.GroupKey,
                item.ReceivedAt
            })
            .ToListAsync(cancellationToken);

        var buckets = new Dictionary<string, TrendingBucket>(StringComparer.OrdinalIgnoreCase);
        foreach (var message in messages)
        {
            var url = string.IsNullOrWhiteSpace(message.PrimaryProductUrl) ? message.OriginalUrl : message.PrimaryProductUrl;
            if (message.Price is not > 0
                || !ProductSkuRules.TryExtract(url, message.PlatformType, out var platform, out var sku))
            {
                continue;
            }

            var key = platform + "|" + sku;
            if (!buckets.TryGetValue(key, out var bucket))
            {
                bucket = new TrendingBucket(platform, sku);
                buckets[key] = bucket;
            }

            bucket.Urls.Add(url);
            bucket.Groups.Add(message.GroupKey);
            bucket.Count++;
            if (message.Price < bucket.CurrentPrice || bucket.CurrentPrice == 0)
            {
                bucket.CurrentPrice = message.Price.Value;
                bucket.CurrentUrl = url;
                bucket.Name = string.IsNullOrWhiteSpace(message.ProductName) ? bucket.Name : message.ProductName!;
                bucket.Image = message.ProductImageUrl ?? bucket.Image;
            }
        }

        var existing = await _context.GlobalTrendingDeals.ToListAsync(cancellationToken);
        var byKey = existing.ToDictionary(item => item.Platform + "|" + item.PlatformProductId, StringComparer.OrdinalIgnoreCase);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var bucket in buckets.Values)
        {
            var history = await _context.ProductPriceHistories
                .AsNoTracking()
                .Where(item => item.Platform == bucket.Platform && item.PlatformProductId == bucket.Sku)
                .OrderByDescending(item => item.CapturedAt)
                .Take(20)
                .ToListAsync(cancellationToken);
            var previousPrice = history
                .Where(item => item.Price > bucket.CurrentPrice)
                .Select(item => (decimal?)item.Price)
                .FirstOrDefault()
                ?? history.Select(item => (decimal?)item.Price).FirstOrDefault();
            var drop = previousPrice is > 0
                ? DealCreditRules.PriceDropPercent(previousPrice.Value, bucket.CurrentPrice)
                : 0;
            var engagement = Math.Max(bucket.Count, bucket.Groups.Count);
            if (!DealCreditRules.IsSuperAchado(drop, engagement))
            {
                continue;
            }

            var key = bucket.Platform + "|" + bucket.Sku;
            seen.Add(key);
            if (!byKey.TryGetValue(key, out var deal))
            {
                deal = new GlobalTrendingDeal
                {
                    Platform = bucket.Platform,
                    PlatformProductId = bucket.Sku
                };
                _context.GlobalTrendingDeals.Add(deal);
                byKey[key] = deal;
            }

            deal.ProductName = string.IsNullOrWhiteSpace(bucket.Name) ? "Super Achado" : Truncate(bucket.Name, 255)!;
            deal.ProductImageUrl = Truncate(bucket.Image, 500);
            deal.OriginalUrl = Truncate(bucket.CurrentUrl, 1000) ?? bucket.CurrentUrl;
            deal.CurrentPrice = bucket.CurrentPrice;
            deal.PreviousPrice = previousPrice;
            deal.PriceDropPercent = drop;
            deal.EngagementCount = engagement;
            deal.IsActive = true;
            deal.LastSeenAt = now;
            deal.Touch();
        }

        foreach (var deal in existing)
        {
            var key = deal.Platform + "|" + deal.PlatformProductId;
            if (!seen.Contains(key) && now - deal.LastSeenAt > TimeSpan.FromDays(3))
            {
                deal.IsActive = false;
                deal.Touch();
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Pool de achadinhos atualizado. Ativos={Count}", seen.Count);
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private sealed class TrendingBucket
    {
        public TrendingBucket(string platform, string sku)
        {
            Platform = platform;
            Sku = sku;
        }

        public string Platform { get; }
        public string Sku { get; }
        public HashSet<string> Urls { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> Groups { get; } = new(StringComparer.OrdinalIgnoreCase);
        public int Count { get; set; }
        public decimal CurrentPrice { get; set; }
        public string CurrentUrl { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Image { get; set; }
    }
}

public sealed class GlobalTrendingDealsHost
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GlobalTrendingDealsHost> _logger;

    public GlobalTrendingDealsHost(IServiceScopeFactory scopeFactory, ILogger<GlobalTrendingDealsHost> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task RunForeverAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var engine = scope.ServiceProvider.GetRequiredService<IGlobalTrendingDealsEngine>();
                await engine.RunCycleAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Worker de achadinhos globais falhou.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(10), cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
