using TecFlow.Core.Enums;

namespace TecFlow.Business.Dto;

public class OfferHealthAlertDto
{
    public int Id { get; set; }

    public string Channel { get; set; } = string.Empty;

    public int? CampaignId { get; set; }

    public string? ProductUrl { get; set; }

    public string? CouponCode { get; set; }

    public string AlertType { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public decimal? Price { get; set; }

    public decimal? ComparedPrice { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class OfferHealthAlertResponseDto
{
    public bool Status { get; set; } = true;

    public string Descricao { get; set; } = string.Empty;

    public List<OfferHealthAlertDto> Alerts { get; set; } = [];
}

public class GroupAttributionRowDto
{
    public string Channel { get; set; } = string.Empty;

    public string GroupKey { get; set; } = string.Empty;

    public string SubId { get; set; } = string.Empty;

    public int Clicks { get; set; }

    public int Links { get; set; }
}

public class GroupAttributionResponseDto
{
    public bool Status { get; set; } = true;

    public string Descricao { get; set; } = string.Empty;

    public List<GroupAttributionRowDto> Rows { get; set; } = [];
}

public class EvergreenOfferDto
{
    public int Id { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string? ProductImageUrl { get; set; }

    public string AffiliateUrl { get; set; } = string.Empty;

    public string? OriginalUrl { get; set; }

    public MarketplaceType? PlatformType { get; set; }

    public string? PlatformName { get; set; }

    public int ClickCount { get; set; }

    public int ChampionScore { get; set; }

    public DateTime? LastUsedAt { get; set; }

    public bool IsChampion { get; set; }
}

public class EvergreenLibraryResponseDto
{
    public bool Status { get; set; } = true;

    public string Descricao { get; set; } = string.Empty;

    public List<EvergreenOfferDto> Items { get; set; } = [];
}

public class OfferMediaRequestDto
{
    public string? ImageUrl { get; set; }

    public string? PageUrl { get; set; }

    public string Caption { get; set; } = "OFERTA";
}

public class OfferMediaResponseDto
{
    public bool Status { get; set; } = true;

    public string Descricao { get; set; } = string.Empty;

    public string? FramedImageUrl { get; set; }

    public string? VideoUrl { get; set; }
}
