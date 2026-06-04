using TecFlow.Core.Entities;
using TecFlow.Core.Enums;

namespace TecFlow.Business.Interfaces.Repositories;

public interface IMarketplaceOrderRepository
{
    Task<bool> ExistsAsync(string externalOrderId, string shopId, MarketplaceType marketplaceType);

    Task<MarketplaceOrder?> GetByExternalIdAsync(
        string externalOrderId,
        string shopId,
        MarketplaceType marketplaceType);

    Task<MarketplaceOrder> CreateAsync(MarketplaceOrder order);
}
