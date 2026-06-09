using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TecFlow.Business.Configuration;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Repositories;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Core.Enums;
using TecFlow.Database.Filter;
using TecFlow.Database.Pagin;

namespace TecFlow.Infrastructure.Services.ShortLinks;

public sealed class AffiliateLinkHistoryService : IAffiliateLinkHistoryService
{
    private readonly IShortAffiliateLinkRepository _shortLinkRepository;
    private readonly ILinkClickLogRepository _clickLogRepository;
    private readonly ShortLinkOptions _options;
    private readonly ILogger<AffiliateLinkHistoryService> _logger;

    public AffiliateLinkHistoryService(
        IShortAffiliateLinkRepository shortLinkRepository,
        ILinkClickLogRepository clickLogRepository,
        IOptions<ShortLinkOptions> options,
        ILogger<AffiliateLinkHistoryService> logger)
    {
        _shortLinkRepository = shortLinkRepository;
        _clickLogRepository = clickLogRepository;
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

            var page = filter.Page < 1 ? 1 : filter.Page;
            var pageSize = PagedListHelper.NormalizePageSize(filter.PageSize);
            var dtoItems = items
                .Select(link => MapItem(link, clickCounts.GetValueOrDefault(link.AffiliateLinkId)))
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

    private AffiliateLinkHistoryItemDto MapItem(Core.Entities.ShortAffiliateLink link, int clickCount)
    {
        var baseUrl = (_options.PublicBaseUrl ?? "http://localhost:5001/r").TrimEnd('/');

        return new AffiliateLinkHistoryItemDto
        {
            AffiliateLinkId = link.AffiliateLinkId,
            PlatformType = link.PlatformType,
            PlatformName = GetPlatformName(link.PlatformType),
            DisplayTitle = BuildDisplayTitle(link.CustomNickname, link.OriginalUrl),
            OriginalUrl = link.OriginalUrl,
            ShortenedUrl = $"{baseUrl}/{link.ShortCode}",
            CreatedAt = link.CreatedAt,
            ClickCount = clickCount
        };
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
