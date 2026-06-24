namespace Mufutu.Mobile.Core.Connectivity;

/// <summary>Abstracção da rede do dispositivo (Wi‑Fi, dados móveis, offline).</summary>
public interface INetworkStatusProvider
{
    bool IsInternetAvailable { get; }

    bool IsWifi { get; }

    bool IsCellular { get; }

    string NetworkDescription { get; }
}
