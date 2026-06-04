using TecFlow.Core.Enums;

namespace TecFlow.Business.Dto;

public class MarketplaceAffiliateLinkDto
{
    public MarketplaceType MarketplaceType { get; set; }
    public string OriginalProductUrl { get; set; } = string.Empty;
    public string? PlatformProductId { get; set; }
    public string? CustomTrackingParameters { get; set; }
}
