using Ofertio.Business.Messaging;

namespace Ofertio.Business.Interfaces.Messaging;

public interface IEngagementEventPublisher
{
    Task PublishCommentReceivedAsync(
        SocialMediaCommentReceivedEvent message,
        CancellationToken cancellationToken = default);
}
