namespace TecFlow.SharedUi.Services.UI;

public class LoadingService : ILoadingService
{
    private int _counter;

    public bool IsLoading => _counter > 0;
    public string? Message { get; private set; }
    public event Action? OnChange;

    public IDisposable BeginScope(string? message = null)
    {
        _counter++;
        Message = message;
        OnChange?.Invoke();
        return new LoadingScope(this);
    }

    private void EndScope()
    {
        if (_counter > 0)
        {
            _counter--;
        }

        if (_counter == 0)
        {
            Message = null;
        }

        OnChange?.Invoke();
    }

    private sealed class LoadingScope : IDisposable
    {
        private readonly LoadingService _owner;
        private bool _disposed;

        public LoadingScope(LoadingService owner) => _owner = owner;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _owner.EndScope();
        }
    }
}
