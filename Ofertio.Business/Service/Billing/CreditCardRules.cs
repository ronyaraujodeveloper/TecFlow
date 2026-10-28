using Ofertio.Business.Dto;

namespace Ofertio.Business.Service.Billing;

/// <summary>
/// Validação do cartão antes de enviar ao gateway. Nada aqui grava ou registra o número:
/// só bandeira e últimos 4 dígitos podem ser persistidos.
/// </summary>
public static class CreditCardRules
{
    private static readonly string[] EloPrefixes =
        ["636368", "636369", "438935", "504175", "451416", "636297", "506699", "5067", "4576", "4011", "509", "650", "651", "655"];

    private static readonly string[] HipercardPrefixes = ["606282", "3841"];

    public static string Digits(string? value) => new((value ?? string.Empty).Where(char.IsDigit).ToArray());

    public static bool IsLuhnValid(string digits)
    {
        if (digits.Length is < 13 or > 19 || !digits.All(char.IsDigit))
        {
            return false;
        }

        var sum = 0;
        var doubleIt = false;
        for (var i = digits.Length - 1; i >= 0; i--)
        {
            var digit = digits[i] - '0';
            if (doubleIt)
            {
                digit *= 2;
                if (digit > 9)
                {
                    digit -= 9;
                }
            }

            sum += digit;
            doubleIt = !doubleIt;
        }

        return sum % 10 == 0;
    }

    public static string DetectBrand(string? number)
    {
        var digits = Digits(number);
        if (digits.Length == 0)
        {
            return "Cartão";
        }

        if (EloPrefixes.Any(digits.StartsWith))
        {
            return "Elo";
        }

        if (HipercardPrefixes.Any(digits.StartsWith))
        {
            return "Hipercard";
        }

        if (digits.StartsWith("34") || digits.StartsWith("37"))
        {
            return "Amex";
        }

        if (digits.Length >= 2 && int.TryParse(digits[..2], out var two) && two is >= 51 and <= 55)
        {
            return "Mastercard";
        }

        if (digits.Length >= 4 && int.TryParse(digits[..4], out var four) && four is >= 2221 and <= 2720)
        {
            return "Mastercard";
        }

        return digits[0] == '4' ? "Visa" : "Cartão";
    }

    public static string? Last4(string? number)
    {
        var digits = Digits(number);
        return digits.Length >= 4 ? digits[^4..] : null;
    }

    /// <summary>Aceita MM/AA ou MM/AAAA (com ou sem barra).</summary>
    public static bool TryParseExpiry(string? expiry, out int month, out int year)
    {
        month = 0;
        year = 0;
        var digits = Digits(expiry);
        if (digits.Length is not (4 or 6) || !int.TryParse(digits[..2], out month) || month is < 1 or > 12)
        {
            return false;
        }

        if (!int.TryParse(digits[2..], out year))
        {
            return false;
        }

        if (digits.Length == 4)
        {
            year += 2000;
        }

        return true;
    }

    /// <summary>O cartão vale até o último dia do mês da validade.</summary>
    public static bool IsExpired(int month, int year, DateTime nowUtc) =>
        year < nowUtc.Year || (year == nowUtc.Year && month < nowUtc.Month);

    public static List<string> Validate(CreditCardDto? card, DateTime nowUtc)
    {
        var errors = new List<string>();
        if (card is null)
        {
            errors.Add("Informe os dados do cartão.");
            return errors;
        }

        var number = Digits(card.Number);
        if (!IsLuhnValid(number))
        {
            errors.Add("Número do cartão inválido.");
        }

        var holder = card.HolderName?.Trim() ?? string.Empty;
        if (holder.Length < 3 || !holder.Contains(' '))
        {
            errors.Add("Informe o nome impresso no cartão (nome e sobrenome).");
        }

        if (!TryParseExpiry(card.Expiry, out var month, out var year))
        {
            errors.Add("Validade inválida. Use MM/AA.");
        }
        else if (IsExpired(month, year, nowUtc) || year > nowUtc.Year + 20)
        {
            errors.Add("Cartão vencido ou validade fora do prazo.");
        }

        var cvv = Digits(card.Cvv);
        var expectedCvv = DetectBrand(number) == "Amex" ? 4 : 3;
        if (cvv.Length != expectedCvv || cvv.Length != (card.Cvv?.Trim().Length ?? 0))
        {
            errors.Add($"Código de segurança inválido ({expectedCvv} dígitos).");
        }

        return errors;
    }
}
