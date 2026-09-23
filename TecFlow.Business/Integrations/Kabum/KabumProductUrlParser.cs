using System.Text.RegularExpressions;
using TecFlow.Business.Service.LinkStrategies;

namespace TecFlow.Business.Integrations.Kabum;

/// <summary>Extrai o ID numérico do produto Kabum após /produto/.</summary>
public static class KabumProductUrlParser
{
    public const string UnrecognizedLinkMessage =
        "Não reconhecemos este link da Kabum!. Use a URL do produto (/produto/{id}) ou o encurtador kb.um.";

    public const string HomologProductId = "123456";

    private static readonly Regex PathProductRegex = new(
        @"/produto/(?<id>\d{4,12})(?:/|$|\?|#)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex QueryProductRegex = new(
        @"[?&](?:productId|product_id|codigo)=(?<id>\d{4,12})(?:[&#]|$)",
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
        return host is "kb.um" or "www.kb.um" or "kabum.me" or "www.kabum.me"
            || host.EndsWith(".kb.um", StringComparison.Ordinal)
            || host.EndsWith(".kabum.me", StringComparison.Ordinal);
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
