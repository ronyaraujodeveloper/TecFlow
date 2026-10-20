namespace Ofertio.Database.Filter;

public class OfficialCatalogSearchFilter
{
    public string? Keyword { get; set; }

    public decimal? MinPrice { get; set; }

    public decimal? MaxPrice { get; set; }

    public bool? HasCoupon { get; set; }

    public bool? FreeShippingOnly { get; set; }

    /// <summary><c>relevancia</c> (padrão), <c>menor_preco</c> ou <c>maior_desconto</c>.</summary>
    public string? SortBy { get; set; }

    /// <summary>Domínio de catálogo do Mercado Livre (ex.: <c>MLB-NOTEBOOKS</c>).</summary>
    public string? MercadoLivreCategory { get; set; }

    /// <summary>Offset do <c>products/search</c> para "Carregar mais" (só Mercado Livre).</summary>
    public int MercadoLivreOffset { get; set; }

    public bool MercadoLivre { get; set; } = true;

    public string? MercadoLivreTrackingId { get; set; }

    public bool Shopee { get; set; } = true;

    public bool Amazon { get; set; } = true;

    public int Limit { get; set; } = 20;
}
