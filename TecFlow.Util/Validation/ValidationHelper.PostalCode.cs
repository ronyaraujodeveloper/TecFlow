using System.Text.RegularExpressions;

namespace TecFlow.Util.Validation;

public static partial class ValidationHelper
{
    private const int CepLength = 8;

    [GeneratedRegex(@"^[0-9]{5}-?[0-9]{3}$", RegexOptions.CultureInvariant)]
    private static partial Regex CepFormatRegex();

    /// <summary>
    /// Valida formato de CEP brasileiro (8 dígitos, com ou sem hífen).
    /// </summary>
    public static bool IsValidCep(string? cep)
    {
        if (string.IsNullOrWhiteSpace(cep))
        {
            return false;
        }

        return CepFormatRegex().IsMatch(cep.Trim());
    }

    /// <summary>
    /// Normaliza CEP válido para 8 dígitos (sem hífen), pronto para consulta externa.
    /// </summary>
    public static string? NormalizeCepDigits(string? cep)
    {
        if (!IsValidCep(cep))
        {
            return null;
        }

        Span<char> buffer = stackalloc char[CepLength];
        var length = 0;

        foreach (var character in cep!)
        {
            if (char.IsDigit(character))
            {
                buffer[length++] = character;
            }
        }

        return length == CepLength ? new string(buffer) : null;
    }
}
