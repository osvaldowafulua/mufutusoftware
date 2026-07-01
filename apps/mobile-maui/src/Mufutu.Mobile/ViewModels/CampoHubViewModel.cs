using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    private readonly ILocalizationService _l10n;

    [ObservableProperty]
    private string _siteLabel = "MUA";

    [ObservableProperty]
    private string _userLabel = "Técnico";

    [ObservableProperty]
    private string _statusLine = string.Empty;

    [ObservableProperty]
    private int _pendingCount;

    [ObservableProperty]
    private int _unreadCount;

    public string HubTitle => _l10n.Get("hub_title");
    public string HubSubtitle => _l10n.Get("hub_subtitle");
    public string MyWorkLabel => _l10n.Get("my_work");
    public string ReportFaultLabel => _l10n.Get("report_fault");
    public string ChecklistLabel => _l10n.Get("checklist");
    public string SyncLabel => _l10n.Get("sync");
    public string NotificationBadge => UnreadCount > 0 ? UnreadCount.ToString() : "🔔";

    public CampoHubViewModel(
        IAuthSessionStore session,
        IConnectivityMonitor network,
        ICampoDataService data,
        ICampoNotificationService notifications,
        ILocalizationService l10n)
    {
        _session = session;
        _network = network;
        _data = data;
        _notifications = notifications;
        _l10n = l10n;
        _statusLine = _l10n.Get("offline");
        _network.ConnectivityChanged += (_, _) => _ = RefreshStatusAsync();
        _notifications.NotificationsChanged += (_, _) => _ = RefreshStatusAsync();
        _l10n.LanguageChanged += (_, _) => RefreshLabels();
    }

    public async Task InitializeAsync()
    {
        SiteLabel = await _session.GetSiteCodeAsync();
        UserLabel = (await _session.GetUserNameAsync()) ?? _l10n.Get("technician");
        await RefreshStatusAsync();
    }

    private async Task RefreshStatusAsync()
    {
        PendingCount = await _data.GetPendingCountAsync();
        UnreadCount = await _notifications.GetUnreadCountAsync();
        var net = _data.IsOnline ? _l10n.Get("online") : _l10n.Get("offline");
        StatusLine = PendingCount > 0
            ? $"{net} · {_l10n.Format("pending_count", PendingCount)}"
            : net;
        OnPropertyChanged(nameof(NotificationBadge));
    }

    private void RefreshLabels()
    {
        OnPropertyChanged(nameof(HubTitle));
        OnPropertyChanged(nameof(HubSubtitle));
        OnPropertyChanged(nameof(MyWorkLabel));
        OnPropertyChanged(nameof(ReportFaultLabel));
        OnPropertyChanged(nameof(ChecklistLabel));
        OnPropertyChanged(nameof(SyncLabel));
        _ = RefreshStatusAsync();
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
