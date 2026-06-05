using TecFlow.Core.Entities;

namespace TecFlow.Business.Interfaces.Repositories;

public interface IContentRepository
{
    Task<Content?> GetByIdAsync(int id);
    Task<Content> CreateAsync(Content content);
    Task<IEnumerable<Content>> GetAllAsync();
    Task AddAsync(Content entity);
    Task UpdateAsync(Content entity);
    Task DeleteAsync(int id);
    Task<IEnumerable<Content>> GetByOwnerIdAsync(int ownerId);
}
