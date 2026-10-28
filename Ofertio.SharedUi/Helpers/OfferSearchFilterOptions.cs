using Ofertio.Business.Dto;
using Ofertio.Business.Service.Radar;
using Ofertio.Database.Filter;

namespace Ofertio.SharedUi.Helpers;

/// <summary>Opções dos combos da Buscar Ofertas (Plataformas, Filtros e Qualificadores, Categorias) e o mapeamento para o filtro.</summary>
public static class OfferSearchFilterOptions
{
    public const string MercadoLivre = "mercado-livre";
    public const string Shopee = "shopee";
    public const string Amazon = "amazon";
    public const string TikTokShop = "tiktok-shop";

    public const string CouponOnly = "cupom";
    public const string FreeShippingOnly = "frete-gratis";
    public const string OfficialStoreOnly = "loja-oficial";
    public const string TrustedSellersOnly = "vendedores-confiaveis";
    public const string BestSellersOnly = "mais-vendidos";
    public const string MainProductsOnly = "produtos-principais";

    public static IReadOnlyList<MultiSelectOption> Platforms { get; } =
    [
        new(MercadoLivre, "Mercado Livre"),
        new(Shopee, "Shopee"),
        new(Amazon, "Amazon"),
        new(TikTokShop, "TikTok Shop")
    ];

    public static IReadOnlyList<string> DefaultPlatforms { get; } = [MercadoLivre, Shopee, Amazon, TikTokShop];

    public static IReadOnlyList<MultiSelectOption> Qualifiers { get; } =
    [
        new(CouponOnly, "Apenas com cupom"),
        new(FreeShippingOnly, "Somente frete grátis"),
        new(OfficialStoreOnly, "Somente loja oficial"),
        new(TrustedSellersOnly, "Somente vendedores confiáveis"),
        new(BestSellersOnly, "Somente mais vendidos"),
        new(MainProductsOnly, "Apenas produtos principais (ocultar acessórios)",
            Hint: "Esconde capa, película, carregador, cabo, fonte e outros acessórios quando o termo buscado é o produto")
    ];

    /// <summary>Categorias sugeridas pelo Mercado Livre, com quantas ofertas carregadas há em cada uma.</summary>
    public static IReadOnlyList<MultiSelectOption> Categories(
        IEnumerable<OfficialCatalogCategoryDto> categories,
        IEnumerable<OfficialCatalogProductDto> loaded)
    {
        var counts = OfficialCatalogSearchRules.CountByCategory(loaded);
        return categories
            .Where(category => !string.IsNullOrWhiteSpace(category.Id))
            .Select(category => new MultiSelectOption(
                category.Id.Trim(),
                string.IsNullOrWhiteSpace(category.Name) ? category.Id.Trim() : category.Name,
                counts.TryGetValue(category.Id.Trim(), out var count) ? count : 0))
            .ToList();
    }

    /// <summary>Mantém marcadas só as categorias que ainda existem no combo (ex.: depois de buscar outro termo).</summary>
    public static IReadOnlyList<string> KeepKnownCategories(
        IReadOnlyCollection<string> selected,
        IEnumerable<OfficialCatalogCategoryDto> categories)
    {
        var known = categories.Select(category => category.Id.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return selected.Where(known.Contains).ToList();
    }

    /// <summary>
    /// Categorias marcadas sem nenhuma oferta carregada (contador 0): a tela avisa que "Buscar" consulta o Mercado Livre
    /// só nelas, em vez de deixar a vitrine vazia sem explicação.
    /// </summary>
    public static IReadOnlyList<MultiSelectOption> SelectedWithoutOffers(
        IReadOnlyCollection<string> selected,
        IEnumerable<MultiSelectOption> options) =>
        options
            .Where(option => option.Count is 0 && selected.Contains(option.Value, StringComparer.OrdinalIgnoreCase))
            .ToList();

    /// <summary>
    /// Copia as seleções dos combos para o filtro. Lojas e qualificadores consultam as lojas; as categorias filtram a tela
    /// na hora (em memória) e, ao clicar em "Buscar", vão para a API como <c>domain_id</c> do Mercado Livre.
    /// </summary>
    public static OfficialCatalogSearchFilter ApplySelections(
        OfficialCatalogSearchFilter filter,
        IReadOnlyCollection<string> platforms,
        IReadOnlyCollection<string> qualifiers,
        IReadOnlyCollection<string> categories)
    {
        filter.MercadoLivre = Has(platforms, MercadoLivre);
        filter.Shopee = Has(platforms, Shopee);
        filter.Amazon = Has(platforms, Amazon);
        filter.TikTokShop = Has(platforms, TikTokShop);
        filter.HasCoupon = Has(qualifiers, CouponOnly) ? true : null;
        filter.FreeShippingOnly = Has(qualifiers, FreeShippingOnly) ? true : null;
        filter.OfficialStoreOnly = Has(qualifiers, OfficialStoreOnly) ? true : null;
        filter.TrustedSellersOnly = Has(qualifiers, TrustedSellersOnly) ? true : null;
        filter.BestSellersOnly = Has(qualifiers, BestSellersOnly) ? true : null;
        filter.MainProductsOnly = Has(qualifiers, MainProductsOnly) ? true : null;
        filter.MercadoLivreCategories = categories.Count == 0 ? null : [.. categories];
        return filter;
    }

    private static bool Has(IReadOnlyCollection<string> selected, string value) =>
        selected.Contains(value, StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Campos do modal "Filtros avançados". O modal edita uma cópia (<see cref="Copy"/>) e só aplica em "Aplicar Filtros";
/// um novo campo entra aqui, em <see cref="ActiveCount"/> e no corpo do modal.
/// </summary>
public sealed class OfferSearchAdvancedFilters
{
    public decimal? MinPrice { get; set; }

    public decimal? MaxPrice { get; set; }

    public string SortBy { get; set; } = OfficialCatalogSearchRules.SortRelevance;

    public OfferSearchAdvancedFilters Copy() => (OfferSearchAdvancedFilters)MemberwiseClone();

    /// <summary>Quantos campos fogem do padrão (contador do botão "Filtros").</summary>
    public int ActiveCount =>
        (MinPrice is > 0 ? 1 : 0)
        + (MaxPrice is > 0 ? 1 : 0)
        + (string.Equals(SortBy, OfficialCatalogSearchRules.SortRelevance, StringComparison.OrdinalIgnoreCase) ? 0 : 1);

    public string? Validate() =>
        MinPrice is < 0 || MaxPrice is < 0
            ? "Os preços não podem ser negativos."
            : MinPrice is > 0 && MaxPrice is > 0 && MinPrice > MaxPrice
                ? "O preço mínimo não pode ser maior que o preço máximo."
                : null;
}
