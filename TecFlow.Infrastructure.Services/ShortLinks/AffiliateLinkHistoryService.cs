using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TecFlow.Business.Configuration;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Core.Enums;
using TecFlow.Database;
using TecFlow.Database.Filter;
using TecFlow.Database.Pagin;
using TecFlow.Util.Text;

namespace TecFlow.Infrastructure.Services.ShortLinks;

public sealed class AffiliateLinkHistoryService : IAffiliateLinkHistoryService
{
    private readonly IShortAffiliateLinkRepository _shortLinkRepository;
    private readonly ILinkClickLogRepository _clickLogRepository;
    private readonly AppDbContext _context;
    private readonly ShortLinkOptions _options;
    private readonly ILogger<AffiliateLinkHistoryService> _logger;

    public AffiliateLinkHistoryService(
        IShortAffiliateLinkRepository shortLinkRepository,
        ILinkClickLogRepository clickLogRepository,
        AppDbContext context,
        IOptions<ShortLinkOptions> options,
        ILogger<AffiliateLinkHistoryService> logger)
    {
        _shortLinkRepository = shortLinkRepository;
        _clickLogRepository = clickLogRepository;
        _context = context;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AffiliateLinkHistoryResponseDto> ListByUserAsync(
        int userId,
        AffiliateLinkFilter filter,
        CancellationToken cancellationToken = default)
    {
        try
        {
            filter ??= new AffiliateLinkFilter();
            var (items, totalCount) = await _shortLinkRepository.ListByUserAsync(userId, filter, cancellationToken);
            var clickCounts = await _clickLogRepository.GetClickCountsByAffiliateLinkIdsAsync(
                items.Select(item => item.AffiliateLinkId),
                cancellationToken);

            var storeNames = await LoadStoreFriendlyNamesAsync(items, cancellationToken);

            var page = filter.Page < 1 ? 1 : filter.Page;
            var pageSize = PagedListHelper.NormalizePageSize(filter.PageSize);
            var dtoItems = items
                .Select(link => MapItem(link, clickCounts.GetValueOrDefault(link.AffiliateLinkId), storeNames))
                .ToList();

            var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

            return new AffiliateLinkHistoryResponseDto
            {
                Status = true,
                Descricao = "OK",
                DataList = dtoItems,
                Paging = PagingInfoDto.FromMeta(new PagedListMeta
                {
                    TotalCount = totalCount,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    HasNextPage = page < totalPages
                })
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar histórico de links para UserId={UserId}.", userId);
            return new AffiliateLinkHistoryResponseDto
            {
                Status = false,
                Descricao = "Não foi possível carregar o histórico de links."
            };
        }
    }

    private AffiliateLinkHistoryItemDto MapItem(
        Core.Entities.ShortAffiliateLink link,
        int clickCount,
        IReadOnlyDictionary<int, string> storeNames)
    {
        string? friendlyName = null;
        if (link.MarketplaceAccountId is int accountId)
        {
            storeNames.TryGetValue(accountId, out friendlyName);
        }

        return new AffiliateLinkHistoryItemDto
        {
            AffiliateLinkId = link.AffiliateLinkId,
            PlatformType = link.PlatformType,
            PlatformName = GetPlatformName(link.PlatformType),
            DisplayTitle = BuildDisplayTitle(link.CustomNickname, link.OriginalUrl),
            OriginalUrl = link.OriginalUrl,
            AffiliateUrl = string.IsNullOrWhiteSpace(link.AffiliateUrl) ? link.DestinationUrl : link.AffiliateUrl,
            ShortenedUrl = ShortLinkPublicUrl.Build(_options.PublicBaseUrl, friendlyName, link.ShortCode),
            CreatedAt = link.CreatedAt,
            ClickCount = clickCount
        };
    }

    private async Task<Dictionary<int, string>> LoadStoreFriendlyNamesAsync(
        IReadOnlyList<Core.Entities.ShortAffiliateLink> items,
        CancellationToken cancellationToken)
    {
        var accountIds = items
            .Where(item => item.MarketplaceAccountId is > 0)
            .Select(item => item.MarketplaceAccountId!.Value)
            .Distinct()
            .ToList();

        if (accountIds.Count == 0)
        {
            return [];
        }

        return await _context.MarketplaceAccounts
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(account => accountIds.Contains(account.Id))
            .ToDictionaryAsync(
                account => account.Id,
                account => account.FriendlyName ?? string.Empty,
                cancellationToken);
    }

    private static string GetPlatformName(MarketplaceType platformType) => platformType switch
    {
        MarketplaceType.Shopee => "Shopee",
        MarketplaceType.TikTokShop => "TikTok Shop",
        _ => platformType.ToString()
    };

    private static string BuildDisplayTitle(string? nickname, string originalUrl)
    {
        if (!string.IsNullOrWhiteSpace(nickname))
        {
            return nickname.Trim();
        }

        if (string.IsNullOrWhiteSpace(originalUrl))
        {
            return "Link de comissão";
        }

        var trimmed = originalUrl.Trim();
        return trimmed.Length <= 56 ? trimmed : trimmed[..53] + "...";
    }
}
