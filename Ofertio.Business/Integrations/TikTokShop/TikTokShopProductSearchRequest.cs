namespace Ofertio.Business.Integrations.TikTokShop;

/// <summary>Busca de produtos em colaboração aberta (Affiliate Creator) com o token Master.</summary>
public sealed class TikTokShopProductSearchRequest
{
    public const int MaxPageSize = 20;

    public string Query { get; init; } = string.Empty;

    /// <summary><c>next_page_token</c> da página anterior; vazio na primeira página.</summary>
    public string? PageToken { get; init; }

    public int PageSize { get; init; } = MaxPageSize;

    public decimal? MinPrice { get; init; }

    public decimal? MaxPrice { get; init; }

    /// <summary>Comissão mínima em centésimos de ponto percentual (ex.: 1000 = 10%), como a API espera.</summary>
    public int? MinCommissionRate { get; init; }
}
