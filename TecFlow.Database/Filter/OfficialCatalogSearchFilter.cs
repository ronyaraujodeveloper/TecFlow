namespace TecFlow.Database.Filter;

public class OfficialCatalogSearchFilter
{
    public string? Keyword { get; set; }

    public decimal? MinPrice { get; set; }

    public decimal? MaxPrice { get; set; }

    public bool? HasCoupon { get; set; }

    public bool MercadoLivre { get; set; } = true;

    public bool Shopee { get; set; } = true;

    public bool Amazon { get; set; } = true;

    public int Limit { get; set; } = 20;
}
