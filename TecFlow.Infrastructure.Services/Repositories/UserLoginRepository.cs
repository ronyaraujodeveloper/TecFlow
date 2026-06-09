using Microsoft.EntityFrameworkCore;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Core.Entities;
using TecFlow.Database;

namespace TecFlow.Infrastructure.Services.Repositories;

public class UserLoginRepository : IUserLoginRepository
{
    private readonly AppDbContext _context;

    public UserLoginRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UserExternalLogin?> GetByProviderAsync(
        string loginProvider,
        string providerKey,
        CancellationToken cancellationToken = default) =>
        await _context.UserExternalLogins
            .AsNoTracking()
            .FirstOrDefaultAsync(
                login => login.LoginProvider == loginProvider && login.ProviderKey == providerKey,
                cancellationToken);

    public async Task<IReadOnlyList<UserExternalLogin>> GetByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default) =>
        await _context.UserExternalLogins
            .AsNoTracking()
            .Where(login => login.UserId == userId)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(UserExternalLogin login, CancellationToken cancellationToken = default)
    {
        await _context.UserExternalLogins.AddAsync(login, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(
        string loginProvider,
        string providerKey,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.UserExternalLogins
            .FirstOrDefaultAsync(
                login => login.LoginProvider == loginProvider && login.ProviderKey == providerKey,
                cancellationToken);

        if (entity is null)
        {
            return;
        }

        _context.UserExternalLogins.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
