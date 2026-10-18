namespace TecFlow.SharedUi.Services.UI;

public sealed class ToastService : IToastService, IDisposable
{
    public const int MaxVisibleToasts = 5;

    private readonly List<ToastMessage> _toasts = [];
    private readonly object _sync = new();
    private readonly CancellationTokenSource _disposeCts = new();

    public IReadOnlyList<ToastMessage> Toasts
    {
        get
        {
            lock (_sync)
            {
                return _toasts.ToArray();
            }
        }
    }

    public event Action? OnChange;

    public ToastMessage? ShowSuccess(string? message, TimeSpan? duration = null) =>
        Show(message, ToastType.Success, duration);

    public ToastMessage? ShowError(string? message, TimeSpan? duration = null) =>
        Show(message, ToastType.Error, duration);

    public ToastMessage? ShowWarning(string? message, TimeSpan? duration = null) =>
        Show(message, ToastType.Warning, duration);

    public ToastMessage? ShowInfo(string? message, TimeSpan? duration = null) =>
        Show(message, ToastType.Info, duration);

    public ToastMessage? Show(string? message, ToastType type, TimeSpan? duration = null)
    {
        if (string.IsNullOrWhiteSpace(message) || _disposeCts.IsCancellationRequested)
        {
            return null;
        }

        var toast = new ToastMessage
        {
            Message = message.Trim(),
            Type = type,
            Duration = duration is { } custom && custom > TimeSpan.Zero ? custom : ToastMessage.DefaultDuration
        };

        lock (_sync)
        {
            _toasts.RemoveAll(existing => existing.Type == toast.Type
                && string.Equals(existing.Message, toast.Message, StringComparison.Ordinal));
            _toasts.Add(toast);
            if (_toasts.Count > MaxVisibleToasts)
            {
                _toasts.RemoveRange(0, _toasts.Count - MaxVisibleToasts);
            }
        }

        OnChange?.Invoke();
        _ = DismissLaterAsync(toast);
        return toast;
    }

    public void Dismiss(Guid id)
    {
        bool removed;
        lock (_sync)
        {
            removed = _toasts.RemoveAll(item => item.Id == id) > 0;
        }

        if (removed)
        {
            OnChange?.Invoke();
        }
    }

    public void Clear()
    {
        lock (_sync)
        {
            if (_toasts.Count == 0)
            {
                return;
            }

            _toasts.Clear();
        }

        OnChange?.Invoke();
    }

    public void Dispose()
    {
        _disposeCts.Cancel();
        _disposeCts.Dispose();
    }

    private async Task DismissLaterAsync(ToastMessage toast)
    {
        try
        {
            await Task.Delay(toast.Duration, _disposeCts.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (ObjectDisposedException)
        {
            return;
        }

        Dismiss(toast.Id);
    }
}
