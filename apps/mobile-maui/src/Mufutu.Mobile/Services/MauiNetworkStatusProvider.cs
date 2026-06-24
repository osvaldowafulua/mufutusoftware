using Mufutu.Mobile.Core.Connectivity;

namespace Mufutu.Mobile.Services;

public sealed class MauiNetworkStatusProvider : INetworkStatusProvider
{
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
            var profileNames = profiles.Count == 0
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
}
