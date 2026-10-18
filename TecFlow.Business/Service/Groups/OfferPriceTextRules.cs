using System.Globalization;
using System.Text.RegularExpressions;

namespace TecFlow.Business.Service.Groups;

public static class OfferPriceTextRules
{
    public const decimal MaxPlausiblePrice = 1_000_000m;
    public const decimal SignificantDivergenceRatio = 0.30m;

    private static readonly Regex CurrencyPriceRegex = new(
        @"(?:R\$|\$|Valor:?|✅|💲)\s*(?<num>\d{1,3}(?:\.\d{3})+(?:,\d{1,2})?|\d+\.\d{2}|\d+(?:,\d{1,2})?)(?!\d)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex InstallmentPrefixRegex = new(
        @"(?:\d{1,2}\s*[xX×]|\bparcelas?\b|\bparcelad[oa]s?\b|\bvezes\b|\bmensais\b)\s*(?:de\s*|sem\s+juros\s+de\s*|s/\s*juros\s+de\s*)?[:\-]?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex InstallmentSuffixRegex = new(
        @"^\s*(?:/\s*m[eê]s|por\s+m[eê]s|ao\s+m[eê]s|mensais|cada\s+parcela)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex OldPricePrefixRegex = new(
        @"(?:^|[\s(~❌🔻])(?:de|era|antes)\s*:?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex NewPricePrefixRegex = new(
        @"(?:^|\s)(?:por|agora)\s*(?:apenas|s[oó])?\s*:?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex TagRegex = new(
        @"<[^>]*>",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    /// <summary>"R$ 16,60 / unidade", "(R$ 16,60 por frasco)"; o preço repetido cobre o span aria-hidden da Amazon.</summary>
    private static readonly Regex UnitPriceSuffixRegex = new(
        @"^\s*(?:R\$\s*[\d.,]+\s*)?\)?\s*(?:/|\bpor\b|\bcada\b)\s*(?:unidade|unid\.?|un\b|frasco|lata|garrafa|litro|kg|quilo|100\s*(?:g|ml)\b|ml\b|c[aá]psula|sach[eê]|rolo|metro|dose|lavagem|fralda|pe[cç]a|item)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex UnitPricePrefixRegex = new(
        @"(?:\bunidade|\bunit[aá]rio|\bpor\s+(?:unidade|frasco|lata|litro|kg)|\bcada)\s*:?\s*\(?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static decimal? TryExtractCashPrice(string? text)
    {
        var candidates = ReadCandidates(text);
        if (candidates.Count == 0)
        {
            return null;
        }

        var cash = candidates.Where(item => !item.IsInstallment && !item.IsUnitPrice).ToList();
        for (var i = 0; i < cash.Count; i++)
        {
            var current = cash[i];
            var next = i + 1 < cash.Count ? cash[i + 1] : null;
            if (current.IsOldPrice && next is { IsNewPrice: true })
            {
                continue;
            }

            return current.Value;
        }

        return null;
    }

    public static bool HasCurrencyPrice(string? text) => ReadCandidates(text).Count > 0;

    public static bool IsInstallmentContext(string text, int matchIndex, int matchLength)
    {
        var prefix = ReadPrefix(text, matchIndex);
        if (InstallmentPrefixRegex.IsMatch(prefix))
        {
            return true;
        }

        var end = Math.Min(text.Length, matchIndex + matchLength + 40);
        var suffix = TagRegex.Replace(text[(matchIndex + matchLength)..end], " ");
        return InstallmentSuffixRegex.IsMatch(suffix);
    }

    /// <summary>Preço por unidade/frasco/litro: nunca é o valor total a pagar.</summary>
    public static bool IsUnitPriceContext(string text, int matchIndex, int matchLength)
    {
        if (UnitPricePrefixRegex.IsMatch(DecodeSpaces(ReadPrefix(text, matchIndex))))
        {
            return true;
        }

        var end = Math.Min(text.Length, matchIndex + matchLength + 160);
        var suffix = DecodeSpaces(TagRegex.Replace(text[(matchIndex + matchLength)..end], " "));
        return UnitPriceSuffixRegex.IsMatch(suffix);
    }

    public static bool IsNonCashContext(string text, int matchIndex, int matchLength) =>
        IsInstallmentContext(text, matchIndex, matchLength) || IsUnitPriceContext(text, matchIndex, matchLength);

    private static string DecodeSpaces(string value) =>
        value.Replace("&nbsp;", " ", StringComparison.OrdinalIgnoreCase)
            .Replace("&#160;", " ", StringComparison.Ordinal)
            .Replace('\u00A0', ' ');

    public static bool IsPlausiblePrice(decimal? price) =>
        price is > 0 and < MaxPlausiblePrice;

    public static bool IsSignificantDivergence(decimal? savedPrice, decimal? pagePrice)
    {
        if (savedPrice is not > 0 || !IsPlausiblePrice(pagePrice))
        {
            return false;
        }

        var saved = savedPrice.Value;
        var page = pagePrice!.Value;
        return Math.Abs(page - saved) / Math.Max(page, saved) >= SignificantDivergenceRatio;
    }

    private static List<PriceCandidate> ReadCandidates(string? text)
    {
        var result = new List<PriceCandidate>();
        if (string.IsNullOrWhiteSpace(text))
        {
            return result;
        }

        foreach (Match match in CurrencyPriceRegex.Matches(text))
        {
            var value = ParseBrazilianPrice(match.Groups["num"].Value);
            if (!IsPlausiblePrice(value))
            {
                continue;
            }

            var prefix = ReadPrefix(text, match.Index);
            result.Add(new PriceCandidate(
                value!.Value,
                IsInstallmentContext(text, match.Index, match.Length),
                IsUnitPriceContext(text, match.Index, match.Length),
                OldPricePrefixRegex.IsMatch(prefix),
                NewPricePrefixRegex.IsMatch(prefix)));
        }

        return result;
    }

    private static string ReadPrefix(string text, int matchIndex)
    {
        var start = Math.Max(0, matchIndex - 60);
        var window = TagRegex.Replace(text[start..matchIndex], " ");
        var lineBreak = window.LastIndexOfAny(['\n', '\r']);
        return lineBreak >= 0 ? window[(lineBreak + 1)..] : window;
    }

    private static decimal? ParseBrazilianPrice(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var normalized = Regex.IsMatch(raw, @"^\d+\.\d{2}$")
            ? raw
            : raw.Replace(".", string.Empty, StringComparison.Ordinal).Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? decimal.Round(value, 2, MidpointRounding.AwayFromZero)
            : null;
    }

    private sealed record PriceCandidate(decimal Value, bool IsInstallment, bool IsUnitPrice, bool IsOldPrice, bool IsNewPrice);
}
