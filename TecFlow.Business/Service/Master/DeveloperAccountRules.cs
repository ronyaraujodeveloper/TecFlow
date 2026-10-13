namespace TecFlow.Business.Service.Master;

public static class DeveloperPlatforms
{
    public const string MercadoLivre = "MercadoLivre";
    public const string Shopee = "Shopee";
    public const string Amazon = "Amazon";

    public static readonly IReadOnlyList<string> All = [MercadoLivre, Shopee, Amazon];

    public static string? Normalize(string? platform) =>
        All.FirstOrDefault(item => string.Equals(item, platform?.Trim(), StringComparison.OrdinalIgnoreCase));

    public static string Label(string? platform) => Normalize(platform) switch
    {
        MercadoLivre => "Mercado Livre",
        Shopee => "Shopee Afiliados",
        Amazon => "Amazon PA-API",
        _ => platform ?? string.Empty
    };
}

public static class DeveloperAccountKinds
{
    public const string App = "App";
    public const string Test = "Teste";

    public static readonly IReadOnlyList<string> All = [App, Test];

    public static string? Normalize(string? kind) =>
        All.FirstOrDefault(item => string.Equals(item, kind?.Trim(), StringComparison.OrdinalIgnoreCase));
}

public static class DeveloperEnvironments
{
    public const string Production = "Producao";
    public const string Sandbox = "Sandbox";

    public static readonly IReadOnlyList<string> All = [Production, Sandbox];

    public static string Normalize(string? environment) =>
        All.FirstOrDefault(item => string.Equals(item, environment?.Trim(), StringComparison.OrdinalIgnoreCase)) ?? Production;
}

public static class SecretExpirationStatus
{
    public const string None = "sem-validade";
    public const string Valid = "valido";
    public const string Expiring = "expirando";
    public const string Expired = "expirado";
}

/// <summary>Regras das contas de desenvolvedor (App Credentials) e do vínculo com as chaves globais.</summary>
public static class DeveloperAccountRules
{
    public const int ExpiringWindowDays = 15;

    public static string ExpirationStatus(DateTime? expiresAt, DateTime now)
    {
        if (expiresAt is null)
        {
            return SecretExpirationStatus.None;
        }

        if (expiresAt.Value <= now)
        {
            return SecretExpirationStatus.Expired;
        }

        return expiresAt.Value <= now.AddDays(ExpiringWindowDays)
            ? SecretExpirationStatus.Expiring
            : SecretExpirationStatus.Valid;
    }

    /// <summary>Chaves de SystemSettings preenchidas ao publicar a conta como credencial global.</summary>
    public static IReadOnlyDictionary<string, string?> GlobalSettingValues(string platform, string? clientId, string? secret, string? extra)
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        switch (DeveloperPlatforms.Normalize(platform))
        {
            case DeveloperPlatforms.MercadoLivre:
                values["Integrations:MercadoLivre:AppId"] = clientId;
                values["Integrations:MercadoLivre:SecretKey"] = secret;
                if (!string.IsNullOrWhiteSpace(extra))
                {
                    values["Integrations:MercadoLivre:RedirectUri"] = extra;
                }

                break;
            case DeveloperPlatforms.Shopee:
                values["Integrations:Shopee:AppKey"] = clientId;
                values["Integrations:Shopee:AppSecret"] = secret;
                break;
            case DeveloperPlatforms.Amazon:
                values["Integrations:AmazonPaApi:AccessKey"] = clientId;
                values["Integrations:AmazonPaApi:SecretKey"] = secret;
                if (!string.IsNullOrWhiteSpace(extra))
                {
                    values["Integrations:AmazonPaApi:PartnerTag"] = extra;
                }

                break;
        }

        return values;
    }

    /// <summary>Retorna a mensagem de erro de validação ou null.</summary>
    public static string? Validate(string? platform, string? kind, string? label, string? clientId, bool hasSecret)
    {
        if (DeveloperPlatforms.Normalize(platform) is null)
        {
            return "Plataforma inválida. Use Mercado Livre, Shopee ou Amazon.";
        }

        if (DeveloperAccountKinds.Normalize(kind) is null)
        {
            return "Tipo inválido. Use App (credenciais) ou Teste.";
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            return "Informe um nome para identificar a conta.";
        }

        if (DeveloperAccountKinds.Normalize(kind) == DeveloperAccountKinds.App
            && (string.IsNullOrWhiteSpace(clientId) || !hasSecret))
        {
            return "Credenciais de app exigem o ID (AppId / AppKey / Access Key) e o segredo.";
        }

        return null;
    }
}
