namespace TecFlow.Mobile.Services.Push;

public static class PushBootstrap
{
    public static void RequestTokenIfAvailable()
    {
#if ANDROID
        Platforms.Android.Push.AndroidPushInitializer.Initialize();
#elif IOS
        UIKit.UIApplication.SharedApplication.RegisterForRemoteNotifications();
#endif
    }
}
