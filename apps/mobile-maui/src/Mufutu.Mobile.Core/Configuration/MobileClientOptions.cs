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

    /// <summary>
    /// Manifesto público que controla o "version gate": suba minimumVersion
    /// aí e faça commit para main para forçar todos os clientes a actualizar
    /// — não precisa de publicar um novo release.
    /// </summary>
    public string UpdateGateManifestUrl { get; set; } =
        "https://raw.githubusercontent.com/osvaldowafulua/mufutusoftware/main/releases/latest.json";

    public string UpdateGatePlatformKey { get; set; } = "android";

    public int UpdateGateTimeoutSeconds { get; set; } = 6;
}
