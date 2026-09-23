using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Integrations.Auth;
using TecFlow.Business.Integrations.CasasBahia;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Core.Enums;

namespace TecFlow.Business.Service.LinkStrategies;

/// <summary>
/// Estratégia Casas Bahia: reconhece casasbahia.com.br / cb.com.br / app.link,
/// extrai productId e injeta parceiro (TrackingId) e sub_id (FriendlyName).
/// </summary>
public sealed class CasasBahiaLinkStrategy : IPlatformLinkStrategy
{
    private static readonly string[] SupportedHosts =
    [
        "casasbahia.com.br",
        "cb.com.br",
        "casasbahia.app.link"
    ];

    private readonly IUrlExpansionService _urlExpansionService;
    private readonly IIntegracaoLojaScopeResolver _storeResolver;
    private readonly IAffiliateLinkGenerationContext _generationContext;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<CasasBahiaLinkStrategy> _logger;

    public CasasBahiaLinkStrategy(
        IUrlExpansionService urlExpansionService,
        IIntegracaoLojaScopeResolver storeResolver,
        IAffiliateLinkGenerationContext generationContext,
        IHostEnvironment hostEnvironment,
        ILogger<CasasBahiaLinkStrategy> logger)
    {
        _urlExpansionService = urlExpansionService;
        _storeResolver = storeResolver;
        _generationContext = generationContext;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
    }

    public MarketplaceType PlatformType => MarketplaceType.CasasBahia;

    public string PlatformName => MarketplaceType.CasasBahia.GetDisplayName();

    public bool CanProcess(string url)
    {
        if (!Uri.TryCreate(CasasBahiaProductUrlParser.Sanitize(url), UriKind.Absolute, out var uri))
        {
            return false;
        }

        var host = uri.Host.ToLowerInvariant();
        return SupportedHosts.Any(supported =>
            host.Equals(supported, StringComparison.OrdinalIgnoreCase)
            || host.EndsWith("." + supported, StringComparison.OrdinalIgnoreCase)
            || (supported.Contains('.', StringComparison.Ordinal)
                && host.EndsWith(supported, StringComparison.OrdinalIgnoreCase)));
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
            MarketplaceType.CasasBahia,
            cancellationToken);

        var workingUrl = CasasBahiaProductUrlParser.Sanitize(originalUrl);
        if (CasasBahiaProductUrlParser.IsShortUrl(workingUrl)
            || !CasasBahiaProductUrlParser.TryParse(workingUrl, out _))
        {
            _logger.LogInformation("Expandindo URL encurtada Casas Bahia antes da geração do link de afiliado.");
            workingUrl = CasasBahiaProductUrlParser.Sanitize(
                await _urlExpansionService.ExpandUrlAsync(workingUrl, cancellationToken));
        }

        var allowHomologFallback = HomologMarketplaceAuth.ShouldSkipRemoteOAuth(
            _hostEnvironment.EnvironmentName,
            authorizationCode: null);

        if (!CanProcess(workingUrl)
            && !CasasBahiaProductUrlParser.TryParse(workingUrl, out _)
            && !allowHomologFallback)
        {
            throw new AffiliateLinkGenerationException(CasasBahiaProductUrlParser.UnrecognizedLinkMessage);
        }

        var productId = CasasBahiaProductUrlParser.ParseOrThrow(workingUrl, allowHomologFallback);
        var parceiro = CasasBahiaCommissionUrlBuilder.ResolveParceiro(store);
        var subId = CasasBahiaCommissionUrlBuilder.ResolveSubId(store);
        var affiliateUrl = CasasBahiaCommissionUrlBuilder.BuildProductAffiliateUrl(productId, parceiro, subId);

        _logger.LogInformation(
            "Casas Bahia affiliate URL gerada. ProductId={ProductId} Parceiro={Parceiro} StoreId={StoreId}",
            productId,
            parceiro,
            store.Id);

        return affiliateUrl;
    }
}
