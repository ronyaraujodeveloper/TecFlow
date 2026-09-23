using TecFlow.Business.Integrations.Shopee;
using TecFlow.Database.Entity;

namespace TecFlow.Business.Integrations.MercadoLivre;

/// <summary>Monta o link de afiliado do Mercado Livre com matt_tool e matt_word.</summary>
public static class MercadoLivreCommissionUrlBuilder
{
    public const string CatalogProductBase = "https://www.mercadolivre.com.br/p/";
    public const string MattToolQuery = "matt_tool";
    public const string MattWordQuery = "matt_word";

    public static string BuildCatalogAffiliateUrl(string itemId, string mattTool, string mattWord)
    {
        var catalogId = MercadoLivreProductUrlParser.NormalizeItemId(itemId);
        if (string.IsNullOrWhiteSpace(catalogId))
        {
            throw new ArgumentException("itemId é obrigatório.", nameof(itemId));
        }

        return ApplyMattParams($"{CatalogProductBase}{catalogId}", mattTool, mattWord);
    }

    public static string ApplyMattParams(string url, string mattTool, string mattWord)
    {
        var sanitized = MercadoLivreProductUrlParser.Sanitize(url);
        if (!Uri.TryCreate(sanitized, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("URL inválida.", nameof(url));
        }

        var pairs = ParseQuery(uri.Query);
        pairs[MattToolQuery] = ResolveMattTool(mattTool);
        pairs[MattWordQuery] = ResolveMattWord(mattWord);

        var builder = new UriBuilder(uri) { Query = BuildQuery(pairs) };
        return builder.Uri.ToString();
    }

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
