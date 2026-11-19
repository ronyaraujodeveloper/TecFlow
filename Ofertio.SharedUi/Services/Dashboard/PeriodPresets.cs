using Ofertio.Business.Service.Attribution;

namespace Ofertio.SharedUi.Services.Dashboard;

/// <summary>Período escolhido no painel: datas de Brasília e agrupamento.</summary>
public sealed record PeriodSelection(DateOnly From, DateOnly To, AnalyticsGrain Grain);

/// <summary>Atalhos de período (Hoje, 7 dias, 30 dias, 12 meses) e a ligação deles com o agrupamento sugerido.</summary>
public static class PeriodPresets
{
    public const string Today = "hoje";
    public const string Last7 = "7d";
    public const string Last30 = "30d";
    public const string Last12Months = "12m";
    public const string Custom = "custom";

    public static readonly IReadOnlyList<(string Key, string Label)> All =
    [
        (Today, "Hoje"),
        (Last7, "7 dias"),
        (Last30, "30 dias"),
        (Last12Months, "12 meses"),
        (Custom, "Personalizado")
    ];

    public static DateOnly TodayLocal() => AnalyticsPeriodRules.ToLocalDate(DateTime.UtcNow);

    public static PeriodSelection Resolve(string key, DateOnly today) => key switch
    {
        Today => new PeriodSelection(today, today, AnalyticsGrain.Day),
        Last7 => new PeriodSelection(today.AddDays(-6), today, AnalyticsGrain.Day),
        Last12Months => new PeriodSelection(FirstDayOfMonth(today.AddMonths(-11)), today, AnalyticsGrain.Month),
        _ => new PeriodSelection(today.AddDays(-29), today, AnalyticsGrain.Day)
    };

    /// <summary>Qual atalho corresponde ao período atual; "custom" se nenhum.</summary>
    public static string KeyFor(PeriodSelection selection, DateOnly today)
    {
        foreach (var key in new[] { Today, Last7, Last30, Last12Months })
        {
            var preset = Resolve(key, today);
            if (preset.From == selection.From && preset.To == selection.To)
            {
                return key;
            }
        }

        return Custom;
    }

    /// <summary>Lê o período da URL (?from=&to=&grain=); se faltar ou for inválido, usa 30 dias.</summary>
    public static PeriodSelection FromQuery(string? from, string? to, string? grain, DateOnly today)
    {
        if (DateOnly.TryParseExact(from, "yyyy-MM-dd", out var fromDate)
            && DateOnly.TryParseExact(to, "yyyy-MM-dd", out var toDate))
        {
            var parsed = AnalyticsPeriodRules.TryParseGrain(grain, out var parsedGrain)
                ? parsedGrain
                : AnalyticsPeriodRules.SuggestGrain(fromDate, toDate);
            if (AnalyticsPeriodRules.Validate(fromDate, toDate, parsed) is null)
            {
                return new PeriodSelection(fromDate, toDate, parsed);
            }
        }

        return Resolve(Last30, today);
    }

    public static string ToQuery(PeriodSelection selection) =>
        $"from={selection.From:yyyy-MM-dd}&to={selection.To:yyyy-MM-dd}&grain={AnalyticsPeriodRules.GrainName(selection.Grain)}";

    private static DateOnly FirstDayOfMonth(DateOnly date) => new(date.Year, date.Month, 1);
}
