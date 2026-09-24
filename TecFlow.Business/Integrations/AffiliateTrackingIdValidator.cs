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

    public static string ExtractedCredentialMessage(string id, MarketplaceType platform) =>
        $"✅ Credencial {id} extraída com sucesso para {platform.GetDisplayName()}!";

    public static string ExtractedFromShortLinkMessage(string id) =>
        ExtractedCredentialMessage(id, MarketplaceType.Shopee);

    private static readonly string[] KnownParams =
        ["sub_id", "affiliate_id", "an_id", "mmp_pid", "utm_source", "utm_campaign", "promoter_id", "tag", "matt_tool", "matt_word", "parceiro", "tt_from", "unique_id", "user_id", "sec_user_id", "sec_uid", "penn", "penn_id", "afiliado"];

    private static readonly string[] ShortenerHosts =
    [
        "br.shp.ee",
        "shp.ee",
        "shope.ee",
        "s.shopee.com.br",
        "s.shopee.com",
        "amzn.to",
        "a.co",
        "magalu.me",
        "magazineluiza.onelink.me",
        "magazinevoce.com.br",
        "vt.tiktok.com",
        "vm.tiktok.com",
        "meli.la"
    ];

    private static readonly (string Host, string PathPrefix)[] ShortenerPathPrefixes =
    [
        ("mercadolivre.com", "/sec/"),
        ("mercadolivre.com.br", "/sec/")
    ];

    private static readonly (string Host, MarketplaceType Platform)[] DetectionHosts =
    [
        ("vt.tiktok.com", MarketplaceType.TikTokShop),
        ("vm.tiktok.com", MarketplaceType.TikTokShop),
        ("l.tiktok.com", MarketplaceType.TikTokShop),
        ("shop.tiktok.com", MarketplaceType.TikTokShop),
        ("tiktokshop.com", MarketplaceType.TikTokShop),
        ("tiktok.com", MarketplaceType.TikTokShop),
        ("magazineluiza.onelink.me", MarketplaceType.MagazineLuiza),
        ("magazinevoce.com.br", MarketplaceType.MagazineLuiza),
        ("magazineluiza.com.br", MarketplaceType.MagazineLuiza),
        ("magalu.com.br", MarketplaceType.MagazineLuiza),
        ("magalu.me", MarketplaceType.MagazineLuiza),
        ("mglz.ne", MarketplaceType.MagazineLuiza),
        ("meli.la", MarketplaceType.MercadoLivre),
        ("mercadolivre.com.br", MarketplaceType.MercadoLivre),
        ("produto.mercadolivre.com.br", MarketplaceType.MercadoLivre),
        ("mercadolivre.com", MarketplaceType.MercadoLivre),
        ("mercadolibre.com", MarketplaceType.MercadoLivre),
        ("ml.com.br", MarketplaceType.MercadoLivre),
        ("s.shopee.com.br", MarketplaceType.Shopee),
        ("s.shopee.com", MarketplaceType.Shopee),
        ("br.shp.ee", MarketplaceType.Shopee),
        ("shp.ee", MarketplaceType.Shopee),
        ("shope.ee", MarketplaceType.Shopee),
        ("shopee.com.br", MarketplaceType.Shopee),
        ("shopee.com", MarketplaceType.Shopee),
        ("amzn.to", MarketplaceType.Amazon),
        ("a.co", MarketplaceType.Amazon),
        ("amazon.com.br", MarketplaceType.Amazon),
        ("amazon.com", MarketplaceType.Amazon),
        ("cb.com.br", MarketplaceType.CasasBahia),
        ("casasbahia.com.br", MarketplaceType.CasasBahia),
        ("casasbahia.app.link", MarketplaceType.CasasBahia),
        ("kb.um", MarketplaceType.Kabum),
        ("kabum.me", MarketplaceType.Kabum),
        ("kabum.com.br", MarketplaceType.Kabum)
    ];

    private static readonly Regex[] ShopeeAffiliateIdPatterns =
    [
        new(@"mmp_pid=an_([0-9]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled),
        new(@"utm_source=an_([0-9]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled),
        new(@"(?:affiliate_id=|an_id=)([0-9]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled),
        new(@"sub_id=([0-9]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled)
    ];

    private static readonly Regex TikTokUniqueIdRegex = new(
        @"(?:^|[?&#])unique_id=@?(?<id>[^&#]+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex TikTokUserIdRegex = new(
        @"(?:^|[?&#])user_id=(?<id>[0-9]+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex TikTokSecUserIdRegex = new(
        @"(?:^|[?&#])sec_user_id=(?<id>[^&#]+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex TikTokBareHandleRegex = new(
        @"^@?(?<id>[A-Za-z0-9._]{3,64})$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static string ExtractAffiliateIdFromUrl(string input, string platform)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var trimmed = input.Trim().Trim('"', '\'');
        var marketplace = ParsePlatform(platform);
        if (marketplace is MarketplaceType.TikTokShop)
        {
            trimmed = UnwrapTikTokLoginRedirect(trimmed);
        }

        if (TryExtractPlatformAffiliateId(marketplace, trimmed, out var extractedId)
            && !IsBooleanLiteral(extractedId))
        {
            return Truncate(extractedId);
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

            if (IsBooleanLiteral(value) || IsIgnoredMagaluUtmSource(marketplace, key, value))
            {
                continue;
            }

            if (marketplace is MarketplaceType.MagazineLuiza
                && string.Equals(key, "utm_campaign", StringComparison.OrdinalIgnoreCase)
                && !IsNumericAffiliateId(value))
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

        return IsBooleanLiteral(trimmed) ? string.Empty : Truncate(trimmed);
    }

    public static bool TryDetectPlatformFromUrl(string? input, out MarketplaceType platform)
    {
        platform = MarketplaceType.Shopee;
        MarketplaceType? best = null;
        var bestLength = -1;

        foreach (var candidate in AbsoluteUrlCandidates(input))
        {
            if (!TryParseAbsoluteUri(candidate, out var uri))
            {
                continue;
            }

            var host = NormalizeHost(uri.Host);
            foreach (var (pattern, detected) in DetectionHosts)
            {
                if (host != pattern && !host.EndsWith("." + pattern, StringComparison.Ordinal))
                {
                    continue;
                }

                if (pattern.Length <= bestLength)
                {
                    continue;
                }

                bestLength = pattern.Length;
                best = detected;
            }
        }

        if (best is null)
        {
            return false;
        }

        platform = best.Value;
        return true;
    }

    public static bool IsBooleanLiteral(string? value) =>
        string.Equals(value?.Trim(), "true", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value?.Trim(), "false", StringComparison.OrdinalIgnoreCase);

    public static string PreferExtractedCredential(string? extractedId, string? expandedUrl, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(extractedId)
            && !IsBooleanLiteral(extractedId)
            && !LooksLikeUrl(extractedId))
        {
            return extractedId.Trim();
        }

        if (!string.IsNullOrWhiteSpace(expandedUrl) && !IsBooleanLiteral(expandedUrl))
        {
            return expandedUrl.Trim();
        }

        return fallback ?? string.Empty;
    }

    public static string UnwrapTikTokLoginRedirect(string? url)
    {
        var current = (url ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(current))
        {
            return string.Empty;
        }

        for (var hop = 0; hop < 8; hop++)
        {
            if (!IsTikTokLoginRedirect(current, out var encodedRedirect))
            {
                return current;
            }

            var decoded = UrlDecodeRecursive(encodedRedirect);
            if (string.IsNullOrWhiteSpace(decoded)
                || string.Equals(decoded, current, StringComparison.OrdinalIgnoreCase))
            {
                return current;
            }

            current = decoded.Trim().Trim('"', '\'');
        }

        return current;
    }

    public static bool TryExtractPlatformAffiliateId(MarketplaceType platform, string? input, out string id) =>
        platform switch
        {
            MarketplaceType.Shopee => TryExtractShopeeAffiliateId(input, out id),
            MarketplaceType.TikTokShop => TryExtractTikTokAffiliateId(input, out id),
            MarketplaceType.MagazineLuiza => TryExtractMagazineLuizaAffiliateId(input, out id),
            MarketplaceType.MercadoLivre => TryExtractMercadoLivreAffiliateId(input, out id),
            _ => TryExtractByKeys(platform, input, out id)
        };

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

            var host = NormalizeHost(uri.Host);

            if (ShortenerHosts.Any(item =>
                    host == item || host.EndsWith("." + item, StringComparison.Ordinal)))
            {
                return true;
            }

            var path = uri.AbsolutePath ?? string.Empty;
            if (ShortenerPathPrefixes.Any(item =>
                    (host == item.Host || host.EndsWith("." + item.Host, StringComparison.Ordinal))
                    && path.StartsWith(item.PathPrefix, StringComparison.OrdinalIgnoreCase)))
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
        MarketplaceType.TikTokShop => ["unique_id", "user_id", "sec_user_id", "sec_uid", "tt_from", "sub_id", "affiliate_id"],
        MarketplaceType.MercadoLivre => ["matt_tool", "matt_word", "penn", "penn_id"],
        MarketplaceType.MagazineLuiza => ["promoter_id", "utm_campaign", "parceiro", "afiliado", "p", "sub_id"],
        MarketplaceType.Kabum => ["sub_id"],
        MarketplaceType.CasasBahia => ["parceiro", "sub_id"],
        _ => KnownParams
    };

    private static bool TryReadParam(string input, string key, out string value)
    {
        value = string.Empty;
        var token = key + "=";
        var searchFrom = 0;
        while (searchFrom < input.Length)
        {
            var index = input.IndexOf(token, searchFrom, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                return false;
            }

            if (index > 0)
            {
                var previous = input[index - 1];
                if (previous is not ('?' or '&' or '#'))
                {
                    searchFrom = index + 1;
                    continue;
                }
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
                searchFrom = index + 1;
                continue;
            }

            value = Decode(input[start..end].Trim());
            if (string.IsNullOrWhiteSpace(value) || IsBooleanLiteral(value))
            {
                searchFrom = index + 1;
                continue;
            }

            return true;
        }

        return false;
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

    private static bool TryExtractByKeys(MarketplaceType platform, string? input, out string id)
    {
        id = string.Empty;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        foreach (var key in KeysFor(platform))
        {
            if (!TryReadParam(input, key, out var value))
            {
                continue;
            }

            if (IsBooleanLiteral(value) || IsIgnoredMagaluUtmSource(platform, key, value))
            {
                continue;
            }

            if (platform is MarketplaceType.MagazineLuiza
                && string.Equals(key, "utm_campaign", StringComparison.OrdinalIgnoreCase)
                && !IsNumericAffiliateId(value))
            {
                continue;
            }

            id = Truncate(value);
            return true;
        }

        return false;
    }

    private static bool TryExtractTikTokAffiliateId(string? input, out string id)
    {
        id = string.Empty;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        input = UnwrapTikTokLoginRedirect(input);

        if (TryMatchTikTokRegex(TikTokUniqueIdRegex, input, out id)
            || TryReadTikTokCreatorParam(input, "unique_id", out id))
        {
            return true;
        }

        if (TryMatchTikTokRegex(TikTokUserIdRegex, input, out id)
            || TryReadTikTokCreatorParam(input, "user_id", out id))
        {
            return true;
        }

        if (TryMatchTikTokRegex(TikTokSecUserIdRegex, input, out id)
            || TryReadTikTokCreatorParam(input, "sec_user_id", out id)
            || TryReadTikTokCreatorParam(input, "sec_uid", out id))
        {
            return true;
        }

        var handle = Regex.Match(
            input,
            @"tiktok\.com/@([A-Za-z0-9._]+)|/@([A-Za-z0-9._]+)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (handle.Success)
        {
            id = NormalizeTikTokHandle(handle.Groups[1].Success ? handle.Groups[1].Value : handle.Groups[2].Value);
            if (!string.IsNullOrWhiteSpace(id) && !IsBooleanLiteral(id))
            {
                return true;
            }
        }

        if (TryReadTikTokCreatorParam(input, "tt_from", out id))
        {
            return true;
        }

        if (TryExtractTikTokBareHandle(input, out id))
        {
            return true;
        }

        return TryExtractByKeys(MarketplaceType.TikTokShop, input, out id);
    }

    private static bool TryExtractTikTokBareHandle(string input, out string id)
    {
        id = string.Empty;
        var trimmed = input.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)
            || StartsWithHttp(trimmed)
            || trimmed.Contains('/', StringComparison.Ordinal)
            || trimmed.Contains('?', StringComparison.Ordinal)
            || trimmed.Contains('=', StringComparison.Ordinal)
            || IsBooleanLiteral(trimmed))
        {
            return false;
        }

        var match = TikTokBareHandleRegex.Match(trimmed);
        if (!match.Success)
        {
            return false;
        }

        id = NormalizeTikTokHandle(match.Groups["id"].Value);
        return !string.IsNullOrWhiteSpace(id);
    }

    private static bool IsTikTokLoginRedirect(string url, out string encodedRedirect)
    {
        encodedRedirect = string.Empty;
        foreach (var candidate in AbsoluteUrlCandidates(url))
        {
            if (!TryParseAbsoluteUri(candidate, out var uri))
            {
                continue;
            }

            var host = NormalizeHost(uri.Host);
            if (!host.Contains("tiktok.com", StringComparison.Ordinal))
            {
                continue;
            }

            var path = uri.AbsolutePath ?? string.Empty;
            if (!path.Contains("/login", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (TryReadParam(candidate, "redirect_url", out var value)
                || TryReadParam(uri.Query.TrimStart('?'), "redirect_url", out value))
            {
                encodedRedirect = value;
                return !string.IsNullOrWhiteSpace(encodedRedirect);
            }
        }

        return false;
    }

    private static string UrlDecodeRecursive(string value)
    {
        var current = value ?? string.Empty;
        for (var hop = 0; hop < 8; hop++)
        {
            var decoded = System.Net.WebUtility.UrlDecode(current) ?? current;
            if (string.Equals(decoded, current, StringComparison.Ordinal))
            {
                return decoded;
            }

            current = decoded;
        }

        return current;
    }

    private static bool TryMatchTikTokRegex(Regex pattern, string input, out string id)
    {
        id = string.Empty;
        var match = pattern.Match(input);
        if (!match.Success)
        {
            return false;
        }

        id = NormalizeTikTokHandle(Decode(match.Groups["id"].Value));
        return !string.IsNullOrWhiteSpace(id) && !IsBooleanLiteral(id);
    }

    private static bool TryReadTikTokCreatorParam(string input, string key, out string id)
    {
        id = string.Empty;
        if (!TryReadParam(input, key, out var value) || IsBooleanLiteral(value))
        {
            return false;
        }

        id = NormalizeTikTokHandle(value);
        return !string.IsNullOrWhiteSpace(id);
    }

    private static string NormalizeTikTokHandle(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        return trimmed.StartsWith('@') ? trimmed[1..] : trimmed;
    }

    private static bool TryExtractMagazineLuizaAffiliateId(string? input, out string id)
    {
        id = string.Empty;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        if (TryReadNumericParam(input, "promoter_id", out id))
        {
            return true;
        }

        if (TryReadNumericParam(input, "utm_campaign", out id))
        {
            return true;
        }

        if (TryParseAbsoluteUri(EnsureAbsoluteHttpUrl(input), out var uri)
            && TryMagazineVoceSlug(uri, out var slug))
        {
            id = slug;
            return true;
        }

        foreach (var key in new[] { "parceiro", "afiliado", "p" })
        {
            if (!TryReadParam(input, key, out var value))
            {
                continue;
            }

            if (IsBooleanLiteral(value) || IsIgnoredMagaluUtmSource(MarketplaceType.MagazineLuiza, key, value))
            {
                continue;
            }

            id = value;
            return true;
        }

        return TryExtractByKeys(MarketplaceType.MagazineLuiza, input, out id);
    }

    private static bool TryReadNumericParam(string input, string key, out string id)
    {
        id = string.Empty;
        if (!TryReadParam(input, key, out var value))
        {
            return false;
        }

        var digits = ExtractNumericSequence(value);
        if (!IsNumericAffiliateId(digits))
        {
            return false;
        }

        id = digits;
        return true;
    }

    private static bool IsNumericAffiliateId(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.All(char.IsDigit);

    private static bool IsIgnoredMagaluUtmSource(MarketplaceType platform, string key, string value)
    {
        if (platform is not MarketplaceType.MagazineLuiza
            || !string.Equals(key, "utm_source", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return string.Equals(value, "divulgador", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "magalu", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryExtractMercadoLivreAffiliateId(string? input, out string id)
    {
        id = string.Empty;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        foreach (var key in new[] { "matt_tool", "matt_word", "penn", "penn_id" })
        {
            if (!TryReadParam(input, key, out var value))
            {
                continue;
            }

            if (IsBooleanLiteral(value))
            {
                continue;
            }

            id = value;
            return true;
        }

        return false;
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

    private static string NormalizeHost(string host)
    {
        var normalized = (host ?? string.Empty).Trim().TrimStart('.').ToLowerInvariant();
        if (normalized.StartsWith("www.", StringComparison.Ordinal))
        {
            normalized = normalized[4..];
        }

        return normalized;
    }
}
