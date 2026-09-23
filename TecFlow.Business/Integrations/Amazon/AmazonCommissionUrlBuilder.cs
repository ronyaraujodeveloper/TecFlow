using TecFlow.Business.Integrations.Shopee;
using TecFlow.Database.Entity;

namespace TecFlow.Business.Integrations.Amazon;

/// <summary>Monta o link de afiliado da Amazon com a tag de associado.</summary>
public static class AmazonCommissionUrlBuilder
{
    public const string ProductBase = "https://www.amazon.com.br/dp/";
    public const string TagQuery = "tag";
    public const string HomologAssociateTag = "sualoja-20";

    public static string BuildProductAffiliateUrl(string asin, string associateTag)
    {
        if (!AmazonProductUrlParser.IsValidAsin(asin, out var normalized))
        {
            throw new ArgumentException("ASIN inválido.", nameof(asin));
        }

        var tag = ResolveAssociateTag(associateTag);
        return $"{ProductBase}{normalized}?{TagQuery}={Uri.EscapeDataString(tag)}";
    }

    public static string ResolveAssociateTag(IntegracaoLoja store)
    {
        if (!string.IsNullOrWhiteSpace(store.AffiliateTrackingId))
        {
            return store.AffiliateTrackingId.Trim();
        }

        return ShopeeCommissionUrlBuilder.ResolveUniversalSubId(store);
    }

    public static string ResolveAssociateTag(string? associateTag) =>
        string.IsNullOrWhiteSpace(associateTag) ? HomologAssociateTag : associateTag.Trim();
}
