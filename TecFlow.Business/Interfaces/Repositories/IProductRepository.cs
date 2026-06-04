using TecFlow.Core.Entities;
using TecFlow.Core.Enums;

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

    Task<Product?> GetByMarketplaceSkuAsync(
        string shopId,
        MarketplaceType marketplaceType,
        string externalSkuId);

    Task<int> AdjustStockAsync(int productId, int delta, CancellationToken cancellationToken = default);
}

