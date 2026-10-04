namespace TecFlow.Core.Enums;

public static class MarketplaceTypeExtensions
{
    public static bool IsUniversalAffiliatePlatform(this MarketplaceType platform) =>
        platform is MarketplaceType.Shopee
            or MarketplaceType.TikTokShop
            or MarketplaceType.MercadoLivre
            or MarketplaceType.Amazon
            or MarketplaceType.MagazineLuiza
            or MarketplaceType.Kabum
            or MarketplaceType.CasasBahia;

    public static string GetDisplayName(this MarketplaceType platform) => platform switch
    {
        MarketplaceType.Shopee => "Shopee",
        MarketplaceType.TikTokShop => "TikTok Shop",
        MarketplaceType.MercadoLivre => "Mercado Livre",
        MarketplaceType.Amazon => "Amazon",
        MarketplaceType.MagazineLuiza => "Magazine Luiza",
        MarketplaceType.Kabum => "Kabum!",
        MarketplaceType.CasasBahia => "Casas Bahia",
        MarketplaceType.AliExpress => "AliExpress",
        _ => platform.ToString()
    };

    /// <summary>
    /// Comparação flexível: Magalu, Magazine Luiza, MagazineLuiza e Magazine_Luiza são a mesma plataforma.
    /// </summary>
    public static bool AreSamePlatform(this MarketplaceType left, MarketplaceType right) =>
        CanonicalKey(left) == CanonicalKey(right);

    public static bool AreSamePlatform(this MarketplaceType platform, string? alias) =>
        TryCanonicalKey(alias, out var key) && key == CanonicalKey(platform);

    public static bool AreSamePlatform(string? left, string? right) =>
        TryCanonicalKey(left, out var leftKey)
        && TryCanonicalKey(right, out var rightKey)
        && leftKey == rightKey;

    public static string CompactKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var chars = value.Where(char.IsLetterOrDigit).ToArray();
        return new string(chars).ToLowerInvariant();
    }

    public static string CanonicalKey(this MarketplaceType platform) => platform switch
    {
        MarketplaceType.MagazineLuiza => "magazineluiza",
        MarketplaceType.MercadoLivre => "mercadolivre",
        MarketplaceType.TikTokShop => "tiktokshop",
        MarketplaceType.CasasBahia => "casasbahia",
        MarketplaceType.Kabum => "kabum",
        MarketplaceType.Amazon => "amazon",
        MarketplaceType.Shopee => "shopee",
        MarketplaceType.AliExpress => "aliexpress",
        _ => CompactKey(platform.ToString())
    };

    public static bool TryCanonicalKey(string? value, out string key)
    {
        key = string.Empty;
        var compact = CompactKey(value);
        if (compact.Length == 0)
        {
            return false;
        }

        if (compact is "magalu" or "magazineluiza" or "magazinevoce" or "magazine")
        {
            key = "magazineluiza";
            return true;
        }

        if (compact is "mercadolivre" or "mercadolibre" or "mlb" or "ml")
        {
            key = "mercadolivre";
            return true;
        }

        if (compact.Contains("tiktok", StringComparison.Ordinal))
        {
            key = "tiktokshop";
            return true;
        }

        if (compact.Contains("casasbahia", StringComparison.Ordinal) || compact.Contains("casas", StringComparison.Ordinal))
        {
            key = "casasbahia";
            return true;
        }

        if (compact.Contains("kabum", StringComparison.Ordinal))
        {
            key = "kabum";
            return true;
        }

        if (compact.Contains("amazon", StringComparison.Ordinal) || compact is "amzn")
        {
            key = "amazon";
            return true;
        }

        if (compact.Contains("shopee", StringComparison.Ordinal))
        {
            key = "shopee";
            return true;
        }

        if (compact.Contains("aliexpress", StringComparison.Ordinal))
        {
            key = "aliexpress";
            return true;
        }

        if (Enum.TryParse<MarketplaceType>(compact, ignoreCase: true, out var parsed))
        {
            key = CanonicalKey(parsed);
            return true;
        }

        return false;
    }
}
