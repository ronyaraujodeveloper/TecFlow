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

    /// <summary>Anúncio que vende o produto (ML: <c>item_id</c> do ganhador da buy box).</summary>
    public string? ListingId { get; set; }

    public string? CategoryId { get; set; }

    /// <summary>Percentual de comissão de afiliado (Shopee: informado pela API; ML/Amazon: taxa do perfil do usuário).</summary>
    public decimal? CommissionPercent { get; set; }

    public decimal? EstimatedCommission { get; set; }

    /// <summary>"API Shopee" ou "Sua taxa" (perfil); nulo quando não há como estimar.</summary>
    public string? CommissionSource { get; set; }

    /// <summary>Unidades vendidas, quando a loja informa pela API.</summary>
    public int? SoldQuantity { get; set; }

    public decimal? RatingAverage { get; set; }

    public int? RatingCount { get; set; }

    public string? SellerId { get; set; }

    public string? SellerName { get; set; }

    /// <summary>Reputação do vendedor no ML: <c>5_green</c>, <c>4_light_green</c>, <c>3_yellow</c>, <c>2_orange</c>, <c>1_red</c>.</summary>
    public string? SellerReputationLevel { get; set; }

    /// <summary>ML: <c>platinum</c>/<c>gold</c>/<c>silver</c> (MercadoLíder); Shopee: <c>preferred</c>.</summary>
    public string? SellerPowerStatus { get; set; }

    /// <summary>Fração de avaliações positivas do vendedor (0 a 1).</summary>
    public decimal? SellerPositiveRate { get; set; }

    public int? SellerCompletedSales { get; set; }

    public bool IsOfficialStore { get; set; }

    /// <summary>Envio Full (estoque no centro de distribuição do Mercado Livre).</summary>
    public bool IsFulfillment { get; set; }

    public string? Warranty { get; set; }

    /// <summary><c>new</c> ou <c>used</c>.</summary>
    public string? Condition { get; set; }

    /// <summary>Posição no ranking de mais vendidos da categoria (ML <c>highlights</c>).</summary>
    public int? BestSellerPosition { get; set; }

    /// <summary>0 a 100: desconto, frete, comissão, avaliação e vendas.</summary>
    public int AdvantageScore { get; set; }

    /// <summary>0 a 100: loja oficial, reputação, MercadoLíder, Full, garantia e condição.</summary>
    public int SafetyScore { get; set; }

    /// <summary>Média de vantagem e segurança, usada em "Melhor vantagem + segurança".</summary>
    public int OpportunityScore { get; set; }

    public List<string> Highlights { get; set; } = [];

    public List<string> Warnings { get; set; } = [];
}

/// <summary>Resumo das ofertas visíveis na tela (métricas da busca).</summary>
public sealed class OfficialCatalogSearchMetricsDto
{
    public int Count { get; init; }

    public decimal? MinPrice { get; init; }

    public decimal? MaxPrice { get; init; }

    public decimal? MedianPrice { get; init; }

    public decimal? AveragePrice { get; init; }

    public int FreeShippingPercent { get; init; }

    public int OfficialStorePercent { get; init; }

    public int TrustedSellerPercent { get; init; }

    public decimal? AverageRating { get; init; }

    public int? TotalSoldKnown { get; init; }

    public OfficialCatalogProductDto? BestOpportunity { get; init; }

    public OfficialCatalogProductDto? BestSeller { get; init; }

    public OfficialCatalogProductDto? TopCommission { get; init; }

    public OfficialCatalogProductDto? Cheapest { get; init; }
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

    /// <summary>Reputação do vendedor, avaliações, vendas e ranking de mais vendidos (chamadas extras à API).</summary>
    public bool IncludeMetrics { get; init; } = true;

    public string? TrackingId { get; init; }
}
