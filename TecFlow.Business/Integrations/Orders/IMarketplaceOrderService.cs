using TecFlow.Business.Dto;
using TecFlow.Core.Enums;

namespace TecFlow.Business.Integrations.Orders;

public interface IMarketplaceOrderService
{
    Task<MarketplaceOrderResult> ProcessWebhookOrderAsync(
        string rawJson,
        MarketplaceType type,
        CancellationToken cancellationToken = default);

    Task<MarketplaceOrderResult> SyncMissingOrdersPollingAsync(
        string shopId,
        MarketplaceType type,
        int hoursBack = 24,
        CancellationToken cancellationToken = default);
}
