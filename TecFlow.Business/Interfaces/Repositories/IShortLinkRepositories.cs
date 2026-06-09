using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Database.Entity;
using TecFlow.Database.Filter;

namespace TecFlow.Business.Interfaces.Repositories;

public interface IShortAffiliateLinkRepository
{
    Task<ShortAffiliateLink?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default);

    Task<ShortAffiliateLink?> GetByAffiliateLinkIdAsync(Guid affiliateLinkId, CancellationToken cancellationToken = default);

    Task<bool> ShortCodeExistsAsync(string shortCode, CancellationToken cancellationToken = default);

    Task AddAsync(ShortAffiliateLink entity, CancellationToken cancellationToken = default);

    Task<(List<ShortAffiliateLink> Items, int TotalCount)> ListByUserAsync(
        int userId,
        AffiliateLinkFilter filter,
        CancellationToken cancellationToken = default);
}

public interface ILinkClickLogRepository
{
    Task AddAsync(LinkClickLog entity, CancellationToken cancellationToken = default);

    Task<Dictionary<Guid, int>> GetClickCountsByAffiliateLinkIdsAsync(
        IEnumerable<Guid> affiliateLinkIds,
        CancellationToken cancellationToken = default);
}
