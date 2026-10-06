using TecFlow.Core.Entities;
using TecFlow.Core.Enums;

namespace TecFlow.Business.Interfaces.Services;

public interface IGroupCapturedMessagesService
{
    Task<IReadOnlyList<MarketplaceType>> ListActivePlatformsAsync(
        int userId,
        CancellationToken cancellationToken = default);

    IQueryable<GroupCapturedMessage> ApplyRelevanceFilter(
        IQueryable<GroupCapturedMessage> query,
        IReadOnlyCollection<MarketplaceType> activePlatforms,
        bool ignored);

    Task<bool> SetIgnoredAsync(
        int userId,
        int offerId,
        bool ignored,
        CancellationToken cancellationToken = default);

    void ApplyStructuredParse(GroupCapturedMessage entity, string? rawMessage);

    Task<int> AttachProductPhotoAsync(
        int userId,
        string externalMessageId,
        byte[] photoBytes,
        CancellationToken cancellationToken = default);

    Task<int> SetProductImageUrlAsync(
        int userId,
        string externalMessageId,
        string webRelativeUrl,
        CancellationToken cancellationToken = default);
}
