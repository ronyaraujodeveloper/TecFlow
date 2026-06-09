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

    public string ShortenedUrl { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public int ClickCount { get; set; }
}
