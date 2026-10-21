using Microsoft.Extensions.Logging;
using Ofertio.Business.Integrations.MercadoLivre;
using Ofertio.Business.Interfaces.Services;
using Ofertio.Core.Enums;

namespace Ofertio.Business.Service.LinkStrategies;

/// <summary>
/// Estratégia Mercado Livre: reconhece URLs MLB e /sec/, injeta matt_tool (TrackingId)
/// e matt_word (apelido) sem descartar query string existente nos encurtadores. A URL do produto
/// (path e código MLB) é preservada; link sem produto (vitrine/perfil) é recusado.
/// </summary>
public sealed class MercadoLivreLinkStrategy : IPlatformLinkStrategy
{
    private static readonly string[] SupportedHosts =
    [
        "mercadolivre.com.br",
        "produto.mercadolivre.com.br",
        "mercadolivre.com",
        "mercadolibre.com",
        "mercadolibre.com.br",
        "ml.com.br",
        "meli.la"
    ];

    private readonly IUrlExpansionService _urlExpansionService;
    private readonly IIntegracaoLojaScopeResolver _storeResolver;
    private readonly IAffiliateLinkGenerationContext _generationContext;
    private readonly ILogger<MercadoLivreLinkStrategy> _logger;

    public MercadoLivreLinkStrategy(
        IUrlExpansionService urlExpansionService,
        IIntegracaoLojaScopeResolver storeResolver,
        IAffiliateLinkGenerationContext generationContext,
        ILogger<MercadoLivreLinkStrategy> logger)
    {
        _urlExpansionService = urlExpansionService;
        _storeResolver = storeResolver;
        _generationContext = generationContext;
        _logger = logger;
    }

    public MarketplaceType PlatformType => MarketplaceType.MercadoLivre;

    public string PlatformName => MarketplaceType.MercadoLivre.GetDisplayName();

    public bool CanProcess(string url)
    {
        if (!Uri.TryCreate(MercadoLivreProductUrlParser.Sanitize(url), UriKind.Absolute, out var uri))
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
            MarketplaceType.MercadoLivre,
            cancellationToken);

        var workingUrl = MercadoLivreProductUrlParser.Sanitize(originalUrl);
        var mattTool = MercadoLivreCommissionUrlBuilder.ResolveMattTool(store);
        var mattWord = MercadoLivreCommissionUrlBuilder.ResolveMattWord(store);

        if (MercadoLivreProductUrlParser.IsSecShortUrl(workingUrl))
        {
            var shortAffiliate = MercadoLivreCommissionUrlBuilder.ApplyMattParams(workingUrl, mattTool, mattWord);
            _logger.LogInformation(
                "Mercado Livre /sec/ preservado com matt_tool/matt_word. StoreId={StoreId}",
                store.Id);
            return shortAffiliate;
        }

        if (!MercadoLivreProductUrlParser.TryParse(workingUrl, out _))
        {
            _logger.LogInformation("Expandindo URL Mercado Livre antes da geração do link de afiliado.");
            workingUrl = MercadoLivreProductUrlParser.Sanitize(
                await _urlExpansionService.ExpandUrlAsync(workingUrl, cancellationToken));
        }

        if (!CanProcess(workingUrl))
        {
            throw new AffiliateLinkGenerationException(MercadoLivreProductUrlParser.UnrecognizedLinkMessage);
        }

        if (MercadoLivreShowcaseRules.IsAffiliateShowcaseUrl(workingUrl))
        {
            if (!MercadoLivreShowcaseRules.TryExtractFocusedProductUrl(workingUrl, out var focusedProductUrl))
            {
                throw new AffiliateLinkGenerationException(MercadoLivreProductUrlParser.ShowcaseWithoutProductMessage);
            }

            workingUrl = focusedProductUrl;
        }

        var itemId = MercadoLivreProductUrlParser.ParseOrThrow(workingUrl);
        var affiliateUrl = MercadoLivreProductUrlParser.HasProductIdInPath(workingUrl)
            ? MercadoLivreCommissionUrlBuilder.BuildProductAffiliateUrl(workingUrl, mattTool, mattWord)
            : MercadoLivreCommissionUrlBuilder.BuildCatalogAffiliateUrl(itemId, mattTool, mattWord);

        _logger.LogInformation(
            "Mercado Livre affiliate URL gerada. ItemId={ItemId} MattTool={MattTool} StoreId={StoreId}",
            itemId,
            mattTool,
            store.Id);

        return affiliateUrl;
    }
}
