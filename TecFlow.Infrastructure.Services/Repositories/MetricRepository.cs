using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TecFlow.Core.Entities;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Database;

namespace TecFlow.Infrastructure.Services.Repositories
{
    public class MetricRepository : IMetricRepository
    {
        private readonly AppDbContext _context;

        public MetricRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Metric?> GetByIdAsync(int id) => await _context.Metrics.FindAsync(id);

        public async Task<IEnumerable<Metric>> GetAllAsync() => await _context.Metrics.ToListAsync();

        public async Task AddAsync(Metric entity)
        {
            await _context.Metrics.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Metric entity)
        {
            _context.Metrics.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _context.Metrics.FindAsync(id);
            if (entity != null)
            {
                _context.Metrics.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Metric>> GetByOwnerIdAsync(int ownerId)
        {
            return await _context.Metrics
                .Where(x => x.OwnerId == ownerId)
                .ToListAsync();
        }

        // Implementação do método que o Orquestrador vai consumir
        public async Task<IEnumerable<Metric>> GetByCampaignIdAsync(int campanhaId)
        {
            return await _context.Metrics
                .Where(m => m.CampaignId == campanhaId)
                .ToListAsync();
        }
    }
}