using System.Text.RegularExpressions;

namespace Ofertio.Business.Integrations.MercadoLivre;

/// <summary>
/// Vitrines de divulgadores (Meli Social / affiliate-profile) e rastreio de afiliados de terceiros no Mercado Livre:
/// extrai o produto focado da vitrine e remove os parâmetros matt_* alheios antes de aplicar o nosso matt_tool.
/// </summary>
public static class MercadoLivreShowcaseRules
{
    public const string DirectProductBase = MercadoLivreCommissionUrlBuilder.DirectProductBase;

    public static readonly string[] ThirdPartyTrackingParameters =
    [
        "matt_tool_id",
        "matt_tool",
        "matt_d2id",
        "matt_tracing_id",
        "matt_word"
    ];

    private static readonly string[] CatalogIdKeys = ["product_id", "productId", "catalog_product_id"];
    private static readonly string[] ItemIdKeys = ["item_id", "itemId", "wid"];

    private static readonly Regex CatalogPathRegex = new(
        @"/p/(?<id>MLB\d{6,14})",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex ItemPathRegex = new(
        @"/MLB-?(?<digits>\d{8,14})",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex IdValueRegex = new(
        @"^MLB-?(?<digits>\d{6,14})",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static bool IsAffiliateShowcaseUrl(string? url)
    {
        if (!TryGetMercadoLivreUri(url, out var uri))
        {
            return false;
        }

        return uri.AbsolutePath.Contains("/social/", StringComparison.OrdinalIgnoreCase)
            || uri.AbsolutePath.EndsWith("/social", StringComparison.OrdinalIgnoreCase)
            || url!.Contains("affiliate-profile", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Página direta do produto focado: <c>https://produto.mercadolivre.com.br/MLB-{id}</c>.
    /// </summary>
    public static bool TryExtractFocusedProductUrl(string? url, out string productUrl)
    {
        productUrl = string.Empty;
        if (!IsAffiliateShowcaseUrl(url) || !TryGetMercadoLivreUri(url, out var uri))
        {
            return false;
        }

        var parameters = ParseParameters(uri.Query, uri.Fragment);
        if (TryReadId(parameters, CatalogIdKeys, out var catalogDigits))
        {
            productUrl = MercadoLivreCommissionUrlBuilder.CatalogProductBase + catalogDigits;
            return true;
        }

        var catalog = CatalogPathRegex.Match(uri.AbsolutePath);
        if (catalog.Success)
        {
            productUrl = MercadoLivreCommissionUrlBuilder.CatalogProductBase + DigitsOf(catalog.Groups["id"].Value);
            return true;
        }

        if (TryReadId(parameters, ItemIdKeys, out var itemDigits))
        {
            productUrl = DirectProductBase + itemDigits;
            return true;
        }

        if (ItemPathRegex.IsMatch(uri.AbsolutePath))
        {
            productUrl = $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}";
            return true;
        }

        return false;
    }

    /// <summary>
    /// URL final de um redirecionamento (meli.la, vitrine ou página do produto) convertida na página direta
    /// e limpa do produto; falso quando não é um produto do Mercado Livre.
    /// </summary>
    public static bool TryResolveDirectProductUrl(string? url, out string productUrl)
    {
        if (TryExtractFocusedProductUrl(url, out productUrl))
        {
            return true;
        }

        productUrl = string.Empty;
        if (IsAffiliateShowcaseUrl(url)
            || !TryGetMercadoLivreUri(url, out var uri)
            || MercadoLivreProductUrlParser.IsSecShortUrl(url))
        {
            return false;
        }

        if (!CatalogPathRegex.IsMatch(uri.AbsolutePath) && !ItemPathRegex.IsMatch(uri.AbsolutePath))
        {
            return false;
        }

        productUrl = MercadoLivreCommissionUrlBuilder.RemoveCatalogDirectory($"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}");
        return true;
    }

    private static string DigitsOf(string itemId)
    {
        var normalized = itemId.Trim();
        return normalized.StartsWith("MLB", StringComparison.OrdinalIgnoreCase)
            ? normalized[3..].TrimStart('-')
            : normalized;
    }

    public static string RemoveThirdPartyTracking(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || string.IsNullOrEmpty(uri.Query))
        {
            return url;
        }

        var kept = uri.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Where(part => !IsThirdPartyTrackingKey(KeyOf(part)))
            .ToList();
        var builder = new UriBuilder(uri) { Query = string.Join("&", kept) };
        return builder.Uri.ToString();
    }

    private static bool IsThirdPartyTrackingKey(string key) =>
        ThirdPartyTrackingParameters.Any(parameter => parameter.Equals(key, StringComparison.OrdinalIgnoreCase));

    private static string KeyOf(string part)
    {
        var separator = part.IndexOf('=');
        var raw = separator < 0 ? part : part[..separator];
        return Uri.UnescapeDataString(raw);
    }

    private static Dictionary<string, string> ParseParameters(params string[] sources)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var source in sources)
        {
            foreach (var part in source.TrimStart('?', '#').Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var separator = part.IndexOf('=');
                if (separator <= 0)
                {
                    continue;
                }

                result.TryAdd(Uri.UnescapeDataString(part[..separator]), Uri.UnescapeDataString(part[(separator + 1)..]));
            }
        }

        return result;
    }

    private static bool TryReadId(IReadOnlyDictionary<string, string> parameters, IEnumerable<string> keys, out string digits)
    {
        digits = string.Empty;
        foreach (var key in keys)
        {
            if (parameters.TryGetValue(key, out var value))
            {
                var match = IdValueRegex.Match(value.Trim());
                if (match.Success)
                {
                    digits = match.Groups["digits"].Value;
                    return true;
                }
            }
        }

        return false;
    }

    internal static bool TryGetMercadoLivreUri(string? url, out Uri uri)
    {
        uri = null!;
        if (string.IsNullOrWhiteSpace(url)
            || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out uri!)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return false;
        }

        var host = uri.Host;
        return host.Contains("mercadolivre.", StringComparison.OrdinalIgnoreCase)
            || host.Contains("mercadolibre.", StringComparison.OrdinalIgnoreCase);
    }
}
