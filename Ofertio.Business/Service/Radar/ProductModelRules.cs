using System.Text.RegularExpressions;

namespace Ofertio.Business.Service.Radar;

/// <summary>Extrai marca e código de modelo a partir do título do anúncio.</summary>
public static partial class ProductModelRules
{
    private static readonly string[] Brands =
    [
        "Philips", "Samsung", "Motorola", "Xiaomi", "iPhone", "iPad", "MacBook", "Apple",
        "Lenovo", "Dell", "Asus", "Acer", "Sony", "Panasonic", "Electrolux", "Brastemp",
        "Consul", "Mondial", "Logitech", "Intelbras", "Multilaser", "Positivo", "Havaianas",
        "Adidas", "Nike", "Canon", "Nikon", "Epson", "Brother", "Britânia", "Britania",
        "Cadence", "GeForce", "Ryzen", "Intel", "Nvidia", "JBL", "TCL", "AOC", "LG", "HP"
    ];

    private static readonly string[] BrandsByLength = Brands
        .OrderByDescending(brand => brand.Length)
        .ThenBy(brand => brand, StringComparer.Ordinal)
        .ToArray();

    [GeneratedRegex(
        @"\b([A-Za-z0-9]*[A-Za-z][A-Za-z0-9]*\d[A-Za-z0-9/-]*|\d+[A-Za-z][A-Za-z0-9/-]*)\b",
        RegexOptions.CultureInvariant)]
    private static partial Regex ModelTokenRegex();

    /// <summary>
    /// Identifica marca e código. "Smart TV Philips 43 43PFG6825/78" vira "Philips 43PFG6825".
    /// </summary>
    public static string ExtractProductModel(string? title)
    {
        var brand = FindBrand(title);
        var code = FindModelCode(title);
        if (brand.Length > 0 && code.Length > 0)
        {
            return $"{brand} {code}";
        }

        if (code.Length > 0)
        {
            return code;
        }

        return brand;
    }

    public static string FindBrand(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return string.Empty;
        }

        foreach (var brand in BrandsByLength)
        {
            if (ContainsWord(title, brand))
            {
                return brand;
            }
        }

        return string.Empty;
    }

    public static string FindModelCode(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return string.Empty;
        }

        foreach (Match match in ModelTokenRegex().Matches(title))
        {
            var token = match.Groups[1].Value;
            var slash = token.IndexOf('/');
            var code = (slash > 0 ? token[..slash] : token).Trim();
            if (code.Length is < 4 or > 24)
            {
                continue;
            }

            if (!code.Any(char.IsLetter) || !code.Any(char.IsDigit))
            {
                continue;
            }

            return code.ToUpperInvariant();
        }

        return string.Empty;
    }

    /// <summary>Mesmo código de modelo. Sem código nos dois títulos, compara a marca extraída.</summary>
    public static bool SameProductModel(string? leftTitle, string? rightTitle)
    {
        var leftCode = FindModelCode(leftTitle);
        var rightCode = FindModelCode(rightTitle);
        if (leftCode.Length > 0 && rightCode.Length > 0)
        {
            return string.Equals(leftCode, rightCode, StringComparison.OrdinalIgnoreCase);
        }

        if (leftCode.Length > 0 || rightCode.Length > 0)
        {
            return false;
        }

        var left = ExtractProductModel(leftTitle);
        var right = ExtractProductModel(rightTitle);
        return left.Length >= 3 && string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Com código, exige o mesmo modelo. Sem código, aceita títulos parecidos.</summary>
    public static bool IsComparableOffer(string? referenceTitle, string? candidateTitle)
    {
        if (SameProductModel(referenceTitle, candidateTitle))
        {
            return true;
        }

        if (FindModelCode(referenceTitle).Length > 0)
        {
            return false;
        }

        return AffiliateMiningRules.TitlesLookAlike(referenceTitle, candidateTitle);
    }

    private static bool ContainsWord(string text, string word)
    {
        var index = 0;
        while (index < text.Length)
        {
            var found = text.IndexOf(word, index, StringComparison.OrdinalIgnoreCase);
            if (found < 0)
            {
                return false;
            }

            var before = found == 0 || !char.IsLetterOrDigit(text[found - 1]);
            var afterIndex = found + word.Length;
            var after = afterIndex >= text.Length || !char.IsLetterOrDigit(text[afterIndex]);
            if (before && after)
            {
                return true;
            }

            index = found + word.Length;
        }

        return false;
    }
}
