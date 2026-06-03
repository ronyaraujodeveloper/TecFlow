using Microsoft.Extensions.Logging;
using TecFlow.Core.Entities;
using TecFlow.Business.Interfaces.Repositories;

namespace TecFlow.Business.Service.Application;

public class ProductsApplicationService
{
    private readonly IProductRepository _productRepository;
    private readonly ILogger<ProductsApplicationService> _logger;

    public ProductsApplicationService(
        IProductRepository productRepository,
        ILogger<ProductsApplicationService> logger)
    {
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<IEnumerable<Product>> GetAllAsync() => _productRepository.GetAllAsync();

    public Task<Product?> GetByIdAsync(int id) => _productRepository.GetByIdAsync(id);

    public Task AddAsync(Product entity) => _productRepository.AddAsync(entity);

    public Task UpdateAsync(Product entity) => _productRepository.UpdateAsync(entity);

    public Task DeleteAsync(int id) => _productRepository.DeleteAsync(id);
}
