using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mufutu.Mobile.Core;
using Mufutu.Mobile.Core.Notifications;
using Mufutu.Mobile.Core.Offline;
using Mufutu.Mobile.Core.Services;
using Mufutu.Mobile.Services;

namespace Mufutu.Mobile.ViewModels;

public partial class CampoHubViewModel : ObservableObject
{
    private readonly IAuthSessionStore _session;
    private readonly IConnectivityMonitor _network;
    private readonly ICampoDataService _data;
    private readonly ICampoNotificationService _notifications;

    [ObservableProperty]
    private string _siteLabel = "MUA";

    [ObservableProperty]
    private string _userLabel = "Técnico";

    [ObservableProperty]
    private string _statusLine = FieldCopy.Offline;

    [ObservableProperty]
    private int _pendingCount;

    [ObservableProperty]
    private int _unreadCount;

    public string HubTitle => FieldCopy.HubTitle;
    public string HubSubtitle => FieldCopy.HubSubtitle;
    public string NotificationBadge => UnreadCount > 0 ? UnreadCount.ToString() : "🔔";

    public CampoHubViewModel(
        IAuthSessionStore session,
        IConnectivityMonitor network,
        ICampoDataService data,
        ICampoNotificationService notifications)
    {
        _session = session;
        _network = network;
        _data = data;
        _notifications = notifications;
        _network.ConnectivityChanged += (_, _) => _ = RefreshStatusAsync();
        _notifications.NotificationsChanged += (_, _) => _ = RefreshStatusAsync();
    }

    public async Task InitializeAsync()
    {
        SiteLabel = await _session.GetSiteCodeAsync();
        UserLabel = (await _session.GetUserNameAsync()) ?? "Técnico";
        await RefreshStatusAsync();
    }

    private async Task RefreshStatusAsync()
    {
        PendingCount = await _data.GetPendingCountAsync();
        UnreadCount = await _notifications.GetUnreadCountAsync();
        var net = _data.IsOnline ? FieldCopy.Online : FieldCopy.Offline;
        StatusLine = PendingCount > 0
            ? $"{net} · {PendingCount} pendente(s)"
            : net;
        OnPropertyChanged(nameof(NotificationBadge));
    }

    [RelayCommand]
    private Task GoNotificationsAsync() => Shell.Current.GoToAsync("//notifications");

    [RelayCommand]
    private Task GoOtsAsync() => Shell.Current.GoToAsync("//ots");

    [RelayCommand]
    private Task GoAvariaAsync() => Shell.Current.GoToAsync("//avaria");

    [RelayCommand]
    private Task GoChecklistAsync() => Shell.Current.GoToAsync("//checklist");

    [RelayCommand]
    private Task GoSyncAsync() => Shell.Current.GoToAsync("//sync");
}
