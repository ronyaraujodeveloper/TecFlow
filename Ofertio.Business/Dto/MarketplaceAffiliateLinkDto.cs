using Ofertio.Core.Enums;

namespace Ofertio.Business.Dto;

public class MarketplaceAffiliateLinkDto
{
    public MarketplaceType MarketplaceType { get; set; }
    public string OriginalProductUrl { get; set; } = string.Empty;
    public string? PlatformProductId { get; set; }
    public string? CustomTrackingParameters { get; set; }
}
