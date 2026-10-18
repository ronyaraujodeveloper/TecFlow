using Ofertio.Core.Entities;

namespace Ofertio.Business.Interfaces.Repositories;

public interface ICampaignRepository
{
    Task<Campaign?> GetByIdAsync(int id);
    Task<Campaign> CreateAsync(Campaign campaign);
    Task<IEnumerable<Campaign>> GetAllAsync();
    Task AddAsync(Campaign entity);
    Task UpdateAsync(Campaign entity);
    Task DeleteAsync(int id);
    Task<IEnumerable<Campaign>> GetByOwnerIdAsync(int ownerId, int? lojaId = null);
}
