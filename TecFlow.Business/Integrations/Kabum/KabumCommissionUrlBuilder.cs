using TecFlow.Business.Integrations.Shopee;
using TecFlow.Database.Entity;

namespace TecFlow.Business.Integrations.Kabum;

/// <summary>Monta o link de afiliado da Kabum! com sub_id e utm_source=afiliado.</summary>
public static class KabumCommissionUrlBuilder
{
    public const string ProductBase = "https://www.kabum.com.br/produto/";
    public const string SubIdQuery = "sub_id";
    public const string UtmSourceQuery = "utm_source";
    public const string UtmSourceValue = "afiliado";
    public const string HomologSubId = "tecflow_kabum";

    public static string BuildProductAffiliateUrl(string productId, string subId)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            throw new ArgumentException("productId é obrigatório.", nameof(productId));
        }

        var resolvedSubId = string.IsNullOrWhiteSpace(subId) ? HomologSubId : subId.Trim();
        return $"{ProductBase}{productId.Trim()}?{SubIdQuery}={Uri.EscapeDataString(resolvedSubId)}&{UtmSourceQuery}={UtmSourceValue}";
    }

    public static string ResolveSubId(IntegracaoLoja store) =>
        ShopeeCommissionUrlBuilder.ResolveUniversalSubId(store);
}
