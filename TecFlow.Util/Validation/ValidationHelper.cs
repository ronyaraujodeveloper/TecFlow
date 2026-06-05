using System.Text.RegularExpressions;

namespace TecFlow.Util.Validation;

/// <summary>
/// Utilitários de validação compartilhados pelo ecossistema TecFlow.
/// </summary>
public static partial class ValidationHelper
{
    private const int MaxEmailLength = 254;

    [GeneratedRegex(
        @"^(?![.])(?!.*[.]{2})[a-zA-Z0-9._%+-]+(?<![.])@[a-zA-Z0-9-]+(?:\.[a-zA-Z0-9-]+)*\.[a-zA-Z]{2,}$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex EmailFormatRegex();

    /// <summary>
    /// Valida formato de e-mail. Retorna false para null, vazio, whitespace ou formato inválido.
    /// </summary>
    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        var normalized = email.Trim();

        if (normalized.Length > MaxEmailLength)
        {
            return false;
        }

        return EmailFormatRegex().IsMatch(normalized);
    }
}
