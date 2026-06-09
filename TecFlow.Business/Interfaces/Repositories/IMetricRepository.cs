using TecFlow.Core.Entities;

namespace TecFlow.Business.Interfaces.Repositories;

public interface IMetricRepository
{
    Task<Metric?> GetByIdAsync(int id);
    Task<IEnumerable<Metric>> GetAllAsync();
    Task AddAsync(Metric entity);
    Task UpdateAsync(Metric entity);
    Task DeleteAsync(int id);
    Task<IEnumerable<Metric>> GetByOwnerIdAsync(int ownerId, int? lojaId = null);
    Task<IEnumerable<Metric>> GetByCampaignIdAsync(int campaignId, int? lojaId = null);
}
