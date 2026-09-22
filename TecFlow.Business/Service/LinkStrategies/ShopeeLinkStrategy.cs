using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TecFlow.Business.Integrations.Auth;
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
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<ShopeeLinkStrategy> _logger;

    public ShopeeLinkStrategy(
        IUrlExpansionService urlExpansionService,
        IIntegracaoLojaScopeResolver storeResolver,
        IShopeeAffiliateLinkClient shopeeAffiliateClient,
        IAffiliateLinkGenerationContext generationContext,
        IOptions<ShopeeIntegrationOptions> options,
        IHostEnvironment hostEnvironment,
        ILogger<ShopeeLinkStrategy> logger)
    {
        _urlExpansionService = urlExpansionService;
        _storeResolver = storeResolver;
        _shopeeAffiliateClient = shopeeAffiliateClient;
        _generationContext = generationContext;
        _options = options.Value;
        _hostEnvironment = hostEnvironment;
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

        var workingUrl = ShopeeProductUrlParser.Sanitize(originalUrl);
        if (string.IsNullOrWhiteSpace(workingUrl))
        {
            workingUrl = originalUrl.Trim();
        }

        if (ShopeeLinkHostMatcher.IsShortenerUrl(workingUrl)
            || !ShopeeProductUrlParser.TryParse(workingUrl, out _))
        {
            _logger.LogInformation("Expandindo URL encurtada Shopee antes da geração do link de afiliado.");
            workingUrl = ShopeeProductUrlParser.Sanitize(
                await _urlExpansionService.ExpandUrlAsync(workingUrl, cancellationToken));
        }

        var allowHomologFallback = HomologMarketplaceAuth.ShouldSkipRemoteOAuth(
            _hostEnvironment.EnvironmentName,
            authorizationCode: null)
            || _options.IsSandboxMode;

        if (!CanProcess(workingUrl) && !allowHomologFallback)
        {
            throw new AffiliateLinkGenerationException(ShopeeProductUrlParser.UnrecognizedLinkMessage);
        }

        ShopeeProductUrlIds productIds;
        try
        {
            productIds = ShopeeProductUrlParser.ParseOrThrow(
                workingUrl,
                originalUrl.Trim(),
                allowHomologFallback);
        }
        catch (AffiliateLinkGenerationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao analisar URL Shopee: {Url}", originalUrl);
            throw new AffiliateLinkGenerationException(
                ShopeeProductUrlParser.UnrecognizedLinkMessage,
                ex);
        }

        var usedHomologIds = productIds.ShopId == ShopeeProductUrlParser.HomologShopId
            && productIds.ItemId == ShopeeProductUrlParser.HomologItemId
            && !ShopeeProductUrlParser.TryParse(workingUrl, out _);

        if (usedHomologIds)
        {
            workingUrl = ShopeeCommissionUrlBuilder.ToUniversalWebUrl(productIds);
            _logger.LogInformation(
                "Shopee homologação: IDs simulados a partir do hash {ShortHash}. ShopId={ShopId} ItemId={ItemId}",
                ShopeeProductUrlParser.TryExtractShortHash(originalUrl),
                productIds.ShopId,
                productIds.ItemId);
        }
        else
        {
            workingUrl = ShopeeProductUrlParser.Sanitize(workingUrl);
            if (string.IsNullOrWhiteSpace(workingUrl) || !ShopeeProductUrlParser.TryParse(workingUrl, out _))
            {
                workingUrl = ShopeeCommissionUrlBuilder.ToUniversalWebUrl(productIds);
            }

            _logger.LogInformation(
                "Shopee URL expandida extraída. ShopId={ShopId} ItemId={ItemId} CleanUrl={CleanUrl}",
                productIds.ShopId,
                productIds.ItemId,
                workingUrl);
        }

        string generated;
        try
        {
            generated = await _shopeeAffiliateClient.GenerateCustomLinkAsync(
                store,
                workingUrl,
                affiliateId,
                _generationContext.CustomNickname,
                cancellationToken);
        }
        catch (Exception ex) when (allowHomologFallback)
        {
            _logger.LogWarning(ex, "Falha na API Shopee. Montando URL de afiliado com ShopId/ItemId extraídos.");
            if (usedHomologIds)
            {
                return ShopeeCommissionUrlBuilder.BuildHomologConvertedLink(
                    ShopeeProductUrlParser.TryExtractShortHash(originalUrl));
            }

            return BuildAffiliateUrlFromProductIds(store, productIds, originalUrl.Trim(), affiliateId);
        }

        if (string.IsNullOrWhiteSpace(generated) || usedHomologIds)
        {
            if (usedHomologIds)
            {
                return ShopeeCommissionUrlBuilder.BuildHomologConvertedLink(
                    ShopeeProductUrlParser.TryExtractShortHash(originalUrl));
            }

            return BuildAffiliateUrlFromProductIds(store, productIds, originalUrl.Trim(), affiliateId);
        }

        return ApplyCommissionTracking(
            generated,
            store,
            productIds,
            originalUrl.Trim(),
            affiliateId);
    }

    private string BuildAffiliateUrlFromProductIds(
        TecFlow.Database.Entity.IntegracaoLoja store,
        ShopeeProductUrlIds productIds,
        string originalUrl,
        string affiliateId)
    {
        var canonical = ShopeeCommissionUrlBuilder.ToUniversalWebUrl(productIds);
        return ApplyCommissionTracking(canonical, store, productIds, originalUrl, affiliateId);
    }

    private string ApplyCommissionTracking(
        string generatedUrl,
        TecFlow.Database.Entity.IntegracaoLoja store,
        ShopeeProductUrlIds productIds,
        string originalUrl,
        string affiliateId)
    {
        var trackingCode = string.IsNullOrWhiteSpace(_options.SandboxTrackingCode)
            ? ShopeeCommissionUrlBuilder.DefaultTrackingCode
            : _options.SandboxTrackingCode.Trim();

        var subId = ShopeeCommissionUrlBuilder.BuildSubId(store.UserId, store.TenantId);
        var universalLink = ShopeeCommissionUrlBuilder.ToUniversalWebUrl(productIds);
        var deepLink = ShopeeLinkHostMatcher.IsNativeDeepLink(originalUrl)
            ? originalUrl.Trim()
            : null;
        var resolvedAffiliateId = !string.IsNullOrWhiteSpace(store.ShopId)
            ? store.ShopId
            : affiliateId;

        _logger.LogInformation(
            "Shopee URL de comissão. TrackingCode={TrackingCode} SubId={SubId} ShopId={ShopId} ItemId={ItemId} AffiliateId={AffiliateId}",
            trackingCode,
            subId,
            productIds.ShopId,
            productIds.ItemId,
            resolvedAffiliateId);

        return ShopeeCommissionUrlBuilder.Merge(
            generatedUrl,
            trackingCode,
            subId,
            universalLink,
            deepLink,
            resolvedAffiliateId);
    }
}
