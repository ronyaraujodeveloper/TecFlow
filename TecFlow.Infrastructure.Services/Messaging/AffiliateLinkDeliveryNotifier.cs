using Microsoft.Extensions.Logging;
using TecFlow.Business.Interfaces.Messaging;
using TecFlow.Business.Messaging;

namespace TecFlow.Infrastructure.Services.Messaging;

/// <summary>Simula a entrega do link de afiliado (DM/resposta automática) até integração com APIs das redes.</summary>
public class AffiliateLinkDeliveryNotifier : IAffiliateLinkDeliveryNotifier
{
    private readonly ILogger<AffiliateLinkDeliveryNotifier> _logger;

    public AffiliateLinkDeliveryNotifier(ILogger<AffiliateLinkDeliveryNotifier> logger)
    {
        _logger = logger;
    }

    public Task NotifyDeliveryRequestedAsync(
        AffiliateLinkDeliveryRequestedEvent deliveryEvent,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Entrega simulada de link afiliado para @{Username} no post {PostId} ({Platform}). Keyword={Keyword} Url={Url}",
            deliveryEvent.Username,
            deliveryEvent.PostId,
            deliveryEvent.Platform,
            deliveryEvent.MatchedKeyword,
            deliveryEvent.SimulatedAffiliateUrl);

        return Task.CompletedTask;
    }
}
