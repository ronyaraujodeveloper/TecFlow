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
        _ => platform.ToString()
    };
}
