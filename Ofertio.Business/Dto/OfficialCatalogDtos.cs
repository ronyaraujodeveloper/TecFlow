using Ofertio.Core.Enums;

namespace Ofertio.Business.Dto;

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

    /// <summary>Domínio do catálogo do Mercado Livre (ex.: <c>MLB-NOTEBOOKS</c>), usado no filtro de categoria da tela.</summary>
    public string? DomainId { get; set; }
}

public class OfficialCatalogChannelResult
{
    public IReadOnlyList<OfficialCatalogProductDto> Items { get; init; } = [];

    public bool MissingCredentials { get; init; }

    public string? ErrorMessage { get; init; }

    /// <summary>Produtos de catálogo analisados na varredura (Mercado Livre).</summary>
    public int ScannedCount { get; init; }

    /// <summary>Próximo <c>offset</c> do <c>products/search</c> para "Carregar mais"; nulo quando acabou.</summary>
    public int? NextOffset { get; init; }

    /// <summary>Termo realmente usado (pode ser mais curto que o digitado quando o original não trouxe ofertas).</summary>
    public string? EffectiveQuery { get; init; }

    public IReadOnlyList<OfficialCatalogCategoryDto> Categories { get; init; } = [];

    public static OfficialCatalogChannelResult Empty { get; } = new();

    public static OfficialCatalogChannelResult Unconfigured() => new() { MissingCredentials = true };

    public static OfficialCatalogChannelResult Failed(string errorMessage) =>
        new() { ErrorMessage = errorMessage };

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

    public int? MercadoLivreNextOffset { get; set; }

    public string? MercadoLivreEffectiveQuery { get; set; }

    public List<OfficialCatalogCategoryDto> MercadoLivreCategories { get; set; } = [];
}

/// <summary>Categoria (domínio de catálogo) do Mercado Livre sugerida para o termo, ex.: <c>MLB-NOTEBOOKS</c>.</summary>
public class OfficialCatalogCategoryDto
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}

/// <summary>Varredura do catálogo oficial do Mercado Livre: pagina <c>products/search</c> até juntar as ofertas.</summary>
public sealed class MercadoLivreCatalogSearchRequest
{
    public string Query { get; init; } = string.Empty;

    public int TargetOffers { get; init; } = 20;

    public int Offset { get; init; }

    public string? DomainId { get; init; }

    public decimal? MinPrice { get; init; }

    public decimal? MaxPrice { get; init; }

    public bool FreeShippingOnly { get; init; }

    /// <summary>Sem ofertas para o termo digitado, tenta versões mais curtas ("Dell Inspiron i7 16GB" ➔ "Dell Inspiron i7").</summary>
    public bool AllowShorterQuery { get; init; } = true;

    public bool IncludeCategories { get; init; } = true;

    public string? TrackingId { get; init; }
}
