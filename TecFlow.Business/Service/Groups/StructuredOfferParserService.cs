using System.Globalization;
using System.Text.RegularExpressions;
using TecFlow.Business.Dto;
using TecFlow.Business.Interfaces.Services;
using TecFlow.Business.Service.Groups;
using TecFlow.Business.Service.LinkStrategies;
using TecFlow.Business.Service.Telegram;
using TecFlow.Core.Enums;

namespace TecFlow.Business.Service.Groups;

public sealed class StructuredOfferParserService : IStructuredOfferParserService
{
    private static readonly Regex LeadingJunkRegex = new(
        @"^[^\w\s]+",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex DecorativePrefixRegex = new(
        @"^(?:oferta|promo(?:[cç][aã]o)?|garanta\s+j[aá]|confira)[:\-\s]+",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex UserPriceRegex = new(
        @"(?:R\$|\$|Valor:?|✅|💲)\s*(\d{1,3}(?:\.\d{3})*|\d+)(?:,\d{2})?",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex ResilientPriceRegex = new(
        @"(?:(?:✅|💲|Valor:?)\s*)?(?:R\$|\$)\s*(\d{1,3}(?:\.\d{3})*|\d+)(?:,\d{2})?",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex CouponRegex = new(
        @"(?:CUPOM|Cupom|cupom|Code)[:\s\-]+([A-Z0-9_\+\-]+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly string[] ShowcaseHosts =
    [
        "t.me",
        "telegram.me",
        "www.t.me",
        "instagram.com",
        "www.instagram.com",
        "facebook.com",
        "www.facebook.com",
        "youtube.com",
        "www.youtube.com",
        "youtu.be",
        "linktr.ee",
        "bio.link"
    ];

    public OfferDataExtraction Parse(string? rawText)
    {
        var text = rawText ?? string.Empty;
        var title = ExtractTitle(text);
        var price = ExtractPrice(text);
        var coupon = ExtractCoupon(text);
        var urls = TelegramUserMonitorRules.ExtractHttpUrls(text);
        var primary = SelectPrimaryProductUrl(text, title, urls);
        var platform = ResolvePlatform(primary);

        return new OfferDataExtraction
        {
            ProductTitle = title ?? string.Empty,
            Price = price,
            CouponCode = coupon ?? string.Empty,
            PrimaryProductUrl = primary ?? string.Empty,
            Platform = platform ?? string.Empty
        };
    }

    public static IReadOnlyList<string> FilterPersistableUrls(
        IReadOnlyList<string> urls,
        OfferDataExtraction parsed)
    {
        if (urls.Count == 0)
        {
            return string.IsNullOrWhiteSpace(parsed.PrimaryProductUrl)
                ? []
                : [parsed.PrimaryProductUrl];
        }

        var result = new List<string>();
        void Add(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            if (result.Any(item => string.Equals(item, value, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            result.Add(value);
        }

        Add(parsed.PrimaryProductUrl);
        foreach (var url in urls)
        {
            if (urls.Count > 1 && IsShowcaseUrl(url) && !string.IsNullOrWhiteSpace(parsed.PrimaryProductUrl))
            {
                continue;
            }

            if (GroupOfferCaptureRules.HasDirectProductUrl(url) || urls.Count == 1)
            {
                Add(url);
            }
        }

        if (result.Count == 0)
        {
            Add(urls[0]);
        }

        return result;
    }

    internal static string? ExtractTitle(string text)
    {
        foreach (var rawLine in text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (rawLine.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var line = LeadingJunkRegex.Replace(rawLine, string.Empty).Trim();
            line = DecorativePrefixRegex.Replace(line, string.Empty).Trim();
            line = LeadingJunkRegex.Replace(line, string.Empty).Trim();
            if (line.Length < 3)
            {
                continue;
            }

            return ProductMetadataHtmlParser.NormalizePersistedProductName(line);
        }

        return null;
    }

    internal static decimal? ExtractPrice(string text)
    {
        foreach (var regex in new[] { UserPriceRegex, ResilientPriceRegex })
        {
            var match = regex.Match(text);
            if (!match.Success)
            {
                continue;
            }

            var parsed = ProductMetadataHtmlParser.TryParseDisplayPrice(match.Value);
            if (parsed is > 0)
            {
                return parsed;
            }

            if (decimal.TryParse(
                    match.Groups[1].Value.Replace(".", string.Empty, StringComparison.Ordinal),
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var integer)
                && integer > 0)
            {
                return integer;
            }
        }

        return GroupOfferCaptureRules.TryExtractTelegramPrice(text);
    }

    internal static string? ExtractCoupon(string text)
    {
        var match = CouponRegex.Match(text);
        if (!match.Success)
        {
            return null;
        }

        var code = match.Groups[1].Value.Trim().TrimEnd('.', ',', ';').ToUpperInvariant();
        return code.Length is >= 3 and <= 64 ? code : null;
    }

    internal static bool IsShowcaseUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            return true;
        }

        var host = uri.Host.Trim().TrimStart('.').ToLowerInvariant();
        if (ShowcaseHosts.Contains(host) || host.EndsWith(".t.me", StringComparison.Ordinal))
        {
            return true;
        }

        if (GroupOfferCaptureRules.HasDirectProductUrl(url))
        {
            return false;
        }

        var path = uri.AbsolutePath.Trim('/');
        return string.IsNullOrWhiteSpace(path)
            || path.Equals("ofertas", StringComparison.OrdinalIgnoreCase)
            || path.Equals("mall", StringComparison.OrdinalIgnoreCase);
    }

    private static string? SelectPrimaryProductUrl(string text, string? title, IReadOnlyList<string> urls)
    {
        if (urls.Count == 0)
        {
            return null;
        }

        var titleIndex = 0;
        if (!string.IsNullOrWhiteSpace(title))
        {
            var idx = text.IndexOf(title, StringComparison.OrdinalIgnoreCase);
            titleIndex = idx >= 0 ? idx : 0;
        }

        var withIndex = urls
            .Select(url => (Url: url, Index: text.IndexOf(url, StringComparison.OrdinalIgnoreCase)))
            .Where(item => item.Index >= 0)
            .OrderBy(item => item.Index)
            .ToList();

        var afterTitle = withIndex.Where(item => item.Index >= titleIndex).Select(item => item.Url).ToList();
        var afterProduct = afterTitle.FirstOrDefault(url => GroupOfferCaptureRules.HasDirectProductUrl(url));
        if (!string.IsNullOrWhiteSpace(afterProduct))
        {
            return afterProduct;
        }

        var anyProduct = withIndex.Select(item => item.Url).FirstOrDefault(GroupOfferCaptureRules.HasDirectProductUrl);
        if (!string.IsNullOrWhiteSpace(anyProduct))
        {
            return anyProduct;
        }

        if (urls.Count > 1)
        {
            return withIndex.Select(item => item.Url).FirstOrDefault(url => !IsShowcaseUrl(url)) ?? urls[0];
        }

        return urls[0];
    }

    private static string? ResolvePlatform(string? url)
    {
        var platform = GroupOfferCaptureRules.DetectPlatform(url);
        return platform?.GetDisplayName();
    }
}
