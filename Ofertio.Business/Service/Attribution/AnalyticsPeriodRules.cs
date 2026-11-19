using System.Globalization;

namespace Ofertio.Business.Service.Attribution;

public enum AnalyticsGrain
{
    Day,
    Week,
    Month,
    Year
}

/// <summary>
/// Períodos do painel: datas locais de Brasília (fixo UTC-3, o país não tem horário de verão desde 2019),
/// agrupamento por dia, semana (segunda a domingo), mês e ano, e o período anterior de mesmo tamanho.
/// </summary>
public static class AnalyticsPeriodRules
{
    public static readonly TimeSpan LocalOffset = TimeSpan.FromHours(-3);
    public const int MaxSpanDays = 800;
    public const int MaxDaySpanDays = 120;

    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public static bool TryParseGrain(string? raw, out AnalyticsGrain grain)
    {
        grain = AnalyticsGrain.Day;
        switch (raw?.Trim().ToLowerInvariant())
        {
            case "day" or "dia":
                grain = AnalyticsGrain.Day;
                return true;
            case "week" or "semana":
                grain = AnalyticsGrain.Week;
                return true;
            case "month" or "mes" or "mês":
                grain = AnalyticsGrain.Month;
                return true;
            case "year" or "ano":
                grain = AnalyticsGrain.Year;
                return true;
            default:
                return false;
        }
    }

    public static string GrainName(AnalyticsGrain grain) => grain.ToString().ToLowerInvariant();

    /// <summary>Mensagem de erro se o pedido não é válido; nulo se está tudo certo.</summary>
    public static string? Validate(DateOnly from, DateOnly to, AnalyticsGrain grain)
    {
        if (to < from)
        {
            return "A data final não pode ser anterior à inicial.";
        }

        var span = to.DayNumber - from.DayNumber + 1;
        if (span > MaxSpanDays)
        {
            return $"O período máximo é de {MaxSpanDays} dias.";
        }

        if (grain == AnalyticsGrain.Day && span > MaxDaySpanDays)
        {
            return $"Para períodos acima de {MaxDaySpanDays} dias, agrupe por semana, mês ou ano.";
        }

        return null;
    }

    public static AnalyticsGrain SuggestGrain(DateOnly from, DateOnly to)
    {
        var span = to.DayNumber - from.DayNumber + 1;
        return span switch
        {
            <= 45 => AnalyticsGrain.Day,
            <= 180 => AnalyticsGrain.Week,
            <= 800 => AnalyticsGrain.Month,
            _ => AnalyticsGrain.Year
        };
    }

    /// <summary>Intervalo UTC [início, fim) que cobre as datas locais de Brasília.</summary>
    public static (DateTime FromUtc, DateTime ToUtcExclusive) ToUtcRange(DateOnly fromLocal, DateOnly toLocal) =>
    (
        DateTime.SpecifyKind(fromLocal.ToDateTime(TimeOnly.MinValue) - LocalOffset, DateTimeKind.Utc),
        DateTime.SpecifyKind(toLocal.AddDays(1).ToDateTime(TimeOnly.MinValue) - LocalOffset, DateTimeKind.Utc)
    );

    /// <summary>Período imediatamente anterior, com o mesmo número de dias.</summary>
    public static (DateOnly From, DateOnly To) Previous(DateOnly from, DateOnly to)
    {
        var span = to.DayNumber - from.DayNumber + 1;
        return (from.AddDays(-span), from.AddDays(-1));
    }

    public static DateOnly ToLocalDate(DateTime utc) =>
        DateOnly.FromDateTime(DateTime.SpecifyKind(utc, DateTimeKind.Utc).Add(LocalOffset));

    public static DateTime ToLocalTime(DateTime utc) =>
        DateTime.SpecifyKind(utc, DateTimeKind.Utc).Add(LocalOffset);

    public static DateOnly BucketStart(DateOnly localDate, AnalyticsGrain grain) => grain switch
    {
        AnalyticsGrain.Week => localDate.AddDays(-(((int)localDate.DayOfWeek + 6) % 7)),
        AnalyticsGrain.Month => new DateOnly(localDate.Year, localDate.Month, 1),
        AnalyticsGrain.Year => new DateOnly(localDate.Year, 1, 1),
        _ => localDate
    };

    public static DateOnly NextBucket(DateOnly start, AnalyticsGrain grain) => grain switch
    {
        AnalyticsGrain.Week => start.AddDays(7),
        AnalyticsGrain.Month => start.AddMonths(1),
        AnalyticsGrain.Year => start.AddYears(1),
        _ => start.AddDays(1)
    };

    /// <summary>Todos os grupos do intervalo, inclusive os sem cliques (o gráfico não pula datas).</summary>
    public static IReadOnlyList<DateOnly> Buckets(DateOnly from, DateOnly to, AnalyticsGrain grain)
    {
        var result = new List<DateOnly>();
        for (var cursor = BucketStart(from, grain); cursor <= to; cursor = NextBucket(cursor, grain))
        {
            result.Add(cursor);
        }

        return result;
    }

    public static string BucketLabel(DateOnly start, AnalyticsGrain grain) => grain switch
    {
        AnalyticsGrain.Week => "sem. " + start.ToString("dd/MM", PtBr),
        AnalyticsGrain.Month => start.ToString("MMM/yy", PtBr),
        AnalyticsGrain.Year => start.Year.ToString(PtBr),
        _ => start.ToString("dd/MM", PtBr)
    };

    /// <summary>Linha 0 = segunda-feira ... 6 = domingo.</summary>
    public static int WeekdayRow(DateTime localTime) => ((int)localTime.DayOfWeek + 6) % 7;
}
