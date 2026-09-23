using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TecFlow.Business.Integrations.Auth;
using TecFlow.Business.Integrations.Kabum;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Core.Enums;

namespace TecFlow.Business.Service.LinkStrategies;

/// <summary>
/// Estratégia Kabum!: reconhece kabum.com.br / kb.um / kabum.me,
/// extrai productId em /produto/ e injeta sub_id + utm_source=afiliado.
/// </summary>
public sealed class KabumLinkStrategy : IPlatformLinkStrategy
{
    private static readonly string[] SupportedHosts =
    [
        "kabum.com.br",
        "kb.um",
        "kabum.me"
    ];

    private readonly IUrlExpansionService _urlExpansionService;
    private readonly IIntegracaoLojaScopeResolver _storeResolver;
    private readonly IAffiliateLinkGenerationContext _generationContext;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<KabumLinkStrategy> _logger;

    public KabumLinkStrategy(
        IUrlExpansionService urlExpansionService,
        IIntegracaoLojaScopeResolver storeResolver,
        IAffiliateLinkGenerationContext generationContext,
        IHostEnvironment hostEnvironment,
        ILogger<KabumLinkStrategy> logger)
    {
        _urlExpansionService = urlExpansionService;
        _storeResolver = storeResolver;
        _generationContext = generationContext;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
    }

    public MarketplaceType PlatformType => MarketplaceType.Kabum;

    public string PlatformName => MarketplaceType.Kabum.GetDisplayName();

    public bool CanProcess(string url)
    {
        if (!Uri.TryCreate(KabumProductUrlParser.Sanitize(url), UriKind.Absolute, out var uri))
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
            MarketplaceType.Kabum,
            cancellationToken);

        var workingUrl = KabumProductUrlParser.Sanitize(originalUrl);
        if (KabumProductUrlParser.IsShortUrl(workingUrl) || !KabumProductUrlParser.TryParse(workingUrl, out _))
        {
            _logger.LogInformation("Expandindo URL encurtada Kabum! antes da geração do link de afiliado.");
            workingUrl = KabumProductUrlParser.Sanitize(
                await _urlExpansionService.ExpandUrlAsync(workingUrl, cancellationToken));
        }

        var allowHomologFallback = HomologMarketplaceAuth.ShouldSkipRemoteOAuth(
            _hostEnvironment.EnvironmentName,
            authorizationCode: null);

        if (!CanProcess(workingUrl)
            && !KabumProductUrlParser.TryParse(workingUrl, out _)
            && !allowHomologFallback)
        {
            throw new AffiliateLinkGenerationException(KabumProductUrlParser.UnrecognizedLinkMessage);
        }

        var productId = KabumProductUrlParser.ParseOrThrow(workingUrl, allowHomologFallback);
        var subId = KabumCommissionUrlBuilder.ResolveSubId(store);
        var affiliateUrl = KabumCommissionUrlBuilder.BuildProductAffiliateUrl(productId, subId);

        _logger.LogInformation(
            "Kabum! affiliate URL gerada. ProductId={ProductId} SubId={SubId} StoreId={StoreId}",
            productId,
            subId,
            store.Id);

        return affiliateUrl;
    }
}
