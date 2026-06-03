using TecFlow.Core.Entities;

namespace TecFlow.Business.Interfaces.Repositories;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id);
    Task<Product> CreateAsync(Product product);
    Task<IEnumerable<Product>> GetAllAsync();
    Task AddAsync(Product entity);
    Task UpdateAsync(Product entity);
    Task DeleteAsync(int id);
    Task<Product?> GetByIdIncludingOwnerAsync(int id);
    Task<IEnumerable<Product>> GetByOwnerIdAsync(int ownerId);
}
