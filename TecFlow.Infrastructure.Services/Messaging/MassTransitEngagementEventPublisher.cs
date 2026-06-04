using MassTransit;
using TecFlow.Business.Interfaces.Messaging;
using TecFlow.Business.Messaging;

namespace TecFlow.Infrastructure.Services.Messaging;

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
