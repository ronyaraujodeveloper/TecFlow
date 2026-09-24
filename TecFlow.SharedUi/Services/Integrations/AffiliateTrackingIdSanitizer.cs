using TecFlow.Core.Enums;

namespace TecFlow.SharedUi.Services.Integrations;

/// <summary>Extrai o ID de afiliado limpo quando o usuário cola uma URL ou query string.</summary>
public static class AffiliateTrackingIdSanitizer
{
    public const int MaxLength = 64;

    public static string Extract(MarketplaceType platform, string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var trimmed = raw.Trim().Trim('"', '\'');
        var keys = KeysFor(platform);

        if (TryParseAbsoluteUri(trimmed, out var uri))
        {
            var fromUrl = FirstQueryValue(uri.Query, keys)
                ?? FirstQueryValue(uri.Fragment.TrimStart('#'), keys);
            if (!string.IsNullOrWhiteSpace(fromUrl))
            {
                return Truncate(Decode(fromUrl));
            }

            if (platform is MarketplaceType.MagazineLuiza
                && TryMagazineVoceSlug(uri, out var slug))
            {
                return Truncate(slug);
            }
        }

        var queryLike = NormalizeQueryLike(trimmed);
        if (!string.IsNullOrWhiteSpace(queryLike))
        {
            var fromQuery = FirstQueryValue(queryLike, keys);
            if (!string.IsNullOrWhiteSpace(fromQuery))
            {
                return Truncate(Decode(fromQuery));
            }
        }

        return Truncate(trimmed);
    }

    private static string[] KeysFor(MarketplaceType platform) => platform switch
    {
        MarketplaceType.Amazon => ["tag", "associateTag"],
        MarketplaceType.Shopee => ["affiliate_id", "sub_id", "tracking_code"],
        MarketplaceType.TikTokShop => ["sub_id", "affiliate_id"],
        MarketplaceType.MercadoLivre => ["matt_tool"],
        MarketplaceType.MagazineLuiza => ["parceiro"],
        MarketplaceType.Kabum => ["sub_id"],
        MarketplaceType.CasasBahia => ["parceiro", "sub_id"],
        _ => ["tag", "matt_tool", "affiliate_id", "sub_id", "parceiro", "tracking_id"]
    };

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

    private static string? FirstQueryValue(string? query, IReadOnlyList<string> keys)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        var normalized = query.StartsWith('?') || query.StartsWith('#')
            ? query[1..]
            : query;

        var map = ParseQuery(normalized);
        foreach (var key in keys)
        {
            if (map.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static string? NormalizeQueryLike(string value)
    {
        var start = value.IndexOf('?');
        if (start >= 0 && start < value.Length - 1)
        {
            return value[(start + 1)..];
        }

        return value.Contains('=') ? value : null;
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

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = part.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var key = Decode(part[..separator]);
            var value = Decode(part[(separator + 1)..]);
            if (!string.IsNullOrWhiteSpace(key) && !map.ContainsKey(key))
            {
                map[key] = value;
            }
        }

        return map;
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
