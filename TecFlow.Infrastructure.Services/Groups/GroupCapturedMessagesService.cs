using Microsoft.EntityFrameworkCore;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Database;

namespace TecFlow.Infrastructure.Services.Groups;

public sealed class GroupCapturedMessagesService : IGroupCapturedMessagesService
{
    private readonly AppDbContext _context;

    public GroupCapturedMessagesService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<MarketplaceType>> ListActivePlatformsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var userKey = userId.ToString();
        return await _context.MarketplaceAccounts
            .AsNoTracking()
            .Where(account => account.IsActive && account.UserId == userKey)
            .Select(account => account.MarketplaceType)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public IQueryable<GroupCapturedMessage> ApplyRelevanceFilter(
        IQueryable<GroupCapturedMessage> query,
        IReadOnlyCollection<MarketplaceType> activePlatforms,
        bool ignored)
    {
        query = query.Where(item => item.IsIgnored == ignored && item.HasDirectProductUrl);
        if (!ignored)
        {
            query = query.Where(item => item.IsAvailable);
        }

        if (activePlatforms.Count == 0)
        {
            return query.Where(_ => false);
        }

        return query.Where(item =>
            item.PlatformType != null && activePlatforms.Contains(item.PlatformType.Value));
    }

    public async Task<bool> SetIgnoredAsync(
        int userId,
        int offerId,
        bool ignored,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.GroupCapturedMessages
            .FirstOrDefaultAsync(item => item.Id == offerId && item.UserId == userId, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        entity.IsIgnored = ignored;
        entity.IgnoredAt = ignored ? DateTime.UtcNow : null;
        entity.Touch();
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
