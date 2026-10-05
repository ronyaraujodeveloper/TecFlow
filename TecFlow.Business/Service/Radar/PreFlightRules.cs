using System.Globalization;
using System.Text.RegularExpressions;
using TecFlow.Business.Service.Groups;

namespace TecFlow.Business.Service.Radar;

public static class PreFlightRules
{
    public static readonly TimeSpan Lookahead = TimeSpan.FromMinutes(15);

    public const string AlertSoldOut = "Esgotado";
    public const string AlertCoupon = "CupomExpirado";
    public const string AlertPriceUp = "PrecoAlterado";

    private static readonly Regex HttpUrlRegex = new(
        @"https?://[^\s<>""']+",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static bool IsInLookahead(DateTime scheduledAtUtc, DateTime utcNow) =>
        scheduledAtUtc <= utcNow.Add(Lookahead);

    public static bool IsPriceIncrease(decimal? expected, decimal? current)
    {
        if (expected is not > 0 || current is not > 0)
        {
            return false;
        }

        return current.Value > expected.Value + 0.009m;
    }

    public static string FormatMoney(decimal? value)
    {
        if (value is not > 0)
        {
            return "—";
        }

        return value.Value.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));
    }

    public static string BuildPauseMessage(string alertType, decimal? expected, decimal? current, string? coupon)
    {
        if (alertType == AlertPriceUp)
        {
            return $"Cancelado: Preço alterado de {FormatMoney(expected)} para {FormatMoney(current)}.";
        }

        if (alertType == AlertCoupon)
        {
            var code = string.IsNullOrWhiteSpace(coupon) ? "informado" : coupon.Trim();
            return $"Cancelado: cupom {code} expirado ou ausente na página.";
        }

        return "Cancelado: produto esgotado ou página indisponível.";
    }

    public static decimal? ExtractExpectedPrice(string? message, string? url) =>
        GroupOfferCaptureRules.ExtractPrice(message, url);

    public static string ReplaceFirstUrl(string? message, string replacement)
    {
        var text = message ?? string.Empty;
        var match = HttpUrlRegex.Match(text);
        if (!match.Success || string.IsNullOrWhiteSpace(replacement))
        {
            return text;
        }

        return string.Concat(text.AsSpan(0, match.Index), replacement.Trim(), text.AsSpan(match.Index + match.Length));
    }
}
