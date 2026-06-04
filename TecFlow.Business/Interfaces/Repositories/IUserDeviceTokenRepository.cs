using TecFlow.Core.Entities;

namespace TecFlow.Business.Interfaces.Repositories;

public interface IUserDeviceTokenRepository
{
    Task<UserDeviceToken?> GetByOwnerAndTokenAsync(int ownerId, string token);
    Task UpsertAsync(UserDeviceToken entity);
    Task<IReadOnlyList<UserDeviceToken>> GetActiveByOwnerIdAsync(int ownerId);
    Task DeactivateAsync(int ownerId, string token);
}
