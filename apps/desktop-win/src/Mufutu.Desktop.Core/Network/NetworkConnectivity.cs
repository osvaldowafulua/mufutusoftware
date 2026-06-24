using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Mufutu.Desktop.Core.Network;

public static class NetworkConnectivity
{
    /// <summary>Indica se existe interface de rede activa (Wi-Fi ou Ethernet).</summary>
    public static bool IsNetworkAvailable()
    {
        if (!NetworkInterface.GetIsNetworkAvailable())
        {
            return false;
        }

        return NetworkInterface.GetAllNetworkInterfaces()
            .Any(n =>
                n.OperationalStatus == OperationalStatus.Up
                && n.NetworkInterfaceType is not (
                    NetworkInterfaceType.Loopback
                    or NetworkInterfaceType.Tunnel));
    }

    /// <summary>Testa ligação TCP ao host (DNS + socket do sistema).</summary>
    public static async Task<bool> CanReachHostAsync(
        string host,
        int port,
        int timeoutMs = 8000,
        CancellationToken ct = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeoutMs);
            using var client = new TcpClient();
            await client.ConnectAsync(host, port, cts.Token);
            return client.Connected;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<bool> CanReachApiAsync(Uri apiBase, CancellationToken ct = default)
    {
        if (!IsNetworkAvailable())
        {
            return false;
        }

        var host = apiBase.Host;
        var port = apiBase.IsDefaultPort
            ? apiBase.Scheme == "https" ? 443 : 80
            : apiBase.Port;

        return await CanReachHostAsync(host, port, ct: ct);
    }
}
