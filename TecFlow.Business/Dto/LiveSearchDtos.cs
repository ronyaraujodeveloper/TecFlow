using TecFlow.Core.Enums;

namespace TecFlow.Business.Dto;

public class LiveSearchFilterDto
{
    public string? Keyword { get; set; }

    public decimal? MinPrice { get; set; }

    public decimal? MaxPrice { get; set; }

    public bool? HasCoupon { get; set; }

    public string? Store { get; set; }

    /// <summary>Característica (<c>ProductTag.Id</c>) do combo do feed; null = Todas as Características.</summary>
    public int? TagId { get; set; }

    public bool HasAny =>
        !string.IsNullOrWhiteSpace(Keyword)
        || MinPrice is > 0
        || MaxPrice is > 0
        || HasCoupon == true
        || !string.IsNullOrWhiteSpace(Store)
        || TagId is > 0;
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

    /// <summary>Página direta e limpa do produto após seguir redirects e vitrines de divulgadores.</summary>
    public string? ResolvedProductUrl { get; set; }
}
