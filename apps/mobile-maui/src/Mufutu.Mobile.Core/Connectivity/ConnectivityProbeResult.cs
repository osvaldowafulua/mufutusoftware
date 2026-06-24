namespace Mufutu.Mobile.Core.Connectivity;

public enum ConnectivityProbeStatus
{
    Success,
    NoNetwork,
    HttpError,
    Timeout,
    DnsOrTlsError,
    Unknown,
}

public sealed record ConnectivityProbeResult(
    ConnectivityProbeStatus Status,
    int? HttpStatusCode,
    string? ResponseBody,
    string? ErrorMessage,
    string NetworkDescription,
    bool IsWifi,
    long ElapsedMs);
