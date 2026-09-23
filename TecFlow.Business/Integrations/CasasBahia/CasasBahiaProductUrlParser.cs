using System.Text.RegularExpressions;
using TecFlow.Business.Service.LinkStrategies;

namespace TecFlow.Business.Integrations.CasasBahia;

/// <summary>Extrai o ID numérico do produto Casas Bahia após /p/ ou imediatamente antes de /p.</summary>
public static class CasasBahiaProductUrlParser
{
    public const string UnrecognizedLinkMessage =
        "Não reconhecemos este link das Casas Bahia. Use a URL do produto (/p/{id}) ou o encurtador cb.com.br.";

    public const string HomologProductId = "55014612";

    private static readonly Regex AfterPRegex = new(
        @"/p/(?<id>\d{6,14})(?:/|$|\?|#)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex BeforePRegex = new(
        @"/(?<id>\d{6,14})/p(?:/|$|\?|#)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex QueryProductRegex = new(
        @"[?&](?:productId|product_id|idsku|sku)=(?<id>\d{6,14})(?:[&#]|$)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static bool TryParse(string? url, out string productId)
    {
        productId = string.Empty;
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        var sanitized = Sanitize(url);
        var after = AfterPRegex.Match(sanitized);
        if (after.Success)
        {
            productId = after.Groups["id"].Value;
            return true;
        }

        var before = BeforePRegex.Match(sanitized);
        if (before.Success)
        {
            productId = before.Groups["id"].Value;
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
        return host is "cb.com.br" or "www.cb.com.br"
            || host.Equals("casasbahia.app.link", StringComparison.Ordinal)
            || host.EndsWith(".cb.com.br", StringComparison.Ordinal)
            || host.EndsWith(".casasbahia.app.link", StringComparison.Ordinal);
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
