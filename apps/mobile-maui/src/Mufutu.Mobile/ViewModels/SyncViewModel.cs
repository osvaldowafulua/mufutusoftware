using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mufutu.Mobile.Core;
using Mufutu.Mobile.Core.Configuration;
using Mufutu.Mobile.Core.Connectivity;
using Mufutu.Mobile.Core.Notifications;
using Mufutu.Mobile.Core.Offline;
using Mufutu.Mobile.Core.Services;
using Mufutu.Mobile.Services;

namespace Mufutu.Mobile.ViewModels;

public partial class SyncViewModel : ObservableObject
{
    private readonly ConnectivityProbeService _probe;
    private readonly IAuthSessionStore _session;
    private readonly IApiSettings _settings;
    private readonly IConnectivityMonitor _network;
    private readonly ICampoDataService _data;
    private readonly ICampoSyncEngine _sync;
    private readonly ICampoOfflineStore _store;
    private readonly MauiConnectivityMonitor _runtime;

    [ObservableProperty]
    private string _siteLabel = "MUA";

    [ObservableProperty]
    private string _userLabel = "Técnico";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusText = "Pronto";

    [ObservableProperty]
    private string _detailText = string.Empty;

    [ObservableProperty]
    private int _pendingCount;

    public SyncViewModel(
        ConnectivityProbeService probe,
        IAuthSessionStore session,
        IApiSettings settings,
        IConnectivityMonitor network,
        ICampoDataService data,
        ICampoSyncEngine sync,
        ICampoOfflineStore store,
        MauiConnectivityMonitor runtime)
    {
        _probe = probe;
        _session = session;
        _settings = settings;
        _network = network;
        _data = data;
        _sync = sync;
        _store = store;
        _runtime = runtime;
        _sync.ProgressChanged += (_, _) => _ = RefreshPendingAsync();
        _network.ConnectivityChanged += (_, _) => _ = RefreshPendingAsync();
    }

    public async Task InitializeAsync()
    {
        SiteLabel = await _session.GetSiteCodeAsync();
        UserLabel = (await _session.GetUserNameAsync()) ?? "Técnico";
        StatusText = _network.IsInternetAvailable ? FieldCopy.Online : FieldCopy.Offline;
        await RefreshPendingAsync();
    }

    private async Task RefreshPendingAsync()
    {
        PendingCount = await _data.GetPendingCountAsync();
        DetailText = PendingCount > 0
            ? $"{PendingCount} alteração(ões) aguardam envio"
            : "Tudo sincronizado";
    }

    [RelayCommand]
    private async Task SyncNowAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            if (!_network.IsInternetAvailable)
            {
                StatusText = FieldCopy.Offline;
                DetailText = "Sem rede — dados guardados localmente";
                return;
            }

            var result = await _sync.ProcessQueueAsync();
            await RefreshPendingAsync();
            StatusText = result.Remaining == 0 ? "Sincronizado" : "Parcial";
            DetailText = $"Enviados: {result.Processed} · Erros: {result.Errors} · Pendentes: {result.Remaining}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ProbeAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            _settings.ApiBaseUrl = _settings.ApiBaseUrl.TrimEnd('/');
            var result = await _probe.ProbeHealthAsync(apiBaseUrlOverride: _settings.ApiBaseUrl);
            StatusText = result.Status switch
            {
                ConnectivityProbeStatus.Success => "API ligada",
                ConnectivityProbeStatus.NoNetwork => FieldCopy.Offline,
                _ => "Falha na ligação",
            };
            var pending = await _data.GetPendingCountAsync();
            DetailText = result.ErrorMessage
                ?? $"HTTP {result.HttpStatusCode} · {result.ElapsedMs} ms · {result.NetworkDescription} · {pending} pendente(s)";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await _runtime.StopCampoRuntimeAsync();
        await _store.ClearAllAsync();
        _session.Clear();
        await Shell.Current.GoToAsync("//login");
    }
}
