using TecFlow.Core.Enums;

namespace TecFlow.Business.Dto;

public class LiveSearchFilterDto
{
    public string? Keyword { get; set; }

    public decimal? MinPrice { get; set; }

    public decimal? MaxPrice { get; set; }

    public bool? HasCoupon { get; set; }

    public string? Store { get; set; }

    public bool HasAny =>
        !string.IsNullOrWhiteSpace(Keyword)
        || MinPrice is > 0
        || MaxPrice is > 0
        || HasCoupon == true
        || !string.IsNullOrWhiteSpace(Store);
}

public class OfficialOfferSnapshotDto
{
    public bool IsAvailable { get; set; } = true;

    public string Status { get; set; } = "Ativo";

    public decimal? Price { get; set; }

    public decimal? OriginalPrice { get; set; }

    public string? CouponCode { get; set; }

    public string? ProductName { get; set; }

    public string? ImageUrl { get; set; }

    public string? Shipping { get; set; }

    public string Source { get; set; } = "Html";

    public MarketplaceType? Platform { get; set; }
}
