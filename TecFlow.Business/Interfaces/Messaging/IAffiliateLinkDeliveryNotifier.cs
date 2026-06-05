using TecFlow.Business.Messaging;

namespace TecFlow.Business.Interfaces.Messaging;

public interface IAffiliateLinkDeliveryNotifier
{
    Task NotifyDeliveryRequestedAsync(
        AffiliateLinkDeliveryRequestedEvent deliveryEvent,
        CancellationToken cancellationToken = default);
}
