using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TecFlow.Business.Integrations.Shopee;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Core.Enums;

namespace TecFlow.Business.Service.LinkStrategies;

/// <summary>Estratégia Shopee com unshorten, extração ShopId/ItemId e URL de comissão rastreada.</summary>
public sealed class ShopeeLinkStrategy : IPlatformLinkStrategy
{
    private readonly IUrlExpansionService _urlExpansionService;
    private readonly IIntegracaoLojaScopeResolver _storeResolver;
    private readonly IShopeeAffiliateLinkClient _shopeeAffiliateClient;
    private readonly IAffiliateLinkGenerationContext _generationContext;
    private readonly ShopeeIntegrationOptions _options;
    private readonly ILogger<ShopeeLinkStrategy> _logger;

    public ShopeeLinkStrategy(
        IUrlExpansionService urlExpansionService,
        IIntegracaoLojaScopeResolver storeResolver,
        IShopeeAffiliateLinkClient shopeeAffiliateClient,
        IAffiliateLinkGenerationContext generationContext,
        IOptions<ShopeeIntegrationOptions> options,
        ILogger<ShopeeLinkStrategy> logger)
    {
        _urlExpansionService = urlExpansionService;
        _storeResolver = storeResolver;
        _shopeeAffiliateClient = shopeeAffiliateClient;
        _generationContext = generationContext;
        _options = options.Value;
        _logger = logger;
    }

    public MarketplaceType PlatformType => MarketplaceType.Shopee;

    public string PlatformName => "Shopee";

    public bool CanProcess(string url) => ShopeeLinkHostMatcher.IsShopeeUrl(url);

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
        if (ShopeeLinkHostMatcher.IsShortenerUrl(workingUrl))
        {
            _logger.LogInformation("Expandindo URL encurtada Shopee antes da geração do link de afiliado.");
            workingUrl = await _urlExpansionService.ExpandUrlAsync(workingUrl, cancellationToken);
        }

        if (!CanProcess(workingUrl))
        {
            throw new AffiliateLinkGenerationException(
                "Não foi possível identificar a URL canônica do produto Shopee após expandir o link.");
        }

        var productIds = ShopeeProductUrlParser.ParseOrThrow(workingUrl);
        _logger.LogInformation(
            "Shopee URL expandida extraída. ShopId={ShopId} ItemId={ItemId}",
            productIds.ShopId,
            productIds.ItemId);

        var generated = await _shopeeAffiliateClient.GenerateCustomLinkAsync(
            store,
            workingUrl,
            affiliateId,
            _generationContext.CustomNickname,
            cancellationToken);

        return ApplyCommissionTracking(
            generated,
            store.UserId,
            store.TenantId,
            productIds,
            originalUrl.Trim());
    }

    private string ApplyCommissionTracking(
        string generatedUrl,
        int userId,
        Guid tenantId,
        ShopeeProductUrlIds productIds,
        string originalUrl)
    {
        var trackingCode = string.IsNullOrWhiteSpace(_options.SandboxTrackingCode)
            ? ShopeeCommissionUrlBuilder.DefaultTrackingCode
            : _options.SandboxTrackingCode.Trim();

        var subId = ShopeeCommissionUrlBuilder.BuildSubId(userId, tenantId);
        var universalLink = ShopeeCommissionUrlBuilder.ToUniversalWebUrl(productIds);
        var deepLink = ShopeeLinkHostMatcher.IsNativeDeepLink(originalUrl)
            ? originalUrl.Trim()
            : null;

        _logger.LogInformation(
            "Shopee URL de comissão. TrackingCode={TrackingCode} SubId={SubId} ShopId={ShopId} ItemId={ItemId}",
            trackingCode,
            subId,
            productIds.ShopId,
            productIds.ItemId);

        return ShopeeCommissionUrlBuilder.Merge(
            generatedUrl,
            trackingCode,
            subId,
            universalLink,
            deepLink);
    }
}
