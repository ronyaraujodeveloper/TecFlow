using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Infrastructure.Configuration;
using TecFlow.Database;
using TecFlow.Database.MultiTenancy;

namespace TecFlow.Infrastructure.Services.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly AppDbContext _context;
        private readonly ICurrentTenantService _currentTenant;

        public ProductRepository(AppDbContext context, ICurrentTenantService currentTenant)
        {
            _context = context;
            _currentTenant = currentTenant;
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            return await _context.Products.FindAsync(id);
        }

        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            return await _context.Products.ToListAsync();
        }

        public async Task AddAsync(Product produto)
        {
            await _context.Products.AddAsync(produto);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Product entity)
        {
            _context.Products.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _context.Products.FindAsync(id);
            if (entity != null)
            {
                _context.Products.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }
        public async Task<Product?> GetByIdIncludingOwnerAsync(int id)
        {
            // O EF Core lida com a consulta assíncrona ao banco de dados
            return await _context.Products.FirstOrDefaultAsync(c => c.Id == id);
        }


        public async Task<Product> CriarAsync(Product produto)
        {
            // Geralmente, o CreatedAt e UpdatedAt são definidos pelo BD ou interceptor.
            // Se precisar definir aqui:
            produto.CreatedAt = DateTime.UtcNow;
            produto.UpdatedAt = DateTime.UtcNow;

            _context.Products.Add(produto);
            await _context.SaveChangesAsync(); // Salva no BD e o EF Core popula o ID gerado.

            return produto; // Retorna a entidade com o ID atribuído
        }
        public async Task<IEnumerable<Product>> GetAllByOwnerAsync(int ownerId)
        {
            return await _context.Products
                .Where(x => x.OwnerId == ownerId)
                .ToListAsync();
        }
        public async Task<IEnumerable<Product>> GetByOwnerIdAsync(int ownerId)
        {
            return await _context.Products
                .Where(c => c.OwnerId == ownerId)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Product>> ListConsolidatedForCurrentTenantAsync()
        {
            return await _context.Products
                .OrderByDescending(p => p.ModifiedOn)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Product>> ListForShopAsync(string shopId)
        {
            var list = await _context.Products
                .WithManualTenantScope(_currentTenant)
                .Where(p => p.MarketplaceShopId == shopId || p.MarketplaceShopId == null)
                .OrderByDescending(p => p.ModifiedOn)
                .ToListAsync();
            return list;
        }
        public async Task<Product> CreateAsync(Product produto)
        {
            _context.Products.Add(produto);
            await _context.SaveChangesAsync();
            return produto;
        }

        public async Task<Product?> GetByMarketplaceSkuAsync(
            string shopId,
            MarketplaceType marketplaceType,
            string externalSkuId)
        {
            if (string.IsNullOrWhiteSpace(externalSkuId))
            {
                return null;
            }

            return await _context.Products.FirstOrDefaultAsync(p =>
                p.MarketplaceShopId == shopId &&
                p.MarketplaceSource == marketplaceType &&
                (p.SkuCode == externalSkuId || p.ExternalProductId == externalSkuId));
        }

        public async Task<int> AdjustStockAsync(
            int productId,
            int delta,
            CancellationToken cancellationToken = default)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

            if (product is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return -1;
            }

            product.Stock = Math.Max(0, product.Stock + delta);
            product.ModifiedOn = DateTime.UtcNow;
            product.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return product.Stock;
        }
    }
}