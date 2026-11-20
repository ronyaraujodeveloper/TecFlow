using Ofertio.Business.Integrations.MercadoLivre;
using Ofertio.Business.Integrations.TikTokShop;

namespace Ofertio.Business.Service.Master;

/// <summary>
/// Chave do Cofre. <paramref name="Group"/> é a empresa ou serviço (um box por grupo na tela);
/// <paramref name="Field"/> é o nome do campo dentro do box.
/// </summary>
public sealed record SystemSettingDefinition(
    string Key,
    string Category,
    string Group,
    string Field,
    bool IsSecret,
    string? Placeholder = null)
{
    /// <summary>Nome completo, usado em mensagens fora do box (ex.: "Mercado Livre — App ID").</summary>
    public string Label => Group + " — " + Field;
}

/// <summary>Chaves globais editáveis no Painel Master (persistidas criptografadas em SystemSettings).</summary>
public static class SystemSettingCatalog
{
    public const string CategoryMarketplaces = "Marketplaces";
    public const string CategoryMessaging = "Mensageria";
    public const string CategoryVault = "Cofre";
    public const string CategoryBilling = "Pagamentos";
    public const string CategoryEmail = "E-mail";

    public const string GroupMercadoLivre = "Mercado Livre";
    public const string GroupShopee = "Shopee";
    public const string GroupAmazon = "Amazon";
    public const string GroupTikTokShop = "TikTok Shop";
    public const string GroupEvolution = "Evolution API (WhatsApp)";
    public const string GroupTelegram = "Telegram";
    public const string GroupOpenAi = "OpenAI";
    public const string GroupShortLinks = "Encurtador de links";
    public const string GroupAsaas = "ASAAS";
    public const string GroupGrace = "Carência após cancelamento";
    public const string GroupSmtp = "SMTP";

    public const string SmtpHost = "Email:Smtp:Host";
    public const string SmtpUser = "Email:Smtp:User";
    public const string SmtpPassword = "Email:Smtp:Password";
    public const string EmailFromAddress = "Email:FromAddress";

    public const string AsaasApiKey = "Payments:Asaas:ApiKey";
    public const string AsaasBaseUrl = "Payments:Asaas:BaseUrl";
    public const string AsaasWebhookToken = "Payments:Asaas:WebhookToken";
    public const string AsaasDefaultBaseUrl = "https://sandbox.asaas.com/api/v3";

    public const string LinkGraceDays = "Billing:LinkGraceDaysAfterCancel";

    public const string ShortLinkDomain = "Ofertio:ShortLinks:PublicBaseUrl";
    public const string DefaultShortLinkDomain = "https://tflow.link";

    public static readonly IReadOnlyList<SystemSettingDefinition> All =
    [
        new("Integrations:MercadoLivre:AppId", CategoryMarketplaces, GroupMercadoLivre, "App ID", false, "1234567890123456"),
        new("Integrations:MercadoLivre:SecretKey", CategoryMarketplaces, GroupMercadoLivre, "Client Secret", true),
        new("Integrations:MercadoLivre:RedirectUri", CategoryMarketplaces, GroupMercadoLivre, "Redirect URI", false, "https://dev.ofertio.com.br/api/mercadolivre/callback"),
        new(MercadoLivreSearchCacheRules.SettingKey, CategoryMarketplaces, GroupMercadoLivre, "Tempo de cache de buscas (minutos)", false, "15"),
        new("Integrations:Shopee:PartnerId", CategoryMarketplaces, GroupShopee, "Partner ID", false),
        new("Integrations:Shopee:PartnerKey", CategoryMarketplaces, GroupShopee, "Partner Key", true),
        new("Integrations:Shopee:AppKey", CategoryMarketplaces, GroupShopee, "Afiliados — AppKey", false),
        new("Integrations:Shopee:AppSecret", CategoryMarketplaces, GroupShopee, "Afiliados — AppSecret", true),
        new("Integrations:AmazonPaApi:AccessKey", CategoryMarketplaces, GroupAmazon, "PA-API — Access Key", false),
        new("Integrations:AmazonPaApi:SecretKey", CategoryMarketplaces, GroupAmazon, "PA-API — Secret Key", true),
        new("Integrations:AmazonPaApi:PartnerTag", CategoryMarketplaces, GroupAmazon, "Partner Tag", false, "minhaloja-20"),
        new("Integrations:TikTokShop:AppKey", CategoryMarketplaces, GroupTikTokShop, "App Key", false),
        new("Integrations:TikTokShop:AppSecret", CategoryMarketplaces, GroupTikTokShop, "App Secret", true),
        new("Integrations:TikTokShop:ServiceId", CategoryMarketplaces, GroupTikTokShop, "Service ID", false),
        new("Integrations:TikTokShop:RedirectUri", CategoryMarketplaces, GroupTikTokShop, "Redirect URL", false, "https://dev.ofertio.com.br/integracoes/tiktok/callback"),
        new(TikTokShopSearchCacheRules.SettingKey, CategoryMarketplaces, GroupTikTokShop, "Tempo de cache de buscas (minutos)", false, "15"),

        new("EvolutionApi:BaseUrl", CategoryMessaging, GroupEvolution, "URL base", false, "http://localhost:8080"),
        new("EvolutionApi:ApiKey", CategoryMessaging, GroupEvolution, "Global API Key", true),
        new("Telegram:WebhookBaseUrl", CategoryMessaging, GroupTelegram, "URL pública do webhook", false, "https://api.seu-dominio"),
        new("Telegram:DefaultApiId", CategoryMessaging, GroupTelegram, "MTProto — ApiId padrão", false),
        new("Telegram:DefaultApiHash", CategoryMessaging, GroupTelegram, "MTProto — ApiHash padrão", true),

        new("OpenAI:ApiKey", CategoryVault, GroupOpenAi, "API Key", true, "sk-..."),
        new(ShortLinkDomain, CategoryVault, GroupShortLinks, "Domínio do encurtador", false, DefaultShortLinkDomain),

        new(AsaasApiKey, CategoryBilling, GroupAsaas, "API Key", true, "$aact_..."),
        new(AsaasBaseUrl, CategoryBilling, GroupAsaas, "URL base", false, AsaasDefaultBaseUrl),
        new(AsaasWebhookToken, CategoryBilling, GroupAsaas, "Token do webhook", true),
        new(LinkGraceDays, CategoryBilling, GroupGrace, "Dias de carência dos links e páginas públicas após cancelar ou suspender", false, "30"),

        new(SmtpHost, CategoryEmail, GroupSmtp, "Servidor (vazio = só registra no log)", false, "smtp.gmail.com"),
        new(SmtpUser, CategoryEmail, GroupSmtp, "Usuário", false, "ofertiotecso@gmail.com"),
        new(SmtpPassword, CategoryEmail, GroupSmtp, "Senha de app do Gmail (16 letras, não a senha da conta)", true),
        new(EmailFromAddress, CategoryEmail, GroupSmtp, "Remetente (verificado em \"Enviar e-mail como\" no Gmail)", false, "contato@ofertio.com.br"),
    ];

    /// <summary>
    /// Seções cujas chaves só valem depois de reiniciar a API. O e-mail lê a configuração a cada envio
    /// (IOptionsMonitor), então salvar já basta.
    /// </summary>
    public static bool RequiresRestart(string? category) =>
        !string.Equals(category, CategoryEmail, StringComparison.OrdinalIgnoreCase);

    public const string GmailSmtpHost = "smtp.gmail.com";

    /// <summary>Limpa o que costuma vir colado no campo: espaços, "smtp://", "https://", ":587" e maiúsculas.</summary>
    public static string NormalizeSmtpHost(string? value)
    {
        var host = (value ?? string.Empty).Trim().ToLowerInvariant();
        foreach (var prefix in new[] { "smtp://", "smtps://", "https://", "http://" })
        {
            if (host.StartsWith(prefix, StringComparison.Ordinal))
            {
                host = host[prefix.Length..];
            }
        }

        host = host.TrimEnd('/');
        var colon = host.LastIndexOf(':');
        if (colon > 0 && int.TryParse(host[(colon + 1)..], out _))
        {
            host = host[..colon];
        }

        return host;
    }

    /// <summary>
    /// Erro para mostrar ao master, ou null quando o servidor serve. Vazio é válido (desliga o envio).
    /// <paramref name="user"/> é o usuário SMTP efetivo: conta @gmail.com só funciona com smtp.gmail.com.
    /// </summary>
    public static string? ValidateSmtpHost(string? normalizedHost, string? user)
    {
        var host = normalizedHost ?? string.Empty;
        if (host.Length == 0)
        {
            return null;
        }

        if (host.Contains('@'))
        {
            return "\"" + host + "\" é um e-mail. O servidor do Gmail é " + GmailSmtpHost + "; o e-mail vai no campo Usuário.";
        }

        if (host is "localhost" or "127.0.0.1" or "::1" || host.StartsWith("127.", StringComparison.Ordinal))
        {
            return "\"" + host + "\" aponta para o próprio container da API, onde não há servidor de e-mail. Para o Gmail use " + GmailSmtpHost + ".";
        }

        if (host.Any(char.IsWhiteSpace) || !host.Contains('.'))
        {
            return "\"" + host + "\" não é um servidor válido. Para o Gmail use " + GmailSmtpHost + ".";
        }

        var gmailUser = !string.IsNullOrWhiteSpace(user)
            && user.Trim().EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase);
        if (gmailUser && !string.Equals(host, GmailSmtpHost, StringComparison.Ordinal))
        {
            return "Para conta @gmail.com o servidor é " + GmailSmtpHost + " (\"" + host + "\" não aceita essa conta; smtp.google.com é só para Google Workspace).";
        }

        return null;
    }

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

    /// <summary>Valor recebido é exatamente a máscara exibida do segredo atual (campo não alterado).</summary>
    public static bool IsUnchangedMask(string? incoming, string? current) =>
        !string.IsNullOrEmpty(incoming)
        && string.Equals(incoming, Mask(current), StringComparison.Ordinal);

    /// <summary>Valor editado que ainda carrega o caractere da máscara (ex.: digitado após "••••1234").</summary>
    public static bool ContainsMaskCharacter(string? value) =>
        !string.IsNullOrEmpty(value) && value.Contains(MaskCharacter);

    public const char MaskCharacter = '•';
}
