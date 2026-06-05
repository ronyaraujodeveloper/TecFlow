using TecFlow.Core.Enums;

namespace TecFlow.Business.Dto;

public class AffiliateLinkDto
{
    public int ProductId { get; set; }
    public string OriginalUrl { get; set; } = string.Empty;
    public string? ShopeeTrackedUrl { get; set; }
    public string? TikTokShopTrackedUrl { get; set; }
    public string? ShopeeExternalProductId { get; set; }
    public string? TikTokShopExternalProductId { get; set; }
    public string? TrackingCode { get; set; }
    public SocialMediaType? PrimarySocialMedia { get; set; }
    public MarketplaceType? PrimaryMarketplace { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ExtraTrackingParameters { get; set; }
}
