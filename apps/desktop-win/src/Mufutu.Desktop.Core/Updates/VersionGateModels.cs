namespace Mufutu.Desktop.Core.Updates;

public enum VersionGateStatus
{
    /// <summary>Versão instalada está actualizada ou o manifesto não trouxe versão mais recente.</summary>
    UpToDate,

    /// <summary>Há uma versão mais recente, mas a instalada ainda é aceite.</summary>
    UpdateAvailable,

    /// <summary>Versão instalada está abaixo da mínima aceite — arranque deve ser bloqueado.</summary>
    UpdateRequired,

    /// <summary>Não foi possível verificar (sem rede, manifesto indisponível) — nunca bloqueia.</summary>
    CheckFailed,
}

public sealed class VersionGateResult
{
    public required VersionGateStatus Status { get; init; }
    public string? LatestVersion { get; init; }
    public string? MinimumVersion { get; init; }
    public string? DownloadUrl { get; init; }
    public string? Notes { get; init; }
}
