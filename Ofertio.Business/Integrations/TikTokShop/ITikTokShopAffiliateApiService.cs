using Ofertio.Business.Dto;

namespace Ofertio.Business.Integrations.TikTokShop;

/// <summary>
/// API Affiliate Creator do TikTok Shop com o token Master (Painel Master ➔ Integrações Globais): só busca produtos
/// em colaboração aberta; os links de divulgação usam o token de criador de cada cliente.
/// </summary>
public interface ITikTokShopAffiliateApiService
{
    Task<OfficialCatalogChannelResult> SearchProductsAsync(
        TikTokShopProductSearchRequest request,
        CancellationToken cancellationToken = default);
}
