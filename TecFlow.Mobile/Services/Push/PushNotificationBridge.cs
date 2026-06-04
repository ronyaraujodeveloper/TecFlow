using TecFlow.SharedUi.Navigation;
using TecFlow.SharedUi.Services.Navigation;

namespace TecFlow.Mobile.Services.Push;

public static class PushNotificationBridge
{
    private static INavigationIntentService? _navigationIntent;

    public static event Action<string>? TokenRefreshed;

    public static void Initialize(INavigationIntentService navigationIntent) =>
        _navigationIntent = navigationIntent;

    public static void NotifyToken(string token) => TokenRefreshed?.Invoke(token);

    public static void HandleNotificationOpened(IReadOnlyDictionary<string, string> data)
    {
        if (DeepLinkRoutes.TryMapFromNotificationData(data, out var route))
        {
            _navigationIntent?.RequestNavigation(route);
        }
    }

    public static void HandleDeepLink(Uri uri)
    {
        if (DeepLinkRoutes.TryMapToAppRoute(uri, out var route))
        {
            _navigationIntent?.RequestNavigation(route);
        }
    }
}
