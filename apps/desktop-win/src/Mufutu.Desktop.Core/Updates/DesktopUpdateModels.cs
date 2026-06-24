namespace Mufutu.Desktop.Core.Updates;

public sealed class DesktopUpdateInfo
{
    public required string TagName { get; init; }
    public required string Version { get; init; }
    public required string DownloadUrl { get; init; }
    public required string AssetName { get; init; }
    public string? ReleaseNotes { get; init; }
}

public enum DesktopUpdateCheckResult
{
    UpToDate,
    UpdateAvailable,
    NoReleaseFound,
    Error,
}

public sealed class DesktopUpdateCheckResponse
{
    public DesktopUpdateCheckResult Result { get; init; }
    public DesktopUpdateInfo? Update { get; init; }
    public string? ErrorMessage { get; init; }
}
