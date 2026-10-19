namespace Ofertio.Business.Service.Groups;

public static class MonitoredGroupSyncRules
{
    public const string BackgroundStartedMessage =
        "Sincronização iniciada! Os links estão sendo capturados em segundo plano.";

    public const string FiltersResetMessage = "Filtros redefinidos com sucesso.";

    public static readonly TimeSpan ClientTimeout = TimeSpan.FromMinutes(3);

    public static readonly TimeSpan StatusClockTolerance = TimeSpan.FromSeconds(5);

    public static TimeSpan ResolveHttpTimeout(int configuredSeconds) =>
        configuredSeconds > ClientTimeout.TotalSeconds
            ? TimeSpan.FromSeconds(configuredSeconds)
            : ClientTimeout;

    public static string BuildCompletionMessage(int groupsBefore, int groupsAfter, int offersBefore, int offersAfter)
    {
        var newGroups = Math.Max(0, groupsAfter - groupsBefore);
        var newOffers = Math.Max(0, offersAfter - offersBefore);
        var groupsLabel = newGroups == 1 ? "novo grupo" : "novos grupos";
        var offersLabel = newOffers == 1 ? "nova mensagem atualizada" : "novas mensagens atualizadas";
        return $"Sincronização concluída! {newGroups} {groupsLabel} e {newOffers} {offersLabel}.";
    }

    public static bool IsStatusFromCurrentRun(DateTime? statusUpdatedAtUtc, DateTime syncStartedAtUtc) =>
        statusUpdatedAtUtc is { } updatedAt
        && updatedAt >= syncStartedAtUtc - StatusClockTolerance;
}
