using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using TecFlow.Core.Entities;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Infrastructure.Configuration;
using TecFlow.Database;

namespace TecFlow.Infrastructure.Services.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly AppDbContext _context;

        public ProductRepository(AppDbContext context)
        {
            _context = context;
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
        public async Task<Product> CreateAsync(Product produto)
        {
            _context.Products.Add(produto);
            await _context.SaveChangesAsync();
            return produto;
        }
    }
}