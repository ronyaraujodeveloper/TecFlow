using Android.App;
using Android.Content;
using AndroidX.Core.App;
using Firebase.Messaging;
using TecFlow.Mobile.Services.Push;

namespace TecFlow.Mobile.Platforms.Android;

[Service(Exported = false)]
[IntentFilter(["com.google.firebase.MESSAGING_EVENT"])]
public class TecFlowFirebaseMessagingService : FirebaseMessagingService
{
    public override void OnMessageReceived(RemoteMessage message)
    {
        base.OnMessageReceived(message);

        var data = message.Data.ToDictionary(
            static pair => pair.Key,
            static pair => pair.Value ?? string.Empty,
            StringComparer.Ordinal);

        if (IsAppInForeground())
        {
            ShowLocalNotification(message.Notification?.Title, message.Notification?.Body, data);
        }

        PushNotificationBridge.HandleNotificationOpened(data);
    }

    public override void OnNewToken(string token)
    {
        base.OnNewToken(token);
        PushNotificationBridge.NotifyToken(token);
    }

    private static bool IsAppInForeground() =>
        MauiApplication.Current?.Application is not null;

    private void ShowLocalNotification(string? title, string? body, IDictionary<string, string> data)
    {
        var channelId = "tecflow_alerts";
        var manager = NotificationManagerCompat.From(this);

        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            var channel = new NotificationChannel(channelId, "TecFlow", NotificationImportance.Default);
            manager.CreateNotificationChannel(channel);
        }

        var intent = new Intent(this, typeof(MainActivity));
        intent.SetAction(Intent.ActionView);
        if (data.TryGetValue("route", out var route))
        {
            intent.SetData(Android.Net.Uri.Parse($"tecflow://{route.TrimStart('/')}"));
        }

        var pending = PendingIntent.GetActivity(
            this,
            0,
            intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        var builder = new NotificationCompat.Builder(this, channelId)
            .SetContentTitle(title ?? "TecFlow")
            .SetContentText(body ?? string.Empty)
            .SetSmallIcon(Resource.Mipmap.AppIcon)
            .SetAutoCancel(true)
            .SetContentIntent(pending);

        manager.Notify(Random.Shared.Next(), builder.Build());
    }
}
