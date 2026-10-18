using Ofertio.Core.Enums;
using Ofertio.Core.Entities;

namespace Ofertio.Business.Interfaces.Repositories;

public interface IMarketplaceTokenRepository
{
    Task<MarketplaceToken?> GetByShopAndMarketplaceAsync(string shopId, MarketplaceType marketplaceType);

    Task<MarketplaceToken?> GetByShopAndMarketplaceIgnoreTenantAsync(
        string shopId,
        MarketplaceType marketplaceType);

    Task<IReadOnlyList<MarketplaceToken>> ListConsolidatedForCurrentTenantAsync();

    Task<IReadOnlyList<MarketplaceToken>> ListForShopAsync(string shopId);

    Task UpsertAsync(MarketplaceToken token);
}
