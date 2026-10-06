namespace TecFlow.Business.Dto;

public static class UserBotSyncPhases
{
    public const string Idle = "Idle";
    public const string Running = "Running";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
}

public class UserBotSyncStatusDto
{
    public bool Status { get; set; } = true;

    public string Phase { get; set; } = UserBotSyncPhases.Idle;

    public string Message { get; set; } = string.Empty;

    public DateTime? UpdatedAt { get; set; }

    public bool IsRunning => Phase == UserBotSyncPhases.Running;

    public bool IsFailed => Phase == UserBotSyncPhases.Failed;

    public bool IsCompleted => Phase == UserBotSyncPhases.Completed;
}
