namespace Ofertio.Business.Service.Master;

/// <summary>Regras do teste grátis. Sem acesso a banco nem a gateway.</summary>
public static class TrialRules
{
    public const int DefaultDays = 14;
    public const int MaxDays = 365;

    /// <summary>Dias de antecedência do aviso de fim do teste.</summary>
    public const int NoticeDaysBefore = 3;

    public const string DefaultPostTrialPlan = SaasPlanCodes.Starter;

    public static bool IsTrialPlan(string? code) =>
        string.Equals(code?.Trim(), SaasPlanCodes.Trial, StringComparison.OrdinalIgnoreCase);

    public static int NormalizeDays(int days) => days <= 0 ? DefaultDays : Math.Min(days, MaxDays);

    public static DateTime EndsAt(DateTime nowUtc, int days) => nowUtc.AddDays(NormalizeDays(days));

    public static bool ShouldSendEndingNotice(string? status, DateTime? trialEndsAt, DateTime? noticeSentAt, DateTime nowUtc) =>
        TenantStatuses.Normalize(status) == TenantStatuses.Trial
        && trialEndsAt.HasValue
        && noticeSentAt is null
        && trialEndsAt.Value > nowUtc
        && trialEndsAt.Value <= nowUtc.AddDays(NoticeDaysBefore);

    /// <summary>Com assinatura no gateway (cartão), o teste termina virando o plano pago; sem ela, a conta é bloqueada pelo ciclo de cobrança.</summary>
    public static bool ShouldConvertToPaid(string? status, DateTime? trialEndsAt, string? subscriptionId, DateTime nowUtc) =>
        TenantStatuses.Normalize(status) == TenantStatuses.Trial
        && trialEndsAt.HasValue
        && trialEndsAt.Value <= nowUtc
        && !string.IsNullOrWhiteSpace(subscriptionId);

    public static string ResolvePostTrialPlan(string? configured) =>
        string.IsNullOrWhiteSpace(configured) || IsTrialPlan(configured) ? DefaultPostTrialPlan : configured.Trim();
}