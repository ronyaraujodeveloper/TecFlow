using System.Globalization;
using System.Text;

namespace Ofertio.Util.Text;

/// <summary>Sanitiza nomes amigáveis de loja para segmentos de URL (sem @, acentos nem espaços).</summary>
public static class SlugHelper
{
    private static readonly HashSet<string> ReservedSlugs = new(StringComparer.OrdinalIgnoreCase)
    {
        "r", "api", "swagger", "metrics", "health", "dashboard", "hangfire"
    };

    public static string GenerateSlug(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "loja";
        }

        var withoutAt = name.Replace("@", string.Empty, StringComparison.Ordinal);
        var builder = new StringBuilder(withoutAt.Length);
        var capitalizeNext = true;

        foreach (var ch in withoutAt.Normalize(NormalizationForm.FormD))
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsWhiteSpace(ch) || ch is '-' or '_' or '.')
            {
                capitalizeNext = true;
                continue;
            }

            if (!char.IsAsciiLetterOrDigit(ch))
            {
                continue;
            }

            if (char.IsLetter(ch) && capitalizeNext)
            {
                builder.Append(char.ToUpperInvariant(ch));
                capitalizeNext = false;
                continue;
            }

            builder.Append(ch);
            capitalizeNext = false;
        }

        var slug = builder.ToString();
        if (string.IsNullOrWhiteSpace(slug))
        {
            return "loja";
        }

        if (ReservedSlugs.Contains(slug))
        {
            slug = $"loja{slug}";
        }

        return slug.Length <= 80 ? slug : slug[..80];
    }
}

/// <summary>Monta a URL pública de rastreio Ofertio: {base}/{storeSlug}/{code}.</summary>
public static class ShortLinkPublicUrl
{
    public const string DefaultHost = "http://localhost:5001";

    public static string NormalizeHost(string? publicBaseUrl)
    {
        var configured = (publicBaseUrl ?? string.Empty).Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(configured))
        {
            return DefaultHost;
        }

        if (configured.EndsWith("/r", StringComparison.OrdinalIgnoreCase))
        {
            configured = configured[..^2].TrimEnd('/');
        }

        return string.IsNullOrWhiteSpace(configured) ? DefaultHost : configured;
    }

    public static string Build(string? publicBaseUrl, string? friendlyName, string code)
    {
        var slug = SlugHelper.GenerateSlug(friendlyName);
        return $"{NormalizeHost(publicBaseUrl)}/{slug}/{code}";
    }

    /// <summary>
    /// Troca o host interno (loopback, ex.: http://localhost:5001) do link de rastreio pela URL base do ambiente
    /// em que a tela está aberta ({base}/{storeSlug}/{code}). Um domínio de encurtador configurado
    /// (ex.: https://tflow.link) é mantido, assim como bases sem endereço público (o WebView do app usa https://0.0.0.x).
    /// </summary>
    public static string RebaseToCurrentHost(string? shortUrl, string? currentBaseUrl)
    {
        var link = shortUrl?.Trim() ?? string.Empty;
        if (!Uri.TryCreate(link, UriKind.Absolute, out var linkUri) || !linkUri.IsLoopback)
        {
            return link;
        }

        if (!Uri.TryCreate(currentBaseUrl?.Trim(), UriKind.Absolute, out var baseUri)
            || (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps)
            || IsUnroutableHost(baseUri.Host))
        {
            return link;
        }

        return baseUri.GetLeftPart(UriPartial.Path).TrimEnd('/') + linkUri.PathAndQuery + linkUri.Fragment;
    }

    private static bool IsUnroutableHost(string host) =>
        System.Net.IPAddress.TryParse(host, out var address)
        && address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
        && address.GetAddressBytes()[0] == 0;
}
