namespace TecFlow.Business.Service.Master;

public sealed record SystemSettingDefinition(
    string Key,
    string Category,
    string Label,
    bool IsSecret,
    string? Placeholder = null);

/// <summary>Chaves globais editáveis no Painel Master (persistidas criptografadas em SystemSettings).</summary>
public static class SystemSettingCatalog
{
    public const string CategoryMarketplaces = "Marketplaces";
    public const string CategoryMessaging = "Mensageria";
    public const string CategoryVault = "Cofre";
    public const string CategoryBilling = "Pagamentos";

    public const string AsaasApiKey = "Payments:Asaas:ApiKey";
    public const string AsaasBaseUrl = "Payments:Asaas:BaseUrl";
    public const string AsaasWebhookToken = "Payments:Asaas:WebhookToken";
    public const string AsaasDefaultBaseUrl = "https://sandbox.asaas.com/api/v3";

    public const string ShortLinkDomain = "TecFlow:ShortLinks:PublicBaseUrl";
    public const string DefaultShortLinkDomain = "https://tflow.link";

    public static readonly IReadOnlyList<SystemSettingDefinition> All =
    [
        new("Integrations:MercadoLivre:AppId", CategoryMarketplaces, "Mercado Livre — App ID", false, "1234567890123456"),
        new("Integrations:MercadoLivre:SecretKey", CategoryMarketplaces, "Mercado Livre — Client Secret", true),
        new("Integrations:MercadoLivre:RedirectUri", CategoryMarketplaces, "Mercado Livre — Redirect URI", false, "https://seu-dominio/integracoes/oauth/callback"),
        new("Integrations:Shopee:PartnerId", CategoryMarketplaces, "Shopee — Partner ID", false),
        new("Integrations:Shopee:PartnerKey", CategoryMarketplaces, "Shopee — Partner Key", true),
        new("Integrations:Shopee:AppKey", CategoryMarketplaces, "Shopee Afiliados — AppKey", false),
        new("Integrations:Shopee:AppSecret", CategoryMarketplaces, "Shopee Afiliados — AppSecret", true),
        new("Integrations:AmazonPaApi:AccessKey", CategoryMarketplaces, "Amazon PA-API — Access Key", false),
        new("Integrations:AmazonPaApi:SecretKey", CategoryMarketplaces, "Amazon PA-API — Secret Key", true),
        new("Integrations:AmazonPaApi:PartnerTag", CategoryMarketplaces, "Amazon — Partner Tag", false, "minhaloja-20"),

        new("EvolutionApi:BaseUrl", CategoryMessaging, "Evolution API — URL base", false, "http://localhost:8080"),
        new("EvolutionApi:ApiKey", CategoryMessaging, "Evolution API — Global API Key", true),
        new("Telegram:WebhookBaseUrl", CategoryMessaging, "Telegram — URL pública do webhook", false, "https://api.seu-dominio"),
        new("Telegram:DefaultApiId", CategoryMessaging, "Telegram MTProto — ApiId padrão", false),
        new("Telegram:DefaultApiHash", CategoryMessaging, "Telegram MTProto — ApiHash padrão", true),

        new("OpenAI:ApiKey", CategoryVault, "OpenAI — API Key", true, "sk-..."),
        new(ShortLinkDomain, CategoryVault, "Domínio do encurtador", false, DefaultShortLinkDomain),

        new(AsaasApiKey, CategoryBilling, "ASAAS — API Key", true, "$aact_..."),
        new(AsaasBaseUrl, CategoryBilling, "ASAAS — URL base", false, AsaasDefaultBaseUrl),
        new(AsaasWebhookToken, CategoryBilling, "ASAAS — Token do webhook", true),
    ];

    public static SystemSettingDefinition? Find(string? key) =>
        string.IsNullOrWhiteSpace(key)
            ? null
            : All.FirstOrDefault(item => string.Equals(item.Key, key.Trim(), StringComparison.OrdinalIgnoreCase));

    public static string Mask(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Length <= 4 ? "••••" : $"••••{value[^4..]}";
    }
}
