using System.Text.RegularExpressions;
using Ofertio.Business.Integrations.Shopee;
using Ofertio.Database.Entity;

namespace Ofertio.Business.Integrations.MercadoLivre;

/// <summary>Monta o link de afiliado do Mercado Livre com matt_tool e matt_word.</summary>
public static class MercadoLivreCommissionUrlBuilder
{
    public const string ProductHost = "produto.mercadolivre.com.br";
    public const string DirectProductBase = "https://produto.mercadolivre.com.br/MLB-";
    public const string MattToolQuery = "matt_tool";
    public const string MattWordQuery = "matt_word";

    private static readonly Regex CatalogDirectoryRegex = new(
        @"/p/MLB-?(?<digits>\d{1,14})(?!\d)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex BareProductPathRegex = new(
        @"^/MLB-(?<digits>\d{1,14})$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static string BuildCatalogAffiliateUrl(string itemId, string mattTool, string mattWord)
    {
        var directUrl = BuildDirectProductUrl(itemId);
        if (string.IsNullOrWhiteSpace(directUrl))
        {
            throw new ArgumentException("itemId é obrigatório.", nameof(itemId));
        }

        return ApplyMattParams(directUrl, mattTool, mattWord);
    }

    /// <summary>
    /// Página do produto: <c>https://produto.mercadolivre.com.br/MLB-{id}</c>.
    /// </summary>
    public static string BuildDirectProductUrl(string itemId)
    {
        var normalized = MercadoLivreProductUrlParser.NormalizeItemId(itemId);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return string.Empty;
        }

        var digits = normalized.StartsWith("MLB", StringComparison.OrdinalIgnoreCase)
            ? normalized[3..].TrimStart('-')
            : normalized;
        return string.IsNullOrWhiteSpace(digits) ? string.Empty : DirectProductBase + digits;
    }

    /// <summary>Tira <c>/p/</c> do caminho. <c>/p/MLB123</c> e <c>/slug/p/MLB123</c> viram <c>/MLB-123</c>.</summary>
    public static string RemoveCatalogDirectory(string url)
    {
        var sanitized = MercadoLivreProductUrlParser.Sanitize(url);
        if (!Uri.TryCreate(sanitized, UriKind.Absolute, out var uri))
        {
            return sanitized;
        }

        var match = CatalogDirectoryRegex.Match(uri.AbsolutePath);
        if (!match.Success)
        {
            return sanitized;
        }

        var builder = new UriBuilder(uri)
        {
            Host = UseProductHost(uri.Host) ? ProductHost : uri.Host,
            Path = "/MLB-" + match.Groups["digits"].Value,
            Fragment = string.Empty
        };
        return builder.Uri.GetComponents(UriComponents.HttpRequestUrl, UriFormat.UriEscaped);
    }

    /// <summary>
    /// Link de afiliado do produto, sem o diretório <c>/p/</c>. Os <c>matt_*</c> de terceiros saem antes
    /// de aplicar o <c>matt_tool</c> e o <c>matt_word</c> desta conta.
    /// </summary>
    public static string BuildProductAffiliateUrl(string productUrl, string mattTool, string mattWord)
    {
        var cleaned = RemoveCatalogDirectory(
            MercadoLivreShowcaseRules.RemoveThirdPartyTracking(MercadoLivreProductUrlParser.Sanitize(productUrl)));
        if (!Uri.TryCreate(cleaned, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("URL inválida.", nameof(productUrl));
        }

        var withoutFragment = new UriBuilder(uri) { Fragment = string.Empty }.Uri.ToString();
        return ApplyMattParams(withoutFragment, mattTool, mattWord);
    }

    public static string InjectMattTool(string? productUrl, string? trackingId)
    {
        if (string.IsNullOrWhiteSpace(productUrl))
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(trackingId))
        {
            return productUrl.Trim();
        }

        try
        {
            return ApplyMattParams(productUrl, trackingId, trackingId);
        }
        catch (Exception)
        {
            return productUrl.Trim();
        }
    }

    public static string ApplyMattParams(string url, string mattTool, string mattWord)
    {
        var sanitized = RemoveCatalogDirectory(url);
        if (!Uri.TryCreate(sanitized, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("URL inválida.", nameof(url));
        }

        var pairs = ParseQuery(uri.Query);
        pairs[MattToolQuery] = ResolveMattTool(mattTool);
        pairs[MattWordQuery] = ResolveMattWord(mattWord);

        var builder = new UriBuilder(uri) { Query = BuildQuery(pairs) };
        if (UseProductHost(uri.Host) && BareProductPathRegex.IsMatch(uri.AbsolutePath))
        {
            builder.Host = ProductHost;
        }

        return builder.Uri.ToString();
    }

    private static bool UseProductHost(string host) =>
        host.Equals("www.mercadolivre.com.br", StringComparison.OrdinalIgnoreCase)
        || host.Equals("mercadolivre.com.br", StringComparison.OrdinalIgnoreCase)
        || host.Equals(ProductHost, StringComparison.OrdinalIgnoreCase);

    public static string ResolveMattTool(IntegracaoLoja store)
    {
        if (!string.IsNullOrWhiteSpace(store.AffiliateTrackingId))
        {
            return store.AffiliateTrackingId.Trim();
        }

        return ShopeeCommissionUrlBuilder.ResolveUniversalSubId(store);
    }

    public static string ResolveMattWord(IntegracaoLoja store) =>
        string.IsNullOrWhiteSpace(store.FriendlyName)
            ? ShopeeCommissionUrlBuilder.ResolveUniversalSubId(store)
            : store.FriendlyName.Trim();

    private static string ResolveMattTool(string mattTool) =>
        string.IsNullOrWhiteSpace(mattTool) ? ShopeeCommissionUrlBuilder.HomologTrackingSubId : mattTool.Trim();

    private static string ResolveMattWord(string mattWord) =>
        string.IsNullOrWhiteSpace(mattWord) ? ShopeeCommissionUrlBuilder.HomologTrackingSubId : mattWord.Trim();

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(query))
        {
            return result;
        }

        var trimmed = query.TrimStart('?');
        foreach (var part in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = part.IndexOf('=');
            if (separator < 0)
            {
                result[Uri.UnescapeDataString(part)] = string.Empty;
                continue;
            }

            var key = Uri.UnescapeDataString(part[..separator]);
            var value = Uri.UnescapeDataString(part[(separator + 1)..]);
            result[key] = value;
        }

        return result;
    }

    private static string BuildQuery(IReadOnlyDictionary<string, string> pairs) =>
        string.Join(
            "&",
            pairs.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value ?? string.Empty)}"));
}
