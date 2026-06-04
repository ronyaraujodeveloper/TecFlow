namespace TecFlow.Business.Integrations.TikTokShop;

/// <summary>Credenciais e endpoints da TikTok Shop Open API (produção).</summary>
public class TikTokShopIntegrationOptions
{
    public const string SectionName = "Integrations:TikTokShop";

    public string AppKey { get; set; } = string.Empty;
    public string AppSecret { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = "https://open-api.tiktokglobalshop.com";
    public string AuthorizeUrl { get; set; } = "https://auth.tiktok-shops.com/oauth/authorize";
    public string TokenUrl { get; set; } = "https://auth.tiktok-shops.com/api/v2/token/get";
    public string RefreshTokenUrl { get; set; } = "https://auth.tiktok-shops.com/api/v2/token/refresh";
    /// <summary>Path relativo à ApiBaseUrl (ex.: api/v1/products/search ou product/202309/products/search).</summary>
    public string ProductsSearchPath { get; set; } = "api/v1/products/search";
    public string OrdersSearchPath { get; set; } = "api/v1/orders/search";
    public string UpdateStocksPath { get; set; } = "api/v1/products/stocks";
    public string AffiliatePerformanceReportPath { get; set; } = "api/v1/affiliate/performance";
    public string? WebhookSecret { get; set; }
    public int TimeoutSeconds { get; set; } = 100;
    public bool EnableRequestLogging { get; set; } = true;
}
