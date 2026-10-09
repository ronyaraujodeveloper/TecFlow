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

public class OfficialCatalogChannelResult
{
    public IReadOnlyList<OfficialCatalogProductDto> Items { get; init; } = [];

    public bool MissingCredentials { get; init; }

    public static OfficialCatalogChannelResult Empty { get; } = new();

    public static OfficialCatalogChannelResult Unconfigured() => new() { MissingCredentials = true };

    public static OfficialCatalogChannelResult From(IReadOnlyList<OfficialCatalogProductDto> items) =>
        new() { Items = items };
}

public class OfficialCatalogChannelStatusDto
{
    public string Key { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public bool Included { get; set; }

    public int ProductCount { get; set; }

    public string State { get; set; } = "empty";

    public string Message { get; set; } = string.Empty;
}

public class OfficialCatalogSearchResponseDto
{
    public bool Status { get; set; }

    public string Descricao { get; set; } = string.Empty;

    public List<OfficialCatalogProductDto>? DataList { get; set; }

    public OfficialCatalogProductDto? Data { get; set; }

    public List<OfficialCatalogChannelStatusDto> Channels { get; set; } = [];
}
