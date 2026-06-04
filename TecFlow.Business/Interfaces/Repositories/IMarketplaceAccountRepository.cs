using TecFlow.Core.Entities;
using TecFlow.Core.Enums;

namespace TecFlow.Business.Interfaces.Repositories;

public interface IMarketplaceAccountRepository
{
    Task<MarketplaceAccount?> GetByShopAsync(string shopId, MarketplaceType marketplaceType);

    Task<IReadOnlyList<MarketplaceAccount>> ListForCurrentTenantAsync(bool consolidatedAllShops = true);

    Task<IReadOnlyList<MarketplaceAccount>> ListForShopAsync(string shopId);

    Task UpsertAsync(MarketplaceAccount account);
}
