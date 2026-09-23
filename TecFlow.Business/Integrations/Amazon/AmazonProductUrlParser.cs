using System.Text.RegularExpressions;
using TecFlow.Business.Service.LinkStrategies;

namespace TecFlow.Business.Integrations.Amazon;

/// <summary>Extrai o ASIN de URLs canônicas e encurtadas da Amazon.</summary>
public static class AmazonProductUrlParser
{
    public const string UnrecognizedLinkMessage =
        "Não reconhecemos este link da Amazon. Use a URL do produto (/dp/ASIN) ou o encurtador amzn.to / a.co.";

    public const string HomologAsin = "B08N5WRWNW";

    private static readonly Regex PathAsinRegex = new(
        @"/(?:dp|gp/product|gp/aw/d|product)/(?<asin>[A-Z0-9]{10})(?:[/?#]|$)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex QueryAsinRegex = new(
        @"[?&](?:asin|ASIN)=(?<asin>[A-Z0-9]{10})(?:[&#]|$)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex BareAsinRegex = new(
        @"\b(?<asin>(?:B[A-Z0-9]{9}|\d{10}))\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static bool TryParse(string? url, out string asin)
    {
        asin = string.Empty;
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        var sanitized = Sanitize(url);
        var path = PathAsinRegex.Match(sanitized);
        if (path.Success && IsValidAsin(path.Groups["asin"].Value, out asin))
        {
            return true;
        }

        var query = QueryAsinRegex.Match(sanitized);
        if (query.Success && IsValidAsin(query.Groups["asin"].Value, out asin))
        {
            return true;
        }

        var bare = BareAsinRegex.Match(sanitized);
        if (bare.Success && IsValidAsin(bare.Groups["asin"].Value, out asin))
        {
            return true;
        }

        return false;
    }

    public static string ParseOrThrow(string? url, bool allowHomologFallback)
    {
        if (TryParse(url, out var asin))
        {
            return asin;
        }

        if (allowHomologFallback)
        {
            return HomologAsin;
        }

        throw new AffiliateLinkGenerationException(UnrecognizedLinkMessage);
    }

    public static bool IsShortUrl(string? url)
    {
        if (!Uri.TryCreate(Sanitize(url), UriKind.Absolute, out var uri))
        {
            return false;
        }

        var host = uri.Host.ToLowerInvariant();
        return host is "amzn.to" or "www.amzn.to" or "a.co" or "www.a.co"
            || host.EndsWith(".amzn.to", StringComparison.Ordinal);
    }

    public static string Sanitize(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return string.Empty;
        }

        var trimmed = url.Trim();
        if (!trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return "https://" + trimmed;
        }

        return trimmed;
    }

    public static bool IsValidAsin(string? raw, out string asin)
    {
        asin = (raw ?? string.Empty).Trim().ToUpperInvariant();
        if (asin.Length != 10 || !asin.All(char.IsLetterOrDigit))
        {
            asin = string.Empty;
            return false;
        }

        if (asin[0] is not ('B' or >= '0' and <= '9'))
        {
            asin = string.Empty;
            return false;
        }

        return true;
    }
}
