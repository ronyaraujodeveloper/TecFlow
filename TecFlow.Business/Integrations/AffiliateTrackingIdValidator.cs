using System.Text.RegularExpressions;
using TecFlow.Core.Enums;

namespace TecFlow.Business.Integrations;

/// <summary>Extração e validação estrita de Tracking ID / ID de afiliado.</summary>
public static class AffiliateTrackingIdValidator
{
    public const int MaxLength = 64;

    public const string InvalidMessage =
        "O valor informado não é um ID de afiliado válido. Informe o seu ID numérico/tag ou cole um link longo de comissão.";

    public static string DuplicateTrackingIdMessage(MarketplaceType platform) =>
        $"⚠️ Este ID de Afiliado já está cadastrado no sistema para a plataforma {platform.GetDisplayName()}. Não é permitido duplicar credenciais.";

    public static string ExtractedFromShortLinkMessage(string id) =>
        $"✅ ID de Afiliado {id} extraído com sucesso a partir do link encurtado!";

    private static readonly string[] KnownParams =
        ["sub_id", "affiliate_id", "an_id", "mmp_pid", "utm_source", "tag", "matt_tool", "matt_word", "parceiro"];

    private static readonly string[] ShortenerHosts =
    [
        "br.shp.ee",
        "shp.ee",
        "shope.ee",
        "s.shopee.com.br",
        "s.shopee.com",
        "amzn.to",
        "a.co",
        "magalu.me"
    ];

    private static readonly Regex[] ShopeeAffiliateIdPatterns =
    [
        new(@"mmp_pid=an_([0-9]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled),
        new(@"utm_source=an_([0-9]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled),
        new(@"(?:affiliate_id=|an_id=)([0-9]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled),
        new(@"sub_id=([0-9]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled)
    ];

    public static string ExtractAffiliateIdFromUrl(string input, string platform)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var trimmed = input.Trim().Trim('"', '\'');
        var marketplace = ParsePlatform(platform);

        if (marketplace is MarketplaceType.Shopee
            && TryExtractShopeeAffiliateId(trimmed, out var shopeeId))
        {
            return Truncate(shopeeId);
        }

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

                continue;
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

    public static bool TryExtractShopeeAffiliateId(string? input, out string id)
    {
        id = string.Empty;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        foreach (var pattern in ShopeeAffiliateIdPatterns)
        {
            var match = pattern.Match(input);
            if (!match.Success || match.Groups.Count < 2)
            {
                continue;
            }

            id = match.Groups[1].Value;
            if (!string.IsNullOrWhiteSpace(id))
            {
                return true;
            }
        }

        return false;
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
        foreach (var candidate in AbsoluteUrlCandidates(value))
        {
            if (!TryParseAbsoluteUri(candidate, out var uri))
            {
                continue;
            }

            var host = uri.Host.Trim().TrimStart('.').ToLowerInvariant();
            if (host.StartsWith("www.", StringComparison.Ordinal))
            {
                host = host[4..];
            }

            if (ShortenerHosts.Any(item =>
                    host == item || host.EndsWith("." + item, StringComparison.Ordinal)))
            {
                return true;
            }
        }

        return false;
    }

    public static string EnsureAbsoluteHttpUrl(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim().Trim('"', '\'');
        if (string.IsNullOrWhiteSpace(trimmed) || StartsWithHttp(trimmed))
        {
            return trimmed;
        }

        if (trimmed.Contains('.', StringComparison.Ordinal) && !trimmed.Contains(' ', StringComparison.Ordinal))
        {
            return "https://" + trimmed.TrimStart('/');
        }

        return trimmed;
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
        MarketplaceType.Shopee => ["mmp_pid", "utm_source", "sub_id", "affiliate_id", "an_id"],
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

    private static IEnumerable<string> AbsoluteUrlCandidates(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim().Trim('"', '\'');
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            yield break;
        }

        yield return trimmed;
        var ensured = EnsureAbsoluteHttpUrl(trimmed);
        if (!string.Equals(ensured, trimmed, StringComparison.Ordinal))
        {
            yield return ensured;
        }
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
