using MassTransit;
using Ofertio.Business.Interfaces.Messaging;
using Ofertio.Business.Messaging;

namespace Ofertio.Infrastructure.Services.Messaging;

public class MassTransitEngagementEventPublisher : IEngagementEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public MassTransitEngagementEventPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public Task PublishCommentReceivedAsync(
        SocialMediaCommentReceivedEvent message,
        CancellationToken cancellationToken = default) =>
        _publishEndpoint.Publish(message, cancellationToken);
}
