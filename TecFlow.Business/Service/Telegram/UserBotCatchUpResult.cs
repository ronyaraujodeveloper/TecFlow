namespace TecFlow.Business.Service.Telegram;

public sealed class UserBotCatchUpResult
{
    public bool UserBotReady { get; init; }

    public bool Completed { get; init; }

    public int Channels { get; init; }

    public int Persisted { get; init; }

    public string Message { get; init; } = string.Empty;

    public static UserBotCatchUpResult Offline() => new()
    {
        UserBotReady = false,
        Completed = false,
        Message = TelegramUserMonitorRules.UserBotOfflineSyncMessage
    };

    public static UserBotCatchUpResult Done(int channels, int persisted) => new()
    {
        UserBotReady = true,
        Completed = true,
        Channels = channels,
        Persisted = persisted,
        Message = persisted > 0
            ? $"Histórico UserBot lido: {persisted} mensagem(ns) com link em {channels} canal(is)."
            : $"UserBot autenticado ({channels} canal(is)), mas o histórico do período não trouxe links novos."
    };
}
