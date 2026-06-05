using TecFlow.Business.Messaging;

namespace TecFlow.Business.Interfaces.Messaging;

public interface IEngagementEventPublisher
{
    Task PublishCommentReceivedAsync(
        SocialMediaCommentReceivedEvent message,
        CancellationToken cancellationToken = default);
}
