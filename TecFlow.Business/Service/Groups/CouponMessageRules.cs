using System.Globalization;
using System.Text.RegularExpressions;
using TecFlow.Core.Enums;

namespace TecFlow.Business.Service.Groups;

public static class CouponMessageRules
{
    private const RegexOptions Options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled;

    public const string AllPlatformsFilter = "Todos";

    public const string OtherPlatformsFilter = "Outros";

    public static readonly MarketplaceType[] HighlightedPlatforms =
    [
        MarketplaceType.MercadoLivre,
        MarketplaceType.Shopee,
        MarketplaceType.Amazon
    ];

    private static readonly Regex CouponKeywordRegex = new(
        @"\b(?:cupo(?:m|ns)|c[oó]digo|coupon|voucher)\b",
        Options);

    private static readonly Regex AmountOffRegex = new(
        @"R\$\s*(\d{1,3}(?:\.\d{3})*|\d+)(?:,(\d{1,2}))?\s*(?:de\s+)?(?:OFF\b|de\s+desconto|desconto)",
        Options);

    private static readonly Regex PercentOffRegex = new(
        @"(\d{1,2})\s*%\s*(?:de\s+)?(?:OFF\b|de\s+desconto|desconto)",
        Options);

    private static readonly Regex DiscountOfAmountRegex = new(
        @"desconto\s+de\s+R\$\s*(\d{1,3}(?:\.\d{3})*|\d+)(?:,(\d{1,2}))?",
        Options);

    private static readonly Regex CouponLimitRegex = new(
        @"limit(?:e|ad[oa])?\s+(?:de\s+|a\s+)?(?:at[eé]\s+)?R\$\s*(\d{1,3}(?:\.\d{3})*|\d+)(?:,(\d{1,2}))?(?:\s*(?:OFF\b|de\s+desconto))?",
        Options);

    private static readonly Regex OffOverAmountRegex = new(
        @"OFF\s+(?:em|nas?|nos?|para|a\s+partir\s+de)\s+(?:compras?\s+(?:de\s+|acima\s+de\s+)?)?R\$\s*(\d{1,3}(?:\.\d{3})*|\d+)(?:,(\d{1,2}))?",
        Options);

    private static readonly Regex CouponMinimumStripRegex = new(
        @"(?:acima\s+de|compras?\s+(?:acima\s+)?de|pedido\s+m[ií]nimo(?:\s+de)?|m[ií]nimo(?:\s+de)?)\s*R\$\s*(\d{1,3}(?:\.\d{3})*|\d+)(?:,(\d{1,2}))?",
        Options);

    private static readonly Regex CouponHeadlineRegex = new(
        @"^(?:novos?\s+cupo(?:m|ns)\b|cupons\b|cupo(?:m|ns)\s+(?:de\s+desconto\s+)?(?:(?:no|na|do|da)\s+)?(?:mercado\s*livre|meli|shopee|amazon|magalu|aliexpress|shein|kabum)\b)",
        Options);

    private static readonly Regex FreeShippingRegex = new(
        @"frete\s+gr[aá]tis",
        Options);

    private static readonly Regex MinimumPurchaseRegex = new(
        @"(?:acima\s+de|a\s+partir\s+de|compras?\s+(?:acima\s+)?de|pedido\s+m[ií]nimo(?:\s+de)?|m[ií]nimo(?:\s+de)?)\s*R\$\s*(\d{1,3}(?:\.\d{3})*|\d+)(?:,(\d{1,2}))?",
        Options);

    private static readonly Regex ValidityDateRegex = new(
        @"(?:v[aá]lid[oa]s?\s+at[eé]|at[eé]\s+(?:o\s+dia\s+)?|expira\s+(?:em\s+)?|validade[:\s]+)\s*(\d{1,2}/\d{1,2}(?:/\d{2,4})?)",
        Options);

    private static readonly Regex ValidityPhraseRegex = new(
        @"(?:v[aá]lid[oa]\s+)?(?:s[oó]\s+)?hoje|at[eé]\s+(?:[aà]s\s+)?\d{1,2}h(?:\d{2})?|at[eé]\s+meia[\s\-]noite|enquanto\s+durar(?:em)?(?:\s+os\s+estoques)?|[uú]ltimas\s+horas",
        Options);

    private static readonly Regex CouponCodeAnchorRegex = new(
        @"\b(?:cupom|c[oó]digo|code|coupon|voucher|use|usar|utilize|aplique)\b",
        Options);

    private static readonly Regex UrlRegex = new(
        @"(?:https?://|www\.)\S+",
        Options);

    private static readonly Regex CodeTokenRegex = new(
        @"(?<![\p{L}\p{N}])[A-Za-z0-9][A-Za-z0-9_\-]{2,31}(?![\p{L}\p{N}])",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex RuleLineRegex = new(
        @"limit|m[ií]nim|acima\s+de|a\s+partir\s+de|v[aá]lid|primeira\s+compra|novos?\s+(?:usu[aá]rios|clientes)|selecionad|por\s+cpf|uso\s+[uú]nico|no\s+app|resgat|frete\s+gr[aá]tis",
        Options);

    private static readonly Regex LeadingJunkRegex = new(
        @"^[^\p{L}\p{N}$]+",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly HashSet<string> CodeStopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "OFF", "DE", "DO", "DA", "NO", "NA", "EM", "PARA", "COM", "POR", "PELO", "PELA", "NOS", "NAS",
        "CUPOM", "CUPONS", "CODIGO", "CÓDIGO", "CODE", "COUPON", "VOUCHER", "USE", "USAR", "UTILIZE", "APLIQUE",
        "DESCONTO", "SHOPEE", "AMAZON", "MERCADO", "LIVRE", "MELI", "APP", "FRETE", "GRATIS", "GRÁTIS", "PIX",
        "ACIMA", "VALIDO", "VÁLIDO", "HOJE", "LINK", "AQUI", "ATIVAR", "RESGATE", "RESGATAR", "LOJA", "SITE",
        "COMPRAS", "COMPRA", "TODO", "TODA", "TODOS", "TODAS", "PROMO", "PROMOCAO", "PROMOÇÃO", "OFERTA", "NOVO",
        "VALE", "SOMENTE", "APENAS", "AGORA", "CORRE", "CORRA", "ESSE", "ESTE", "ABAIXO", "ACESSE", "CLIQUE"
    };

    public static bool HasCouponSignal(string? text) =>
        !string.IsNullOrWhiteSpace(text)
        && (CouponKeywordRegex.IsMatch(text) || AmountOffRegex.IsMatch(text) || PercentOffRegex.IsMatch(text));

    /// <summary>
    /// Mensagens de produto com cupom continuam <see cref="OfferType.Product"/>: só vira cupom quando
    /// não há link de produto, quando a mensagem é um anúncio de cupons (título "NOVO CUPOM ..." ou
    /// dois ou mais códigos) ou quando nenhum preço à vista sobra após remover os valores do próprio cupom.
    /// </summary>
    public static OfferType Classify(string? text, bool hasDirectProductUrl)
    {
        if (!HasCouponSignal(text))
        {
            return OfferType.Product;
        }

        if (!hasDirectProductUrl || IsCouponAnnouncement(text))
        {
            return OfferType.Coupon;
        }

        var stripped = StripCouponAmounts(text!);
        return OfferPriceTextRules.TryExtractCashPrice(stripped) is > 0
            ? OfferType.Product
            : OfferType.Coupon;
    }

    public static bool IsCouponAnnouncement(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var headline = ExtractHeadline(text);
        return (headline is not null && CouponHeadlineRegex.IsMatch(headline))
            || ExtractCouponCodes(text).Count >= 2;
    }

    public static string StripCouponAmounts(string text)
    {
        var result = OffOverAmountRegex.Replace(text, "OFF ");
        result = CouponLimitRegex.Replace(result, " ");
        result = AmountOffRegex.Replace(result, " ");
        result = DiscountOfAmountRegex.Replace(result, " ");
        result = CouponMinimumStripRegex.Replace(result, " ");
        return result;
    }

    public static string? ExtractCouponCode(string? text) =>
        ExtractCouponCodes(text).FirstOrDefault();

    public static IReadOnlyList<string> ExtractCouponCodes(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        text = UrlRegex.Replace(text, " ");
        var codes = new List<string>();
        foreach (Match anchor in CouponCodeAnchorRegex.Matches(text))
        {
            var start = anchor.Index + anchor.Length;
            var window = text.Substring(start, Math.Min(48, text.Length - start));
            var lineBreak = window.IndexOfAny(['\r', '\n']);
            if (lineBreak == 0)
            {
                window = window.TrimStart('\r', '\n');
                lineBreak = window.IndexOfAny(['\r', '\n']);
            }

            if (lineBreak > 0)
            {
                window = window[..lineBreak];
            }

            foreach (Match token in CodeTokenRegex.Matches(window))
            {
                var candidate = token.Value.Trim('-', '_');
                if (IsCodeCandidate(candidate))
                {
                    var code = candidate.ToUpperInvariant();
                    if (!codes.Contains(code))
                    {
                        codes.Add(code);
                    }

                    break;
                }
            }
        }

        return codes;
    }

    public static string? ExtractDiscount(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var amount = AmountOffRegex.Match(text);
        if (amount.Success)
        {
            return $"{FormatBrl(amount.Groups[1].Value, amount.Groups[2].Value)} OFF";
        }

        var discountOf = DiscountOfAmountRegex.Match(text);
        if (discountOf.Success)
        {
            return $"{FormatBrl(discountOf.Groups[1].Value, discountOf.Groups[2].Value)} OFF";
        }

        var percent = PercentOffRegex.Match(text);
        if (percent.Success)
        {
            return $"{percent.Groups[1].Value}% OFF";
        }

        return FreeShippingRegex.IsMatch(text) ? "Frete grátis" : null;
    }

    public static string? ExtractMinimumPurchase(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var match = MinimumPurchaseRegex.Match(text);
        return match.Success
            ? $"Compras acima de {FormatBrl(match.Groups[1].Value, match.Groups[2].Value)}"
            : null;
    }

    public static string? ExtractValidity(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var date = ValidityDateRegex.Match(text);
        if (date.Success)
        {
            return $"Válido até {date.Groups[1].Value}";
        }

        var phrase = ValidityPhraseRegex.Match(text);
        if (!phrase.Success)
        {
            return null;
        }

        var value = phrase.Value.Trim();
        return char.ToUpper(value[0], CultureInfo.GetCultureInfo("pt-BR")) + value[1..];
    }

    public static IReadOnlyList<string> ExtractRules(string? text, int maxRules = 3)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var rules = new List<string>();
        var minimum = ExtractMinimumPurchase(text);
        if (minimum is not null)
        {
            rules.Add(minimum);
        }

        foreach (var rawLine in text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (rules.Count >= maxRules)
            {
                break;
            }

            if (rawLine.Contains("http", StringComparison.OrdinalIgnoreCase) || !RuleLineRegex.IsMatch(rawLine))
            {
                continue;
            }

            var line = LeadingJunkRegex.Replace(rawLine, string.Empty).Trim();
            if (line.Length < 4)
            {
                continue;
            }

            if (line.Length > 140)
            {
                line = line[..140].TrimEnd() + "…";
            }

            if (!rules.Exists(existing => existing.Equals(line, StringComparison.OrdinalIgnoreCase)))
            {
                rules.Add(line);
            }
        }

        return rules;
    }

    public static MarketplaceType? DetectPlatformFromText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (text.Contains("mercado livre", StringComparison.OrdinalIgnoreCase)
            || text.Contains("mercadolivre", StringComparison.OrdinalIgnoreCase)
            || Regex.IsMatch(text, @"\bmeli\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            return MarketplaceType.MercadoLivre;
        }

        if (text.Contains("shopee", StringComparison.OrdinalIgnoreCase))
        {
            return MarketplaceType.Shopee;
        }

        if (text.Contains("amazon", StringComparison.OrdinalIgnoreCase))
        {
            return MarketplaceType.Amazon;
        }

        return null;
    }

    public static string? ExtractHeadline(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        foreach (var rawLine in text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (rawLine.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var line = LeadingJunkRegex.Replace(rawLine, string.Empty).Trim();
            if (line.Length < 3)
            {
                continue;
            }

            return line.Length <= 255 ? line : line[..255];
        }

        return null;
    }

    private static bool IsCodeCandidate(string candidate)
    {
        if (candidate.Length is < 4 or > 32 || CodeStopWords.Contains(candidate))
        {
            return false;
        }

        if (!candidate.Any(char.IsLetter))
        {
            return false;
        }

        return candidate.Where(char.IsLetter).All(char.IsUpper);
    }

    private static string FormatBrl(string integerPart, string decimalPart)
    {
        var digits = integerPart.Replace(".", string.Empty, StringComparison.Ordinal);
        if (!decimal.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            return "R$ " + integerPart;
        }

        if (!string.IsNullOrWhiteSpace(decimalPart)
            && int.TryParse(decimalPart.PadRight(2, '0'), NumberStyles.Integer, CultureInfo.InvariantCulture, out var cents))
        {
            value += cents / 100m;
        }

        var format = value == decimal.Truncate(value) ? "N0" : "N2";
        return "R$ " + value.ToString(format, CultureInfo.GetCultureInfo("pt-BR"));
    }
}
