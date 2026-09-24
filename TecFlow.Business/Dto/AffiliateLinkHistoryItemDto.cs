using TecFlow.Core.Enums;

namespace TecFlow.Business.Dto;

/// <summary>Item do histórico de links de comissão com telemetria agregada.</summary>
public class AffiliateLinkHistoryItemDto
{
    public Guid AffiliateLinkId { get; set; }

    public MarketplaceType PlatformType { get; set; }

    public string PlatformName { get; set; } = string.Empty;

    public string DisplayTitle { get; set; } = string.Empty;

    public string OriginalUrl { get; set; } = string.Empty;

    public string AffiliateUrl { get; set; } = string.Empty;

    public string ShortenedUrl { get; set; } = string.Empty;

    public string? ProductName { get; set; }

    public decimal? ProductPrice { get; set; }

    public string? ProductImageUrl { get; set; }

    public string FormattedProductPrice =>
        TecFlow.Business.Service.LinkStrategies.ProductMetadataHtmlParser.FormatBrl(ProductPrice);

    public DateTime CreatedAt { get; set; }

    public int ClickCount { get; set; }

    public Guid LinkGroupId { get; set; }

    public List<AffiliateLinkAccountVariantDto> Accounts { get; set; } = [];
}

/// <summary>Projeção de <c>ShortAffiliateLink</c> para a ação Visualizar no gerador.</summary>
public class ShortAffiliateLinkDto : AffiliateLinkHistoryItemDto
{
    public static ShortAffiliateLinkDto FromHistory(AffiliateLinkHistoryItemDto item)
    {
        if (item is ShortAffiliateLinkDto typed)
        {
            return typed;
        }

        return new ShortAffiliateLinkDto
        {
            AffiliateLinkId = item.AffiliateLinkId,
            PlatformType = item.PlatformType,
            PlatformName = item.PlatformName ?? string.Empty,
            DisplayTitle = item.DisplayTitle ?? string.Empty,
            OriginalUrl = item.OriginalUrl ?? string.Empty,
            AffiliateUrl = item.AffiliateUrl ?? string.Empty,
            ShortenedUrl = item.ShortenedUrl ?? string.Empty,
            ProductName = item.ProductName,
            ProductPrice = item.ProductPrice,
            ProductImageUrl = item.ProductImageUrl,
            LinkGroupId = item.LinkGroupId,
            Accounts = item.Accounts ?? [],
            CreatedAt = item.CreatedAt,
            ClickCount = item.ClickCount
        };
    }
}
