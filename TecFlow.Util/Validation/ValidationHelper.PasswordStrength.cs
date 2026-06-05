using System.Text.RegularExpressions;

namespace TecFlow.Util.Validation;

public static partial class ValidationHelper
{
    private const int MinimumPasswordLength = 8;

    private const string PasswordRequiredMessage = "A senha é obrigatória.";
    private const string PasswordMinLengthMessage = "A senha deve ter pelo menos 8 caracteres.";
    private const string PasswordUppercaseMessage = "A senha deve conter pelo menos uma letra maiúscula.";
    private const string PasswordLowercaseMessage = "A senha deve conter pelo menos uma letra minúscula.";
    private const string PasswordDigitMessage = "A senha deve conter pelo menos um número.";
    private const string PasswordSpecialCharacterMessage = "A senha deve conter pelo menos um caractere especial.";

    [GeneratedRegex(@"[A-Z]", RegexOptions.CultureInvariant)]
    private static partial Regex PasswordUppercaseRegex();

    [GeneratedRegex(@"[a-z]", RegexOptions.CultureInvariant)]
    private static partial Regex PasswordLowercaseRegex();

    [GeneratedRegex(@"[0-9]", RegexOptions.CultureInvariant)]
    private static partial Regex PasswordDigitRegex();

    // Caracteres especiais: qualquer símbolo que não seja letra ou dígito (ex.: @, #, $, %).
    [GeneratedRegex(@"[^a-zA-Z0-9]", RegexOptions.CultureInvariant)]
    private static partial Regex PasswordSpecialCharacterRegex();

    /// <summary>
    /// Valida força da senha conforme critérios de segurança do ecossistema TecFlow.
    /// </summary>
    public static PasswordValidationResult ValidatePasswordStrength(string? password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return new PasswordValidationResult
            {
                IsValid = false,
                Errors = [PasswordRequiredMessage]
            };
        }

        var errors = new List<string>(capacity: 5);

        if (password.Length < MinimumPasswordLength)
        {
            errors.Add(PasswordMinLengthMessage);
        }

        if (!PasswordUppercaseRegex().IsMatch(password))
        {
            errors.Add(PasswordUppercaseMessage);
        }

        if (!PasswordLowercaseRegex().IsMatch(password))
        {
            errors.Add(PasswordLowercaseMessage);
        }

        if (!PasswordDigitRegex().IsMatch(password))
        {
            errors.Add(PasswordDigitMessage);
        }

        if (!PasswordSpecialCharacterRegex().IsMatch(password))
        {
            errors.Add(PasswordSpecialCharacterMessage);
        }

        return new PasswordValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }
}
