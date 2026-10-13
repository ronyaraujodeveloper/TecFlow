namespace TecFlow.Business.Service.Master;

public static class TenantStatuses
{
    public const string Ativo = "Ativo";
    public const string Trial = "Trial";
    public const string Inadimplente = "Inadimplente";
    public const string Pausado = "Pausado";
    public const string Suspenso = "Suspenso";
    public const string Cancelado = "Cancelado";

    public static readonly IReadOnlyList<string> All = [Ativo, Trial, Inadimplente, Pausado, Suspenso, Cancelado];

    public static string Normalize(string? status) =>
        All.FirstOrDefault(item => string.Equals(item, status?.Trim(), StringComparison.OrdinalIgnoreCase)) ?? Ativo;
}

public static class TenantLifecycleActions
{
    public const string Pausar = "pausar";
    public const string Suspender = "suspender";
    public const string Reativar = "reativar";
    public const string Cancelar = "cancelar";
    public const string EstenderTrial = "estender-trial";
    public const string MarcarInadimplente = "marcar-inadimplente";
}

/// <summary>Regras de ciclo de vida e bloqueio de acesso de inquilinos.</summary>
public static class TenantLifecycleRules
{
    public const int OverdueGraceDays = 5;
    public const int DefaultTrialExtensionDays = 7;

    public const string BlockedMessage =
        "Sua conta TecFlow está {0}. Regularize a assinatura ou fale com o suporte para reativar o acesso.";

    public static bool IsAccessBlocked(string? status)
    {
        var normalized = TenantStatuses.Normalize(status);
        return normalized is TenantStatuses.Pausado or TenantStatuses.Suspenso or TenantStatuses.Cancelado;
    }

    public static string FormatBlockedMessage(string? status) =>
        string.Format(BlockedMessage, TenantStatuses.Normalize(status).ToLowerInvariant());

    public static bool RequiresReason(string action) =>
        action is TenantLifecycleActions.Pausar or TenantLifecycleActions.Suspender or TenantLifecycleActions.Cancelar;

    /// <summary>Calcula o novo estado; retorna null se a ação for inválida para o status atual.</summary>
    public static TenantLifecycleTransition? Apply(
        string? currentStatus,
        string action,
        DateTime now,
        DateTime? currentTrialEndsAt,
        int? trialDays = null)
    {
        var status = TenantStatuses.Normalize(currentStatus);
        var normalizedAction = action?.Trim().ToLowerInvariant() ?? string.Empty;
        if (status == TenantStatuses.Cancelado && normalizedAction != TenantLifecycleActions.Reativar)
        {
            return null;
        }

        return normalizedAction switch
        {
            TenantLifecycleActions.Pausar => new TenantLifecycleTransition(TenantStatuses.Pausado, currentTrialEndsAt, null),
            TenantLifecycleActions.Suspender => new TenantLifecycleTransition(TenantStatuses.Suspenso, currentTrialEndsAt, null),
            TenantLifecycleActions.Cancelar => new TenantLifecycleTransition(TenantStatuses.Cancelado, currentTrialEndsAt, now),
            TenantLifecycleActions.MarcarInadimplente => new TenantLifecycleTransition(TenantStatuses.Inadimplente, currentTrialEndsAt, null),
            TenantLifecycleActions.Reativar => new TenantLifecycleTransition(
                currentTrialEndsAt > now ? TenantStatuses.Trial : TenantStatuses.Ativo,
                currentTrialEndsAt,
                null),
            TenantLifecycleActions.EstenderTrial => new TenantLifecycleTransition(
                TenantStatuses.Trial,
                (currentTrialEndsAt > now ? currentTrialEndsAt.Value : now)
                    .AddDays(trialDays is > 0 ? trialDays.Value : DefaultTrialExtensionDays),
                null),
            _ => null
        };
    }

    /// <summary>Inadimplente além da carência é suspenso automaticamente.</summary>
    public static bool ShouldAutoSuspend(string? status, DateTime? statusChangedAt, DateTime now) =>
        TenantStatuses.Normalize(status) == TenantStatuses.Inadimplente
        && statusChangedAt.HasValue
        && statusChangedAt.Value.AddDays(OverdueGraceDays) <= now;

    /// <summary>Trial vencido sem assinatura no gateway passa a inadimplente.</summary>
    public static bool ShouldExpireTrial(string? status, DateTime? trialEndsAt, string? subscriptionId, DateTime now) =>
        TenantStatuses.Normalize(status) == TenantStatuses.Trial
        && trialEndsAt.HasValue
        && trialEndsAt.Value <= now
        && string.IsNullOrWhiteSpace(subscriptionId);
}

public sealed record TenantLifecycleTransition(string Status, DateTime? TrialEndsAt, DateTime? CanceledAt);
