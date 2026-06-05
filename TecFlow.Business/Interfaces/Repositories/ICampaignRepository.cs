using TecFlow.Core.Entities;

namespace TecFlow.Business.Interfaces.Repositories;

public interface ICampaignRepository
{
    Task<Campaign?> GetByIdAsync(int id);
    Task<Campaign> CreateAsync(Campaign campaign);
    Task<IEnumerable<Campaign>> GetAllAsync();
    Task AddAsync(Campaign entity);
    Task UpdateAsync(Campaign entity);
    Task DeleteAsync(int id);
    Task<IEnumerable<Campaign>> GetByOwnerIdAsync(int ownerId);
}
