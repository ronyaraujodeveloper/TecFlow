using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Radar;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Database;

namespace TecFlow.Infrastructure.Services.Radar;

public sealed class ProductArbitrageService : IProductArbitrageService
{
    private readonly AppDbContext _context;
    private readonly IAffiliateMiningProfileService _profiles;
    private readonly IGroupCapturedMessagesService _captured;
    private readonly IAffiliateLinkConverterService _converter;
    private readonly ILogger<ProductArbitrageService> _logger;

    public ProductArbitrageService(
        AppDbContext context,
        IAffiliateMiningProfileService profiles,
        IGroupCapturedMessagesService captured,
        IAffiliateLinkConverterService converter,
        ILogger<ProductArbitrageService> logger)
    {
        _context = context;
        _profiles = profiles;
        _captured = captured;
        _converter = converter;
        _logger = logger;
    }

    public async Task<OfferArbitrageResponseDto> SearchAlternativesAsync(
        int userId,
        OfferArbitrageRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var profileResponse = await _profiles.GetAsync(userId, cancellationToken);
        var profile = profileResponse.Profile ?? new AffiliateMiningProfileDto();
        var active = await _captured.ListActivePlatformsAsync(userId, cancellationToken);
        var platforms = AffiliateMiningRules.ResolveSearchPlatforms(active, profile.RestrictToActiveStores);
        if (platforms.Count == 0)
        {
            return new OfferArbitrageResponseDto
            {
                Status = true,
                Descricao = "Conecte Shopee, Mercado Livre, Amazon ou TikTok Shop para buscar preços menores."
            };
        }

        var since = DateTime.UtcNow.Subtract(AffiliateMiningRules.HistoryWindow);
        var captured = await _context.GroupCapturedMessages
            .AsNoTracking()
            .Where(item => item.UserId == userId
                && item.HasDirectProductUrl
                && item.IsAvailable
                && item.ReceivedAt >= since
                && item.ExtractedPrice > 0)
            .OrderByDescending(item => item.ReceivedAt)
            .Take(400)
            .ToListAsync(cancellationToken);
        var history = await _context.ShortAffiliateLinks
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.IsActive && item.ProductPrice > 0)
            .OrderByDescending(item => item.CreatedAt)
            .Take(200)
            .ToListAsync(cancellationToken);

        var sourcePlatform = DetectSourcePlatform(request.OriginalUrl);
        var candidates = new List<OfferArbitrageSuggestionDto>();
        foreach (var item in captured)
        {
            if (!TryAccept(
                    item.ProductName,
                    item.ExtractedPrice ?? item.ValidatedPrice,
                    item.OriginalUrl,
                    item.PlatformType,
                    item.ProductImageUrl,
                    item.RawText,
                    request,
                    profile,
                    platforms,
                    sourcePlatform,
                    out var suggestion))
            {
                continue;
            }

            candidates.Add(suggestion);
        }

        foreach (var item in history)
        {
            if (!TryAccept(
                    item.ProductName,
                    item.ProductPrice,
                    item.OriginalUrl,
                    item.PlatformType,
                    item.ProductImageUrl,
                    null,
                    request,
                    profile,
                    platforms,
                    sourcePlatform,
                    out var suggestion))
            {
                continue;
            }

            candidates.Add(suggestion);
        }

        var best = candidates
            .GroupBy(item => item.OriginalUrl, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderBy(item => item.Price).First())
            .Where(item => item.Price is > 0 && (request.ProductPrice is not > 0 || item.Price < request.ProductPrice))
            .OrderBy(item => item.Price)
            .Take(AffiliateMiningRules.MaxArbitrageSuggestions)
            .ToList();

        var conversions = best.Select(item => ConvertSafeAsync(userId, item, cancellationToken));
        var converted = await Task.WhenAll(conversions);
        await PersistRadarAsync(userId, converted, cancellationToken);

        return new OfferArbitrageResponseDto
        {
            Status = true,
            Descricao = converted.Length == 0
                ? "Nenhum preço menor encontrado nas lojas conectadas."
                : $"Encontramos {converted.Length} alternativa(s) mais barata(s).",
            Suggestions = converted.ToList()
        };
    }

    private static bool TryAccept(
        string? name,
        decimal? price,
        string? url,
        MarketplaceType? platform,
        string? imageUrl,
        string? rawText,
        OfferArbitrageRequestDto request,
        AffiliateMiningProfileDto profile,
        IReadOnlyCollection<MarketplaceType> platforms,
        MarketplaceType? sourcePlatform,
        out OfferArbitrageSuggestionDto suggestion)
    {
        suggestion = new OfferArbitrageSuggestionDto();
        if (string.IsNullOrWhiteSpace(url) || platform is null || !platforms.Contains(platform.Value))
        {
            return false;
        }

        if (sourcePlatform is not null && platform == sourcePlatform)
        {
            return false;
        }

        if (!AffiliateMiningRules.TitlesLookAlike(request.ProductName, name)
            && !string.IsNullOrWhiteSpace(request.ProductName))
        {
            return false;
        }

        if (!AffiliateMiningRules.MatchesTicket(price, profile.MinTicket, profile.MaxTicket))
        {
            return false;
        }

        var niches = profile.Niches;
        if (!AffiliateMiningRules.MatchesNiche(name, niches))
        {
            return false;
        }

        suggestion = new OfferArbitrageSuggestionDto
        {
            ProductName = string.IsNullOrWhiteSpace(name) ? "Oferta alternativa" : name.Trim(),
            ProductImageUrl = imageUrl,
            OriginalUrl = url.Trim(),
            PlatformType = platform,
            PlatformName = platform.ToString(),
            Price = price,
            ComparedPrice = request.ProductPrice,
            CouponCode = AffiliateMiningRules.ExtractCoupon(rawText),
            AttractivenessScore = AffiliateMiningRules.ComputeAttractivenessScore(
                price,
                request.ProductPrice,
                AffiliateMiningRules.ExtractCoupon(rawText) is not null,
                true,
                false,
                false)
        };
        return true;
    }

    private async Task<OfferArbitrageSuggestionDto> ConvertSafeAsync(
        int userId,
        OfferArbitrageSuggestionDto item,
        CancellationToken cancellationToken)
    {
        try
        {
            var converted = await _converter.ConvertAsync(userId, item.OriginalUrl, "Arbitragem", cancellationToken);
            var data = converted.Data;
            if (converted.Status && data is not null && !string.IsNullOrWhiteSpace(data.AffiliateUrl))
            {
                item.AffiliateUrl = data.AffiliateUrl;
                if (!string.IsNullOrWhiteSpace(data.Title))
                {
                    item.ProductName = data.Title;
                }

                if (data.Price is > 0)
                {
                    item.Price = data.Price;
                }

                if (!string.IsNullOrWhiteSpace(data.ImageUrl))
                {
                    item.ProductImageUrl = data.ImageUrl;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Conversão da alternativa de arbitragem falhou. Url={Url}", item.OriginalUrl);
        }

        item.AffiliateUrl ??= item.OriginalUrl;
        return item;
    }

    private async Task PersistRadarAsync(
        int userId,
        IReadOnlyCollection<OfferArbitrageSuggestionDto> suggestions,
        CancellationToken cancellationToken)
    {
        var since = DateTime.UtcNow.AddHours(-24);
        foreach (var suggestion in suggestions)
        {
            var exists = await _context.OfferRadarItems.AnyAsync(
                item => item.UserId == userId
                    && item.OriginalUrl == suggestion.OriginalUrl
                    && item.ReceivedAt >= since,
                cancellationToken);
            if (exists)
            {
                continue;
            }

            _context.OfferRadarItems.Add(new OfferRadarItem
            {
                UserId = userId,
                ProductName = suggestion.ProductName,
                ProductImageUrl = suggestion.ProductImageUrl,
                OriginalUrl = suggestion.OriginalUrl,
                AffiliateUrl = suggestion.AffiliateUrl,
                PlatformType = suggestion.PlatformType,
                PlatformName = suggestion.PlatformName,
                Price = suggestion.Price,
                ComparedPrice = suggestion.ComparedPrice,
                CouponCode = suggestion.CouponCode,
                AttractivenessScore = suggestion.AttractivenessScore,
                Source = OfferRadarSources.Arbitrage,
                ReceivedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static MarketplaceType? DetectSourcePlatform(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        return TecFlow.Business.Service.Groups.GroupOfferCaptureRules.DetectPlatform(url);
    }
}
