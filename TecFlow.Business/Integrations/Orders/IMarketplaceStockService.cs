using TecFlow.Core.Enums;

namespace TecFlow.Business.Integrations.Orders;

public interface IMarketplaceStockService
{
    Task<bool> UpdatePlatformStockAsync(
        string shopId,
        string externalSkuId,
        int newQuantity,
        MarketplaceType type,
        CancellationToken cancellationToken = default);

    Task<int?> DeductLocalStockAsync(
        string shopId,
        string externalSkuId,
        int quantity,
        MarketplaceType type,
        CancellationToken cancellationToken = default);
}
