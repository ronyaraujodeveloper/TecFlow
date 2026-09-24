using Microsoft.Extensions.Logging;
using TecFlow.Business.Integrations;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Core.Enums;

namespace TecFlow.Business.Service.LinkStrategies;

/// <summary>
/// Resolve dinamicamente a estratégia de link com base no domínio da URL original.
/// Expande encurtadores (Shopee, TikTok, Magalu, Mercado Livre) antes da extração.
/// </summary>
public sealed class PlatformLinkResolver
{
    private readonly IEnumerable<IPlatformLinkStrategy> _strategies;
    private readonly ILogger<PlatformLinkResolver> _logger;
    private readonly IUrlExpansionService? _urlExpansionService;

    public PlatformLinkResolver(
        IEnumerable<IPlatformLinkStrategy> strategies,
        ILogger<PlatformLinkResolver> logger,
        IUrlExpansionService? urlExpansionService = null)
    {
        _strategies = strategies;
        _logger = logger;
        _urlExpansionService = urlExpansionService;
    }

    public async Task<string> ExpandIfShortenedAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return url;
        }

        var workingUrl = AffiliateTrackingIdValidator.EnsureAbsoluteHttpUrl(url.Trim());
        if (_urlExpansionService is null
            || (!ShopeeLinkHostMatcher.IsShortenerUrl(workingUrl)
                && !AffiliateTrackingIdValidator.IsShortenerUrl(workingUrl)))
        {
            return workingUrl;
        }

        _logger.LogInformation(
            "Expandindo URL encurtada {Host} antes de resolver a plataforma.",
            TryGetHost(workingUrl));
        return await _urlExpansionService.ExpandUrlAsync(workingUrl, cancellationToken);
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
            if (strategy.CanProcess(url))
            {
                _logger.LogDebug(
                    "Estratégia {Platform} selecionada para a URL {Host}.",
                    strategy.PlatformName,
                    TryGetHost(url));

                return strategy;
            }
        }

        _logger.LogWarning(
            "Nenhuma estratégia de link encontrada para a URL informada: {Url}",
            url);

        throw new AffiliateLinkGenerationException(
            "Plataforma não suportada para a URL informada. Marketplaces disponíveis: Shopee, TikTok Shop, Mercado Livre, Amazon, Magazine Luiza, Kabum! e Casas Bahia.");
    }

    private static string TryGetHost(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host : url;
}
