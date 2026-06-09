using Microsoft.EntityFrameworkCore;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Core.Enums;
using TecFlow.Database;
using TecFlow.Database.Entity;

namespace TecFlow.Infrastructure.Services.Repositories;

public class IntegracaoLojaRepository : IIntegracaoLojaRepository
{
    private readonly AppDbContext _context;

    public IntegracaoLojaRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<IntegracaoLoja>> ListByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default) =>
        await _context.IntegracaoLojas
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IntegracaoLoja?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await _context.IntegracaoLojas.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public async Task<IntegracaoLoja?> GetByUserShopPlatformAsync(
        int userId,
        string shopId,
        MarketplaceType platformType,
        CancellationToken cancellationToken = default) =>
        await _context.IntegracaoLojas.FirstOrDefaultAsync(
            item => item.UserId == userId
                && item.ShopId == shopId
                && item.PlatformType == platformType,
            cancellationToken);

    public async Task AddAsync(IntegracaoLoja entity, CancellationToken cancellationToken = default)
    {
        await _context.IntegracaoLojas.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(IntegracaoLoja entity, CancellationToken cancellationToken = default)
    {
        _context.IntegracaoLojas.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.IntegracaoLojas.FindAsync([id], cancellationToken);
        if (entity is null)
        {
            return;
        }

        _context.IntegracaoLojas.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
