using Microsoft.EntityFrameworkCore;
using TecFlow.Core.Entities;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Database;

namespace TecFlow.Infrastructure.Services.Repositories
{
    public class CampaignRepository : ICampaignRepository
    {
        private readonly AppDbContext _context;

        public CampaignRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Campaign?> GetByIdAsync(int id)
        {
            return await _context.Campaigns.FindAsync(id);
        }

        public async Task<IEnumerable<Campaign>> GetAllAsync()
        {
            return await _context.Campaigns.ToListAsync();

           // return await _context.Campaigns
           //.Where(c => c.OwnerId == userId)
           //.ToListAsync();
        }

        public async Task AddAsync(Campaign campanha)
        {
            await _context.Campaigns.AddAsync(campanha);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Campaign entity)
        {
            _context.Campaigns.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _context.Campaigns.FindAsync(id);
            if (entity != null)
            {
                _context.Campaigns.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }
        public async Task<Campaign?> GetByIdIncludingOwnerAsync(int id)
        {
            // O EF Core lida com a consulta assíncrona ao banco de dados
            return await _context.Campaigns.FirstOrDefaultAsync(c => c.Id == id);
        }


        public async Task<Campaign> CriarAsync(Campaign campanha)
        {
            // Geralmente, o CreatedAt e UpdatedAt são definidos pelo BD ou interceptor.
            // Se precisar definir aqui:
            campanha.CreatedAt = DateTime.UtcNow;
            campanha.UpdatedAt = DateTime.UtcNow;

            _context.Campaigns.Add(campanha);
            await _context.SaveChangesAsync(); // Salva no BD e o EF Core popula o ID gerado.

            return campanha; // Retorna a entidade com o ID atribuído
        }
        public async Task<IEnumerable<Campaign>> GetAllByOwnerAsync(int ownerId)
        {
            return await _context.Campaigns
                .Where(x => x.OwnerId == ownerId)
                .ToListAsync();
        }
        public async Task<IEnumerable<Campaign>> GetByOwnerIdAsync(int ownerId)
        {
            return await _context.Campaigns
                .Where(c => c.OwnerId == ownerId)
                .ToListAsync();
        }
        public async Task<Campaign> CreateAsync(Campaign campanha)
        {
            _context.Campaigns.Add(campanha);
            await _context.SaveChangesAsync();
            return campanha;
        }
    }
}