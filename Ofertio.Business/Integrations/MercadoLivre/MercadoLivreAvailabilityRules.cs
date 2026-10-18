using Ofertio.Business.Service.Groups;
using Ofertio.Core.Entities;

namespace Ofertio.Business.Integrations.MercadoLivre;

/// <summary>
/// Anúncio do Mercado Livre que não pode mais ser comprado direto: pausado/encerrado (Inativo) ou
/// sem estoque / reduzido a "Produtos relacionados" / redirecionado para a busca (Esgotado).
/// </summary>
public static class MercadoLivreAvailabilityRules
{
    public static readonly string[] SoldOutPhrases =
    [
        "Sem estoque",
        "Estoque esgotado",
        "Produto esgotado",
        "Sem unidades disponíveis",
        "Não há unidades disponíveis",
        "Este produto não está disponível",
        "O produto não está disponível",
        "Este produto está indisponível",
        "Produto indisponível"
    ];

    public static readonly string[] RelatedProductsMarkers =
    [
        "Produtos relacionados",
        "Veja produtos relacionados",
        "Confira produtos relacionados"
    ];

    /// <summary>
    /// Sinais da caixa de compra: com ela presente, "Sem estoque" se refere a uma variação, não ao anúncio.
    /// </summary>
    public static readonly string[] BuyBoxMarkers =
    [
        "Comprar agora",
        "Adicionar ao carrinho",
        "schema.org/InStock"
    ];

    private static readonly string[] OutOfStockSchemaMarkers =
    [
        "schema.org/OutOfStock",
        "schema.org/SoldOut",
        "schema.org/Discontinued"
    ];

    private static readonly string[] ClosedApiStatuses = ["closed", "paused", "inactive", "deleted", "ended", "under_review"];

    public static bool IsMercadoLivreUrl(string? url) =>
        MercadoLivreShowcaseRules.TryGetMercadoLivreUri(url, out _);

    public static bool HasBuyBox(string? html) => ContainsAny(html, BuyBoxMarkers);

    /// <summary>
    /// Produto que virou página de busca (lista.mercadolivre.com.br) depois dos redirects: o ML faz isso
    /// com anúncios finalizados. Só conta quando o link pedido ainda não era uma busca.
    /// </summary>
    public static bool IsRedirectedToSearch(string? requestedUrl, string? finalUrl) =>
        IsSearchPage(finalUrl) && !IsSearchPage(requestedUrl);

    /// <summary>
    /// <see cref="GroupOfferStatuses.Inativo"/>, <see cref="GroupOfferStatuses.Esgotado"/> ou null quando a
    /// página ainda permite compra direta (ou não dá para afirmar o contrário, como numa página de desafio).
    /// </summary>
    public static string? DetectUnavailableStatus(string? html, string? requestedUrl, string? finalUrl)
    {
        if (IsRedirectedToSearch(requestedUrl, finalUrl))
        {
            return GroupOfferStatuses.Esgotado;
        }

        if (string.IsNullOrWhiteSpace(html))
        {
            return null;
        }

        if (OfferIntegrityRules.ContainsClosedListingPhrase(html))
        {
            return GroupOfferStatuses.Inativo;
        }

        if (HasBuyBox(html))
        {
            return null;
        }

        return ContainsAny(html, OutOfStockSchemaMarkers)
            || ContainsAny(html, SoldOutPhrases)
            || ContainsAny(html, RelatedProductsMarkers)
            ? GroupOfferStatuses.Esgotado
            : null;
    }

    /// <summary>Status do item na API pública (/items/{id}); null quando o anúncio segue ativo e com estoque.</summary>
    public static string? FromApiItem(string? status, int? availableQuantity)
    {
        var normalized = (status ?? string.Empty).Trim().ToLowerInvariant();
        if (Array.IndexOf(ClosedApiStatuses, normalized) >= 0)
        {
            return GroupOfferStatuses.Inativo;
        }

        return availableQuantity is <= 0 ? GroupOfferStatuses.Esgotado : null;
    }

    private static bool IsSearchPage(string? url) =>
        MercadoLivreShowcaseRules.TryGetMercadoLivreUri(url, out var uri)
        && uri.Host.StartsWith("lista.", StringComparison.OrdinalIgnoreCase);

    private static bool ContainsAny(string? html, IEnumerable<string> phrases)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return false;
        }

        foreach (var phrase in phrases)
        {
            if (html.Contains(phrase, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
