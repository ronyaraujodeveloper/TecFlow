using TecFlow.Core.Entities;
using TecFlow.Core.Enums;

namespace TecFlow.Business.Interfaces.Repositories;

public interface IMarketplaceAccountRepository
{
    Task<MarketplaceAccount?> GetByShopAsync(string shopId, MarketplaceType marketplaceType);

    Task<IReadOnlyList<MarketplaceAccount>> ListForCurrentTenantAsync(bool consolidatedAllShops = true);

    Task<IReadOnlyList<MarketplaceAccount>> ListForShopAsync(string shopId);

    Task<IReadOnlyList<MarketplaceAccount>> ListByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    Task SanitizeHttpTrackingIdsAsync(string userId, CancellationToken cancellationToken = default);

    Task<MarketplaceAccount?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task UpsertAsync(MarketplaceAccount account);
}
