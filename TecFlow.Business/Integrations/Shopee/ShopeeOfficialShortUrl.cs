using TecFlow.Business.Service.LinkStrategies;

namespace TecFlow.Business.Integrations.Shopee;

/// <summary>Identifica e monta o encurtador oficial da Shopee (br.shp.ee / shp.ee).</summary>
public static class ShopeeOfficialShortUrl
{
    public const string Host = "br.shp.ee";

    public static bool IsOfficialShortener(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        var host = uri.Host.Trim().TrimStart('.').ToLowerInvariant();
        if (host.StartsWith("www.", StringComparison.Ordinal))
        {
            host = host[4..];
        }

        return host is "br.shp.ee" or "shp.ee" or "s.shopee.com.br" or "s.shopee.com";
    }

    public static string Sanitize(string url)
    {
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            return url.Trim();
        }

        var path = uri.AbsolutePath.Trim('/');
        var first = path.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
        var host = uri.Host.Equals("s.shopee.com.br", StringComparison.OrdinalIgnoreCase)
            || uri.Host.Equals("s.shopee.com", StringComparison.OrdinalIgnoreCase)
            ? "br.shp.ee"
            : NormalizeHost(uri.Host);

        return string.IsNullOrWhiteSpace(first)
            ? $"https://{host}/"
            : $"https://{host}/{first}";
    }

    public static string Resolve(
        string? apiOrGeneratedUrl,
        string originalUrl,
        ShopeeProductUrlIds productIds)
    {
        if (IsOfficialShortener(apiOrGeneratedUrl))
        {
            return Sanitize(apiOrGeneratedUrl!);
        }

        if (IsOfficialShortener(originalUrl))
        {
            return Sanitize(originalUrl);
        }

        return BuildFromProductIds(productIds);
    }

    public static string BuildFromProductIds(ShopeeProductUrlIds productIds) =>
        $"https://{Host}/i{productIds.ShopId}x{productIds.ItemId}";

    private static string NormalizeHost(string host)
    {
        var normalized = host.TrimEnd('.').ToLowerInvariant();
        return normalized.StartsWith("www.", StringComparison.Ordinal) ? normalized[4..] : normalized;
    }
}
