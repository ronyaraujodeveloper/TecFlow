using TecFlow.Business.Integrations.MercadoLivre;
using TecFlow.Core.Enums;

namespace TecFlow.Business.Service.LinkStrategies;

public static class AffiliateUrlBuilderRules
{
    public static string Build(
        string? originalUrl,
        string? primaryProductUrl,
        MarketplaceType? platform,
        string? mercadoLivreMattTool)
    {
        var original = originalUrl?.Trim() ?? string.Empty;
        if (!IsMercadoLivre(original, platform))
        {
            return original;
        }

        var target = IsMercadoLivreShortLink(original) && IsDirectMercadoLivreProductUrl(primaryProductUrl)
            ? primaryProductUrl!.Trim()
            : original;
        if (string.IsNullOrWhiteSpace(target) || string.IsNullOrWhiteSpace(mercadoLivreMattTool))
        {
            return target;
        }

        return MercadoLivreCommissionUrlBuilder.InjectMattTool(target, mercadoLivreMattTool);
    }

    public static bool IsMercadoLivre(string? url, MarketplaceType? platform) =>
        platform == MarketplaceType.MercadoLivre || IsMercadoLivreHost(url);

    public static bool IsMercadoLivreShortLink(string? url)
    {
        if (!TryGetUri(url, out var uri))
        {
            return false;
        }

        return uri.Host.Equals("meli.la", StringComparison.OrdinalIgnoreCase)
            || uri.Host.EndsWith(".meli.la", StringComparison.OrdinalIgnoreCase)
            || uri.AbsolutePath.StartsWith("/sec/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDirectMercadoLivreProductUrl(string? url) =>
        IsMercadoLivreHost(url) && !IsMercadoLivreShortLink(url);

    private static bool IsMercadoLivreHost(string? url)
    {
        if (!TryGetUri(url, out var uri))
        {
            return false;
        }

        var host = uri.Host;
        return host.Equals("meli.la", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".meli.la", StringComparison.OrdinalIgnoreCase)
            || host.Contains("mercadolivre.", StringComparison.OrdinalIgnoreCase)
            || host.Contains("mercadolibre.", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryGetUri(string? url, out Uri uri)
    {
        uri = null!;
        return !string.IsNullOrWhiteSpace(url)
            && Uri.TryCreate(url.Trim(), UriKind.Absolute, out uri!)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
