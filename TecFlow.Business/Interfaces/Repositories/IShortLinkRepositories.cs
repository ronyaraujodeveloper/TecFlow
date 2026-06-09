using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Database.Entity;

namespace TecFlow.Business.Interfaces.Repositories;

public interface IShortAffiliateLinkRepository
{
    Task<ShortAffiliateLink?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default);

    Task<ShortAffiliateLink?> GetByAffiliateLinkIdAsync(Guid affiliateLinkId, CancellationToken cancellationToken = default);

    Task<bool> ShortCodeExistsAsync(string shortCode, CancellationToken cancellationToken = default);

    Task AddAsync(ShortAffiliateLink entity, CancellationToken cancellationToken = default);
}

public interface ILinkClickLogRepository
{
    Task AddAsync(LinkClickLog entity, CancellationToken cancellationToken = default);
}
