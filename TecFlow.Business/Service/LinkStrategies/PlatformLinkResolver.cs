using Microsoft.Extensions.Logging;
using TecFlow.Business.Integrations;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Application;
using TecFlow.Core.Enums;

namespace TecFlow.Business.Service.LinkStrategies;

/// <summary>
/// Resolve dinamicamente a estratégia de link com base no domínio da URL original.
/// Expande encurtadores e agregadores de ofertas (ofertou.ai, promoby.me) antes da extração.
/// </summary>
public sealed class PlatformLinkResolver
{
    private readonly IEnumerable<IPlatformLinkStrategy> _strategies;
    private readonly ILogger<PlatformLinkResolver> _logger;
    private readonly IUrlExpansionService? _urlExpansionService;
    private readonly UniversalLinkResolverEngine _universalResolver;

    public PlatformLinkResolver(
        IEnumerable<IPlatformLinkStrategy> strategies,
        ILogger<PlatformLinkResolver> logger,
        IUrlExpansionService? urlExpansionService = null,
        UniversalLinkResolverEngine? universalResolver = null)
    {
        _strategies = strategies;
        _logger = logger;
        _urlExpansionService = urlExpansionService;
        _universalResolver = universalResolver ?? new UniversalLinkResolverEngine(urlExpansionService);
    }

    public async Task<string> ExpandIfShortenedAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return url;
        }

        var workingUrl = AffiliateTrackingIdValidator.EnsureAbsoluteHttpUrl(url.Trim());
        _logger.LogInformation(
            "Expandindo URL encurtada {Host} antes de resolver a plataforma.",
            TryGetHost(workingUrl));
        var canonical = _urlExpansionService is null
            ? await UrlUnshortenerService.ResolveToFinalSupportedMarketplaceAsync(
                workingUrl,
                static (_, _) => Task.FromResult<string?>(null),
                cancellationToken)
            : await UrlUnshortenerService.ResolveToFinalSupportedMarketplaceAsync(
                workingUrl,
                _urlExpansionService,
                cancellationToken);
        if (UrlUnshortenerService.TryDetectMarketplace(canonical, out var platform))
        {
            _logger.LogInformation(
                "URL canônica isolada após o loop de resolução. Host={Host} Platform={Platform}",
                TryGetHost(canonical),
                platform.GetDisplayName());
        }

        return canonical;
    }

    public async Task<(IPlatformLinkStrategy Strategy, string DestinationUrl)> ResolveFromInputAsync(
        string inputUrl,
        CancellationToken cancellationToken = default)
    {
        var destinationUrl = await ExpandIfShortenedAsync(inputUrl, cancellationToken);
        if (!ShortAffiliateLinkService.IsSupportedDomain(destinationUrl))
        {
            throw new AffiliateLinkGenerationException(ShortAffiliateLinkService.UnrecognizedDestinationMessage);
        }

        return (Resolve(destinationUrl), destinationUrl);
    }

    public static string ExtractShopeeAffiliateId(string url) =>
        ExtractAffiliateId(url, MarketplaceType.Shopee);

    public static string ExtractAffiliateId(string url, MarketplaceType platform) =>
        AffiliateTrackingIdValidator.ExtractAffiliateIdFromUrl(url, platform.ToString());

    public static bool TryDetectPlatformFromUrl(string? url, out MarketplaceType platform) =>
        AffiliateTrackingIdValidator.TryDetectPlatformFromUrl(url, out platform);

    public IPlatformLinkStrategy Resolve(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new AffiliateLinkGenerationException("URL original não informada.");
        }

        foreach (var strategy in _strategies)
        {
            if (strategy.CanProcess(url)
                || (UniversalLinkResolverEngine.TryMapDomainToPlatform(url, out var mapped)
                    && strategy.PlatformType.AreSamePlatform(mapped)))
            {
                _logger.LogDebug(
                    "Estratégia {Platform} selecionada para a URL {Host}.",
                    strategy.PlatformName,
                    TryGetHost(url));

                return strategy;
            }
        }

        _logger.LogWarning(
            "Nenhuma estratégia de link encontrada para a URL de destino: {Url}",
            url);

        throw new AffiliateLinkGenerationException(ShortAffiliateLinkService.UnrecognizedDestinationMessage);
    }

    private static string TryGetHost(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host : url;
}
