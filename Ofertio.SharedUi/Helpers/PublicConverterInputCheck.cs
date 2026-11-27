using Ofertio.Business.Service.LinkStrategies;
using Ofertio.Business.Service.PublicPages;
using Ofertio.Core.Enums;

namespace Ofertio.SharedUi.Helpers;

/// <summary>
/// Checagem na tela da página pública, antes de converter: avisa na hora quando o link colado é de
/// uma loja que o afiliado não atende. Na dúvida (link encurtado, nenhuma loja cadastrada), deixa o servidor decidir.
/// </summary>
public static class PublicConverterInputCheck
{
    /// <summary>
    /// Devolve a frase de recusa, ou null quando o link pode seguir para conversão.
    /// Com <paramref name="whileTyping"/>, só julga um endereço que já parece inteiro (http(s)://site/...),
    /// para o aviso não piscar enquanto a pessoa ainda digita.
    /// </summary>
    public static string? NotServedMessage(
        string? url,
        IReadOnlyCollection<MarketplaceType> acceptedPlatforms,
        string? displayName,
        bool whileTyping = false)
    {
        if (acceptedPlatforms.Count == 0 || string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        if (whileTyping && !LooksComplete(url))
        {
            return null;
        }

        var normalized = Normalize(url);
        var host = PublicConverterStoreMessages.TryReadHost(normalized);
        if (host is null || UniversalLinkResolverEngine.IsAggregatorUrl(normalized))
        {
            return null;
        }

        var platform = MarketplaceUrlDetector.Detect(normalized)?.IntegrationType;
        if (platform is null && UniversalLinkResolverEngine.TryMapDomainToPlatform(normalized, out var mapped))
        {
            platform = mapped;
        }

        if (platform is { } known)
        {
            return acceptedPlatforms.Any(accepted => accepted.AreSamePlatform(known))
                ? null
                : PublicConverterStoreMessages.NotServed(known, displayName);
        }

        return PublicConverterStoreMessages.NotServed(PublicConverterStoreMessages.SiteReference(host), displayName);
    }

    private static string Normalize(string url)
    {
        var trimmed = url.Trim();
        return trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? trimmed
            : "https://" + trimmed;
    }

    private static bool LooksComplete(string url)
    {
        var trimmed = url.Trim();
        var schemeEnd = trimmed.IndexOf("://", StringComparison.Ordinal);
        if (schemeEnd <= 0)
        {
            return false;
        }

        var scheme = trimmed[..schemeEnd];
        if (!scheme.Equals("http", StringComparison.OrdinalIgnoreCase)
            && !scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return trimmed.IndexOf('/', schemeEnd + 3) > schemeEnd + 3;
    }
}
