using System.Text.RegularExpressions;
using Ofertio.Business.Service.LinkStrategies;

namespace Ofertio.Business.Integrations.MercadoLivre;

/// <summary>Extrai o código MLB de URLs canônicas e encurtadas do Mercado Livre.</summary>
public static class MercadoLivreProductUrlParser
{
    public const string UnrecognizedLinkMessage =
        "Não reconhecemos este link do Mercado Livre. Use a URL do produto (MLB) ou o encurtador mercadolivre.com/sec/.";

    public const string ShowcaseWithoutProductMessage =
        "Este link do Mercado Livre abre uma vitrine/perfil de divulgador, não um produto. Abra o produto e copie o link dele (/p/MLB... ou /MLB-...).";

    /// <summary>Anúncio (<c>/MLB-1234567890-titulo</c>, <c>/MLB1234567890</c>) ou catálogo (<c>/p/MLB20621890</c>).</summary>
    private static readonly Regex MlbRegex = new(
        @"(?<![A-Za-z0-9])MLB-?(?<digits>\d{6,14})(?![0-9])",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex CatalogPathRegex = new(
        @"/p/(?<itemId>MLB[A-Za-z0-9\-]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex PathIdRegex = new(
        @"/(p/)?MLB-?\d{6,14}(?![0-9])",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static bool TryParse(string? url, out string itemId)
    {
        itemId = string.Empty;
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        var sanitized = Sanitize(url);
        var catalog = CatalogPathRegex.Match(sanitized);
        if (catalog.Success)
        {
            itemId = NormalizeItemId(catalog.Groups["itemId"].Value);
            return itemId.Length > 3;
        }

        var mlb = MlbRegex.Match(sanitized);
        if (mlb.Success)
        {
            itemId = "MLB" + mlb.Groups["digits"].Value;
            return true;
        }

        return false;
    }

    /// <summary>Sem código MLB a geração falha: nunca troca por um ID fictício (o link levaria a outro produto).</summary>
    public static string ParseOrThrow(string? url)
    {
        if (TryParse(url, out var itemId))
        {
            return itemId;
        }

        throw new AffiliateLinkGenerationException(UnrecognizedLinkMessage);
    }

    /// <summary>O código MLB está no caminho da URL (catálogo <c>/p/MLB...</c> ou anúncio <c>/MLB-...</c>), não só na query.</summary>
    public static bool HasProductIdInPath(string? url) =>
        Uri.TryCreate(Sanitize(url), UriKind.Absolute, out var uri)
        && PathIdRegex.IsMatch(uri.AbsolutePath);

    public static bool IsSecShortUrl(string? url)
    {
        if (!Uri.TryCreate(Sanitize(url), UriKind.Absolute, out var uri))
        {
            return false;
        }

        var host = uri.Host.ToLowerInvariant();
        if (host is "meli.la" || host.EndsWith(".meli.la", StringComparison.Ordinal))
        {
            return true;
        }

        var isMercadoLivreHost = host.Contains("mercadolivre", StringComparison.Ordinal)
            || host.Contains("mercadolibre", StringComparison.Ordinal)
            || host.Equals("ml.com.br", StringComparison.Ordinal)
            || host.EndsWith(".ml.com.br", StringComparison.Ordinal);
        return isMercadoLivreHost
            && uri.AbsolutePath.Contains("/sec/", StringComparison.OrdinalIgnoreCase);
    }

    public static string Sanitize(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return string.Empty;
        }

        var trimmed = url.Trim();
        if (!trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return "https://" + trimmed;
        }

        return trimmed;
    }

    public static string NormalizeItemId(string raw)
    {
        var match = MlbRegex.Match(raw ?? string.Empty);
        return match.Success ? "MLB" + match.Groups["digits"].Value : (raw ?? string.Empty).Trim();
    }
}
