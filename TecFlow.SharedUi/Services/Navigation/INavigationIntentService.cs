namespace TecFlow.SharedUi.Services.Navigation;

public interface INavigationIntentService
{
    event Action<string>? NavigateRequested;
    string? PendingRoute { get; }
    void RequestNavigation(string route);
    void ClearPending();
}
