using Microsoft.EntityFrameworkCore;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Core.Entities;
using TecFlow.Database;

namespace TecFlow.Infrastructure.Services.Repositories;

public class UserDeviceTokenRepository : IUserDeviceTokenRepository
{
    private readonly AppDbContext _context;

    public UserDeviceTokenRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UserDeviceToken?> GetByOwnerAndTokenAsync(int ownerId, string token) =>
        await _context.UserDeviceTokens
            .FirstOrDefaultAsync(t => t.OwnerId == ownerId && t.Token == token);

    public async Task UpsertAsync(UserDeviceToken entity)
    {
        var existing = await GetByOwnerAndTokenAsync(entity.OwnerId, entity.Token);
        var now = DateTime.UtcNow;

        if (existing is null)
        {
            entity.CreatedAt = now;
            entity.UpdatedAt = now;
            entity.IsActive = true;
            await _context.UserDeviceTokens.AddAsync(entity);
            await _context.SaveChangesAsync();
            return;
        }

        existing.Platform = entity.Platform;
        existing.DeviceId = entity.DeviceId;
        existing.IsActive = true;
        existing.UpdatedAt = now;
        _context.UserDeviceTokens.Update(existing);
        entity.Id = existing.Id;
        await _context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<UserDeviceToken>> GetActiveByOwnerIdAsync(int ownerId) =>
        await _context.UserDeviceTokens
            .Where(t => t.OwnerId == ownerId && t.IsActive)
            .ToListAsync();

    public async Task DeactivateAsync(int ownerId, string token)
    {
        var existing = await GetByOwnerAndTokenAsync(ownerId, token);
        if (existing is null)
        {
            return;
        }

        existing.IsActive = false;
        existing.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
}
