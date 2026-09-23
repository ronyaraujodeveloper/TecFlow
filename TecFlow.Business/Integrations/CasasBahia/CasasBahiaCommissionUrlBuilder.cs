using TecFlow.Business.Integrations.Shopee;
using TecFlow.Database.Entity;

namespace TecFlow.Business.Integrations.CasasBahia;

/// <summary>Monta o link de afiliado das Casas Bahia com parceiro e sub_id.</summary>
public static class CasasBahiaCommissionUrlBuilder
{
    public const string ProductBase = "https://www.casasbahia.com.br/p/";
    public const string ParceiroQuery = "parceiro";
    public const string SubIdQuery = "sub_id";
    public const string HomologParceiro = "tecflow_cb";

    public static string BuildProductAffiliateUrl(string productId, string parceiro, string subId)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            throw new ArgumentException("productId é obrigatório.", nameof(productId));
        }

        var partner = string.IsNullOrWhiteSpace(parceiro) ? HomologParceiro : parceiro.Trim();
        var tracking = string.IsNullOrWhiteSpace(subId) ? partner : subId.Trim();
        return $"{ProductBase}{productId.Trim()}?{ParceiroQuery}={Uri.EscapeDataString(partner)}&{SubIdQuery}={Uri.EscapeDataString(tracking)}";
    }

    public static string ResolveParceiro(IntegracaoLoja store)
    {
        if (!string.IsNullOrWhiteSpace(store.AffiliateTrackingId))
        {
            return store.AffiliateTrackingId.Trim();
        }

        return ShopeeCommissionUrlBuilder.ResolveUniversalSubId(store);
    }

    public static string ResolveSubId(IntegracaoLoja store)
    {
        if (!string.IsNullOrWhiteSpace(store.FriendlyName))
        {
            return store.FriendlyName.Trim();
        }

        return ResolveParceiro(store);
    }
}
