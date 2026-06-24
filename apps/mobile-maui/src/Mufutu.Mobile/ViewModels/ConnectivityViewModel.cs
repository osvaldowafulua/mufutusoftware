using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mufutu.Mobile.Core.Connectivity;

namespace Mufutu.Mobile.ViewModels;

public partial class ConnectivityViewModel : ObservableObject
{
    private readonly ConnectivityProbeService _probe;
    private readonly INetworkStatusProvider _network;

    public ConnectivityViewModel(ConnectivityProbeService probe, INetworkStatusProvider network)
    {
        _probe = probe;
        _network = network;
        StatusLine = "Pronto para testar ligação.";
    }

    [ObservableProperty]
    private string _statusLine = string.Empty;

    [ObservableProperty]
    private string _networkLine = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [RelayCommand]
    public async Task RunProbeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        NetworkLine = _network.NetworkDescription;

        try
        {
            var result = await _probe.ProbeHealthAsync();
            StatusLine = result.Status switch
            {
                ConnectivityProbeStatus.Success =>
                    $"API OK ({result.HttpStatusCode}) em {result.ElapsedMs} ms — {result.NetworkDescription}",
                ConnectivityProbeStatus.NoNetwork =>
                    "Sem Internet. Active Wi‑Fi ou dados móveis.",
                ConnectivityProbeStatus.Timeout =>
                    $"Timeout ({result.ElapsedMs} ms). Verifique rede ou VPN.",
                ConnectivityProbeStatus.HttpError =>
                    $"API respondeu {result.HttpStatusCode}: {result.ErrorMessage}",
                ConnectivityProbeStatus.DnsOrTlsError =>
                    $"Erro de rede/TLS: {result.ErrorMessage}",
                _ => result.ErrorMessage ?? result.Status.ToString(),
            };
        }
        finally
        {
            IsBusy = false;
        }
    }
}
