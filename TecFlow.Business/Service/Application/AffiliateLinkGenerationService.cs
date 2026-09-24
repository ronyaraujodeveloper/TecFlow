using TecFlow.Business.Dto;
using TecFlow.Business.Integrations.Shopee;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.LinkStrategies;
using TecFlow.Core.Enums;
using Microsoft.Extensions.Logging;

namespace TecFlow.Business.Service.Application;

/// <summary>Orquestra resolução de estratégia, contexto de loja e mapeamento de erros amigáveis.</summary>
public sealed class AffiliateLinkGenerationService : IAffiliateLinkGenerationService
{
    private readonly PlatformLinkResolver _platformLinkResolver;
    private readonly IUrlExpansionService _urlExpansionService;
    private readonly IIntegracaoLojaScopeResolver _storeScopeResolver;
    private readonly IShortLinkService _shortLinkService;
    private readonly ILinkClickTelemetryService _telemetryService;
    private readonly IAffiliateLinkGenerationContext _generationContext;
    private readonly IProductMetadataService _productMetadataService;
    private readonly ILogger<AffiliateLinkGenerationService> _logger;

    public AffiliateLinkGenerationService(
        PlatformLinkResolver platformLinkResolver,
        IUrlExpansionService urlExpansionService,
        IIntegracaoLojaScopeResolver storeScopeResolver,
        IShortLinkService shortLinkService,
        ILinkClickTelemetryService telemetryService,
        IAffiliateLinkGenerationContext generationContext,
        IProductMetadataService productMetadataService,
        ILogger<AffiliateLinkGenerationService> logger)
    {
        _platformLinkResolver = platformLinkResolver;
        _urlExpansionService = urlExpansionService;
        _storeScopeResolver = storeScopeResolver;
        _shortLinkService = shortLinkService;
        _telemetryService = telemetryService;
        _generationContext = generationContext;
        _productMetadataService = productMetadataService;
        _logger = logger;
    }

    public async Task<GerarLinkAfiliadoResponseDto> GenerateAsync(
        GerarLinkAfiliadoDto request,
        int userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.OriginalUrl))
        {
            return Fail("Informe a URL do produto para gerar o link de afiliado.");
        }

        var storeScopes = request.ResolveStoreScopes();
        if (storeScopes.Count == 0)
        {
            return Fail("Selecione ao menos uma conta da plataforma antes de gerar o link.");
        }

        _generationContext.UserId = userId;
        _generationContext.CustomNickname = request.CustomNickname;

        try
        {
            var workingUrl = request.OriginalUrl.Trim();
            var expandedUrl = await _platformLinkResolver.ExpandIfShortenedAsync(workingUrl, cancellationToken);
            var (strategy, resolvedUrl) = await ResolveStrategyAsync(expandedUrl, cancellationToken);
            var productMetadata = await ExtractProductMetadataSafelyAsync(resolvedUrl, cancellationToken);
            var affiliateId = userId.ToString();
            var linkGroupId = await _shortLinkService.ResolveLinkGroupIdAsync(
                userId,
                workingUrl,
                strategy.PlatformType,
                cancellationToken);

            var variants = new List<AffiliateLinkAccountVariantDto>();
            GerarLinkAfiliadoResponseDto? lastSuccess = null;

            foreach (var storeScope in storeScopes)
            {
                _generationContext.OfficialShortenedShopeeUrl = null;

                var generatedLink = await strategy.GenerateDeepLinkAsync(
                    resolvedUrl,
                    storeScope,
                    affiliateId,
                    cancellationToken);

                var store = await _storeScopeResolver.ResolveAsync(
                    storeScope,
                    userId,
                    strategy.PlatformType,
                    cancellationToken);

                var created = await _shortLinkService.EnsureForStoreAsync(
                    generatedLink,
                    workingUrl,
                    strategy.PlatformType,
                    userId,
                    store.TenantId,
                    store.Id,
                    linkGroupId,
                    request.CustomNickname,
                    cancellationToken,
                    productMetadata);

                await _telemetryService.RecordGenerationAsync(
                    created.AffiliateLinkId,
                    store.TenantId,
                    store.ShopId ?? string.Empty,
                    workingUrl,
                    generatedLink,
                    strategy.PlatformType,
                    _generationContext.ClientIpAddress,
                    _generationContext.UserAgent,
                    _generationContext.ReferrerUrl,
                    cancellationToken);

                var officialShort = string.Empty;
                if (strategy.PlatformType == MarketplaceType.Shopee)
                {
                    officialShort = _generationContext.OfficialShortenedShopeeUrl?.Trim() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(officialShort)
                        || !ShopeeOfficialShortUrl.IsOfficialShortener(officialShort))
                    {
                        officialShort = generatedLink;
                    }
                }

                variants.Add(new AffiliateLinkAccountVariantDto
                {
                    AffiliateLinkId = created.AffiliateLinkId,
                    StoreId = store.Id,
                    StoreName = string.IsNullOrWhiteSpace(store.FriendlyName)
                        ? (store.ShopId ?? $"Loja {store.Id}")
                        : store.FriendlyName,
                    AffiliateUrl = generatedLink,
                    ShortenedUrl = created.PublicShortUrl,
                    ShortenedShopeeUrl = officialShort,
                    IsActive = true
                });

                lastSuccess = new GerarLinkAfiliadoResponseDto
                {
                    Success = true,
                    Status = true,
                    Message = "Link de afiliado gerado com sucesso.",
                    Descricao = "Link de afiliado gerado com sucesso.",
                    OriginalUrl = workingUrl,
                    AffiliateUrl = generatedLink,
                    ConvertedUrl = generatedLink,
                    ShortenedUrl = created.PublicShortUrl,
                    ShortenedShopeeUrl = officialShort,
                    PlatformDetected = strategy.PlatformName,
                    ProductName = productMetadata.ProductName,
                    ProductPrice = productMetadata.ProductPrice,
                    ProductImageUrl = productMetadata.ProductImageUrl,
                    AffiliateLinkId = created.AffiliateLinkId,
                    LinkGroupId = linkGroupId,
                    SelectedStoreId = store.Id
                };
            }

            var selectedLojaIds = variants.Select(item => item.StoreId).Distinct().ToList();
            await _shortLinkService.DeactivateUnselectedAccountsAsync(
                linkGroupId,
                selectedLojaIds,
                cancellationToken);

            if (lastSuccess is null)
            {
                return Fail("Não foi possível gerar o link de afiliado no momento. Tente novamente em instantes.");
            }

            lastSuccess.Accounts = variants.Where(item => item.IsActive).ToList();
            lastSuccess.ApplySelectedAccount(lastSuccess.SelectedStoreId ?? lastSuccess.Accounts[0].StoreId);
            return lastSuccess;
        }
        catch (AffiliateLinkGenerationException ex)
        {
            _logger.LogError(
                ex,
                "Falha controlada ao gerar link de afiliado. UserId={UserId} StoreId={StoreId} OriginalUrl={OriginalUrl} ExceptionType={ExceptionType} Causa={Causa}",
                userId,
                request.StoreId,
                request.OriginalUrl,
                ex.GetType().FullName,
                ex.ToString());
            return Fail(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Erro inesperado ao gerar link de afiliado. UserId={UserId} StoreId={StoreId} OriginalUrl={OriginalUrl} ExceptionType={ExceptionType} Causa={Causa}",
                userId,
                request.StoreId,
                request.OriginalUrl,
                ex.GetType().FullName,
                ex.ToString());
            return Fail("Não foi possível gerar o link de afiliado no momento. Tente novamente em instantes.");
        }
    }

    private async Task<(IPlatformLinkStrategy Strategy, string ResolvedUrl)> ResolveStrategyAsync(
        string workingUrl,
        CancellationToken cancellationToken)
    {
        try
        {
            return (_platformLinkResolver.Resolve(workingUrl), workingUrl);
        }
        catch (AffiliateLinkGenerationException)
        {
            _logger.LogInformation(
                "URL não reconhecida; tentando expandir redirecionamentos antes de resolver a plataforma.");

            var expandedUrl = await _urlExpansionService.ExpandUrlAsync(workingUrl, cancellationToken);
            return (_platformLinkResolver.Resolve(expandedUrl), expandedUrl);
        }
    }

    private async Task<ProductMetadataDto> ExtractProductMetadataSafelyAsync(
        string productUrl,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _productMetadataService.ExtractAsync(productUrl, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Extração de metadados falhou; usando slug da URL. Url={Url}",
                productUrl);
            return ProductMetadataHtmlParser.FromUrlFallback(productUrl);
        }
    }

    private static GerarLinkAfiliadoResponseDto Fail(string message) =>
        new()
        {
            Success = false,
            Status = false,
            Message = message,
            Descricao = message,
            OriginalUrl = string.Empty,
            AffiliateUrl = string.Empty,
            ConvertedUrl = string.Empty,
            ShortenedUrl = string.Empty,
            ShortenedShopeeUrl = string.Empty,
            PlatformDetected = string.Empty
        };
}
