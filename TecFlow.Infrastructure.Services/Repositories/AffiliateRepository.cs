// Arquivo: TecFlow.Infrastructure\Data\Repositories\AffiliateRepository.cs

//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.EntityFrameworkCore; // Para DbContext e operações async
//using TecFlow.Core.Entities;
//using TecFlow.Business.Interfaces.Repositories;
//using TecFlow.Database;

using Microsoft.EntityFrameworkCore;
using TecFlow.Core.Entities;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Database;

namespace TecFlow.Infrastructure.Services.Repositories
{
    /// <summary>
    /// Implementação do repositório para a entidade Afiliado, utilizando Entity Framework Core.
    /// </summary>
    public class AffiliateRepository : IAffiliateRepository
    {
        private readonly AppDbContext _context;

        public AffiliateRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Affiliate?> GetByIdAsync(int id)
        {
            return await _context.Affiliates.FindAsync(id);
        }

        public async Task<IEnumerable<Affiliate>> GetAllAsync()
        {
            return await _context.Affiliates.ToListAsync();
        }

        public async Task AddAsync(Affiliate Afiliado)
        {
            await _context.Affiliates.AddAsync(Afiliado);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Affiliate entity)
        {
            _context.Affiliates.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _context.Affiliates.FindAsync(id);
            if (entity != null)
            {
                _context.Affiliates.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Affiliate?> GetByIdIncludingOwnerAsync(int id)
        {
            // O EF Core lida com a consulta assíncrona ao banco de dados
            return await _context.Affiliates.FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Affiliate> CriarAsync(Affiliate afiliado)
        {
            // Geralmente, o CreatedAt e UpdatedAt são definidos pelo BD ou interceptor.
            // Se precisar definir aqui:
            afiliado.CreatedAt = DateTime.UtcNow;
            afiliado.UpdatedAt = DateTime.UtcNow;

            _context.Affiliates.Add(afiliado);
            await _context.SaveChangesAsync(); // Salva no BD e o EF Core popula o ID gerado.

            return afiliado; // Retorna a entidade com o ID atribuído
        }
        public async Task<IEnumerable<Affiliate>> GetAllByOwnerAsync(int ownerId)
        {
            return await _context.Affiliates
                .Where(x => x.OwnerId == ownerId)
                .ToListAsync();
        }
        public async Task<IEnumerable<Affiliate>> GetByOwnerIdAsync(int ownerId)
        {
            return await _context.Affiliates
                .Where(c => c.OwnerId == ownerId)
                .ToListAsync();
        }
        public async Task<Affiliate> CreateAsync(Affiliate afiliado)
        {
            _context.Affiliates.Add(afiliado);
            await _context.SaveChangesAsync();
            return afiliado;
        }
    }
}