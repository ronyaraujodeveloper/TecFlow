namespace Ofertio.Business.Integrations.TikTokShop;

/// <summary>Credenciais e endpoints da TikTok Shop Open API (produção).</summary>
public class TikTokShopIntegrationOptions
{
    public const string SectionName = "Integrations:TikTokShop";

    public string AppKey { get; set; } = string.Empty;
    public string AppSecret { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = "https://open-api.tiktokglobalshop.com";
    public string AuthorizeUrl { get; set; } = "https://auth.tiktok-shops.com/oauth/authorize";

    /// <summary>Service ID do app no Partner Center; quando preenchido, a autorização usa <see cref="ServiceAuthorizeUrl"/>.</summary>
    public string ServiceId { get; set; } = string.Empty;

    public string ServiceAuthorizeUrl { get; set; } = "https://services.tiktokshop.com/open/authorize";

    /// <summary>Redirect URL cadastrada no app (ex.: https://dev.ofertio.com.br/integracoes/oauth/callback).</summary>
    public string RedirectUri { get; set; } = string.Empty;

    public string TokenUrl { get; set; } = "https://auth.tiktok-shops.com/api/v2/token/get";
    public string RefreshTokenUrl { get; set; } = "https://auth.tiktok-shops.com/api/v2/token/refresh";
    /// <summary>Path relativo à ApiBaseUrl (ex.: api/v1/products/search ou product/202309/products/search).</summary>
    public string ProductsSearchPath { get; set; } = "api/v1/products/search";
    public string OrdersSearchPath { get; set; } = "api/v1/orders/search";
    public string UpdateStocksPath { get; set; } = "api/v1/products/stocks";
    public string AffiliatePerformanceReportPath { get; set; } = "api/v1/affiliate/performance";
    public string GenerateAffiliateLinkPath { get; set; } = "api/v1/affiliate/links/generate";

    /// <summary>Affiliate Creator: busca de produtos em colaboração aberta (token de criador, <c>user_type</c> = 1).</summary>
    public string AffiliateProductSearchPath { get; set; } = "affiliate_creator/202405/open_collaborations/products/search";

    /// <summary>
    /// Affiliate Creator: gera o link de promoção do produto com o token do criador.
    /// <c>{product_id}</c> é substituído pelo ID do produto.
    /// </summary>
    public string AffiliatePromotionLinkPath { get; set; } = "affiliate_creator/202405/products/{product_id}/promotion_link/generate";

    /// <summary>
    /// Tempo de cache global das buscas com o token Master (minutos; 0 desliga). Texto para que um valor inválido no
    /// cofre não quebre o binding das opções (cai no padrão).
    /// </summary>
    public string? SearchCacheMinutes { get; set; }

    public int HttpTimeoutSeconds { get; set; } = 30;

    public string? WebhookSecret { get; set; }
    public int TimeoutSeconds { get; set; } = 100;
    public bool EnableRequestLogging { get; set; } = true;

    public bool HasAppCredentials =>
        !string.IsNullOrWhiteSpace(AppKey) && !string.IsNullOrWhiteSpace(AppSecret);

    public int ResolveSearchCacheMinutes() =>
        TikTokShopSearchCacheRules.ResolveMinutes(SearchCacheMinutes);

    public TimeSpan ResolveHttpTimeout() =>
        TimeSpan.FromSeconds(Math.Clamp(HttpTimeoutSeconds <= 0 ? 30 : HttpTimeoutSeconds, 5, 120));
}
