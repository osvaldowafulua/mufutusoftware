namespace Mufutu.Mobile.Core.Configuration;

public sealed class MobileClientOptions
{
    public string ApiBaseUrl { get; set; } = "https://api.mufutu.ao/api";

    public string SiteCode { get; set; } = "MUA";

    public string[] AllowedHostSuffixes { get; set; } = ["mufutu.ao", "localhost", "127.0.0.1"];

    public int HealthProbeTimeoutSeconds { get; set; } = 20;
}
