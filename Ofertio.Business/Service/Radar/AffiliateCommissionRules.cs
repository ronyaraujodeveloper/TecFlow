using System.Globalization;
using System.Text;

namespace Ofertio.Business.Service.Radar;

public sealed record AffiliateCategoryRate(string Id, string Name, decimal Percent);

/// <summary>
/// Regras de comissão de afiliado. Mercado Livre: tabela do afiliado generalista (venda direta) por categoria raiz,
/// conforme a ajuda "Como calcular seus ganhos por produto" (vigente em out/2026). A venda indireta paga a metade
/// e o ML pode alterar as taxas: o valor é uma estimativa, a taxa real aparece no painel de afiliado.
/// </summary>
public static class AffiliateCommissionRules
{
    public const string SourceShopeeApi = "API Shopee";
    public const string SourceMercadoLivreTable = "Tabela ML por categoria";
    public const string SourceUserRate = "Sua taxa";

    public static IReadOnlyList<AffiliateCategoryRate> MercadoLivreCategoryRates { get; } =
    [
        new("MLB1246", "Beleza e Cuidado Pessoal", 16m),
        new("MLB1430", "Calçados, Roupas e Bolsas", 16m),
        new("MLB1276", "Esportes e Fitness", 16m),
        new("MLB1747", "Acessórios para Veículos", 12m),
        new("MLB1384", "Bebês", 12m),
        new("MLB1132", "Brinquedos e Hobbies", 12m),
        new("MLB1574", "Casa, Móveis e Decoração", 12m),
        new("MLB1500", "Construção", 12m),
        new("MLB263532", "Ferramentas", 12m),
        new("MLB1144", "Games", 12m),
        new("MLB3937", "Joias e Relógios", 12m),
        new("MLB3025", "Livros, Revistas e Comics", 12m),
        new("MLB1953", "Mais Categorias", 12m),
        new("MLB1039", "Câmeras e Acessórios", 5m),
        new("MLB1051", "Celulares e Telefones", 5m),
        new("MLB5726", "Eletrodomésticos", 5m),
        new("MLB1000", "Eletrônicos, Áudio e Vídeo", 5m),
        new("MLB1648", "Informática", 5m),
        new("MLB1403", "Alimentos e Bebidas", 0m)
    ];

    /// <summary>Taxa da categoria raiz do Mercado Livre pelo id (<c>MLB1648</c>) ou, se o id mudar, pelo nome.</summary>
    public static AffiliateCategoryRate? FindMercadoLivreRate(string? rootCategoryId, string? rootCategoryName)
    {
        if (!string.IsNullOrWhiteSpace(rootCategoryId))
        {
            var byId = MercadoLivreCategoryRates.FirstOrDefault(rate =>
                string.Equals(rate.Id, rootCategoryId.Trim(), StringComparison.OrdinalIgnoreCase));
            if (byId is not null)
            {
                return byId;
            }
        }

        if (string.IsNullOrWhiteSpace(rootCategoryName))
        {
            return null;
        }

        var name = NormalizeName(rootCategoryName);
        return MercadoLivreCategoryRates.FirstOrDefault(rate => NormalizeName(rate.Name) == name);
    }

    private static string NormalizeName(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value.Trim().Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : ' ');
        }

        return string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
