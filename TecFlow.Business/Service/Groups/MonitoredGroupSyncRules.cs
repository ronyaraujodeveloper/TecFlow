namespace TecFlow.Business.Service.Groups;

public static class MonitoredGroupSyncRules
{
    public const string BackgroundStartedMessage =
        "Sincronização iniciada! Os links estão sendo capturados em segundo plano.";

    public static readonly TimeSpan ClientTimeout = TimeSpan.FromMinutes(3);

    public static TimeSpan ResolveHttpTimeout(int configuredSeconds) =>
        configuredSeconds > ClientTimeout.TotalSeconds
            ? TimeSpan.FromSeconds(configuredSeconds)
            : ClientTimeout;
}
