namespace Mufutu.Desktop.Core.Configuration;

public sealed class MufutuClientOptions
{
    /// <summary>Base URL pública da API (Release: produção; Debug: localhost via App.xaml.cs).</summary>
    public string ApiBaseUrl { get; set; } = "https://api.mufutu.ao/api";
    public string SiteCode { get; set; } = "MUA";
    public string[] PinnedHostSuffixes { get; set; } = ["mufutu.ao", "localhost", "127.0.0.1"];

    public string GitHubOwner { get; set; } = "osvaldowafulua";
    public string GitHubRepo { get; set; } = "mufutusoftware";
    public string GitHubWindowsTagPrefix { get; set; } = "v";
}
