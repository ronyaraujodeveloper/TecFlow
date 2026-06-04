using TecFlow.Core.Enums;
using TecFlow.Core.Entities;

namespace TecFlow.Business.Interfaces.Repositories;

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
