using TecFlow.SharedUi.Services.Devices;
using TecFlow.SharedUi.Services.Navigation;
using TecFlow.SharedUi.Services.State;

namespace TecFlow.Mobile.Services.Push;

public class MobilePushCoordinator
{
    private readonly IDeviceRegistrationApiService _deviceApi;
    private readonly ISessionStateService _sessionState;
    private readonly INavigationIntentService _navigationIntent;

    public MobilePushCoordinator(
        IDeviceRegistrationApiService deviceApi,
        ISessionStateService sessionState,
        INavigationIntentService navigationIntent)
    {
        _deviceApi = deviceApi;
        _sessionState = sessionState;
        _navigationIntent = navigationIntent;
    }

    public void Start()
    {
        PushNotificationBridge.Initialize(_navigationIntent);
        PushNotificationBridge.TokenRefreshed += OnTokenRefreshed;
    }

    public void Stop()
    {
        PushNotificationBridge.TokenRefreshed -= OnTokenRefreshed;
    }

    private async void OnTokenRefreshed(string token)
    {
        if (!_sessionState.IsAuthenticated || string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        var platform = DeviceInfo.Platform.ToString().ToLowerInvariant();
        await _deviceApi.RegisterDeviceAsync(token, platform);
    }
}
