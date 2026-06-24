using Mufutu.Mobile.Core.Connectivity;
using Mufutu.Mobile.Core.Notifications;
using Mufutu.Mobile.Core.Offline;
using Mufutu.Mobile.Core.Services;

namespace Mufutu.Mobile.Services;

public interface IConnectivityMonitor : INetworkStatusProvider
{
    event EventHandler? ConnectivityChanged;
}

public sealed class MauiConnectivityMonitor : IConnectivityMonitor, IDisposable
{
    private readonly ICampoDataService _data;
    private readonly ICampoNotificationService _notifications;
    private readonly ICampoSyncEngine _sync;
    private readonly IAuthSessionStore _session;
    private Timer? _periodicTimer;

    public event EventHandler? ConnectivityChanged;

    public MauiConnectivityMonitor(
        ICampoDataService data,
        ICampoNotificationService notifications,
        ICampoSyncEngine sync,
        IAuthSessionStore session)
    {
        _data = data;
        _notifications = notifications;
        _sync = sync;
        _session = session;
        Connectivity.ConnectivityChanged += OnConnectivityChanged;
    }

    public bool IsInternetAvailable =>
        Connectivity.Current.NetworkAccess is NetworkAccess.Internet or NetworkAccess.ConstrainedInternet;

    public bool IsWifi =>
        Connectivity.Current.ConnectionProfiles.Contains(ConnectionProfile.WiFi);

    public bool IsCellular =>
        Connectivity.Current.ConnectionProfiles.Contains(ConnectionProfile.Cellular);

    public string NetworkDescription
    {
        get
        {
            var access = Connectivity.Current.NetworkAccess;
            var profiles = Connectivity.Current.ConnectionProfiles;
            var profileNames = !profiles.Any()
                ? "desconhecido"
                : string.Join(", ", profiles.Select(p => p switch
                {
                    ConnectionProfile.WiFi => "Wi‑Fi",
                    ConnectionProfile.Cellular => "dados móveis",
                    ConnectionProfile.Ethernet => "ethernet",
                    ConnectionProfile.Bluetooth => "bluetooth",
                    _ => p.ToString(),
                }));
            return $"{access} ({profileNames})";
        }
    }

    public async Task StartCampoRuntimeAsync()
    {
        if (!await _session.HasSessionAsync())
        {
            return;
        }

        try
        {
            await Plugin.LocalNotification.LocalNotificationCenter.Current.RequestNotificationPermission();
        }
        catch
        {
            // permissão opcional
        }

        await _notifications.StartAsync();
        _periodicTimer ??= new Timer(
            async _ => await RunBackgroundSyncAsync(),
            null,
            TimeSpan.FromSeconds(15),
            TimeSpan.FromSeconds(30));

        if (IsInternetAvailable)
        {
            await RunBackgroundSyncAsync();
        }
    }

    public async Task StopCampoRuntimeAsync()
    {
        _periodicTimer?.Dispose();
        _periodicTimer = null;
        await _notifications.StopAsync();
    }

    private async void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        ConnectivityChanged?.Invoke(this, EventArgs.Empty);
        if (IsInternetAvailable)
        {
            await RunBackgroundSyncAsync();
        }
    }

    private async Task RunBackgroundSyncAsync()
    {
        if (!await _session.HasSessionAsync() || !IsInternetAvailable)
        {
            return;
        }

        try
        {
            await _sync.ProcessQueueAsync();
            await _notifications.RefreshAsync();
        }
        catch
        {
            // background — silent
        }
    }

    public void Dispose()
    {
        Connectivity.ConnectivityChanged -= OnConnectivityChanged;
        _periodicTimer?.Dispose();
    }
}
