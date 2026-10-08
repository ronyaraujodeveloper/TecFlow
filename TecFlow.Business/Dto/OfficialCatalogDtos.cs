using TecFlow.Core.Enums;

namespace TecFlow.Business.Dto;

public class OfficialCatalogProductDto
{
    public string Platform { get; set; } = string.Empty;

    public MarketplaceType? PlatformType { get; set; }

    public string? ProductId { get; set; }

    public string? ProductName { get; set; }

    public decimal? Price { get; set; }

    public decimal? OriginalPrice { get; set; }

    public string? CouponCode { get; set; }

    public string? ImageUrl { get; set; }

    public string SourceUrl { get; set; } = string.Empty;

    public string? Shipping { get; set; }

    public string Source { get; set; } = "Api";
}

public class OfficialCatalogSearchResponseDto
{
    public bool Status { get; set; }

    public string Descricao { get; set; } = string.Empty;

    public List<OfficialCatalogProductDto>? DataList { get; set; }

    public OfficialCatalogProductDto? Data { get; set; }
}
