namespace TecFlow.Business.Dto;

public class GlobalAdvertisingProductDto
{
    public string FriendlyName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string GlobalCategory { get; set; } = string.Empty;
    public string? MainImageUrl { get; set; }
    public decimal AveragePrice { get; set; }
    public List<MarketplaceAffiliateLinkDto> MarketplaceLinks { get; set; } = [];
}
