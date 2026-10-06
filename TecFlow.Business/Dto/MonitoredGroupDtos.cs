using TecFlow.Core.Enums;

namespace TecFlow.Business.Dto;

public class MonitoredGroupDto
{
    public string GroupKey { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Channel { get; set; } = string.Empty;
}

public class GroupCapturedOfferDto
{
    public int Id { get; set; }

    public string Channel { get; set; } = string.Empty;

    public string GroupKey { get; set; } = string.Empty;

    public string GroupName { get; set; } = string.Empty;

    public string? ProductName { get; set; }

    public string? Title
    {
        get => ProductName;
        set => ProductName = value;
    }

    public string? CouponCode { get; set; }

    public decimal? ExtractedPrice { get; set; }

    public decimal? Price
    {
        get => ExtractedPrice;
        set => ExtractedPrice = value;
    }

    public decimal? ValidatedPrice { get; set; }

    public string? ProductImageUrl { get; set; }

    public string? ImageUrl
    {
        get => ProductImageUrl;
        set => ProductImageUrl = value;
    }

    public string OriginalUrl { get; set; } = string.Empty;

    public string? PrimaryProductUrl { get; set; }

    public MarketplaceType? PlatformType { get; set; }

    public string? PlatformName { get; set; }

    public string OfferStatus { get; set; } = string.Empty;

    public string OfferStatusLabel { get; set; } = string.Empty;

    public DateTime ReceivedAt { get; set; }

    public bool HasDirectProductUrl { get; set; }

    public bool IsIgnored { get; set; }

    public bool IsAvailable { get; set; } = true;

    public bool IsMediaPending { get; set; }

    public string? ExternalMessageId { get; set; }
}

public class CloneMonitoredOfferResultDto
{
    public bool Status { get; set; }

    public string Descricao { get; set; } = string.Empty;

    public string? AffiliateUrl { get; set; }

    public string? ImageUrl { get; set; }

    public string? Title { get; set; }

    public string? Message { get; set; }

    public string? OfferStatus { get; set; }

    public string? RedirectUrl { get; set; }
}

public class MonitoredGroupsResponseDto
{
    public bool Status { get; set; } = true;

    public string Descricao { get; set; } = string.Empty;

    public List<MonitoredGroupDto> Groups { get; set; } = [];

    public List<GroupCapturedOfferDto> Offers { get; set; } = [];

    public int TotalOffers { get; set; }

    public int Skip { get; set; }

    public int Take { get; set; }

    public CloneMonitoredOfferResultDto? Clone { get; set; }

    public bool Ignored { get; set; }
}

public class PrioritizeMonitoredMediaRequest
{
    public List<int> OfferIds { get; set; } = [];
}
