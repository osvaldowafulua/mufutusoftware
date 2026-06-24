using Mufutu.Mobile.Core.Models;
using Mufutu.Mobile.Core.Notifications;

namespace Mufutu.Mobile.Services;

public sealed class MauiLocalNotificationPresenter : INotificationPresenter
{
    private static int _notificationId = 2000;

    public Task ShowAsync(NotificationDto notification, CancellationToken ct = default)
    {
#if ANDROID
        return ShowAndroidAsync(notification);
#elif IOS
        return ShowIosAsync(notification);
#else
        return Task.CompletedTask;
#endif
    }

    public Task RequestPermissionAsync(CancellationToken ct = default)
    {
#if ANDROID
        return RequestAndroidPermissionAsync();
#elif IOS
        return RequestIosPermissionAsync();
#else
        return Task.CompletedTask;
#endif
    }

#if ANDROID
    private static Task ShowAndroidAsync(NotificationDto notification)
    {
        var context = Microsoft.Maui.ApplicationModel.Platform.AppContext;
        const string channelId = "mufutu_campo_alerts";

        if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.O)
        {
            var channel = new global::Android.App.NotificationChannel(
                channelId,
                "MUFUTU Campo",
                global::Android.App.NotificationImportance.Default);
            var notificationManager = context.GetSystemService(global::Android.Content.Context.NotificationService)
                as global::Android.App.NotificationManager;
            notificationManager?.CreateNotificationChannel(channel);
        }

        var smallIcon = global::Android.Resource.Drawable.IcDialogInfo;
        if (context.ApplicationInfo?.Icon is int icon && icon != 0)
        {
            smallIcon = icon;
        }

        var builder = new AndroidX.Core.App.NotificationCompat.Builder(context, channelId)
            .SetContentTitle(notification.Title ?? "MUFUTU")
            .SetContentText(notification.Message ?? string.Empty)
            .SetSmallIcon(smallIcon)
            .SetAutoCancel(true);

        AndroidX.Core.App.NotificationManagerCompat.From(context)
            .Notify(Interlocked.Increment(ref _notificationId), builder.Build());

        return Task.CompletedTask;
    }

    private static async Task RequestAndroidPermissionAsync()
    {
        if (global::Android.OS.Build.VERSION.SdkInt < global::Android.OS.BuildVersionCodes.Tiramisu)
        {
            return;
        }

        var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
        if (status != PermissionStatus.Granted)
        {
            await Permissions.RequestAsync<Permissions.PostNotifications>();
        }
    }
#endif

#if IOS
    private static Task ShowIosAsync(NotificationDto notification)
    {
        var content = new UserNotifications.UNMutableNotificationContent
        {
            Title = notification.Title ?? "MUFUTU",
            Body = notification.Message ?? string.Empty,
        };
        var trigger = UserNotifications.UNTimeIntervalNotificationTrigger.CreateTrigger(0.1, false);
        var request = UserNotifications.UNNotificationRequest.FromIdentifier(notification.Id, content, trigger);
        UserNotifications.UNUserNotificationCenter.Current.AddNotificationRequest(request, _ => { });
        return Task.CompletedTask;
    }

    private static Task RequestIosPermissionAsync()
    {
        var tcs = new TaskCompletionSource();
        UserNotifications.UNUserNotificationCenter.Current.RequestAuthorization(
            UserNotifications.UNAuthorizationOptions.Alert | UserNotifications.UNAuthorizationOptions.Sound,
            (granted, _) => tcs.TrySetResult());
        return tcs.Task;
    }
#endif
}
