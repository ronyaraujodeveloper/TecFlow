using System.Text.RegularExpressions;
using TecFlow.Business.Service.LinkStrategies;

namespace TecFlow.Business.Integrations.MagazineLuiza;

/// <summary>Extrai o código do produto Magalu após /p/ em URLs canônicas e Magazine Você.</summary>
public static class MagazineLuizaProductUrlParser
{
    public const string UnrecognizedLinkMessage =
        "Não reconhecemos este link do Magazine Luiza. Use a URL do produto (/p/{id}) ou o encurtador magalu.me.";

    public const string HomologProductId = "218434100";

    private static readonly Regex PathProductRegex = new(
        @"/p/(?<id>[A-Za-z0-9]{5,24})(?:/|$|\?|#)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex QueryProductRegex = new(
        @"[?&](?:productId|product_id|sku|codigo)=(?<id>[A-Za-z0-9]{5,24})(?:[&#]|$)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static bool TryParse(string? url, out string productId)
    {
        productId = string.Empty;
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        var sanitized = Sanitize(url);
        var path = PathProductRegex.Match(sanitized);
        if (path.Success)
        {
            productId = path.Groups["id"].Value;
            return true;
        }

        var query = QueryProductRegex.Match(sanitized);
        if (query.Success)
        {
            productId = query.Groups["id"].Value;
            return true;
        }

        return false;
    }

    public static string ParseOrThrow(string? url, bool allowHomologFallback)
    {
        if (TryParse(url, out var productId))
        {
            return productId;
        }

        if (allowHomologFallback)
        {
            return HomologProductId;
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
        return host is "magalu.me" or "www.magalu.me" or "mglz.ne" or "www.mglz.ne"
            || host is "magazineluiza.onelink.me"
            || host.EndsWith(".magalu.me", StringComparison.Ordinal)
            || host.EndsWith(".mglz.ne", StringComparison.Ordinal)
            || host.EndsWith(".onelink.me", StringComparison.Ordinal);
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
}
