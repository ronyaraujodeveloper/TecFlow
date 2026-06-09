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

        public async Task<IEnumerable<Metric>> GetByOwnerIdAsync(int ownerId, int? lojaId = null)
        {
            var query = _context.Metrics.Where(x => x.OwnerId == ownerId);
            if (lojaId.HasValue)
            {
                query = query.Where(x => x.LojaId == lojaId.Value);
            }

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<Metric>> GetByCampaignIdAsync(int campanhaId, int? lojaId = null)
        {
            var query = _context.Metrics.Where(m => m.CampaignId == campanhaId);
            if (lojaId.HasValue)
            {
                query = query.Where(m => m.LojaId == lojaId.Value);
            }

            return await query.ToListAsync();
        }
    }
}