using System.Globalization;

namespace Ofertio.Business.Service.Common;

/// <summary>
/// Valor monetário digitado na tela, em pt-BR. Zero é válido. Negativo, texto e acima de <see cref="Max"/> não são.
/// A lista <see cref="Samples"/> é a matriz que o teste e, depois, o Playwright reutilizam.
/// </summary>
public static class MoneyAmountRules
{
    public const decimal Min = 0m;

    /// <summary>Teto já usado para preço plausível de oferta.</summary>
    public const decimal Max = 1_000_000m;

    public const string InvalidPriceMessage = "Preço médio inválido. Informe um valor de 0 a 1.000.000,00.";

    public const string InvalidBudgetMessage = "Orçamento inválido. Informe um valor de 0 a 1.000.000,00.";

    public static readonly IReadOnlyList<MoneyAmountSample> Samples =
    [
        new("89,90", true, 89.90m),
        new("89.90", true, 89.90m),
        new("0", true, 0m),
        new("0,00", true, 0m),
        new("1.234,56", true, 1234.56m),
        new("1000000", true, 1_000_000m),
        new(null, false, 0m),
        new("", false, 0m),
        new("   ", false, 0m),
        new("abc", false, 0m),
        new("12a", false, 0m),
        new("-1", false, 0m),
        new("-1,50", false, 0m),
        new("1000000,01", false, 0m),
        new("1000000.01", false, 0m)
    ];

    public static bool IsAllowed(decimal amount) =>
        amount >= Min && amount <= Max && amount == decimal.Round(amount, 2, MidpointRounding.AwayFromZero);

    public static bool TryParse(string? text, out decimal amount)
    {
        amount = 0m;
        if (string.IsNullOrWhiteSpace(text) || !TryNormalize(text.Trim(), out var canonical))
        {
            return false;
        }

        if (!decimal.TryParse(canonical, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value)
            || !IsAllowed(value))
        {
            return false;
        }

        amount = value;
        return true;
    }

    private static bool TryNormalize(string text, out string canonical)
    {
        canonical = string.Empty;
        if (text.Contains('-') || text.Contains('+') || text.Any(char.IsLetter)
            || text.StartsWith('.') || text.StartsWith(','))
        {
            return false;
        }

        var comma = text.LastIndexOf(',');
        var dot = text.LastIndexOf('.');

        if (comma >= 0 && dot >= 0)
        {
            return comma > dot
                ? TryGrouped(text, ',', '.', out canonical)
                : TryGrouped(text, '.', ',', out canonical);
        }

        if (comma >= 0)
        {
            return TryDecimalSeparator(text, ',', out canonical);
        }

        if (dot < 0)
        {
            canonical = text;
            return text.Length > 0 && text.All(char.IsDigit);
        }

        var dots = text.Count(c => c == '.');
        if (dots == 1)
        {
            var fractionLength = text.Length - dot - 1;
            if (fractionLength is 1 or 2)
            {
                canonical = text;
                return text.All(c => char.IsDigit(c) || c == '.');
            }

            if (fractionLength == 3 && IsThousandsGroup(text))
            {
                canonical = text.Replace(".", string.Empty);
                return true;
            }

            return false;
        }

        if (!IsThousandsGroup(text))
        {
            return false;
        }

        canonical = text.Replace(".", string.Empty);
        return canonical.All(char.IsDigit);
    }

    private static bool TryDecimalSeparator(string text, char separator, out string canonical)
    {
        canonical = string.Empty;
        var index = text.LastIndexOf(separator);
        var fraction = text[(index + 1)..];
        var integer = text[..index];
        if (fraction.Length is < 1 or > 2 || integer.Length == 0
            || fraction.Any(c => !char.IsDigit(c)) || integer.Any(c => !char.IsDigit(c)))
        {
            return false;
        }

        canonical = integer + "." + fraction;
        return true;
    }

    private static bool TryGrouped(string text, char decimalSeparator, char groupSeparator, out string canonical)
    {
        canonical = string.Empty;
        var index = text.LastIndexOf(decimalSeparator);
        var fraction = text[(index + 1)..];
        if (fraction.Length is < 1 or > 2 || fraction.Any(c => !char.IsDigit(c)))
        {
            return false;
        }

        var integer = text[..index];
        if (!IsThousandsGroup(integer, groupSeparator))
        {
            return false;
        }

        canonical = integer.Replace(groupSeparator.ToString(), string.Empty) + "." + fraction;
        return true;
    }

    private static bool IsThousandsGroup(string integer, char separator = '.')
    {
        if (string.IsNullOrEmpty(integer))
        {
            return false;
        }

        var parts = integer.Split(separator);
        if (parts[0].Length is < 1 or > 3 || parts[0].Any(c => !char.IsDigit(c)))
        {
            return false;
        }

        for (var i = 1; i < parts.Length; i++)
        {
            if (parts[i].Length != 3 || parts[i].Any(c => !char.IsDigit(c)))
            {
                return false;
            }
        }

        return true;
    }
}

/// <summary>Um caso da matriz de preço e orçamento.</summary>
public sealed record MoneyAmountSample(string? Text, bool IsValid, decimal Expected);
