using TecFlow.Core.Enums;

namespace TecFlow.Business.Integrations.Auth;

public static class MarketplacePlatformRoute
{
    public static string ToSlug(MarketplaceType type) => type switch
    {
        MarketplaceType.Shopee => "shopee",
        MarketplaceType.TikTokShop => "tiktok",
        _ => type.ToString().ToLowerInvariant()
    };

    public static bool TryParse(string? plataforma, out MarketplaceType type)
    {
        var key = (plataforma ?? string.Empty)
            .Trim()
            .ToLowerInvariant()
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal);

        switch (key)
        {
            case "shopee":
                type = MarketplaceType.Shopee;
                return true;
            case "tiktok":
            case "tiktokshop":
                type = MarketplaceType.TikTokShop;
                return true;
            case "mercadolivre":
            case "mercadolibre":
            case "mlb":
                type = MarketplaceType.MercadoLivre;
                return true;
            case "amazon":
            case "amzn":
                type = MarketplaceType.Amazon;
                return true;
            case "magalu":
            case "magazineluiza":
            case "magazinevoce":
                type = MarketplaceType.MagazineLuiza;
                return true;
            case "kabum":
            case "kabumloja":
                type = MarketplaceType.Kabum;
                return true;
            case "casasbahia":
            case "via":
            case "cb":
                type = MarketplaceType.CasasBahia;
                return true;
            default:
                type = default;
                return false;
        }
    }
}
