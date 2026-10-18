using Ofertio.Business.Messaging;

namespace Ofertio.Business.Interfaces.Messaging;

public interface IAffiliateLinkDeliveryNotifier
{
    Task NotifyDeliveryRequestedAsync(
        AffiliateLinkDeliveryRequestedEvent deliveryEvent,
        CancellationToken cancellationToken = default);
}
