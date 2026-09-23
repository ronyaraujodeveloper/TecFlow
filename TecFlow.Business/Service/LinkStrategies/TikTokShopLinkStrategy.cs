using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Integrations.Auth;
using TecFlow.Business.Integrations.TikTokShop;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Core.Enums;

namespace TecFlow.Business.Service.LinkStrategies;

/// <summary>
/// Estratégia TikTok Shop: reconhece URLs oficiais/encurtadas, extrai productId
/// e injeta TrackingId/FriendlyName em sub_id no link shop.tiktok.com/view/product.
/// </summary>
public sealed class TikTokShopLinkStrategy : IPlatformLinkStrategy
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
    private readonly IAffiliateLinkGenerationContext _generationContext;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<TikTokShopLinkStrategy> _logger;

    public TikTokShopLinkStrategy(
        IUrlExpansionService urlExpansionService,
        IIntegracaoLojaScopeResolver storeResolver,
        IAffiliateLinkGenerationContext generationContext,
        IHostEnvironment hostEnvironment,
        ILogger<TikTokShopLinkStrategy> logger)
    {
        _urlExpansionService = urlExpansionService;
        _storeResolver = storeResolver;
        _generationContext = generationContext;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
    }

    public MarketplaceType PlatformType => MarketplaceType.TikTokShop;

    public string PlatformName => "TikTok Shop";

    public bool CanProcess(string url)
    {
        if (!Uri.TryCreate(TikTokShopProductUrlParser.Sanitize(url), UriKind.Absolute, out var uri))
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

        var workingUrl = TikTokShopProductUrlParser.Sanitize(originalUrl);
        if (ShouldExpand(workingUrl) || !TikTokShopProductUrlParser.TryParse(workingUrl, out _))
        {
            _logger.LogInformation("Expandindo URL encurtada TikTok Shop antes da geração do link de afiliado.");
            workingUrl = TikTokShopProductUrlParser.Sanitize(
                await _urlExpansionService.ExpandUrlAsync(workingUrl, cancellationToken));
        }

        var allowHomologFallback = HomologMarketplaceAuth.ShouldSkipRemoteOAuth(
            _hostEnvironment.EnvironmentName,
            authorizationCode: null);

        if (!CanProcess(workingUrl) && !allowHomologFallback)
        {
            throw new AffiliateLinkGenerationException(TikTokShopProductUrlParser.UnrecognizedLinkMessage);
        }

        var productId = TikTokShopProductUrlParser.ParseOrThrow(workingUrl, allowHomologFallback);
        var subId = TikTokShopCommissionUrlBuilder.ResolveSubId(store);
        var affiliateUrl = TikTokShopCommissionUrlBuilder.BuildProductAffiliateUrl(productId, subId);

        _logger.LogInformation(
            "TikTok Shop affiliate URL gerada. ProductId={ProductId} SubId={SubId} StoreId={StoreId}",
            productId,
            subId,
            store.Id);

        return affiliateUrl;
    }

    private static bool ShouldExpand(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var host = uri.Host.ToLowerInvariant();
        return ExpandableHosts.Any(item =>
            host.Equals(item, StringComparison.OrdinalIgnoreCase)
            || host.EndsWith("." + item, StringComparison.OrdinalIgnoreCase));
    }
}
