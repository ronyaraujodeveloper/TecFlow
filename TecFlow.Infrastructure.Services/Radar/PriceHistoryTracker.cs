using Microsoft.EntityFrameworkCore;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Radar;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Database;

namespace TecFlow.Infrastructure.Services.Radar;

public sealed class PriceHistoryTracker : IPriceHistoryTracker
{
    private readonly AppDbContext _context;

    public PriceHistoryTracker(AppDbContext context)
    {
        _context = context;
    }

    public async Task RecordAsync(
        string? url,
        MarketplaceType? platform,
        decimal? price,
        DateTime capturedAt,
        CancellationToken cancellationToken = default)
    {
        if (price is not > 0 || !ProductSkuRules.TryExtract(url, platform, out var platformKey, out var sku))
        {
            return;
        }

        var last = await _context.ProductPriceHistories
            .AsNoTracking()
            .Where(item => item.Platform == platformKey && item.PlatformProductId == sku)
            .OrderByDescending(item => item.CapturedAt)
            .FirstOrDefaultAsync(cancellationToken);
        var utc = capturedAt == default ? DateTime.UtcNow : capturedAt.ToUniversalTime();
        if (last is not null && PriceHistoryRules.ShouldSkipDuplicate(last.Price, last.CapturedAt, price.Value, utc))
        {
            return;
        }

        _context.ProductPriceHistories.Add(new ProductPriceHistory
        {
            Platform = platformKey,
            PlatformProductId = sku.Length <= 80 ? sku : sku[..80],
            Price = price.Value,
            SourceUrl = Truncate(url, 1000),
            CapturedAt = utc
        });
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task EnrichOffersAsync(
        IList<GroupCapturedOfferDto> offers,
        CancellationToken cancellationToken = default)
    {
        if (offers.Count == 0)
        {
            return;
        }

        var keys = new List<(GroupCapturedOfferDto Offer, string Platform, string Sku)>();
        foreach (var offer in offers)
        {
            var url = string.IsNullOrWhiteSpace(offer.PrimaryProductUrl) ? offer.OriginalUrl : offer.PrimaryProductUrl;
            if (!ProductSkuRules.TryExtract(url, offer.PlatformType, out var platform, out var sku))
            {
                continue;
            }

            keys.Add((offer, platform, sku));
        }

        if (keys.Count == 0)
        {
            return;
        }

        var platforms = keys.Select(item => item.Platform).Distinct().ToList();
        var skus = keys.Select(item => item.Sku).Distinct().ToList();
        var now = DateTime.UtcNow;
        var cutoff = now.AddDays(-90);
        var rows = await _context.ProductPriceHistories
            .AsNoTracking()
            .Where(item => platforms.Contains(item.Platform) && skus.Contains(item.PlatformProductId) && item.CapturedAt >= cutoff)
            .Select(item => new { item.Platform, item.PlatformProductId, item.Price, item.CapturedAt })
            .ToListAsync(cancellationToken);

        foreach (var group in keys.GroupBy(item => (item.Platform, item.Sku)))
        {
            var history = rows
                .Where(item => item.Platform == group.Key.Platform && item.PlatformProductId == group.Key.Sku)
                .ToList();
            var prices = history.Select(item => item.Price).ToList();
            var dates = history.Select(item => item.CapturedAt).ToList();
            var min30 = PriceHistoryRules.MinInWindow(prices, dates, now, 30);
            var avg30 = PriceHistoryRules.AverageInWindow(prices, dates, now, 30);
            foreach (var entry in group)
            {
                var current = entry.Offer.ExtractedPrice ?? 0;
                entry.Offer.IsLowestPrice30Days = PriceHistoryRules.IsLowestInWindow(current, min30);
                entry.Offer.MinPrice30Days = min30;
                entry.Offer.AveragePrice30Days = avg30;
                entry.Offer.SavingsVersusAverage = PriceHistoryRules.SavingsVersusAverage(current, avg30);
            }
        }
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
}
