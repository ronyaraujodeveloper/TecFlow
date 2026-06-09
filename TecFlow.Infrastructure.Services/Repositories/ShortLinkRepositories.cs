using Microsoft.EntityFrameworkCore;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Database;
using TecFlow.Database.Entity;
using TecFlow.Database.Filter;
using TecFlow.Database.Pagin;

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

    public async Task<(List<ShortAffiliateLink> Items, int TotalCount)> ListByUserAsync(
        int userId,
        AffiliateLinkFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ShortAffiliateLinks
            .AsNoTracking()
            .Where(link => link.UserId == userId && link.IsActive);

        if (filter.LojaId is int lojaId)
        {
            query = query.Where(link => link.IntegracaoLojaId == lojaId);
        }

        if (filter.PlatformType is MarketplaceType platformType)
        {
            query = query.Where(link => link.PlatformType == platformType);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = PagedListHelper.NormalizePageSize(filter.PageSize);

        var items = await query
            .OrderByDescending(link => link.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
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

    public async Task<Dictionary<Guid, int>> GetClickCountsByAffiliateLinkIdsAsync(
        IEnumerable<Guid> affiliateLinkIds,
        CancellationToken cancellationToken = default)
    {
        var ids = affiliateLinkIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        return await _context.LinkClickLogs
            .AsNoTracking()
            .Where(log => ids.Contains(log.AffiliateLinkId))
            .GroupBy(log => log.AffiliateLinkId)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Key, item => item.Count, cancellationToken);
    }
}
