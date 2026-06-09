using Microsoft.Extensions.Logging;

namespace TecFlow.Business.Service.LinkStrategies;

/// <summary>
/// Resolve dinamicamente a estratégia de link com base no domínio da URL original.
/// </summary>
public sealed class PlatformLinkResolver
{
    private readonly IEnumerable<IPlatformLinkStrategy> _strategies;
    private readonly ILogger<PlatformLinkResolver> _logger;

    public PlatformLinkResolver(
        IEnumerable<IPlatformLinkStrategy> strategies,
        ILogger<PlatformLinkResolver> logger)
    {
        _strategies = strategies;
        _logger = logger;
    }

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
                    "Estratégia {Platform} selecionada para a URL informada.",
                    strategy.PlatformName);

                return strategy;
            }
        }

        _logger.LogWarning(
            "Nenhuma estratégia de link encontrada para a URL informada: {Url}",
            url);

        throw new AffiliateLinkGenerationException(
            "Plataforma não suportada para a URL informada. Marketplaces disponíveis: Shopee e TikTok Shop.");
    }
}
