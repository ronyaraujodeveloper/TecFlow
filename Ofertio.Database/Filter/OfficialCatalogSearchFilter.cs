namespace Ofertio.Database.Filter;

public class OfficialCatalogSearchFilter
{
    public string? Keyword { get; set; }

    public decimal? MinPrice { get; set; }

    public decimal? MaxPrice { get; set; }

    public bool? HasCoupon { get; set; }

    public bool? FreeShippingOnly { get; set; }

    /// <summary>Filtros de tela sobre o que já foi carregado (não alteram a consulta às lojas).</summary>
    public bool? OfficialStoreOnly { get; set; }

    public bool? TrustedSellersOnly { get; set; }

    public bool? BestSellersOnly { get; set; }

    /// <summary>Oculta acessórios (capa, película, carregador, cabo...) quando o termo buscado não é um acessório.</summary>
    public bool? MainProductsOnly { get; set; }

    /// <summary>
    /// <c>relevancia</c> (padrão: ordem devolvida pelas lojas), <c>melhor_oportunidade</c> (score Ofertio),
    /// <c>menor_preco</c>, <c>maior_desconto</c>, <c>mais_vendidos</c>, <c>maior_comissao</c> ou <c>melhor_avaliacao</c>.
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>Domínio de catálogo do Mercado Livre (ex.: <c>MLB-NOTEBOOKS</c>).</summary>
    public string? MercadoLivreCategory { get; set; }

    /// <summary>
    /// Domínios do Mercado Livre marcados no combo "Categorias": filtram em memória o que já foi carregado e, na busca,
    /// viram uma varredura do <c>products/search</c> com <c>domain_id</c> por categoria (até 3).
    /// </summary>
    public List<string>? MercadoLivreCategories { get; set; }

    /// <summary>Offset do <c>products/search</c> para "Carregar mais" (só Mercado Livre).</summary>
    public int MercadoLivreOffset { get; set; }

    public bool MercadoLivre { get; set; } = true;

    public string? MercadoLivreTrackingId { get; set; }

    public bool Shopee { get; set; } = true;

    public bool Amazon { get; set; } = true;

    public int Limit { get; set; } = 20;
}
