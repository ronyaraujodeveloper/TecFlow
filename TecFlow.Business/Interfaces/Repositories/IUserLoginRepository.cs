namespace TecFlow.Business.Interfaces.Repositories;

using TecFlow.Core.Entities;

public interface IUserLoginRepository
{
    Task<UserExternalLogin?> GetByProviderAsync(string loginProvider, string providerKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserExternalLogin>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task AddAsync(UserExternalLogin login, CancellationToken cancellationToken = default);
    Task RemoveAsync(string loginProvider, string providerKey, CancellationToken cancellationToken = default);
}
