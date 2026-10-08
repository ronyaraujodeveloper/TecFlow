using TecFlow.Business.Integrations.Amazon;
using TecFlow.Business.Integrations.MercadoLivre;
using TecFlow.Business.Service.LinkStrategies;
using TecFlow.Core.Enums;

namespace TecFlow.Business.Service.Radar;

public static class ProductSkuRules
{
    public static bool TryExtract(string? url, MarketplaceType? platform, out string platformKey, out string productId)
    {
        platformKey = string.Empty;
        productId = string.Empty;
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        var resolved = platform ?? DetectPlatform(url);
        if (resolved is MarketplaceType.MercadoLivre && MercadoLivreProductUrlParser.TryParse(url, out var mlb))
        {
            platformKey = nameof(MarketplaceType.MercadoLivre);
            productId = mlb;
            return true;
        }

        if (resolved is MarketplaceType.Shopee && ShopeeProductUrlParser.TryParse(url, out var shopee))
        {
            platformKey = nameof(MarketplaceType.Shopee);
            productId = $"{shopee.ShopId}:{shopee.ItemId}";
            return true;
        }

        if (resolved is MarketplaceType.Amazon && AmazonProductUrlParser.TryParse(url, out var asin))
        {
            platformKey = nameof(MarketplaceType.Amazon);
            productId = asin.ToUpperInvariant();
            return true;
        }

        if (resolved is MarketplaceType.MercadoLivre or MarketplaceType.Shopee or MarketplaceType.Amazon)
        {
            return false;
        }

        if (MercadoLivreProductUrlParser.TryParse(url, out mlb))
        {
            platformKey = nameof(MarketplaceType.MercadoLivre);
            productId = mlb;
            return true;
        }

        if (ShopeeProductUrlParser.TryParse(url, out shopee))
        {
            platformKey = nameof(MarketplaceType.Shopee);
            productId = $"{shopee.ShopId}:{shopee.ItemId}";
            return true;
        }

        if (AmazonProductUrlParser.TryParse(url, out asin))
        {
            platformKey = nameof(MarketplaceType.Amazon);
            productId = asin.ToUpperInvariant();
            return true;
        }

        return false;
    }

    private static MarketplaceType? DetectPlatform(string url)
    {
        var lower = url.ToLowerInvariant();
        if (lower.Contains("mercadolivre", StringComparison.Ordinal) || lower.Contains("mercadolibre", StringComparison.Ordinal))
        {
            return MarketplaceType.MercadoLivre;
        }

        if (lower.Contains("shopee", StringComparison.Ordinal))
        {
            return MarketplaceType.Shopee;
        }

        if (lower.Contains("amazon.", StringComparison.Ordinal) || lower.Contains("amzn.to", StringComparison.Ordinal))
        {
            return MarketplaceType.Amazon;
        }

        return null;
    }
}
