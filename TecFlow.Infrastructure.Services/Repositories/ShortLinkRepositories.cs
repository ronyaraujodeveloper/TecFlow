using Microsoft.EntityFrameworkCore;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Core.Entities;
using TecFlow.Database;
using TecFlow.Database.Entity;

namespace TecFlow.Infrastructure.Services.Repositories;

public class ShortAffiliateLinkRepository : IShortAffiliateLinkRepository
{
    private readonly AppDbContext _context;

    public ShortAffiliateLinkRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<ShortAffiliateLink?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default) =>
        _context.ShortAffiliateLinks
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                link => link.ShortCode == shortCode && link.IsActive,
                cancellationToken);

    public Task<ShortAffiliateLink?> GetByAffiliateLinkIdAsync(
        Guid affiliateLinkId,
        CancellationToken cancellationToken = default) =>
        _context.ShortAffiliateLinks
            .AsNoTracking()
            .FirstOrDefaultAsync(link => link.AffiliateLinkId == affiliateLinkId, cancellationToken);

    public Task<bool> ShortCodeExistsAsync(string shortCode, CancellationToken cancellationToken = default) =>
        _context.ShortAffiliateLinks.AnyAsync(link => link.ShortCode == shortCode, cancellationToken);

    public async Task AddAsync(ShortAffiliateLink entity, CancellationToken cancellationToken = default)
    {
        await _context.ShortAffiliateLinks.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class LinkClickLogRepository : ILinkClickLogRepository
{
    private readonly AppDbContext _context;

    public LinkClickLogRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(LinkClickLog entity, CancellationToken cancellationToken = default)
    {
        await _context.LinkClickLogs.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
