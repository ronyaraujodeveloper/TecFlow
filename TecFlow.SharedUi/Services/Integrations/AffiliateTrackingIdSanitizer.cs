using TecFlow.Business.Integrations;
using TecFlow.Core.Enums;

namespace TecFlow.SharedUi.Services.Integrations;

/// <summary>Fachada da UI para extração de ID de afiliado a partir de URL colada.</summary>
public static class AffiliateTrackingIdSanitizer
{
    public const int MaxLength = AffiliateTrackingIdValidator.MaxLength;

    public const string InvalidMessage = AffiliateTrackingIdValidator.InvalidMessage;

    public static string Extract(MarketplaceType platform, string? raw) =>
        ExtractAffiliateIdFromUrl(raw ?? string.Empty, platform.ToString());

    public static string ExtractAffiliateIdFromUrl(string input, string platform) =>
        AffiliateTrackingIdValidator.ExtractAffiliateIdFromUrl(input, platform);

    public static bool TryNormalize(MarketplaceType platform, string? input, out string id) =>
        AffiliateTrackingIdValidator.TryNormalize(platform, input, out id);

    public static bool LooksLikeUrl(string? value) => AffiliateTrackingIdValidator.LooksLikeUrl(value);

    public static bool IsShortenerUrl(string? value) => AffiliateTrackingIdValidator.IsShortenerUrl(value);

    public static bool TryExtractPlatformAffiliateId(MarketplaceType platform, string? input, out string id) =>
        AffiliateTrackingIdValidator.TryExtractPlatformAffiliateId(platform, input, out id);

    public static bool TryExtractShopeeAffiliateId(string? input, out string id) =>
        AffiliateTrackingIdValidator.TryExtractShopeeAffiliateId(input, out id);

    public static string EnsureAbsoluteHttpUrl(string? value) =>
        AffiliateTrackingIdValidator.EnsureAbsoluteHttpUrl(value);

    public static string ExtractedCredentialMessage(string id, MarketplaceType platform) =>
        AffiliateTrackingIdValidator.ExtractedCredentialMessage(id, platform);

    public static string ExtractedFromShortLinkMessage(string id) =>
        AffiliateTrackingIdValidator.ExtractedFromShortLinkMessage(id);
}
