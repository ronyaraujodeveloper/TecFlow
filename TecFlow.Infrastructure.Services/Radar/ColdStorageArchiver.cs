using Microsoft.EntityFrameworkCore;
using TecFlow.Business.Service.Radar;
using TecFlow.Core.Entities;
using TecFlow.Database;

namespace TecFlow.Infrastructure.Services.Radar;

public sealed class ColdStorageArchiver
{
    private readonly AppDbContext _context;

    public ColdStorageArchiver(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int> ArchiveDueMessagesAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DataPurgeRules.MessageCutoff(DateTime.UtcNow);
        var rows = await _context.GroupCapturedMessages
            .AsNoTracking()
            .Where(item => item.CreatedAt < cutoff)
            .OrderBy(item => item.Id)
            .Take(DataPurgeRules.DeleteBatchSize)
            .Select(item => new
            {
                item.OriginalUrl,
                item.PrimaryProductUrl,
                item.PlatformType,
                Price = item.ValidatedPrice ?? item.ExtractedPrice,
                item.CouponCode,
                item.ReceivedAt,
                item.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var parsed = new List<(string Platform, string Sku, decimal Price, string? Url, string? Coupon, DateTime Captured)>();
        foreach (var row in rows)
        {
            var url = string.IsNullOrWhiteSpace(row.PrimaryProductUrl) ? row.OriginalUrl : row.PrimaryProductUrl;
            if (row.Price is not > 0
                || !ProductSkuRules.TryExtract(url, row.PlatformType, out var platform, out var sku))
            {
                continue;
            }

            var captured = row.ReceivedAt == default ? row.CreatedAt : row.ReceivedAt;
            parsed.Add((
                platform,
                sku.Length <= 80 ? sku : sku[..80],
                row.Price.Value,
                Truncate(url, 1000),
                Truncate(row.CouponCode, 64),
                captured.ToUniversalTime()));
        }

        if (parsed.Count == 0)
        {
            return 0;
        }

        var skus = parsed.Select(item => item.Sku).Distinct().ToList();
        var minCaptured = parsed.Min(item => item.Captured);
        var existing = await _context.ProductPriceHistories
            .AsNoTracking()
            .Where(item => skus.Contains(item.PlatformProductId) && item.CapturedAt >= minCaptured)
            .Select(item => new { item.Platform, item.PlatformProductId, item.Price, item.CapturedAt })
            .ToListAsync(cancellationToken);
        var keys = existing
            .Select(item => (item.Platform, item.PlatformProductId, item.Price, item.CapturedAt))
            .ToHashSet();

        var added = 0;
        foreach (var row in parsed)
        {
            var key = (row.Platform, row.Sku, row.Price, row.Captured);
            if (!keys.Add(key))
            {
                continue;
            }

            _context.ProductPriceHistories.Add(new ProductPriceHistory
            {
                Platform = row.Platform,
                PlatformProductId = row.Sku,
                Price = row.Price,
                SourceUrl = row.Url,
                CouponCode = row.Coupon,
                CapturedAt = row.Captured
            });
            added++;
        }

        if (added > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return added;
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
