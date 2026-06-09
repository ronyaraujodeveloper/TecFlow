using TecFlow.Business.Dto;
using TecFlow.Core.Enums;
using TecFlow.Database.Entity;

namespace TecFlow.Business.Interfaces.Services;

/// <summary>Contexto da requisição de geração de link (usuário autenticado e apelido opcional).</summary>
public interface IAffiliateLinkGenerationContext
{
    int UserId { get; set; }

    string? CustomNickname { get; set; }
}

/// <summary>Expansão resiliente de URLs encurtadas (301/302) até a URL canônica.</summary>
public interface IUrlExpansionService
{
    Task<string> ExpandUrlAsync(string shortenedUrl, CancellationToken cancellationToken = default);
}

/// <summary>Resolve IntegracaoLoja a partir do escopo global e do usuário autenticado.</summary>
public interface IIntegracaoLojaScopeResolver
{
    Task<IntegracaoLoja> ResolveAsync(
        Guid storeScopeId,
        int userId,
        MarketplaceType expectedPlatform,
        CancellationToken cancellationToken = default);
}

/// <summary>Cliente da API de afiliados Shopee (generateCustomLink).</summary>
public interface IShopeeAffiliateLinkClient
{
    Task<string> GenerateCustomLinkAsync(
        IntegracaoLoja store,
        string expandedProductUrl,
        string affiliateId,
        string? customNickname,
        CancellationToken cancellationToken = default);
}

/// <summary>Cliente do programa de afiliados TikTok Shop.</summary>
public interface ITikTokAffiliateLinkClient
{
    Task<string> GenerateAffiliateLinkAsync(
        IntegracaoLoja store,
        string expandedProductUrl,
        string affiliateId,
        string? customNickname,
        CancellationToken cancellationToken = default);
}

/// <summary>Orquestra a geração omnichannel de links de afiliado.</summary>
public interface IAffiliateLinkGenerationService
{
    Task<GerarLinkAfiliadoResponseDto> GenerateAsync(
        GerarLinkAfiliadoDto request,
        int userId,
        CancellationToken cancellationToken = default);
}
