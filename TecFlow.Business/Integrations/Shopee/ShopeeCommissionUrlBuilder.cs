using TecFlow.Business.Service.LinkStrategies;

namespace TecFlow.Business.Integrations.Shopee;

/// <summary>Monta a query string de comissão Shopee com encoding e atribuição usuário/tenant.</summary>
public static class ShopeeCommissionUrlBuilder
{
    public const string DefaultTrackingCode = "tecflow_sandbox_subid";
    public const string TrackingCodeQuery = "tracking_code";
    public const string SubIdQuery = "sub_id";
    public const string UniversalLinkQuery = "universal_link";
    public const string DeepLinkQuery = "deep_link";
    public const string AffiliateIdQuery = "affiliate_id";
    public const string HomologAffiliateId = "12345";

    public static string BuildSubId(int userId, Guid tenantId)
    {
        if (userId <= 0)
        {
            throw new AffiliateLinkGenerationException(
                "Não foi possível atribuir sub_id: identificação do usuário logado ausente.");
        }

        if (tenantId == Guid.Empty)
        {
            return $"u{userId}";
        }

        return $"u{userId}_t{tenantId:N}";
    }

    public static string ToUniversalWebUrl(ShopeeProductUrlIds ids) =>
        $"https://shopee.com.br/product/{ids.ShopId}/{ids.ItemId}";

    public static string EncodeQueryComponent(string value) =>
        Uri.EscapeDataString(value ?? string.Empty);

    public static string FormatQueryPair(string key, string value) =>
        $"{EncodeQueryComponent(key)}={EncodeQueryComponent(value)}";

    public static bool ContainsEncodedQueryPair(string url, string key, string value) =>
        !string.IsNullOrWhiteSpace(url)
        && url.Contains(FormatQueryPair(key, value), StringComparison.Ordinal);

    public static string Merge(
        string productUrl,
        string? trackingCode = null,
        string? subId = null,
        string? universalLink = null,
        string? deepLink = null,
        string? affiliateId = null)
    {
        var baseUrl = string.IsNullOrWhiteSpace(productUrl)
            ? "https://shopee.com.br/"
            : productUrl.Trim();

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
        {
            throw new AffiliateLinkGenerationException("URL de afiliado inválida para aplicar rastreamento.");
        }

        var query = ParseQuery(uri.Query);
        var code = string.IsNullOrWhiteSpace(trackingCode)
            ? DefaultTrackingCode
            : trackingCode.Trim();

        query[TrackingCodeQuery] = code;
        query[SubIdQuery] = string.IsNullOrWhiteSpace(subId) ? code : subId.Trim();

        if (!string.IsNullOrWhiteSpace(universalLink))
        {
            query[UniversalLinkQuery] = universalLink.Trim();
        }

        if (!string.IsNullOrWhiteSpace(deepLink))
        {
            query[DeepLinkQuery] = deepLink.Trim();
        }

        if (!string.IsNullOrWhiteSpace(affiliateId))
        {
            query[AffiliateIdQuery] = affiliateId.Trim();
        }

        var encodedQuery = string.Join("&", query.Select(pair => FormatQueryPair(pair.Key, pair.Value)));
        var prefix = uri.GetLeftPart(UriPartial.Path);
        var fragment = uri.Fragment;
        var withQuery = string.IsNullOrEmpty(encodedQuery) ? prefix : $"{prefix}?{encodedQuery}";
        return string.IsNullOrEmpty(fragment) ? withQuery : $"{withQuery}{fragment}";
    }

    public static bool TryGetQueryValue(string url, string key, out string? value)
    {
        value = null;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var query = ParseQuery(uri.Query);
        if (!query.TryGetValue(key, out var found) || string.IsNullOrWhiteSpace(found))
        {
            return false;
        }

        value = found;
        return true;
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(query))
        {
            return result;
        }

        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = pair.IndexOf('=');
            var rawKey = separator < 0 ? pair : pair[..separator];
            var rawValue = separator < 0 ? string.Empty : pair[(separator + 1)..];
            var key = Uri.UnescapeDataString(rawKey);
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            result[key] = Uri.UnescapeDataString(rawValue.Replace("+", " "));
        }

        return result;
    }
}
