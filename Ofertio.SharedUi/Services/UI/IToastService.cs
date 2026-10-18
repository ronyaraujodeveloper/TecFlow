namespace Ofertio.SharedUi.Services.UI;

public enum ToastType
{
    Success,
    Error,
    Warning,
    Info
}

public sealed class ToastMessage
{
    public static readonly TimeSpan DefaultDuration = TimeSpan.FromSeconds(10);

    public Guid Id { get; init; } = Guid.NewGuid();

    public string Message { get; init; } = string.Empty;

    public ToastType Type { get; init; } = ToastType.Info;

    public TimeSpan Duration { get; init; } = DefaultDuration;

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

public interface IToastService
{
    IReadOnlyList<ToastMessage> Toasts { get; }

    event Action? OnChange;

    ToastMessage? ShowSuccess(string? message, TimeSpan? duration = null);

    ToastMessage? ShowError(string? message, TimeSpan? duration = null);

    ToastMessage? ShowWarning(string? message, TimeSpan? duration = null);

    ToastMessage? ShowInfo(string? message, TimeSpan? duration = null);

    ToastMessage? Show(string? message, ToastType type, TimeSpan? duration = null);

    void Dismiss(Guid id);

    void Clear();
}
