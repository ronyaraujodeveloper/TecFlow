namespace TecFlow.Business.Integrations.Shopee;

/// <summary>Valores de homologação quando Integrations:Shopee não tem PartnerId/PartnerKey reais.</summary>
public static class ShopeeAuthorizationUrlFactory
{
    public const string SandboxPartnerId = "100000";
    public const string SandboxPartnerKey = "tecflow-homolog-shopee-sandbox";
    public const string AuthPartnerAbsoluteUrl = "https://partner.shopeemobile.com/api/v2/shop/auth_partner";

    public static bool HasLiveCredentials(string? partnerId, string? partnerKey) =>
        !string.IsNullOrWhiteSpace(partnerId) && !string.IsNullOrWhiteSpace(partnerKey);

    public static string ResolvePartnerId(string? partnerId) =>
        string.IsNullOrWhiteSpace(partnerId) ? SandboxPartnerId : partnerId.Trim();

    public static string ResolvePartnerKey(string? partnerKey) =>
        string.IsNullOrWhiteSpace(partnerKey) ? SandboxPartnerKey : partnerKey.Trim();

    public static string ResolveAuthPartnerUrl(string? apiBaseUrl, string? authPartnerPath)
    {
        if (string.IsNullOrWhiteSpace(apiBaseUrl) && string.IsNullOrWhiteSpace(authPartnerPath))
        {
            return AuthPartnerAbsoluteUrl;
        }

        var baseUrl = string.IsNullOrWhiteSpace(apiBaseUrl)
            ? "https://partner.shopeemobile.com/api/v2/"
            : apiBaseUrl.Trim();
        var path = string.IsNullOrWhiteSpace(authPartnerPath)
            ? "shop/auth_partner"
            : authPartnerPath.Trim().TrimStart('/');

        return $"{baseUrl.TrimEnd('/')}/{path}";
    }

    public static string BuildSandboxFallback(string redirectUri, string? state)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var stateValue = string.IsNullOrWhiteSpace(state)
            ? Guid.NewGuid().ToString("N")
            : state.Trim();
        var redirect = AppendQuery(redirectUri, "state", stateValue);

        return AppendQuery(
            AuthPartnerAbsoluteUrl,
            ("partner_id", SandboxPartnerId),
            ("timestamp", timestamp.ToString()),
            ("sign", "homolog-sandbox"),
            ("redirect", redirect));
    }

    private static string AppendQuery(string baseUrl, params (string Key, string Value)[] pairs)
    {
        var url = baseUrl;
        foreach (var (key, value) in pairs)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            url = AppendQuery(url, key, value);
        }

        return url;
    }

    private static string AppendQuery(string baseUrl, string key, string value)
    {
        var separator = baseUrl.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return $"{baseUrl}{separator}{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}";
    }
}
