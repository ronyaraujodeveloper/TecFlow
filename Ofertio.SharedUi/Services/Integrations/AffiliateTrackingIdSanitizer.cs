using Ofertio.Business.Integrations;
using Ofertio.Core.Enums;

namespace Ofertio.SharedUi.Services.Integrations;

/// <summary>Fachada da UI para extração de ID de afiliado a partir de URL colada.</summary>
public static class AffiliateTrackingIdSanitizer
{
    public const int MaxLength = AffiliateTrackingIdValidator.MaxLength;

    public const string InvalidMessage = AffiliateTrackingIdValidator.InvalidMessage;

    public const string MercadoLivrePasteMessage = AffiliateTrackingIdValidator.MercadoLivrePasteMessage;

    public const string MercadoLivreProductPageMessage = AffiliateTrackingIdValidator.MercadoLivreProductPageMessage;

    public static string Extract(MarketplaceType platform, string? raw) =>
        ExtractAffiliateIdFromUrl(raw ?? string.Empty, platform.ToString());

    public static string ExtractAffiliateIdFromUrl(string input, string platform) =>
        AffiliateTrackingIdValidator.ExtractAffiliateIdFromUrl(input, platform);

    public static bool TryNormalize(MarketplaceType platform, string? input, out string id) =>
        AffiliateTrackingIdValidator.TryNormalize(platform, input, out id);

    public static bool LooksLikeUrl(string? value) => AffiliateTrackingIdValidator.LooksLikeUrl(value);

    public static bool IsShortenerUrl(string? value) => AffiliateTrackingIdValidator.IsShortenerUrl(value);

    public static bool NeedsMercadoLivreUrlExpansion(string? value) =>
        AffiliateTrackingIdValidator.NeedsMercadoLivreUrlExpansion(value);

    public static bool TryExtractPlatformAffiliateId(MarketplaceType platform, string? input, out string id) =>
        AffiliateTrackingIdValidator.TryExtractPlatformAffiliateId(platform, input, out id);

    public static bool TryExtractShopeeAffiliateId(string? input, out string id) =>
        AffiliateTrackingIdValidator.TryExtractShopeeAffiliateId(input, out id);

    public static bool TryDetectPlatformFromUrl(string? input, out MarketplaceType platform) =>
        AffiliateTrackingIdValidator.TryDetectPlatformFromUrl(input, out platform);

    public static bool IsBooleanLiteral(string? value) =>
        AffiliateTrackingIdValidator.IsBooleanLiteral(value);

    public static string PreferExtractedCredential(string? extractedId, string? expandedUrl, string fallback) =>
        AffiliateTrackingIdValidator.PreferExtractedCredential(extractedId, expandedUrl, fallback);

    public static string EnsureAbsoluteHttpUrl(string? value) =>
        AffiliateTrackingIdValidator.EnsureAbsoluteHttpUrl(value);

    public static string IsolateHttpUrl(string? value) =>
        AffiliateTrackingIdValidator.IsolateHttpUrl(value);

    public static string PreservePastedValue(string? value) =>
        AffiliateTrackingIdValidator.PreservePastedValue(value);

    public static bool IsMercadoLivreProductPage(string? value) =>
        AffiliateTrackingIdValidator.IsMercadoLivreProductPage(value);

    public static string ExtractedCredentialMessage(string id, MarketplaceType platform) =>
        AffiliateTrackingIdValidator.ExtractedCredentialMessage(id, platform);

    public static string ExtractedFromShortLinkMessage(string id) =>
        AffiliateTrackingIdValidator.ExtractedFromShortLinkMessage(id);
}
