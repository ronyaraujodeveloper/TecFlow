using System.Text.RegularExpressions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TecFlow.Business.Integrations.Auth;
using TecFlow.Business.Integrations.Shopee;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Core.Enums;
using TecFlow.Database.Entity;

namespace TecFlow.Business.Service.LinkStrategies;

/// <summary>Estratégia Shopee com unshorten, extração ShopId/ItemId e URL de comissão rastreada.</summary>
public sealed class ShopeeLinkStrategy : IPlatformLinkStrategy
{
    private readonly IUrlExpansionService _urlExpansionService;
    private readonly IIntegracaoLojaScopeResolver _storeResolver;
    private readonly IAffiliateLinkGenerationContext _generationContext;
    private readonly ShopeeIntegrationOptions _options;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<ShopeeLinkStrategy> _logger;

    public ShopeeLinkStrategy(
        IUrlExpansionService urlExpansionService,
        IIntegracaoLojaScopeResolver storeResolver,
        IAffiliateLinkGenerationContext generationContext,
        IOptions<ShopeeIntegrationOptions> options,
        IHostEnvironment hostEnvironment,
        ILogger<ShopeeLinkStrategy> logger)
    {
        _urlExpansionService = urlExpansionService;
        _storeResolver = storeResolver;
        _generationContext = generationContext;
        _options = options.Value;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
    }

    private static readonly Regex DesktopItemRegex = new(
        @"i\.(?<shopId>\d+)\.(?<itemId>\d+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

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

        if (TryExtractDesktopProductIds(originalUrl, out var desktopIds))
        {
            _logger.LogInformation(
                "Shopee desktop i.shop.item extraído. ShopId={ShopId} ItemId={ItemId}",
                desktopIds.ShopId,
                desktopIds.ItemId);

            return ConvertCleanProductUrl(
                store,
                desktopIds,
                originalUrl.Trim(),
                usedHomologIds: false);
        }

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

        return ConvertCleanProductUrl(
            store,
            productIds,
            originalUrl.Trim(),
            usedHomologIds);
    }

    public static bool TryExtractDesktopProductIds(string? url, out ShopeeProductUrlIds ids)
    {
        ids = default;
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        var match = DesktopItemRegex.Match(url);
        if (!match.Success)
        {
            return false;
        }

        ids = new ShopeeProductUrlIds(match.Groups["shopId"].Value, match.Groups["itemId"].Value);
        return !string.IsNullOrWhiteSpace(ids.ShopId) && !string.IsNullOrWhiteSpace(ids.ItemId);
    }

    private string ConvertCleanProductUrl(
        IntegracaoLoja store,
        ShopeeProductUrlIds productIds,
        string originalUrl,
        bool usedHomologIds)
    {
        var subId = ShopeeCommissionUrlBuilder.ResolveUniversalSubId(store);
        var affiliateUrl = usedHomologIds
            ? ShopeeCommissionUrlBuilder.BuildHomologConvertedLink(
                ShopeeProductUrlParser.TryExtractShortHash(originalUrl))
            : ShopeeCommissionUrlBuilder.BuildUniversalDeeplink(productIds, subId);

        _logger.LogInformation(
            "Shopee Universal Link gerado sem Open API. ShopId={ShopId} ItemId={ItemId} SubId={SubId} StoreId={StoreId} OriginalUrl={OriginalUrl}",
            productIds.ShopId,
            productIds.ItemId,
            subId,
            store.Id,
            originalUrl);

        AssignOfficialShortUrl(affiliateUrl, originalUrl);
        return affiliateUrl;
    }

    private void AssignOfficialShortUrl(string affiliateUrl, string originalUrl)
    {
        if (ShopeeOfficialShortUrl.IsOfficialShortener(_generationContext.OfficialShortenedShopeeUrl))
        {
            _generationContext.OfficialShortenedShopeeUrl =
                ShopeeOfficialShortUrl.Sanitize(_generationContext.OfficialShortenedShopeeUrl!);
            return;
        }

        if (ShopeeOfficialShortUrl.IsOfficialShortener(originalUrl))
        {
            _generationContext.OfficialShortenedShopeeUrl = ShopeeOfficialShortUrl.Sanitize(originalUrl);
            return;
        }

        _generationContext.OfficialShortenedShopeeUrl = affiliateUrl;
    }
}
