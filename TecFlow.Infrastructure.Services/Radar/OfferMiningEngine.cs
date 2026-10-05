using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Radar;
using TecFlow.Core.Entities;
using TecFlow.Database;

namespace TecFlow.Infrastructure.Services.Radar;

public sealed class OfferMiningEngine : IOfferMiningEngine
{
    private readonly AppDbContext _context;
    private readonly IAffiliateMiningProfileService _profiles;
    private readonly IGroupCapturedMessagesService _captured;
    private readonly ILogger<OfferMiningEngine> _logger;

    public OfferMiningEngine(
        AppDbContext context,
        IAffiliateMiningProfileService profiles,
        IGroupCapturedMessagesService captured,
        ILogger<OfferMiningEngine> logger)
    {
        _context = context;
        _profiles = profiles;
        _captured = captured;
        _logger = logger;
    }

    public async Task<int> MineUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var profile = (await _profiles.GetAsync(userId, cancellationToken)).Profile
            ?? new TecFlow.Business.Dto.AffiliateMiningProfileDto();
        var niches = profile.Niches;
        var active = await _captured.ListActivePlatformsAsync(userId, cancellationToken);
        var platforms = AffiliateMiningRules.ResolveSearchPlatforms(active, profile.RestrictToActiveStores);
        if (platforms.Count == 0)
        {
            return 0;
        }

        var since = DateTime.UtcNow.Subtract(AffiliateMiningRules.HistoryWindow);
        var offers = await _context.GroupCapturedMessages
            .AsNoTracking()
            .Where(item => item.UserId == userId
                && item.HasDirectProductUrl
                && item.IsAvailable
                && item.ReceivedAt >= since
                && item.PlatformType != null
                && platforms.Contains(item.PlatformType.Value))
            .OrderByDescending(item => item.ReceivedAt)
            .Take(500)
            .ToListAsync(cancellationToken);

        var created = 0;
        foreach (var offer in offers)
        {
            var price = offer.ValidatedPrice ?? offer.ExtractedPrice;
            if (price is not > 0)
            {
                continue;
            }

            var key = AffiliateMiningRules.BuildProductKey(offer.ProductName);
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            _context.ProductPriceSnapshots.Add(new ProductPriceSnapshot
            {
                UserId = userId,
                ProductKey = key,
                PlatformType = offer.PlatformType,
                Price = price.Value,
                SourceUrl = offer.OriginalUrl,
                CapturedAt = offer.ReceivedAt
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        var snapshots = await _context.ProductPriceSnapshots
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.CapturedAt >= since)
            .ToListAsync(cancellationToken);
        var averages = snapshots
            .GroupBy(item => item.ProductKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Average(item => item.Price),
                StringComparer.OrdinalIgnoreCase);
        var dayAgo = DateTime.UtcNow.AddHours(-24);
        var recentCounts = offers
            .GroupBy(item => AffiliateMiningRules.BuildProductKey(item.ProductName), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Count(item => item.ReceivedAt >= dayAgo),
                StringComparer.OrdinalIgnoreCase);

        foreach (var offer in offers)
        {
            var price = offer.ValidatedPrice ?? offer.ExtractedPrice;
            var key = AffiliateMiningRules.BuildProductKey(offer.ProductName);
            if (price is not > 0 || string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            if (!AffiliateMiningRules.MatchesTicket(price, profile.MinTicket, profile.MaxTicket)
                || !AffiliateMiningRules.MatchesNiche(offer.ProductName, niches))
            {
                continue;
            }

            averages.TryGetValue(key, out var average);
            var isDrop = average > 0 && price < average * AffiliateMiningRules.PriceDropRatio;
            recentCounts.TryGetValue(key, out var recent);
            var isMover = recent >= 3;
            var isTrend = AffiliateMiningRules.LooksLikeSocialTrend(offer.ProductName, offer.RawText) || isMover;
            if (!isDrop && !isMover && !isTrend)
            {
                continue;
            }

            var exists = await _context.OfferRadarItems.AnyAsync(
                item => item.UserId == userId
                    && item.OriginalUrl == offer.OriginalUrl
                    && item.ReceivedAt >= dayAgo,
                cancellationToken);
            if (exists)
            {
                continue;
            }

            var coupon = AffiliateMiningRules.ExtractCoupon(offer.RawText);
            var source = isDrop
                ? OfferRadarSources.PriceDrop
                : isTrend
                    ? OfferRadarSources.Trend
                    : OfferRadarSources.Mining;
            var item = new OfferRadarItem
            {
                UserId = userId,
                ProductName = string.IsNullOrWhiteSpace(offer.ProductName) ? "Oferta minerada" : offer.ProductName,
                ProductImageUrl = offer.ProductImageUrl ?? offer.MediaUrl,
                OriginalUrl = offer.OriginalUrl,
                AffiliateUrl = offer.OriginalUrl,
                PlatformType = offer.PlatformType,
                PlatformName = offer.PlatformName,
                Price = price,
                ComparedPrice = average > 0 ? decimal.Round(average, 2) : null,
                CouponCode = coupon,
                AttractivenessScore = AffiliateMiningRules.ComputeAttractivenessScore(
                    price,
                    average > 0 ? average : null,
                    coupon is not null,
                    true,
                    isMover,
                    isTrend),
                Source = source,
                IsAutoQueued = profile.AutoPilotEnabled,
                ReceivedAt = offer.ReceivedAt
            };
            _context.OfferRadarItems.Add(item);
            created++;
        }

        if (created > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Mineração do radar concluída. UserId={UserId} Novas={Count}", userId, created);
        return created;
    }
}
