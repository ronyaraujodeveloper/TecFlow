using Microsoft.Extensions.Logging;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Core.Enums;

namespace TecFlow.Business.Service.LinkStrategies;

/// <summary>Estratégia TikTok Shop com expansão de URLs sociais e integração de afiliados.</summary>
public sealed class TikTokLinkStrategy : IPlatformLinkStrategy
{
    private static readonly string[] SupportedHosts =
    [
        "tiktok.com",
        "tiktokshop.com",
        "shop.tiktok.com",
        "vm.tiktok.com",
        "vt.tiktok.com",
        "l.tiktok.com"
    ];

    private static readonly string[] ExpandableHosts =
    [
        "vm.tiktok.com",
        "vt.tiktok.com",
        "bit.ly",
        "t.co",
        "l.tiktok.com"
    ];

    private readonly IUrlExpansionService _urlExpansionService;
    private readonly IIntegracaoLojaScopeResolver _storeResolver;
    private readonly ITikTokAffiliateLinkClient _tikTokAffiliateClient;
    private readonly IAffiliateLinkGenerationContext _generationContext;
    private readonly ILogger<TikTokLinkStrategy> _logger;

    public TikTokLinkStrategy(
        IUrlExpansionService urlExpansionService,
        IIntegracaoLojaScopeResolver storeResolver,
        ITikTokAffiliateLinkClient tikTokAffiliateClient,
        IAffiliateLinkGenerationContext generationContext,
        ILogger<TikTokLinkStrategy> logger)
    {
        _urlExpansionService = urlExpansionService;
        _storeResolver = storeResolver;
        _tikTokAffiliateClient = tikTokAffiliateClient;
        _generationContext = generationContext;
        _logger = logger;
    }

    public MarketplaceType PlatformType => MarketplaceType.TikTokShop;

    public string PlatformName => "TikTok Shop";

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
            MarketplaceType.TikTokShop,
            cancellationToken);

        var workingUrl = originalUrl.Trim();
        if (ShouldExpand(workingUrl) || !CanProcess(workingUrl))
        {
            _logger.LogInformation("Expandindo URL encurtada antes da geração do link TikTok Shop.");
            workingUrl = await _urlExpansionService.ExpandUrlAsync(workingUrl, cancellationToken);
        }

        if (!CanProcess(workingUrl))
        {
            throw new AffiliateLinkGenerationException(
                "Não foi possível identificar a URL canônica do produto TikTok Shop após expandir o link.");
        }

        return await _tikTokAffiliateClient.GenerateAffiliateLinkAsync(
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
