using Mufutu.Mobile.Core.Connectivity;
using Mufutu.Mobile.Core.Models;
using Mufutu.Mobile.Core.Notifications;

namespace Mufutu.Mobile.Services;

public sealed class MauiLocalNotificationPresenter : INotificationPresenter
{
    public Task ShowAsync(NotificationDto notification, CancellationToken ct = default)
    {
        var request = new Plugin.LocalNotification.NotificationRequest
        {
            NotificationId = Math.Abs(notification.Id.GetHashCode()),
            Title = notification.Title,
            Description = notification.Message,
            CategoryType = Plugin.LocalNotification.NotificationCategoryType.Status,
        };

        return Plugin.LocalNotification.LocalNotificationCenter.Current.Show(request);
    }
}
