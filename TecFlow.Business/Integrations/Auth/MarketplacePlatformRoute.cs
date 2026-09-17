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
            default:
                type = default;
                return false;
        }
    }
}
