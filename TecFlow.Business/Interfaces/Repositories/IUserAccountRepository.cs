using TecFlow.Core.Entities;

namespace TecFlow.Business.Interfaces.Repositories;

public interface IUserAccountRepository
{
    Task<UserAccount?> GetByIdAsync(int id);
    Task<UserAccount> CreateAsync(UserAccount userAccount);
    Task<IEnumerable<UserAccount>> GetAllAsync();
    Task AddAsync(UserAccount entity);
    Task UpdateAsync(UserAccount entity);
    Task DeleteAsync(int id);
    Task<UserAccount?> GetByEmailAsync(string email);
}
