using TecFlow.Core.Enums;

namespace TecFlow.SharedUi.Services.Integrations;

/// <summary>Extrai o ID de afiliado limpo quando o usuário cola uma URL ou query string.</summary>
public static class AffiliateTrackingIdSanitizer
{
    public const int MaxLength = 64;

    public static string Extract(MarketplaceType platform, string? raw) =>
        ExtractAffiliateIdFromUrl(raw ?? string.Empty, platform.ToString());

    /// <summary>
    /// Sanitiza o valor colado no campo de ID. Se for URL/query, extrai o parâmetro da plataforma;
    /// se for o próprio ID, devolve o texto sem alteração.
    /// </summary>
    public static string ExtractAffiliateIdFromUrl(string input, string platform)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var trimmed = input.Trim().Trim('"', '\'');
        var marketplace = ParsePlatform(platform);
        var keys = KeysFor(marketplace);

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

    private static MarketplaceType ParsePlatform(string? platform)
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

    private static string[] KeysFor(MarketplaceType platform) => platform switch
    {
        MarketplaceType.Amazon => ["tag"],
        MarketplaceType.Shopee => ["sub_id", "affiliate_id", "an_id"],
        MarketplaceType.TikTokShop => ["sub_id", "affiliate_id"],
        MarketplaceType.MercadoLivre => ["matt_tool", "matt_word"],
        MarketplaceType.MagazineLuiza => ["parceiro", "sub_id"],
        MarketplaceType.Kabum => ["sub_id"],
        MarketplaceType.CasasBahia => ["parceiro", "sub_id"],
        _ => ["tag", "matt_tool", "affiliate_id", "an_id", "sub_id", "parceiro", "tracking_id"]
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
        if (Uri.TryCreate(value, UriKind.Absolute, out uri!)
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
