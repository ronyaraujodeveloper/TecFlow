using TecFlow.Business.Integrations.Shopee;
using TecFlow.Database.Entity;

namespace TecFlow.Business.Integrations.MagazineLuiza;

/// <summary>Monta o link de afiliado Magazine Você ou Magalu com parâmetro parceiro.</summary>
public static class MagazineLuizaCommissionUrlBuilder
{
    public const string MagazineVoceBase = "https://www.magazinevoce.com.br/";
    public const string CatalogProductBase = "https://www.magazineluiza.com.br/p/";
    public const string ParceiroQuery = "parceiro";
    public const string HomologPartnerSlug = "magazinematos";

    public static string BuildProductAffiliateUrl(string productId, string partnerKey)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            throw new ArgumentException("productId é obrigatório.", nameof(productId));
        }

        var partner = ResolvePartnerKey(partnerKey);
        var id = productId.Trim();
        if (IsNumericPartner(partner))
        {
            return $"{CatalogProductBase}{id}/?{ParceiroQuery}={Uri.EscapeDataString(partner)}";
        }

        return $"{MagazineVoceBase}{SanitizePartnerSlug(partner)}/p/{id}/";
    }

    public static string ResolvePartnerKey(IntegracaoLoja store)
    {
        if (!string.IsNullOrWhiteSpace(store.AffiliateTrackingId))
        {
            return store.AffiliateTrackingId.Trim();
        }

        return ShopeeCommissionUrlBuilder.ResolveUniversalSubId(store);
    }

    public static string ResolvePartnerKey(string? partnerKey) =>
        string.IsNullOrWhiteSpace(partnerKey) ? HomologPartnerSlug : partnerKey.Trim();

    public static bool IsNumericPartner(string partner) =>
        partner.Length > 0 && partner.All(char.IsDigit);

    public static string SanitizePartnerSlug(string partner)
    {
        var chars = partner
            .Trim()
            .ToLowerInvariant()
            .Where(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_')
            .ToArray();
        var slug = new string(chars).Trim('-', '_');
        return string.IsNullOrWhiteSpace(slug) ? HomologPartnerSlug : slug;
    }
}
