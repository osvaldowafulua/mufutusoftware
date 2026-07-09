namespace Mufutu.Mobile.Core.Configuration;

public sealed class MobileClientOptions
{
    public string ApiBaseUrl { get; set; } = "https://api.mufutu.ao/api";

    public string SiteCode { get; set; } = "MUA";

    public string[] AllowedHostSuffixes { get; set; } = ["mufutu.ao", "localhost", "127.0.0.1"];

    public int HealthProbeTimeoutSeconds { get; set; } = 20;

    /// <summary>
    /// Activar quando a API (CMMS ≥ 1.2) suportar o parâmetro updatedSince.
    /// Com false, cada pull é completo mas o cliente só escreve o que mudou
    /// e remove localmente o que deixou de existir no servidor.
    /// </summary>
    public bool ServerSupportsDeltaSync { get; set; }
}
