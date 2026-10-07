using Microsoft.AspNetCore.Hosting;
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
    private readonly IOfferProductMediaStore _mediaStore;
    private readonly IWebHostEnvironment _environment;

    public GroupCapturedMessagesService(
        AppDbContext context,
        IStructuredOfferParserService parser,
        IOfferProductMediaStore mediaStore,
        IWebHostEnvironment environment)
    {
        _context = context;
        _parser = parser;
        _mediaStore = mediaStore;
        _environment = environment;
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

    public async Task<int> AttachProductPhotoAsync(
        int userId,
        string externalMessageId,
        byte[] photoBytes,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || string.IsNullOrWhiteSpace(externalMessageId) || photoBytes is not { Length: > 0 })
        {
            return 0;
        }

        var relativePath = await _mediaStore.SaveProductPhotoAsync(
            userId,
            externalMessageId,
            photoBytes,
            cancellationToken);
        relativePath = ProductImageStorageRules.ToWebRelativePath(relativePath?.Replace('\\', '/'));
        if (string.IsNullOrWhiteSpace(relativePath)
            || !long.TryParse(externalMessageId, out var messageId))
        {
            return 0;
        }

        return await UpdateImageUrlAsync(messageId, relativePath, userId, cancellationToken);
    }

    public async Task<int> SetProductImageUrlAsync(
        int userId,
        string externalMessageId,
        string webRelativeUrl,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0
            || string.IsNullOrWhiteSpace(externalMessageId)
            || !long.TryParse(externalMessageId, out var messageId))
        {
            return 0;
        }

        return await UpdateImageUrlAsync(messageId, webRelativeUrl, userId, cancellationToken);
    }

    public async Task<int> UpdateImageUrlAsync(
        long messageId,
        string imageWebPath,
        int? userId = null,
        CancellationToken cancellationToken = default)
    {
        var cleanPath = ProductImageStorageRules.EnsureLeadingSlash(
            ProductImageStorageRules.ToWebRelativePath(imageWebPath?.Replace('\\', '/')));
        if (messageId <= 0
            || string.IsNullOrWhiteSpace(cleanPath)
            || !ProductImageStorageRules.IsLocalProductImage(cleanPath))
        {
            return 0;
        }

        var externalId = messageId.ToString();
        var updatedAt = DateTime.UtcNow;
        return await _context.Database.ExecuteSqlRawAsync(
            "UPDATE GroupCapturedMessages SET ProductImageUrl = {0}, MediaUrl = {1}, UpdatedAt = {2} WHERE ExternalMessageId = {3}",
            new object[] { cleanPath, cleanPath, updatedAt, externalId },
            cancellationToken);
    }

    public async Task<int> LinkExistingDownloadedImagesAsync(CancellationToken cancellationToken = default)
    {
        var webRoot = ProductImageStorageRules.ResolveWebRoot(
            _environment.WebRootPath,
            _environment.ContentRootPath,
            AppDomain.CurrentDomain.BaseDirectory);
        var uploadsFolder = Path.Combine(webRoot, "uploads", "products");
        if (!Directory.Exists(uploadsFolder))
        {
            return 0;
        }

        var files = Directory.GetFiles(uploadsFolder, "*.jpg", SearchOption.AllDirectories)
            .OrderBy(path => Path.GetFileNameWithoutExtension(path).Contains('_') ? 1 : 0)
            .ThenByDescending(File.GetLastWriteTimeUtc)
            .ToArray();

        var linked = 0;
        foreach (var filePath in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!ProductImageStorageRules.TryParseMessageIdFromFileName(filePath, out var telegramMsgId))
            {
                continue;
            }

            var relativeUrl = ProductImageStorageRules.EnsureLeadingSlash(
                ProductImageStorageRules.ToWebRelativePath(filePath.Replace(webRoot, string.Empty)));
            if (string.IsNullOrWhiteSpace(relativeUrl)
                || !ProductImageStorageRules.IsLocalProductImage(relativeUrl))
            {
                continue;
            }

            var externalId = telegramMsgId.ToString();
            linked += await _context.Database.ExecuteSqlRawAsync(
                "UPDATE GroupCapturedMessages SET ProductImageUrl = {0}, MediaUrl = {1}, UpdatedAt = {2} WHERE ExternalMessageId = {3} AND (ProductImageUrl IS NULL OR ProductImageUrl = '')",
                new object[] { relativeUrl, relativeUrl, DateTime.UtcNow, externalId },
                cancellationToken);
        }

        return linked;
    }

    public async Task<int> LinkDownloadedImagesForCurrentPageAsync(
        IEnumerable<GroupCapturedMessage> currentItems,
        CancellationToken cancellationToken = default)
    {
        if (currentItems is null)
        {
            return 0;
        }

        var webRoot = ProductImageStorageRules.ResolveWebRoot(
            _environment.WebRootPath,
            _environment.ContentRootPath,
            AppDomain.CurrentDomain.BaseDirectory);
        var uploadsFolder = Path.Combine(webRoot, "uploads", "products");
        if (!Directory.Exists(uploadsFolder))
        {
            return 0;
        }

        var linked = 0;
        foreach (var item in currentItems)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (item.Id <= 0)
            {
                continue;
            }

            if (ProductImageStorageRules.FileExistsOnDisk(webRoot, item.ProductImageUrl))
            {
                item.ProductImageUrl = ProductImageStorageRules.EnsureLeadingSlash(
                    ProductImageStorageRules.ToWebRelativePath(item.ProductImageUrl));
                continue;
            }

            var telegramId = item.ExternalMessageId;
            var relativeUrl = ProductImageStorageRules.TryFindPhotoForMessageId(webRoot, telegramId);
            if (string.IsNullOrWhiteSpace(relativeUrl)
                && !string.IsNullOrWhiteSpace(telegramId))
            {
                relativeUrl = TryMatchPrefixJpg(uploadsFolder, webRoot, telegramId);
            }

            if (string.IsNullOrWhiteSpace(relativeUrl)
                || !ProductImageStorageRules.IsLocalProductImage(relativeUrl))
            {
                continue;
            }

            item.ProductImageUrl = relativeUrl;
            item.MediaUrl = relativeUrl;
            item.Touch();
            linked += await _context.Database.ExecuteSqlRawAsync(
                "UPDATE GroupCapturedMessages SET ProductImageUrl = {0}, MediaUrl = {1}, UpdatedAt = {2} WHERE Id = {3}",
                new object[] { relativeUrl, relativeUrl, DateTime.UtcNow, item.Id },
                cancellationToken);
        }

        return linked;
    }

    private static string? TryMatchPrefixJpg(string uploadsFolder, string webRoot, string telegramMessageId)
    {
        var digits = new string(telegramMessageId.Where(char.IsDigit).ToArray());
        if (string.IsNullOrWhiteSpace(digits) || digits.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            return null;
        }

        string[] matches;
        try
        {
            matches = Directory.GetFiles(uploadsFolder, $"{digits}*.jpg", SearchOption.AllDirectories);
        }
        catch (IOException)
        {
            return null;
        }

        var chosen = matches
            .Select(path => new { path, name = Path.GetFileNameWithoutExtension(path) })
            .Where(item => item.name == digits || item.name.StartsWith(digits + "_", StringComparison.Ordinal))
            .OrderBy(item => item.name == digits ? 0 : 1)
            .ThenByDescending(item => File.GetLastWriteTimeUtc(item.path))
            .Select(item => item.path)
            .FirstOrDefault();
        return chosen is null
            ? null
            : ProductImageStorageRules.EnsureLeadingSlash(
                ProductImageStorageRules.ToWebRelativePath(chosen.Replace(webRoot, string.Empty)));
    }

    public async Task<int> SaveValidatedOfferAsync(
        GroupCapturedMessage entity,
        CancellationToken cancellationToken = default)
    {
        if (entity.UserId <= 0 || string.IsNullOrWhiteSpace(entity.OriginalUrl))
        {
            return 0;
        }

        var exists = await _context.GroupCapturedMessages.AnyAsync(
            item => item.UserId == entity.UserId
                && item.Channel == entity.Channel
                && item.OriginalUrl == entity.OriginalUrl
                && item.ExternalMessageId == entity.ExternalMessageId,
            cancellationToken);
        if (exists)
        {
            return 0;
        }

        _context.GroupCapturedMessages.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return 1;
    }

    public async Task<int> ResetAllCapturedMessagesAndMediaAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return 0;
        }

        var deleted = await _context.Database.ExecuteSqlRawAsync(
            "DELETE FROM GroupCapturedMessages WHERE UserId = {0}",
            new object[] { userId },
            cancellationToken);

        var webRoot = ProductImageStorageRules.ResolveWebRoot(
            _environment.WebRootPath,
            _environment.ContentRootPath,
            AppDomain.CurrentDomain.BaseDirectory);
        var tenantUploadPath = ProductImageStorageRules.TryResolveTenantProductsFolder(webRoot, userId);
        ProductImageStorageRules.TryDeleteTenantProductFiles(tenantUploadPath);

        return deleted;
    }
}
