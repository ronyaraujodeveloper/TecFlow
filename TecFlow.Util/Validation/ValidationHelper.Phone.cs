using System.Text.RegularExpressions;

namespace TecFlow.Util.Validation;

public static partial class ValidationHelper
{
    private const int BrazilianCellPhoneLength = 11;
    private const int BrazilianCellPhoneWithCountryCodeLength = 13;
    private const string BrazilCountryCode = "55";

    // DDDs válidos no Brasil + nono dígito 9 + operadora móvel (6-9) + 7 dígitos restantes.
    [GeneratedRegex(
        @"^(?:11|12|13|14|15|16|17|18|19|21|22|24|27|28|31|32|33|34|35|37|38|41|42|43|44|45|46|47|48|49|51|53|54|61|62|63|64|65|66|67|68|69|71|73|74|75|77|79|81|82|83|84|85|86|87|88|89|91|92|93|94|95|96|97|98|99)9[6-9]\d{7}$",
        RegexOptions.CultureInvariant)]
    private static partial Regex BrazilianCellPhoneRegex();

    /// <summary>
    /// Valida celular brasileiro/WhatsApp (11 dígitos: DDD + 9 + número).
    /// Aceita formatação comum e código do país (+55) opcional.
    /// </summary>
    public static bool IsValidBrazilianCellPhone(string? phone)
    {
        var normalizedPhone = NormalizeBrazilianCellPhoneDigits(phone);
        return normalizedPhone is not null && BrazilianCellPhoneRegex().IsMatch(normalizedPhone);
    }

    /// <summary>
    /// Normaliza celular brasileiro para 11 dígitos (DDD + número), removendo formatação e código +55.
    /// Retorna null se o formato não puder ser normalizado.
    /// </summary>
    public static string? NormalizeBrazilianCellPhone(string? phone) =>
        NormalizeBrazilianCellPhoneDigits(phone);

    /// <summary>
    /// Remove caracteres não numéricos e normaliza código do país (+55), quando presente.
    /// </summary>
    private static string? NormalizeBrazilianCellPhoneDigits(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        Span<char> buffer = stackalloc char[phone.Length];
        var length = 0;

        foreach (var character in phone)
        {
            if (char.IsDigit(character))
            {
                buffer[length++] = character;
            }
        }

        if (length == 0)
        {
            return null;
        }

        var digits = buffer[..length];

        // Remove código do país 55 quando informado no formato internacional (+55...).
        if (length == BrazilianCellPhoneWithCountryCodeLength &&
            digits.StartsWith(BrazilCountryCode))
        {
            digits = digits[2..];
            length = digits.Length;
        }

        return length == BrazilianCellPhoneLength ? digits.ToString() : null;
    }
}
