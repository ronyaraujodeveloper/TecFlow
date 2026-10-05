using TecFlow.Core.Entities;
using TecFlow.Core.Enums;

namespace TecFlow.Business.Dto;

public class AffiliateMiningProfileDto
{
    public int Id { get; set; }

    public List<string> Niches { get; set; } = [];

    public decimal? MinTicket { get; set; }

    public decimal? MaxTicket { get; set; }

    public decimal MinCommissionPercent { get; set; } = 5;

    public bool RestrictToActiveStores { get; set; } = true;

    public bool AutoPilotEnabled { get; set; }

    public string AutoPilotChannel { get; set; } = "WhatsApp";

    public List<string> ActivePlatforms { get; set; } = [];
}

public class AffiliateMiningProfileResponseDto
{
    public bool Status { get; set; } = true;

    public string Descricao { get; set; } = string.Empty;

    public AffiliateMiningProfileDto? Profile { get; set; }
}

public class OfferArbitrageRequestDto
{
    public string OriginalUrl { get; set; } = string.Empty;

    public string? ProductName { get; set; }

    public decimal? ProductPrice { get; set; }
}

public class OfferArbitrageSuggestionDto
{
    public string ProductName { get; set; } = string.Empty;

    public string? ProductImageUrl { get; set; }

    public string OriginalUrl { get; set; } = string.Empty;

    public string? AffiliateUrl { get; set; }

    public MarketplaceType? PlatformType { get; set; }

    public string? PlatformName { get; set; }

    public decimal? Price { get; set; }

    public decimal? ComparedPrice { get; set; }

    public string? CouponCode { get; set; }

    public int AttractivenessScore { get; set; }
}

public class OfferArbitrageResponseDto
{
    public bool Status { get; set; } = true;

    public string Descricao { get; set; } = string.Empty;

    public List<OfferArbitrageSuggestionDto> Suggestions { get; set; } = [];
}

public class OfferRadarItemDto
{
    public int Id { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string? ProductImageUrl { get; set; }

    public string OriginalUrl { get; set; } = string.Empty;

    public string? AffiliateUrl { get; set; }

    public MarketplaceType? PlatformType { get; set; }

    public string? PlatformName { get; set; }

    public decimal? Price { get; set; }

    public decimal? ComparedPrice { get; set; }

    public string? CouponCode { get; set; }

    public int AttractivenessScore { get; set; }

    public string Source { get; set; } = OfferRadarSources.Mining;

    public string SourceLabel { get; set; } = OfferRadarSources.ToUiLabel(OfferRadarSources.Mining);

    public bool IsAutoQueued { get; set; }

    public DateTime ReceivedAt { get; set; }
}

public class OfferRadarResponseDto
{
    public bool Status { get; set; } = true;

    public string Descricao { get; set; } = string.Empty;

    public List<OfferRadarItemDto> Items { get; set; } = [];

    public int Total { get; set; }

    public int Skip { get; set; }

    public int Take { get; set; }

    public bool AutoPilotEnabled { get; set; }

    public string? ScheduleUrl { get; set; }
}
