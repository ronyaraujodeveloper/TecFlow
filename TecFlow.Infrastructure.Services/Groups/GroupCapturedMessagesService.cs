using Microsoft.EntityFrameworkCore;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Groups;
using TecFlow.Core.Entities;
using TecFlow.Core.Enums;
using TecFlow.Database;

namespace TecFlow.Infrastructure.Services.Groups;

public sealed class GroupCapturedMessagesService : IGroupCapturedMessagesService
{
    private readonly AppDbContext _context;
    private readonly IStructuredOfferParserService _parser;

    public GroupCapturedMessagesService(AppDbContext context, IStructuredOfferParserService parser)
    {
        _context = context;
        _parser = parser;
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

    public void ApplyStructuredParse(GroupCapturedMessage entity, string? rawMessage)
    {
        var parsed = _parser.Parse(rawMessage);
        if (!string.IsNullOrWhiteSpace(parsed.ProductTitle))
        {
            entity.ProductName = parsed.ProductTitle.Trim();
        }

        if (parsed.Price is > 0)
        {
            entity.ExtractedPrice = parsed.Price;
        }

        if (!string.IsNullOrWhiteSpace(parsed.CouponCode))
        {
            entity.CouponCode = parsed.CouponCode.Trim();
        }

        if (!string.IsNullOrWhiteSpace(parsed.PrimaryProductUrl))
        {
            entity.PrimaryProductUrl = parsed.PrimaryProductUrl.Trim();
        }

        if (!string.IsNullOrWhiteSpace(parsed.Platform))
        {
            entity.PlatformName = parsed.Platform.Trim();
        }

        entity.ProductImageUrl = ProductImageStorageRules.ToWebRelativePath(entity.ProductImageUrl);
        entity.MediaUrl = ProductImageStorageRules.ToWebRelativePath(entity.MediaUrl);
    }
}
