using Firebase.Messaging;
using TecFlow.Mobile.Services.Push;

namespace TecFlow.Mobile.Platforms.Android.Push;

public static class AndroidPushInitializer
{
    public static async void Initialize()
    {
        try
        {
            await FirebaseMessaging.Instance.GetToken();
            FirebaseMessaging.Instance.TokenRegistrationOnInitEnabled = true;
            var token = await FirebaseMessaging.Instance.GetToken();
            if (!string.IsNullOrWhiteSpace(token))
            {
                PushNotificationBridge.NotifyToken(token);
            }
        }
        catch
        {
            // Firebase não configurado (google-services.json ausente) — ignorar em dev.
        }
    }
}
