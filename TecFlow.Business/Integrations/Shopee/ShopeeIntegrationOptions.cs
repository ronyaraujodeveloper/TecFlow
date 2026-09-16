namespace TecFlow.Business.Integrations.Shopee;

/// <summary>Credenciais e endpoints da Shopee Open Platform (produção).</summary>
public class ShopeeIntegrationOptions
{
    public const string SectionName = "Integrations:Shopee";

    public string PartnerId { get; set; } = string.Empty;
    public string PartnerKey { get; set; } = string.Empty;
    public string AppSecret { get; set; } = string.Empty;
    public string AppKey { get; set; } = string.Empty;
    public string AppSignature { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = "https://partner.shopeemobile.com/api/v2/";
    public string AuthPartnerPath { get; set; } = "shop/auth_partner";
    public string TokenPath { get; set; } = "auth/token/get";
    public string RefreshTokenPath { get; set; } = "auth/access_token/get";
    public string GetItemListPath { get; set; } = "product/get_item_list";
    public string GetItemBaseInfoPath { get; set; } = "product/get_item_base_info";
    public string GetModelListPath { get; set; } = "product/get_model_list";
    public string GetOrderListPath { get; set; } = "order/get_order_list";
    public string GetOrderDetailPath { get; set; } = "order/get_order_detail";
    public string UpdateStockPath { get; set; } = "product/update_stock";
    public string AffiliateCommissionReportPath { get; set; } = "affiliate/get_conversion_report";
    public string AffiliateApiBaseUrl { get; set; } = "https://open-api.affiliate.shopee.com.br";
    public string GenerateCustomLinkPath { get; set; } = "api/v1/custom_link/generate";
    public string? AffiliateAppId { get; set; }
    public string? AffiliateSecret { get; set; }
    public string SandboxTrackingCode { get; set; } = ShopeeSandboxLinkBuilder.DefaultTrackingCode;
    public string WebhookCallbackUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 100;
    public bool EnableRequestLogging { get; set; } = true;

    public string? ResolveAffiliateAppId() =>
        FirstNonEmpty(AffiliateAppId, AppKey, PartnerId);

    public string? ResolveAffiliateSecret() =>
        FirstNonEmpty(AffiliateSecret, AppSecret, AppSignature, PartnerKey);

    public bool HasAffiliateCredentials =>
        !string.IsNullOrWhiteSpace(ResolveAffiliateAppId())
        && !string.IsNullOrWhiteSpace(ResolveAffiliateSecret());

    public bool IsSandboxMode => !HasAffiliateCredentials;

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}
