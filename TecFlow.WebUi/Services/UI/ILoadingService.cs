namespace TecFlow.WebUi.Services.UI;

public interface ILoadingService
{
    bool IsLoading { get; }
    string? Message { get; }
    event Action? OnChange;

    IDisposable BeginScope(string? message = null);
}
