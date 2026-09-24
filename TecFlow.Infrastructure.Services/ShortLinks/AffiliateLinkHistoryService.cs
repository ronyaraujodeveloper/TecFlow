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
            var items = await _shortLinkRepository.ListByUserForGroupingAsync(userId, filter, cancellationToken);
            var grouped = GroupLinks(items);
            var clickCounts = await _clickLogRepository.GetClickCountsByAffiliateLinkIdsAsync(
                grouped.SelectMany(group => group.Links.Select(link => link.AffiliateLinkId)),
                cancellationToken);

            var storeNames = await LoadStoreFriendlyNamesAsync(
                grouped.SelectMany(group => group.Links).ToList(),
                cancellationToken);

            var page = filter.Page < 1 ? 1 : filter.Page;
            var pageSize = PagedListHelper.NormalizePageSize(filter.PageSize);
            var totalCount = grouped.Count;
            var dtoItems = grouped
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(group => MapGroup(group, clickCounts, storeNames))
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

    private static List<LinkGroup> GroupLinks(IReadOnlyList<Core.Entities.ShortAffiliateLink> items)
    {
        var groups = items
            .GroupBy(link => link.LinkGroupId == Guid.Empty ? link.AffiliateLinkId : link.LinkGroupId)
            .Select(group => new LinkGroup
            {
                LinkGroupId = group.Key,
                Links = group.OrderByDescending(link => link.IsActive).ThenByDescending(link => link.CreatedAt).ToList()
            })
            .Where(group => group.Links.Exists(link => link.IsActive))
            .OrderByDescending(group => group.Links.Max(link => link.CreatedAt))
            .ToList();

        return groups;
    }

    private AffiliateLinkHistoryItemDto MapGroup(
        LinkGroup group,
        IReadOnlyDictionary<Guid, int> clickCounts,
        IReadOnlyDictionary<int, string> storeNames)
    {
        var active = group.Links.Where(link => link.IsActive).ToList();
        var primary = active.FirstOrDefault() ?? group.Links[0];
        var variants = group.Links.Select(link => MapVariant(link, storeNames)).ToList();
        var clickCount = group.Links.Sum(link => clickCounts.GetValueOrDefault(link.AffiliateLinkId));

        return new AffiliateLinkHistoryItemDto
        {
            AffiliateLinkId = primary.AffiliateLinkId,
            LinkGroupId = group.LinkGroupId,
            PlatformType = primary.PlatformType,
            PlatformName = GetPlatformName(primary.PlatformType),
            DisplayTitle = BuildDisplayTitle(primary.CustomNickname, primary.OriginalUrl),
            OriginalUrl = primary.OriginalUrl,
            AffiliateUrl = string.IsNullOrWhiteSpace(primary.AffiliateUrl) ? primary.DestinationUrl : primary.AffiliateUrl,
            ShortenedUrl = MapVariant(primary, storeNames).ShortenedUrl,
            CreatedAt = group.Links.Min(link => link.CreatedAt),
            ClickCount = clickCount,
            Accounts = variants
        };
    }

    private AffiliateLinkAccountVariantDto MapVariant(
        Core.Entities.ShortAffiliateLink link,
        IReadOnlyDictionary<int, string> storeNames)
    {
        string? friendlyName = null;
        if (link.MarketplaceAccountId is int accountId)
        {
            storeNames.TryGetValue(accountId, out friendlyName);
        }

        return new AffiliateLinkAccountVariantDto
        {
            AffiliateLinkId = link.AffiliateLinkId,
            StoreId = link.IntegracaoLojaId ?? 0,
            MarketplaceAccountId = link.MarketplaceAccountId,
            StoreName = string.IsNullOrWhiteSpace(friendlyName) ? $"Loja {link.IntegracaoLojaId}" : friendlyName,
            AffiliateUrl = string.IsNullOrWhiteSpace(link.AffiliateUrl) ? link.DestinationUrl : link.AffiliateUrl,
            ShortenedUrl = ShortLinkPublicUrl.Build(_options.PublicBaseUrl, friendlyName, link.ShortCode),
            ShortenedShopeeUrl = string.IsNullOrWhiteSpace(link.AffiliateUrl) ? link.DestinationUrl : link.AffiliateUrl,
            IsActive = link.IsActive
        };
    }

    private AffiliateLinkHistoryItemDto MapItem(
        Core.Entities.ShortAffiliateLink link,
        int clickCount,
        IReadOnlyDictionary<int, string> storeNames)
    {
        var variant = MapVariant(link, storeNames);
        return new AffiliateLinkHistoryItemDto
        {
            AffiliateLinkId = link.AffiliateLinkId,
            LinkGroupId = link.LinkGroupId == Guid.Empty ? link.AffiliateLinkId : link.LinkGroupId,
            PlatformType = link.PlatformType,
            PlatformName = GetPlatformName(link.PlatformType),
            DisplayTitle = BuildDisplayTitle(link.CustomNickname, link.OriginalUrl),
            OriginalUrl = link.OriginalUrl,
            AffiliateUrl = variant.AffiliateUrl,
            ShortenedUrl = variant.ShortenedUrl,
            CreatedAt = link.CreatedAt,
            ClickCount = clickCount,
            Accounts = [variant]
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
        MarketplaceType.MercadoLivre => "Mercado Livre",
        MarketplaceType.Amazon => "Amazon",
        MarketplaceType.MagazineLuiza => "Magazine Luiza",
        MarketplaceType.Kabum => "Kabum!",
        MarketplaceType.CasasBahia => "Casas Bahia",
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

    private sealed class LinkGroup
    {
        public Guid LinkGroupId { get; init; }

        public List<Core.Entities.ShortAffiliateLink> Links { get; init; } = [];
    }
}
