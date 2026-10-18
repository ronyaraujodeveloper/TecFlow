using Microsoft.Extensions.Logging;
using Ofertio.Business.Interfaces.Messaging;
using Ofertio.Business.Messaging;

namespace Ofertio.Infrastructure.Services.Messaging;

public class NoOpEngagementEventPublisher : IEngagementEventPublisher
{
    private readonly ILogger<NoOpEngagementEventPublisher> _logger;

    public NoOpEngagementEventPublisher(ILogger<NoOpEngagementEventPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishCommentReceivedAsync(
        SocialMediaCommentReceivedEvent message,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "RabbitMQ desabilitado — evento {EventId} não publicado (PostId={PostId}).",
            message.Id,
            message.PostId);
        return Task.CompletedTask;
    }
}
