using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.LinkStrategies;
using Microsoft.Extensions.Logging;

namespace TecFlow.Business.Service.Application;

/// <summary>Orquestra resolução de estratégia, contexto de loja e mapeamento de erros amigáveis.</summary>
public sealed class AffiliateLinkGenerationService : IAffiliateLinkGenerationService
{
    private readonly PlatformLinkResolver _platformLinkResolver;
    private readonly IUrlExpansionService _urlExpansionService;
    private readonly IIntegracaoLojaScopeResolver _storeScopeResolver;
    private readonly IShortLinkService _shortLinkService;
    private readonly IAffiliateLinkGenerationContext _generationContext;
    private readonly ILogger<AffiliateLinkGenerationService> _logger;

    public AffiliateLinkGenerationService(
        PlatformLinkResolver platformLinkResolver,
        IUrlExpansionService urlExpansionService,
        IIntegracaoLojaScopeResolver storeScopeResolver,
        IShortLinkService shortLinkService,
        IAffiliateLinkGenerationContext generationContext,
        ILogger<AffiliateLinkGenerationService> logger)
    {
        _platformLinkResolver = platformLinkResolver;
        _urlExpansionService = urlExpansionService;
        _storeScopeResolver = storeScopeResolver;
        _shortLinkService = shortLinkService;
        _generationContext = generationContext;
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

        if (request.StoreId == Guid.Empty)
        {
            return Fail("Selecione uma loja ativa no topo do painel antes de gerar o link.");
        }

        _generationContext.UserId = userId;
        _generationContext.CustomNickname = request.CustomNickname;

        try
        {
            var workingUrl = request.OriginalUrl.Trim();
            var (strategy, resolvedUrl) = await ResolveStrategyAsync(workingUrl, cancellationToken);
            var affiliateId = userId.ToString();

            var generatedLink = await strategy.GenerateDeepLinkAsync(
                resolvedUrl,
                request.StoreId,
                affiliateId,
                cancellationToken);

            var store = await _storeScopeResolver.ResolveAsync(
                request.StoreId,
                userId,
                strategy.PlatformType,
                cancellationToken);

            var (publicShortUrl, affiliateLinkId) = await _shortLinkService.CreateShortLinkAsync(
                generatedLink,
                request.OriginalUrl.Trim(),
                strategy.PlatformType,
                userId,
                store.TenantId,
                store.Id,
                request.CustomNickname,
                cancellationToken);

            return new GerarLinkAfiliadoResponseDto
            {
                Success = true,
                Message = "Link de afiliado gerado com sucesso.",
                ShortenedUrl = publicShortUrl,
                PlatformDetected = strategy.PlatformName,
                AffiliateLinkId = affiliateLinkId
            };
        }
        catch (AffiliateLinkGenerationException ex)
        {
            _logger.LogWarning(ex, "Falha controlada ao gerar link de afiliado.");
            return Fail(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao gerar link de afiliado.");
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

    private static GerarLinkAfiliadoResponseDto Fail(string message) =>
        new()
        {
            Success = false,
            Message = message,
            ShortenedUrl = string.Empty,
            PlatformDetected = string.Empty
        };
}
