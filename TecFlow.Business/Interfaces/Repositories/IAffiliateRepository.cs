using TecFlow.Core.Entities;

namespace TecFlow.Business.Interfaces.Repositories;

public interface IAffiliateRepository
{
    Task<Affiliate?> GetByIdAsync(int id);
    Task<Affiliate> CreateAsync(Affiliate affiliate);
    Task<IEnumerable<Affiliate>> GetAllAsync();
    Task AddAsync(Affiliate entity);
    Task UpdateAsync(Affiliate entity);
    Task DeleteAsync(int id);
    Task<IEnumerable<Affiliate>> GetByOwnerIdAsync(int ownerId);
}
