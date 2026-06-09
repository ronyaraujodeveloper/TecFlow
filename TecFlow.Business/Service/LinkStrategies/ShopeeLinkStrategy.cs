using Microsoft.Extensions.Logging;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Core.Enums;

namespace TecFlow.Business.Service.LinkStrategies;

/// <summary>Estratégia Shopee com expansão de URL e integração generateCustomLink.</summary>
public sealed class ShopeeLinkStrategy : IPlatformLinkStrategy
{
    private static readonly string[] SupportedHosts =
    [
        "shopee.com",
        "shopee.com.br",
        "shope.ee",
        "s.shopee.com.br"
    ];

    private static readonly string[] ExpandableHosts =
    [
        "s.shopee.com.br",
        "shope.ee"
    ];

    private readonly IUrlExpansionService _urlExpansionService;
    private readonly IIntegracaoLojaScopeResolver _storeResolver;
    private readonly IShopeeAffiliateLinkClient _shopeeAffiliateClient;
    private readonly IAffiliateLinkGenerationContext _generationContext;
    private readonly ILogger<ShopeeLinkStrategy> _logger;

    public ShopeeLinkStrategy(
        IUrlExpansionService urlExpansionService,
        IIntegracaoLojaScopeResolver storeResolver,
        IShopeeAffiliateLinkClient shopeeAffiliateClient,
        IAffiliateLinkGenerationContext generationContext,
        ILogger<ShopeeLinkStrategy> logger)
    {
        _urlExpansionService = urlExpansionService;
        _storeResolver = storeResolver;
        _shopeeAffiliateClient = shopeeAffiliateClient;
        _generationContext = generationContext;
        _logger = logger;
    }

    public MarketplaceType PlatformType => MarketplaceType.Shopee;

    public string PlatformName => "Shopee";

    public bool CanProcess(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var host = uri.Host.ToLowerInvariant();
        return SupportedHosts.Any(supported =>
            host.Equals(supported, StringComparison.OrdinalIgnoreCase)
            || host.EndsWith("." + supported, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<string> GenerateDeepLinkAsync(
        string originalUrl,
        Guid storeId,
        string affiliateId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(affiliateId))
        {
            throw new AffiliateLinkGenerationException("Código de afiliado não informado.");
        }

        var store = await _storeResolver.ResolveAsync(
            storeId,
            _generationContext.UserId,
            MarketplaceType.Shopee,
            cancellationToken);

        var workingUrl = originalUrl.Trim();
        if (ShouldExpand(workingUrl))
        {
            _logger.LogInformation("Expandindo URL encurtada Shopee antes da geração do link de afiliado.");
            workingUrl = await _urlExpansionService.ExpandUrlAsync(workingUrl, cancellationToken);
        }

        if (!CanProcess(workingUrl))
        {
            throw new AffiliateLinkGenerationException(
                "Não foi possível identificar a URL canônica do produto Shopee após expandir o link.");
        }

        return await _shopeeAffiliateClient.GenerateCustomLinkAsync(
            store,
            workingUrl,
            affiliateId,
            _generationContext.CustomNickname,
            cancellationToken);
    }

    private static bool ShouldExpand(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var host = uri.Host.ToLowerInvariant();
        return ExpandableHosts.Any(h =>
            host.Equals(h, StringComparison.OrdinalIgnoreCase)
            || host.EndsWith("." + h, StringComparison.OrdinalIgnoreCase));
    }
}
