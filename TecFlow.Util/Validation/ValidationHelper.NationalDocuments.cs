namespace TecFlow.Util.Validation;

public static partial class ValidationHelper
{
    private const int CpfLength = 11;
    private const int CnpjLength = 14;
    private const int CnpjRootLength = 8;
    private const int CnpjSuffixLength = 4;

    private static ReadOnlySpan<int> CpfFirstCheckDigitWeights => [10, 9, 8, 7, 6, 5, 4, 3, 2];
    private static ReadOnlySpan<int> CpfSecondCheckDigitWeights => [11, 10, 9, 8, 7, 6, 5, 4, 3, 2];

    private static ReadOnlySpan<int> CnpjFirstCheckDigitWeights => [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
    private static ReadOnlySpan<int> CnpjSecondCheckDigitWeights => [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

    /// <summary>
    /// Valida CPF brasileiro (11 dígitos + dígitos verificadores módulo 11).
    /// </summary>
    public static bool IsValidCpf(string? cpf)
    {
        if (!TryCleanDigits(cpf, CpfLength, out var cleaned))
        {
            return false;
        }

        if (HasAllSameCharacters(cleaned))
        {
            return false;
        }

        var firstCheckDigit = CalculateMod11CheckDigit(cleaned.AsSpan(0, 9), CpfFirstCheckDigitWeights);
        if (cleaned[9] - '0' != firstCheckDigit)
        {
            return false;
        }

        var secondCheckDigit = CalculateMod11CheckDigit(cleaned.AsSpan(0, 10), CpfSecondCheckDigitWeights);
        return cleaned[10] - '0' == secondCheckDigit;
    }

    /// <summary>
    /// Valida CNPJ no padrão alfanumérico (IN RFB nº 2.229/2024):
    /// raiz alfanumérica (8), sufixo numérico (4) e DVs numéricos (2).
    /// CNPJs legados somente numéricos continuam válidos quando passam no módulo 11 adaptado (ASCII - 48).
    /// </summary>
    public static bool IsValidCnpj(string? cnpj)
    {
        if (!TryCleanAlphanumericDocument(cnpj, CnpjLength, out var cleaned))
        {
            return false;
        }

        // Raiz (8 primeiras posições): letras maiúsculas A-Z ou dígitos 0-9.
        for (var i = 0; i < CnpjRootLength; i++)
        {
            if (!IsAlphanumericCharacter(cleaned[i]))
            {
                return false;
            }
        }

        // Sufixo de ordem (4 posições) e DVs (2 posições): estritamente numéricos.
        for (var i = CnpjRootLength; i < CnpjLength; i++)
        {
            if (!char.IsDigit(cleaned[i]))
            {
                return false;
            }
        }

        if (HasAllSameCharacters(cleaned))
        {
            return false;
        }

        // Módulo 11 com valores ASCII - 48 (ex.: 'A' = 17, '0' = 0), conforme nova legislação.
        var firstCheckDigit = CalculateMod11CheckDigit(
            cleaned.AsSpan(0, CnpjRootLength + CnpjSuffixLength),
            CnpjFirstCheckDigitWeights);

        if (cleaned[12] - '0' != firstCheckDigit)
        {
            return false;
        }

        Span<char> documentWithFirstCheckDigit = stackalloc char[13];
        cleaned.AsSpan(0, 12).CopyTo(documentWithFirstCheckDigit);
        documentWithFirstCheckDigit[12] = (char)('0' + firstCheckDigit);

        var secondCheckDigit = CalculateMod11CheckDigit(
            documentWithFirstCheckDigit,
            CnpjSecondCheckDigitWeights);

        return cleaned[13] - '0' == secondCheckDigit;
    }

    /// <summary>
    /// Remove pontuação e mantém apenas dígitos.
    /// </summary>
    private static bool TryCleanDigits(string? value, int expectedLength, out string cleaned)
    {
        cleaned = string.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        Span<char> buffer = stackalloc char[value.Length];
        var length = 0;

        foreach (var character in value)
        {
            if (char.IsDigit(character))
            {
                buffer[length++] = character;
            }
        }

        if (length != expectedLength)
        {
            return false;
        }

        cleaned = new string(buffer[..length]);
        return true;
    }

    /// <summary>
    /// Remove pontuação (. / -) e normaliza letras para maiúsculas.
    /// </summary>
    private static bool TryCleanAlphanumericDocument(string? value, int expectedLength, out string cleaned)
    {
        cleaned = string.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        Span<char> buffer = stackalloc char[value.Length];
        var length = 0;

        foreach (var character in value)
        {
            if (char.IsDigit(character))
            {
                buffer[length++] = character;
                continue;
            }

            if (character is >= 'A' and <= 'Z')
            {
                buffer[length++] = character;
                continue;
            }

            if (character is >= 'a' and <= 'z')
            {
                buffer[length++] = char.ToUpperInvariant(character);
            }
        }

        if (length != expectedLength)
        {
            return false;
        }

        cleaned = new string(buffer[..length]);
        return true;
    }

    /// <summary>
    /// Converte caractere alfanumérico do CNPJ para valor decimal (ASCII - 48), base do módulo 11 alfanumérico.
    /// </summary>
    private static int GetCnpjCharacterValue(char character) => character - 48;

    private static int CalculateMod11CheckDigit(ReadOnlySpan<char> document, ReadOnlySpan<int> weights)
    {
        var sum = 0;

        for (var i = 0; i < weights.Length; i++)
        {
            var value = char.IsDigit(document[i])
                ? document[i] - '0'
                : GetCnpjCharacterValue(document[i]);

            sum += value * weights[i];
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }

    private static bool IsAlphanumericCharacter(char character) =>
        char.IsDigit(character) || character is >= 'A' and <= 'Z';

    private static bool HasAllSameCharacters(string value)
    {
        for (var i = 1; i < value.Length; i++)
        {
            if (value[i] != value[0])
            {
                return false;
            }
        }

        return true;
    }
}
