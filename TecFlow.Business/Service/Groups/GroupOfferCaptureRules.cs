using System.Text.RegularExpressions;
using TecFlow.Business.Service.LinkStrategies;
using TecFlow.Business.Service.WhatsApp;
using TecFlow.Core.Enums;

namespace TecFlow.Business.Service.Groups;

public static class GroupOfferCaptureRules
{
    public static readonly Regex TelegramPriceRegex = new(
        @"R?\$?\s*(\d{1,3}(\.\d{3})*|\d+)(,\d{2})?",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static readonly string[] ProductPageErrorPhrases =
    [
        "Essa loja falhou ao carregar",
        "Ops! Produto não encontrado",
        "Anúncio pausado",
        "Fora de estoque",
        "Produto não encontrado",
        "publicação pausada",
        "este produto não está disponível"
    ];

    public const string WhatsAppChannel = "WhatsApp";
    public const string TelegramChannel = "Telegram";

    public static string? NormalizeChannel(string? channel)
    {
        if (string.IsNullOrWhiteSpace(channel))
        {
            return null;
        }

        if (channel.Contains("telegram", StringComparison.OrdinalIgnoreCase)
            || channel.Equals("tg", StringComparison.OrdinalIgnoreCase))
        {
            return TelegramChannel;
        }

        if (channel.Contains("whats", StringComparison.OrdinalIgnoreCase)
            || channel.Equals("wa", StringComparison.OrdinalIgnoreCase))
        {
            return WhatsAppChannel;
        }

        return null;
    }

    public static bool MatchesChannel(string? value, string? channel)
    {
        var expected = NormalizeChannel(channel);
        if (expected is null)
        {
            return true;
        }

        return string.Equals(NormalizeChannel(value) ?? value, expected, StringComparison.OrdinalIgnoreCase);
    }

    public static string BuildGroupKey(string channel, string groupId) =>
        $"{channel}:{groupId.Trim()}";

    public const int OffersPageSize = 50;

    public static int ResolveLookbackHours(int hours) =>
        hours switch
        {
            10 => 10,
            48 => 48,
            _ => 24
        };

    public static int ResolveOffersSkip(int skip) => Math.Max(0, skip);

    public static int ResolveOffersTake(int take) =>
        take is > 0 and <= 100 ? take : OffersPageSize;

    public static MarketplaceType? DetectPlatform(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        if (UrlUnshortenerService.TryDetectMarketplace(url, out var platform)
            || UniversalLinkResolverEngine.TryMapDomainToPlatform(url, out platform))
        {
            return platform;
        }

        return null;
    }

    public static bool HasDirectProductUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        var host = uri.Host.Trim().TrimStart('.').ToLowerInvariant();
        if (host is "t.me" or "telegram.me" or "www.t.me" || host.EndsWith(".t.me", StringComparison.Ordinal))
        {
            return false;
        }

        return DetectPlatform(url) is not null;
    }

    public static decimal? ExtractPrice(string? text, string? url)
    {
        var fromText = TryExtractTelegramPrice(text);
        if (fromText is > 0)
        {
            return fromText;
        }

        var parsed = ProductMetadataHtmlParser.Parse(
            string.IsNullOrWhiteSpace(text) ? null : $"<html><body>{text}</body></html>",
            url ?? string.Empty);
        return parsed.ProductPrice;
    }

    public static decimal? TryExtractTelegramPrice(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        foreach (Match match in TelegramPriceRegex.Matches(text))
        {
            if (!match.Success)
            {
                continue;
            }

            var end = match.Index + match.Length;
            while (end < text.Length && char.IsDigit(text[end]))
            {
                end++;
            }

            var raw = text[match.Index..end];
            var parsed = ProductMetadataHtmlParser.TryParseDisplayPrice(raw);
            if (parsed is > 0)
            {
                return parsed;
            }
        }

        return null;
    }

    public static string? ExtractName(string? text, string? url)
    {
        var fromUrl = ProductMetadataHtmlParser.TryExtractMarketplaceProductNameFromUrl(url);
        if (!string.IsNullOrWhiteSpace(fromUrl))
        {
            return fromUrl;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var firstLine = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault(line => !line.StartsWith("http", StringComparison.OrdinalIgnoreCase));
        return ProductMetadataHtmlParser.NormalizePersistedProductName(firstLine);
    }

    public static bool NeedsStructuredFallback(string? name, decimal? price) =>
        price is not > 0 || IsWeakProductName(name);

    public static bool IsWeakProductName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return true;
        }

        var trimmed = name.Trim();
        return trimmed.Length < 8
            || trimmed.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("oferta capturada", StringComparison.OrdinalIgnoreCase);
    }

    public static bool ContainsUnavailableProductPhrase(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return false;
        }

        foreach (var phrase in ProductPageErrorPhrases)
        {
            if (html.Contains(phrase, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
