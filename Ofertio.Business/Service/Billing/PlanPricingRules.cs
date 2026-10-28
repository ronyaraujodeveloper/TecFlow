using Ofertio.Business.Dto;
using Ofertio.Business.Service.Master;

namespace Ofertio.Business.Service.Billing;

/// <summary>Preço por ciclo, economia do anual e vantagens exibidas em Meu plano.</summary>
public static class PlanPricingRules
{
    public const int HighlightsMaxLength = 2000;

    /// <summary>Mensal ou Anual; null quando o ciclo não é reconhecido.</summary>
    public static string? NormalizeCycle(string? cycle)
    {
        var value = cycle?.Trim();
        if (string.Equals(value, SaasMetricsRules.CycleMonthly, StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "monthly", StringComparison.OrdinalIgnoreCase))
        {
            return SaasMetricsRules.CycleMonthly;
        }

        if (string.Equals(value, SaasMetricsRules.CycleAnnual, StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "yearly", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "annual", StringComparison.OrdinalIgnoreCase))
        {
            return SaasMetricsRules.CycleAnnual;
        }

        return null;
    }

    public static decimal Price(SaasPlanDto plan, string? cycle) =>
        NormalizeCycle(cycle) == SaasMetricsRules.CycleAnnual ? plan.AnnualPrice : plan.MonthlyPrice;

    public static bool IsFree(SaasPlanDto plan) => plan.MonthlyPrice <= 0 && plan.AnnualPrice <= 0;

    public static decimal MonthlyEquivalent(decimal annualPrice) =>
        annualPrice <= 0 ? 0 : Math.Round(annualPrice / 12m, 2, MidpointRounding.AwayFromZero);

    /// <summary>Percentual economizado no anual frente a 12 mensalidades (0 quando não há desconto).</summary>
    public static int AnnualSavingsPercent(decimal monthlyPrice, decimal annualPrice)
    {
        if (monthlyPrice <= 0 || annualPrice <= 0)
        {
            return 0;
        }

        var full = monthlyPrice * 12m;
        return annualPrice >= full
            ? 0
            : (int)Math.Round((full - annualPrice) / full * 100m, MidpointRounding.AwayFromZero);
    }

    /// <summary>Uma vantagem por linha, sem linhas vazias, cortada no limite da coluna.</summary>
    public static string? NormalizeHighlights(string? highlights)
    {
        var lines = HighlightLines(highlights);
        if (lines.Count == 0)
        {
            return null;
        }

        var text = string.Join("\n", lines);
        return text.Length <= HighlightsMaxLength ? text : text[..HighlightsMaxLength];
    }

    public static List<string> HighlightLines(string? highlights) =>
        (highlights ?? string.Empty)
            .Split('\n')
            .Select(line => line.Trim().TrimStart('-', '•', '*').Trim())
            .Where(line => line.Length > 0)
            .ToList();

    /// <summary>Limites do plano em linguagem do cliente (0 = ilimitado).</summary>
    public static List<string> LimitLines(SaasPlanDto plan) =>
    [
        plan.MaxStores > 0 ? $"Até {plan.MaxStores} loja(s) conectada(s)" : "Lojas conectadas ilimitadas",
        plan.MaxDailySchedules > 0
            ? $"Até {plan.MaxDailySchedules} disparo(s) por dia (WhatsApp + Telegram)"
            : "Disparos ilimitados (WhatsApp + Telegram)",
        plan.DailyVipQuota > 0 ? $"{plan.DailyVipQuota} achadinho(s) VIP por dia" : "Achadinhos VIP ilimitados",
        plan.RadarEnabled ? "Radar de ofertas incluído" : "Sem radar de ofertas"
    ];
}
