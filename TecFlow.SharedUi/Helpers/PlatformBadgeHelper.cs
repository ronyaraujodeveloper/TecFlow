using TecFlow.Core.Enums;

namespace TecFlow.SharedUi.Helpers;

public static class PlatformBadgeHelper
{
    public const string DefaultBadgeClass = "marketplace-platform-badge--default";

    public static string GetPlatformBadgeClass(MarketplaceType platform) => platform switch
    {
        MarketplaceType.Amazon => "marketplace-platform-badge--amazon",
        MarketplaceType.MercadoLivre => "marketplace-platform-badge--mercadolivre",
        MarketplaceType.Shopee => "marketplace-platform-badge--shopee",
        MarketplaceType.MagazineLuiza => "marketplace-platform-badge--magalu",
        MarketplaceType.TikTokShop => "marketplace-platform-badge--tiktok",
        MarketplaceType.Kabum => "marketplace-platform-badge--kabum",
        MarketplaceType.CasasBahia => "marketplace-platform-badge--casasbahia",
        _ => ResolveAliasClass(platform.ToString()) ?? DefaultBadgeClass
    };

    public static string GetPlatformBadgeClass(string? platformName)
    {
        if (TryResolveMarketplace(platformName, out var platform))
        {
            return GetPlatformBadgeClass(platform);
        }

        return ResolveAliasClass(platformName) ?? DefaultBadgeClass;
    }

    public static string GetPlatformBadgeClass(MarketplaceType platform, string? platformName)
    {
        var alias = ResolveAliasClass(platformName);
        if (alias is not null)
        {
            return alias;
        }

        if (TryResolveMarketplace(platformName, out var resolved))
        {
            return GetPlatformBadgeClass(resolved);
        }

        return GetPlatformBadgeClass(platform);
    }

    public static string GetPlatformColor(MarketplaceType platform) => platform switch
    {
        MarketplaceType.Amazon => "#FF9900",
        MarketplaceType.MercadoLivre => "#FFE600",
        MarketplaceType.Shopee => "#EE4D2D",
        MarketplaceType.MagazineLuiza => "#0086FF",
        MarketplaceType.TikTokShop => "#000000",
        MarketplaceType.Kabum => "#FF6500",
        MarketplaceType.CasasBahia => "#E30613",
        _ => GetPlatformColor(platform.ToString())
    };

    public static string GetPlatformColor(string? platformName)
    {
        if (TryResolveMarketplace(platformName, out var platform))
        {
            return GetPlatformColor(platform);
        }

        var compact = MarketplaceTypeExtensions.CompactKey(platformName);
        return compact switch
        {
            "hotmart" => "#FF5200",
            "braip" => "#12B76A",
            _ => "#6B7280"
        };
    }

    public static string GetPlatformLabel(MarketplaceType platform) => platform.GetDisplayName();

    public static string GetPlatformLabel(string? platformName, MarketplaceType? platformType = null)
    {
        if (!string.IsNullOrWhiteSpace(platformName))
        {
            return platformName.Trim();
        }

        return platformType is { } type ? type.GetDisplayName() : "Outros";
    }

    public static bool TryResolveMarketplace(string? platformName, out MarketplaceType platform)
    {
        platform = default;
        if (MarketplaceTypeExtensions.TryCanonicalKey(platformName, out var key))
        {
            platform = key switch
            {
                "magazineluiza" => MarketplaceType.MagazineLuiza,
                "mercadolivre" => MarketplaceType.MercadoLivre,
                "tiktokshop" => MarketplaceType.TikTokShop,
                "casasbahia" => MarketplaceType.CasasBahia,
                "kabum" => MarketplaceType.Kabum,
                "amazon" => MarketplaceType.Amazon,
                "shopee" => MarketplaceType.Shopee,
                _ => default
            };
            return platform != default || key is "shopee";
        }

        return false;
    }

    private static string? ResolveAliasClass(string? platformName)
    {
        var compact = MarketplaceTypeExtensions.CompactKey(platformName);
        return compact switch
        {
            "hotmart" => "marketplace-platform-badge--hotmart",
            "braip" => "marketplace-platform-badge--braip",
            _ => null
        };
    }
}
