using System.Net;
using System.Text.RegularExpressions;
using Ofertio.Core.Enums;

namespace Ofertio.Business.Service.Legal;

/// <summary>
/// Versão, aceite e higiene do HTML dos documentos legais.
/// A versão 1.0 é o texto que acompanha o sistema. Publicar no Master gera a próxima (1.1, 1.2, …).
/// </summary>
public static partial class TermsDocumentRules
{
    public const string Terms = "Terms";
    public const string Privacy = "Privacy";
    public const string PurposeAccount = "Account";
    public const string PurposeExternalApi = "ExternalApi";
    public const string StaticVersion = "1.0";
    public const int MaxContentLength = 200_000;
    public const int MaxIpLength = 64;
    public const string AcceptanceRequiredMessage =
        "É necessário aceitar os Termos de Uso e a Política de Privacidade.";

    private static readonly HashSet<string> AllowedTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "p", "br", "strong", "b", "em", "i", "u", "h1", "h2", "h3",
        "ul", "ol", "li", "a", "blockquote", "span", "div", "hr"
    };

    public static string? NormalizeKind(string? kind)
    {
        if (string.Equals(kind?.Trim(), Terms, StringComparison.OrdinalIgnoreCase))
        {
            return Terms;
        }

        if (string.Equals(kind?.Trim(), Privacy, StringComparison.OrdinalIgnoreCase))
        {
            return Privacy;
        }

        return null;
    }

    public static string NormalizePurpose(string? purpose) =>
        string.Equals(purpose?.Trim(), PurposeExternalApi, StringComparison.OrdinalIgnoreCase)
            ? PurposeExternalApi
            : PurposeAccount;

    /// <summary>Versão que o usuário precisa ter aceito. Sem publicação no banco, vale o texto do sistema (1.0).</summary>
    public static string RequiredVersion(string? activeDatabaseVersion) =>
        string.IsNullOrWhiteSpace(activeDatabaseVersion) ? StaticVersion : activeDatabaseVersion.Trim();

    /// <summary>Reaceite só quando existe uma versão publicada diferente da que o usuário aceitou.</summary>
    public static bool IsReacceptanceRequired(string? acceptedVersion, string? activeDatabaseVersion)
    {
        if (string.IsNullOrWhiteSpace(activeDatabaseVersion))
        {
            return false;
        }

        return !string.Equals(acceptedVersion?.Trim(), activeDatabaseVersion.Trim(), StringComparison.Ordinal);
    }

    public static bool IsExternalApiPlatform(MarketplaceType platform) =>
        platform is MarketplaceType.MercadoLivre or MarketplaceType.Shopee or MarketplaceType.TikTokShop;

    public static string NextVersion(string? latest)
    {
        var current = string.IsNullOrWhiteSpace(latest) ? StaticVersion : latest.Trim();
        var parts = current.Split('.');
        if (parts.Length == 1 && int.TryParse(parts[0], out var majorOnly) && majorOnly > 0)
        {
            return majorOnly + ".1";
        }

        if (parts.Length >= 2
            && int.TryParse(parts[0], out var major)
            && int.TryParse(parts[1], out var minor)
            && major > 0
            && minor >= 0)
        {
            return major + "." + (minor + 1);
        }

        return "1.1";
    }

    public static string? ValidateContent(string? content)
    {
        var sanitized = Sanitize(content);
        if (string.IsNullOrWhiteSpace(VisibleText(sanitized)))
        {
            return "Informe o texto do documento.";
        }

        if (sanitized.Length > MaxContentLength)
        {
            return "O texto excede o tamanho máximo.";
        }

        return null;
    }

    public static string? ResolveClientIp(string? cloudflareConnectingIp, string? forwardedFor, string? remoteIp)
    {
        var cloudflare = NormalizeIp(cloudflareConnectingIp);
        if (cloudflare is not null)
        {
            return cloudflare;
        }

        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            foreach (var part in forwardedFor.Split(','))
            {
                var forwarded = NormalizeIp(part);
                if (forwarded is not null)
                {
                    return forwarded;
                }
            }
        }

        return NormalizeIp(remoteIp);
    }

    public static string Sanitize(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var text = html.Trim();
        if (text.Length > MaxContentLength)
        {
            text = text[..MaxContentLength];
        }

        text = CommentPattern().Replace(text, string.Empty);
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var next = ForbiddenBlockPattern().Replace(text, string.Empty);
            if (next == text)
            {
                break;
            }

            text = next;
        }

        text = ForbiddenTagPattern().Replace(text, string.Empty);
        text = EventHandlerPattern().Replace(text, string.Empty);
        text = AnyTagPattern().Replace(text, KeepTag);
        return text.Trim();
    }

    public static string VisibleText(string? html) =>
        string.IsNullOrWhiteSpace(html)
            ? string.Empty
            : TagPattern().Replace(html, " ").Trim();

    private static string? NormalizeIp(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > MaxIpLength || !IPAddress.TryParse(trimmed, out var address))
        {
            return null;
        }

        return address.ToString();
    }

    private static string KeepTag(Match match)
    {
        var name = match.Groups[1].Value.ToLowerInvariant();
        if (!AllowedTags.Contains(name))
        {
            return string.Empty;
        }

        if (match.Value.StartsWith("</", StringComparison.Ordinal))
        {
            return "</" + name + ">";
        }

        if (name is "br" or "hr")
        {
            return "<" + name + ">";
        }

        if (name == "a")
        {
            var href = ExtractSafeHref(match.Groups[2].Value);
            return href is null ? "<a>" : "<a href=\"" + href + "\">";
        }

        return "<" + name + ">";
    }

    private static string? ExtractSafeHref(string attributes)
    {
        var match = HrefPattern().Match(attributes);
        if (!match.Success)
        {
            return null;
        }

        var href = (match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value).Trim();
        if (href.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || href.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)
            || href.StartsWith("/legal/", StringComparison.OrdinalIgnoreCase))
        {
            return href.Replace("\"", string.Empty, StringComparison.Ordinal);
        }

        return null;
    }

    [GeneratedRegex(@"<!--.*?-->", RegexOptions.Singleline)]
    private static partial Regex CommentPattern();

    [GeneratedRegex(@"<\s*(script|style|iframe|object|embed|link|meta|form|svg)\b[^>]*>.*?<\s*/\s*\1\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ForbiddenBlockPattern();

    [GeneratedRegex(@"</?\s*(script|style|iframe|object|embed|link|meta|form|svg)\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex ForbiddenTagPattern();

    [GeneratedRegex(@"\s+on[a-z]+\s*=\s*(""[^""]*""|'[^']*'|[^\s>]+)", RegexOptions.IgnoreCase)]
    private static partial Regex EventHandlerPattern();

    [GeneratedRegex(@"</?([a-z0-9]+)([^>]*)>", RegexOptions.IgnoreCase)]
    private static partial Regex AnyTagPattern();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex TagPattern();

    [GeneratedRegex(@"href\s*=\s*(?:""([^""]*)""|'([^']*)')", RegexOptions.IgnoreCase)]
    private static partial Regex HrefPattern();
}
