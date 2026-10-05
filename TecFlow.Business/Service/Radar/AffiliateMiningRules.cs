using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using TecFlow.Core.Enums;

namespace TecFlow.Business.Service.Radar;

public static class AffiliateMiningRules
{
    public const int MaxArbitrageSuggestions = 3;
    public const int RadarPageSize = 30;
    public static readonly TimeSpan MiningInterval = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan HistoryWindow = TimeSpan.FromDays(7);
    public const decimal PriceDropRatio = 0.85m;

    public static readonly string[] DefaultNiches =
    [
        "eletrônicos",
        "casa",
        "beleza",
        "moda",
        "games",
        "pet"
    ];

    public static readonly MarketplaceType[] PreferredMiningPlatforms =
    [
        MarketplaceType.Shopee,
        MarketplaceType.MercadoLivre,
        MarketplaceType.Amazon,
        MarketplaceType.TikTokShop
    ];

    private static readonly Regex CouponRegex = new(
        @"(?:cupom|c[oó]digo)\s*[:\-]?\s*([A-Z0-9]{4,16})",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex NoiseRegex = new(
        @"\b(frete\s+gr[aá]tis|original|promo[cç][aã]o|oferta|oficial|kit)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static IReadOnlyList<string> ParseNiches(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return DefaultNiches;
        }

        var items = csv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(item => item.Length >= 2)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToList();
        return items.Count == 0 ? DefaultNiches : items;
    }

    public static string JoinNiches(IEnumerable<string>? niches) =>
        string.Join(", ", ParseNiches(string.Join(",", niches ?? [])));

    public static string BuildProductKey(string? name)
    {
        var normalized = NormalizeTitle(name);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return string.Empty;
        }

        var tokens = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(6);
        return string.Join('-', tokens);
    }

    public static string NormalizeTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return string.Empty;
        }

        var cleaned = NoiseRegex.Replace(title, " ");
        cleaned = RemoveDiacritics(cleaned).ToLowerInvariant();
        cleaned = Regex.Replace(cleaned, @"[^a-z0-9\s]", " ");
        cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();
        return cleaned;
    }

    public static bool TitlesLookAlike(string? left, string? right)
    {
        var a = NormalizeTitle(left).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var b = NormalizeTitle(right).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (a.Length == 0 || b.Length == 0)
        {
            return false;
        }

        var overlap = a.Intersect(b, StringComparer.Ordinal).Count();
        var min = Math.Min(a.Length, b.Length);
        return overlap >= Math.Max(2, (int)Math.Ceiling(min * 0.45));
    }

    public static bool MatchesNiche(string? title, IReadOnlyCollection<string> niches)
    {
        if (niches.Count == 0)
        {
            return true;
        }

        var normalized = NormalizeTitle(title);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return true;
        }

        return niches.Any(niche => normalized.Contains(NormalizeTitle(niche), StringComparison.Ordinal));
    }

    public static bool MatchesTicket(decimal? price, decimal? minTicket, decimal? maxTicket)
    {
        if (price is not > 0)
        {
            return true;
        }

        if (minTicket is > 0 && price < minTicket)
        {
            return false;
        }

        if (maxTicket is > 0 && price > maxTicket)
        {
            return false;
        }

        return true;
    }

    public static string? ExtractCoupon(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var match = CouponRegex.Match(text);
        return match.Success ? match.Groups[1].Value.ToUpperInvariant() : null;
    }

    public static int ComputeAttractivenessScore(
        decimal? price,
        decimal? comparedPrice,
        bool hasCoupon,
        bool inTicketRange,
        bool isMover,
        bool isTrend)
    {
        var score = 20;
        if (price is > 0 && comparedPrice is > 0 && price < comparedPrice)
        {
            var drop = (comparedPrice.Value - price.Value) / comparedPrice.Value;
            score += Math.Clamp((int)Math.Round(drop * 40), 0, 40);
        }

        if (inTicketRange)
        {
            score += 20;
        }

        if (hasCoupon)
        {
            score += 10;
        }

        if (isMover)
        {
            score += 15;
        }

        if (isTrend)
        {
            score += 15;
        }

        return Math.Clamp(score, 0, 100);
    }

    public static bool LooksLikeSocialTrend(string? title, string? rawText)
    {
        var haystack = $"{title} {rawText}";
        return haystack.Contains("tiktok", StringComparison.OrdinalIgnoreCase)
            || haystack.Contains("reels", StringComparison.OrdinalIgnoreCase)
            || haystack.Contains("viral", StringComparison.OrdinalIgnoreCase)
            || haystack.Contains("trending", StringComparison.OrdinalIgnoreCase);
    }

    public static IReadOnlyList<MarketplaceType> ResolveSearchPlatforms(
        IReadOnlyCollection<MarketplaceType> activePlatforms,
        bool restrictToActiveStores)
    {
        var preferred = PreferredMiningPlatforms.ToHashSet();
        if (restrictToActiveStores)
        {
            return activePlatforms.Where(preferred.Contains).Distinct().ToList();
        }

        return PreferredMiningPlatforms;
    }

    public static int ResolveSkip(int skip) => Math.Max(0, skip);

    public static int ResolveTake(int take) =>
        take is > 0 and <= 60 ? take : RadarPageSize;

    private static string RemoveDiacritics(string value)
    {
        var form = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(form.Length);
        foreach (var ch in form)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(ch);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
