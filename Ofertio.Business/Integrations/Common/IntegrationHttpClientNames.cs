namespace Ofertio.Business.Integrations.Common;

/// <summary>Nomes dos HttpClient registrados via IHttpClientFactory.</summary>
public static class IntegrationHttpClientNames
{
    public const string TikTokShop = "Ofertio.Integrations.TikTokShop";
    public const string Shopee = "Ofertio.Integrations.Shopee";
    public const string UrlExpansion = "Ofertio.UrlExpansion";
    public const string UrlExpansionFollow = "Ofertio.UrlExpansion.Follow";
    public const string UrlResolver = "Ofertio.UrlResolver";
    public const string ProductMetadata = "Ofertio.ProductMetadata";
}
