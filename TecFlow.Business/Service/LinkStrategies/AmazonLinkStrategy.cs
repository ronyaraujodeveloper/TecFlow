using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Integrations.Amazon;
using TecFlow.Business.Integrations.Auth;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Core.Enums;

namespace TecFlow.Business.Service.LinkStrategies;

/// <summary>
/// Estratégia Amazon: reconhece URLs /dp/ e encurtadores amzn.to/a.co,
/// extrai ASIN e injeta a Tag de Associado no parâmetro tag.
/// </summary>
public sealed class AmazonLinkStrategy : IPlatformLinkStrategy
{
    private static readonly string[] SupportedHosts =
    [
        "amazon.com.br",
        "amazon.com",
        "amazon.com.mx",
        "amzn.to",
        "a.co"
    ];

    private readonly IUrlExpansionService _urlExpansionService;
    private readonly IIntegracaoLojaScopeResolver _storeResolver;
    private readonly IAffiliateLinkGenerationContext _generationContext;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<AmazonLinkStrategy> _logger;

    public AmazonLinkStrategy(
        IUrlExpansionService urlExpansionService,
        IIntegracaoLojaScopeResolver storeResolver,
        IAffiliateLinkGenerationContext generationContext,
        IHostEnvironment hostEnvironment,
        ILogger<AmazonLinkStrategy> logger)
    {
        _urlExpansionService = urlExpansionService;
        _storeResolver = storeResolver;
        _generationContext = generationContext;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
    }

    public MarketplaceType PlatformType => MarketplaceType.Amazon;

    public string PlatformName => MarketplaceType.Amazon.GetDisplayName();

    public bool CanProcess(string url)
    {
        if (!Uri.TryCreate(AmazonProductUrlParser.Sanitize(url), UriKind.Absolute, out var uri))
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
            MarketplaceType.Amazon,
            cancellationToken);

        var workingUrl = AmazonProductUrlParser.Sanitize(originalUrl);
        if (AmazonProductUrlParser.IsShortUrl(workingUrl) || !AmazonProductUrlParser.TryParse(workingUrl, out _))
        {
            _logger.LogInformation("Expandindo URL encurtada Amazon antes da geração do link de afiliado.");
            workingUrl = AmazonProductUrlParser.Sanitize(
                await _urlExpansionService.ExpandUrlAsync(workingUrl, cancellationToken));
        }

        var allowHomologFallback = HomologMarketplaceAuth.ShouldSkipRemoteOAuth(
            _hostEnvironment.EnvironmentName,
            authorizationCode: null);

        if (!CanProcess(workingUrl) && !AmazonProductUrlParser.TryParse(workingUrl, out _) && !allowHomologFallback)
        {
            throw new AffiliateLinkGenerationException(AmazonProductUrlParser.UnrecognizedLinkMessage);
        }

        var asin = AmazonProductUrlParser.ParseOrThrow(workingUrl, allowHomologFallback);
        var tag = AmazonCommissionUrlBuilder.ResolveAssociateTag(store);
        var affiliateUrl = AmazonCommissionUrlBuilder.BuildProductAffiliateUrl(asin, tag);

        _logger.LogInformation(
            "Amazon affiliate URL gerada. Asin={Asin} Tag={Tag} StoreId={StoreId}",
            asin,
            tag,
            store.Id);

        return affiliateUrl;
    }
}
