using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Integrations.Auth;
using TecFlow.Business.Integrations.MagazineLuiza;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Core.Enums;

namespace TecFlow.Business.Service.LinkStrategies;

/// <summary>
/// Estratégia Magazine Luiza: reconhece Magalu / Magazine Você / magalu.me,
/// extrai productId em /p/ e monta link de afiliado (loja parceira ou ?parceiro=).
/// </summary>
public sealed class MagazineLuizaLinkStrategy : IPlatformLinkStrategy
{
    private static readonly string[] SupportedHosts =
    [
        "magazineluiza.com.br",
        "magazinevoce.com.br",
        "magalu.com.br",
        "magalu.me",
        "mglz.ne"
    ];

    private readonly IUrlExpansionService _urlExpansionService;
    private readonly IIntegracaoLojaScopeResolver _storeResolver;
    private readonly IAffiliateLinkGenerationContext _generationContext;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<MagazineLuizaLinkStrategy> _logger;

    public MagazineLuizaLinkStrategy(
        IUrlExpansionService urlExpansionService,
        IIntegracaoLojaScopeResolver storeResolver,
        IAffiliateLinkGenerationContext generationContext,
        IHostEnvironment hostEnvironment,
        ILogger<MagazineLuizaLinkStrategy> logger)
    {
        _urlExpansionService = urlExpansionService;
        _storeResolver = storeResolver;
        _generationContext = generationContext;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
    }

    public MarketplaceType PlatformType => MarketplaceType.MagazineLuiza;

    public string PlatformName => MarketplaceType.MagazineLuiza.GetDisplayName();

    public bool CanProcess(string url)
    {
        if (!Uri.TryCreate(MagazineLuizaProductUrlParser.Sanitize(url), UriKind.Absolute, out var uri))
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
            MarketplaceType.MagazineLuiza,
            cancellationToken);

        var workingUrl = MagazineLuizaProductUrlParser.Sanitize(originalUrl);
        if (MagazineLuizaProductUrlParser.IsShortUrl(workingUrl)
            || !MagazineLuizaProductUrlParser.TryParse(workingUrl, out _))
        {
            _logger.LogInformation("Expandindo URL encurtada Magalu antes da geração do link de afiliado.");
            workingUrl = MagazineLuizaProductUrlParser.Sanitize(
                await _urlExpansionService.ExpandUrlAsync(workingUrl, cancellationToken));
        }

        var allowHomologFallback = HomologMarketplaceAuth.ShouldSkipRemoteOAuth(
            _hostEnvironment.EnvironmentName,
            authorizationCode: null);

        if (!CanProcess(workingUrl)
            && !MagazineLuizaProductUrlParser.TryParse(workingUrl, out _)
            && !allowHomologFallback)
        {
            throw new AffiliateLinkGenerationException(MagazineLuizaProductUrlParser.UnrecognizedLinkMessage);
        }

        var productId = MagazineLuizaProductUrlParser.ParseOrThrow(workingUrl, allowHomologFallback);
        var partner = MagazineLuizaCommissionUrlBuilder.ResolvePartnerKey(store);
        var affiliateUrl = MagazineLuizaCommissionUrlBuilder.BuildProductAffiliateUrl(productId, partner);

        _logger.LogInformation(
            "Magazine Luiza affiliate URL gerada. ProductId={ProductId} Partner={Partner} StoreId={StoreId}",
            productId,
            partner,
            store.Id);

        return affiliateUrl;
    }
}
