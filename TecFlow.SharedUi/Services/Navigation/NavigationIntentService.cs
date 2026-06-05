namespace TecFlow.SharedUi.Services.Navigation;

public class NavigationIntentService : INavigationIntentService
{
    public event Action<string>? NavigateRequested;

    public string? PendingRoute { get; private set; }

    public void RequestNavigation(string route)
    {
        var normalized = route.StartsWith('/') ? route : "/" + route;
        PendingRoute = normalized;
        NavigateRequested?.Invoke(normalized);
    }

    public void ClearPending() => PendingRoute = null;
}
