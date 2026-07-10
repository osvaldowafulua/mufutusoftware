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

    /// <summary>
    /// Manifesto público que controla o "version gate": suba minimumVersion
    /// aí e faça commit para main para forçar todos os clientes a actualizar
    /// — não precisa de publicar um novo release.
    /// </summary>
    public string VersionGateManifestUrl { get; set; } =
        "https://raw.githubusercontent.com/osvaldowafulua/mufutusoftware/main/releases/latest.json";

    public string VersionGatePlatformKey { get; set; } = "windows";

    public TimeSpan VersionGateTimeout { get; set; } = TimeSpan.FromSeconds(6);
}
