using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mufutu.Mobile.Core.Models;
using Mufutu.Mobile.Core.Notifications;
using Mufutu.Mobile.Core.Services;

namespace Mufutu.Mobile.ViewModels;

public partial class NotificationsViewModel : ObservableObject
{
    private readonly ICampoNotificationService _notifications;
    private readonly IAuthSessionStore _session;

    [ObservableProperty]
    private string _siteLabel = "MUA";

    [ObservableProperty]
    private string _userLabel = "Técnico";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private int _unreadCount;

    public ObservableCollection<NotificationItem> Items { get; } = [];

    public NotificationsViewModel(ICampoNotificationService notifications, IAuthSessionStore session)
    {
        _notifications = notifications;
        _session = session;
        _notifications.NotificationsChanged += OnNotificationsChanged;
    }

    private async void OnNotificationsChanged(object? sender, EventArgs e) =>
        await LoadAsync();

    public async Task InitializeAsync()
    {
        SiteLabel = await _session.GetSiteCodeAsync();
        UserLabel = (await _session.GetUserNameAsync()) ?? "Técnico";
        await LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var list = await _notifications.GetNotificationsAsync();
            UnreadCount = await _notifications.GetUnreadCountAsync();
            Items.Clear();
            foreach (var n in list)
            {
                Items.Add(NotificationItem.From(n));
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task OpenAsync(NotificationItem item)
    {
        if (!item.Read)
        {
            await _notifications.MarkReadAsync(item.Id);
            item.Read = true;
            UnreadCount = await _notifications.GetUnreadCountAsync();
        }
    }

    [RelayCommand]
    private async Task MarkAllReadAsync()
    {
        await _notifications.MarkAllReadAsync();
        await LoadAsync();
    }
}

public sealed partial class NotificationItem : ObservableObject
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;

    [ObservableProperty]
    private bool _read;

    public static NotificationItem From(NotificationDto dto) => new()
    {
        Id = dto.Id,
        Title = dto.Title,
        Message = dto.Message,
        Type = dto.Type,
        Read = dto.Read,
    };
}
