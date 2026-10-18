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

        var target = ResolveMercadoLivreTarget(original, primaryProductUrl);
        if (string.IsNullOrWhiteSpace(target))
        {
            return target;
        }

        var mattTool = string.IsNullOrWhiteSpace(mercadoLivreMattTool)
            ? MercadoLivreIntegrationOptions.OfficialAffiliateTrackingId
            : mercadoLivreMattTool.Trim();
        return MercadoLivreCommissionUrlBuilder.InjectMattTool(
            MercadoLivreShowcaseRules.RemoveThirdPartyTracking(target),
            mattTool);
    }

    /// <summary>
    /// Destino do link de saída: vitrines de divulgadores viram a página direta do produto focado e
    /// links curtos usam a página do produto já resolvida, quando disponível.
    /// </summary>
    public static string ResolveMercadoLivreTarget(string original, string? primaryProductUrl)
    {
        if (MercadoLivreShowcaseRules.TryExtractFocusedProductUrl(original, out var focused))
        {
            return focused;
        }

        var primary = primaryProductUrl?.Trim();
        var needsResolvedProduct = IsMercadoLivreShortLink(original)
            || MercadoLivreShowcaseRules.IsAffiliateShowcaseUrl(original);
        if (!needsResolvedProduct || !IsDirectMercadoLivreProductUrl(primary))
        {
            return original;
        }

        return MercadoLivreShowcaseRules.TryExtractFocusedProductUrl(primary, out var focusedPrimary)
            ? focusedPrimary
            : primary!;
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
        IsMercadoLivreHost(url)
        && !IsMercadoLivreShortLink(url)
        && (!MercadoLivreShowcaseRules.IsAffiliateShowcaseUrl(url)
            || MercadoLivreShowcaseRules.TryExtractFocusedProductUrl(url, out _));

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
