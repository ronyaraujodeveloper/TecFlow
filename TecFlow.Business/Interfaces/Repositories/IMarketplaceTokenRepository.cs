using TecFlow.Core.Enums;
using TecFlow.Core.Entities;

namespace TecFlow.Business.Interfaces.Repositories;

public interface IMarketplaceTokenRepository
{
    Task<MarketplaceToken?> GetByShopAndMarketplaceAsync(string shopId, MarketplaceType marketplaceType);
    Task UpsertAsync(MarketplaceToken token);
}
