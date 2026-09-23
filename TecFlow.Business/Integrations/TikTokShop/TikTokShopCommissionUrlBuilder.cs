using TecFlow.Business.Integrations.Shopee;
using TecFlow.Database.Entity;

namespace TecFlow.Business.Integrations.TikTokShop;

/// <summary>Monta o link de afiliado do TikTok Shop com sub_id da conta.</summary>
public static class TikTokShopCommissionUrlBuilder
{
    public const string ProductAffiliateBase = "https://shop.tiktok.com/view/product/";

    public static string BuildProductAffiliateUrl(string productId, string subId)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            throw new ArgumentException("productId é obrigatório.", nameof(productId));
        }

        var resolvedSubId = string.IsNullOrWhiteSpace(subId)
            ? ShopeeCommissionUrlBuilder.HomologTrackingSubId
            : subId.Trim();

        return $"{ProductAffiliateBase}{productId.Trim()}?sub_id={Uri.EscapeDataString(resolvedSubId)}";
    }

    /// <summary>sub_id: Tracking ID da conta, senão apelido amigável.</summary>
    public static string ResolveSubId(IntegracaoLoja store) =>
        ShopeeCommissionUrlBuilder.ResolveUniversalSubId(store);
}
