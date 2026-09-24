using TecFlow.Core.Enums;

namespace TecFlow.Business.Integrations;

/// <summary>Extração e validação estrita de Tracking ID / ID de afiliado.</summary>
public static class AffiliateTrackingIdValidator
{
    public const int MaxLength = 64;

    public const string InvalidMessage =
        "O valor informado não é um ID de afiliado válido. Informe o seu ID numérico/tag ou cole um link longo de comissão.";

    private static readonly string[] KnownParams =
        ["sub_id", "affiliate_id", "an_id", "tag", "matt_tool", "matt_word", "parceiro"];

    private static readonly string[] ShortenerHosts =
    [
        "br.shp.ee",
        "shp.ee",
        "s.shopee.com.br",
        "s.shopee.com",
        "amzn.to",
        "a.co",
        "magalu.me"
    ];

    public static string ExtractAffiliateIdFromUrl(string input, string platform)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var trimmed = input.Trim().Trim('"', '\'');
        var marketplace = ParsePlatform(platform);
        var keys = StartsWithHttp(trimmed)
            ? KeysFor(marketplace).Concat(KnownParams).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
            : KeysFor(marketplace);

        foreach (var key in keys)
        {
            if (!TryReadParam(trimmed, key, out var value))
            {
                continue;
            }

            if (marketplace is MarketplaceType.Shopee)
            {
                var digits = ExtractNumericSequence(value);
                if (!string.IsNullOrWhiteSpace(digits))
                {
                    return Truncate(digits);
                }
            }

            return Truncate(value);
        }

        if (marketplace is MarketplaceType.MagazineLuiza
            && TryParseAbsoluteUri(trimmed, out var uri)
            && TryMagazineVoceSlug(uri, out var slug))
        {
            return Truncate(slug);
        }

        return Truncate(trimmed);
    }

    public static bool TryNormalize(MarketplaceType platform, string? input, out string id)
    {
        id = string.Empty;
        if (string.IsNullOrWhiteSpace(input))
        {
            return true;
        }

        id = ExtractAffiliateIdFromUrl(input, platform.ToString());
        return !LooksLikeUrl(id);
    }

    public static bool LooksLikeUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        return StartsWithHttp(trimmed)
            || trimmed.Contains("://", StringComparison.Ordinal)
            || trimmed.Contains('/', StringComparison.Ordinal);
    }

    public static bool ContainsHttp(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Contains("http", StringComparison.OrdinalIgnoreCase);

    public static bool IsShortenerUrl(string? value)
    {
        if (!TryParseAbsoluteUri(value ?? string.Empty, out var uri))
        {
            return false;
        }

        var host = uri.Host.Trim().TrimStart('.').ToLowerInvariant();
        return ShortenerHosts.Any(candidate =>
            host == candidate || host.EndsWith("." + candidate, StringComparison.Ordinal));
    }

    public static MarketplaceType ParsePlatform(string? platform)
    {
        if (string.IsNullOrWhiteSpace(platform))
        {
            return MarketplaceType.Shopee;
        }

        if (Enum.TryParse<MarketplaceType>(platform.Replace(" ", string.Empty), ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        var normalized = platform.Trim();
        if (normalized.Contains("shopee", StringComparison.OrdinalIgnoreCase))
        {
            return MarketplaceType.Shopee;
        }

        if (normalized.Contains("amazon", StringComparison.OrdinalIgnoreCase))
        {
            return MarketplaceType.Amazon;
        }

        if (normalized.Contains("mercado", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("ml", StringComparison.OrdinalIgnoreCase))
        {
            return MarketplaceType.MercadoLivre;
        }

        if (normalized.Contains("magalu", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("magazine", StringComparison.OrdinalIgnoreCase))
        {
            return MarketplaceType.MagazineLuiza;
        }

        if (normalized.Contains("casas", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("bahia", StringComparison.OrdinalIgnoreCase))
        {
            return MarketplaceType.CasasBahia;
        }

        if (normalized.Contains("tiktok", StringComparison.OrdinalIgnoreCase))
        {
            return MarketplaceType.TikTokShop;
        }

        if (normalized.Contains("kabum", StringComparison.OrdinalIgnoreCase))
        {
            return MarketplaceType.Kabum;
        }

        return MarketplaceType.Shopee;
    }

    private static bool StartsWithHttp(string value) =>
        value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    private static string[] KeysFor(MarketplaceType platform) => platform switch
    {
        MarketplaceType.Amazon => ["tag"],
        MarketplaceType.Shopee => ["sub_id", "affiliate_id", "an_id"],
        MarketplaceType.TikTokShop => ["sub_id", "affiliate_id"],
        MarketplaceType.MercadoLivre => ["matt_tool", "matt_word"],
        MarketplaceType.MagazineLuiza => ["parceiro", "sub_id"],
        MarketplaceType.Kabum => ["sub_id"],
        MarketplaceType.CasasBahia => ["parceiro", "sub_id"],
        _ => KnownParams
    };

    private static bool TryReadParam(string input, string key, out string value)
    {
        value = string.Empty;
        var token = key + "=";
        var index = input.IndexOf(token, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return false;
        }

        var start = index + token.Length;
        var end = start;
        while (end < input.Length)
        {
            var current = input[end];
            if (current is '&' or '#' or '"' or '\'')
            {
                break;
            }

            end++;
        }

        if (end <= start)
        {
            return false;
        }

        value = Decode(input[start..end].Trim());
        return !string.IsNullOrWhiteSpace(value);
    }

    private static string ExtractNumericSequence(string value)
    {
        var start = -1;
        var length = 0;
        for (var i = 0; i < value.Length; i++)
        {
            if (char.IsDigit(value[i]))
            {
                if (start < 0)
                {
                    start = i;
                }

                length++;
                continue;
            }

            if (start >= 0)
            {
                break;
            }
        }

        return start < 0 ? string.Empty : value.Substring(start, length);
    }

    private static bool TryParseAbsoluteUri(string value, out Uri uri)
    {
        if (Uri.TryCreate(value.Trim(), UriKind.Absolute, out uri!)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return true;
        }

        uri = null!;
        return false;
    }

    private static bool TryMagazineVoceSlug(Uri uri, out string slug)
    {
        slug = string.Empty;
        if (!uri.Host.Contains("magazinevoce", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            return false;
        }

        slug = segments[0];
        return !string.Equals(slug, "p", StringComparison.OrdinalIgnoreCase)
            && slug.Length > 0;
    }

    private static string Decode(string value)
    {
        try
        {
            return Uri.UnescapeDataString(value.Replace('+', ' ')).Trim();
        }
        catch (UriFormatException)
        {
            return value.Trim();
        }
    }

    private static string Truncate(string value) =>
        value.Length <= MaxLength ? value : value[..MaxLength];
}
