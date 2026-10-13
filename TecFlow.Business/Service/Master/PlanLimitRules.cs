namespace TecFlow.Business.Service.Master;

/// <summary>Limites efetivos de um plano. Valor 0 significa ilimitado.</summary>
public sealed record PlanLimits(
    string PlanCode,
    int MaxStores,
    int DailyVipQuota,
    bool RadarEnabled,
    int MaxDailySchedules)
{
    public static readonly PlanLimits Unlimited = new(string.Empty, 0, 0, true, 0);

    public bool IsUnlimited => string.IsNullOrEmpty(PlanCode);
}

public static class SaasPlanCodes
{
    public const string Starter = "Starter";
    public const string Pro = "Pro";
    public const string Agency = "Agency";
    public const string Enterprise = "Enterprise";

    public static readonly IReadOnlyList<string> All = [Starter, Pro, Agency, Enterprise];
}

public static class PlanLimitRules
{
    public static bool ExceedsCount(int limit, int currentCount) => limit > 0 && currentCount >= limit;

    public static string StoresLimitMessage(PlanLimits limits) =>
        $"Seu plano {limits.PlanCode} permite até {limits.MaxStores} loja(s) conectada(s). Faça upgrade para conectar mais.";

    public static string SchedulesLimitMessage(PlanLimits limits) =>
        $"Seu plano {limits.PlanCode} permite até {limits.MaxDailySchedules} agendamento(s) por dia. Faça upgrade para agendar mais.";

    public static string RadarBlockedMessage(PlanLimits limits) =>
        $"O Radar de Ofertas não está incluído no plano {limits.PlanCode}. Faça upgrade para liberar.";

    public const int UnlimitedDailyQuota = 999;

    /// <summary>Sem plano usa o padrão; plano com cota 0 é ilimitado (teto técnico de 999/dia).</summary>
    public static int EffectiveDailyQuota(PlanLimits limits, int defaultQuota)
    {
        if (limits.IsUnlimited)
        {
            return defaultQuota;
        }

        return limits.DailyVipQuota <= 0 ? UnlimitedDailyQuota : limits.DailyVipQuota;
    }
}
