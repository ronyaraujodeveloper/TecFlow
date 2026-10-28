using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ofertio.Business.Integrations.Auth;
using Ofertio.Business.Integrations.TikTokShop;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Core.Enums;

namespace Ofertio.Business.Service.LinkStrategies;

/// <summary>
/// Estratégia TikTok Shop: reconhece URLs oficiais/encurtadas e extrai o productId. Com a conta de criador do cliente
/// conectada (token real na loja), gera o link de promoção oficial pela API Affiliate Creator; sem ela, ou se a API
/// falhar, injeta TrackingId/FriendlyName em sub_id no link shop.tiktok.com/view/product e avisa o cliente.
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
    private readonly ITikTokAffiliateLinkClient? _affiliateLinkClient;

    public TikTokShopLinkStrategy(
        IUrlExpansionService urlExpansionService,
        IIntegracaoLojaScopeResolver storeResolver,
        IAffiliateLinkGenerationContext generationContext,
        IHostEnvironment hostEnvironment,
        ILogger<TikTokShopLinkStrategy> logger,
        ITikTokAffiliateLinkClient? affiliateLinkClient = null)
    {
        _urlExpansionService = urlExpansionService;
        _storeResolver = storeResolver;
        _generationContext = generationContext;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
        _affiliateLinkClient = affiliateLinkClient;
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
        var officialLink = await TryGenerateOfficialLinkAsync(store, productId, affiliateId, cancellationToken);
        if (!string.IsNullOrWhiteSpace(officialLink))
        {
            return officialLink;
        }

        var subId = TikTokShopCommissionUrlBuilder.ResolveSubId(store);
        var affiliateUrl = TikTokShopCommissionUrlBuilder.BuildProductAffiliateUrl(productId, subId);

        _logger.LogInformation(
            "TikTok Shop affiliate URL gerada. ProductId={ProductId} SubId={SubId} StoreId={StoreId}",
            productId,
            subId,
            store.Id);

        return affiliateUrl;
    }

    /// <summary>Null quando a loja não tem conta de criador conectada ou a API não gerou o link (o aviso fica no contexto).</summary>
    private async Task<string?> TryGenerateOfficialLinkAsync(
        Ofertio.Database.Entity.IntegracaoLoja store,
        string productId,
        string affiliateId,
        CancellationToken cancellationToken)
    {
        if (_affiliateLinkClient is null || !TikTokShopCreatorLinkRules.HasCreatorToken(store.AccessToken))
        {
            _generationContext.GenerationWarning = TikTokShopCreatorLinkRules.NoCreatorAccountWarning;
            return null;
        }

        try
        {
            var link = await _affiliateLinkClient.GenerateAffiliateLinkAsync(
                store,
                string.Format(System.Globalization.CultureInfo.InvariantCulture, TikTokShopAffiliateSearchRules.ProductUrlTemplate, productId),
                affiliateId,
                _generationContext.CustomNickname,
                cancellationToken);
            _logger.LogInformation(
                "TikTok Shop link de promoção oficial gerado. ProductId={ProductId} StoreId={StoreId}",
                productId,
                store.Id);
            return link;
        }
        catch (AffiliateLinkGenerationException ex)
        {
            _logger.LogWarning(
                "TikTok Shop não gerou o link oficial ({Reason}); usando sub_id. ProductId={ProductId} StoreId={StoreId}",
                ex.Message,
                productId,
                store.Id);
            _generationContext.GenerationWarning = TikTokShopCreatorLinkRules.OfficialLinkFailedWarning;
            return null;
        }
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
