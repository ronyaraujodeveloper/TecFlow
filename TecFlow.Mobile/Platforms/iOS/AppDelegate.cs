using Foundation;
using TecFlow.Mobile.Services.Push;
using UIKit;
using UserNotifications;

namespace TecFlow.Mobile;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        var result = base.FinishedLaunching(application, launchOptions);
        UNUserNotificationCenter.Current.Delegate = new TecFlowNotificationDelegate();
        application.RegisterForRemoteNotifications();
        return result;
    }

    public override bool OpenUrl(UIApplication application, NSUrl url, NSDictionary options)
    {
        if (Uri.TryCreate(url.AbsoluteString, UriKind.Absolute, out var uri))
        {
            PushNotificationBridge.HandleDeepLink(uri);
        }

        return base.OpenUrl(application, url, options);
    }

    [Export("application:didRegisterForRemoteNotificationsWithDeviceToken:")]
    public void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
    {
        var bytes = deviceToken.ToArray();
        var token = string.Concat(bytes.Select(b => b.ToString("x2")));
        if (!string.IsNullOrWhiteSpace(token))
        {
            PushNotificationBridge.NotifyToken(token);
        }
    }
}

internal sealed class TecFlowNotificationDelegate : UNUserNotificationCenterDelegate
{
    public override void DidReceiveNotificationResponse(
        UNUserNotificationCenter center,
        UNNotificationResponse response,
        Action completionHandler)
    {
        var data = response.Notification.Request.Content.UserInfo
            .ToDictionary(
                static pair => pair.Key.ToString() ?? string.Empty,
                static pair => pair.Value?.ToString() ?? string.Empty,
                StringComparer.Ordinal);

        PushNotificationBridge.HandleNotificationOpened(data);
        completionHandler();
    }

    public override void WillPresentNotification(
        UNUserNotificationCenter center,
        UNNotification notification,
        Action<UNNotificationPresentationOptions> completionHandler)
    {
        completionHandler(UNNotificationPresentationOptions.Banner | UNNotificationPresentationOptions.Sound);
    }
}
