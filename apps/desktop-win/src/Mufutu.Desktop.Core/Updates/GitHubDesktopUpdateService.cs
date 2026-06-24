using System.Diagnostics;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mufutu.Desktop.Core.Configuration;

namespace Mufutu.Desktop.Core.Updates;

public interface IDesktopUpdateService
{
    string ReleasesPageUrl { get; }
    string CurrentVersion { get; }
    Task<DesktopUpdateCheckResponse> CheckForUpdatesAsync(CancellationToken cancellationToken = default);
    Task<bool> DownloadAndInstallAsync(DesktopUpdateInfo update, IProgress<int>? progress = null, CancellationToken cancellationToken = default);
}

public sealed class GitHubDesktopUpdateService : IDesktopUpdateService
{
    private readonly HttpClient _http;
    private readonly ILogger<GitHubDesktopUpdateService> _logger;
    private readonly MufutuClientOptions _options;

    public GitHubDesktopUpdateService(
        HttpClient http,
        IOptions<MufutuClientOptions> options,
        ILogger<GitHubDesktopUpdateService> logger)
    {
        _http = http;
        _logger = logger;
        _options = options.Value;
    }

    public string ReleasesPageUrl =>
        $"https://github.com/{_options.GitHubOwner}/{_options.GitHubRepo}/releases";

    public string CurrentVersion =>
        Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "1.0.0";

    public async Task<DesktopUpdateCheckResponse> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var release = await FetchLatestWindowsReleaseAsync(cancellationToken);
            if (release is null)
            {
                return new DesktopUpdateCheckResponse { Result = DesktopUpdateCheckResult.NoReleaseFound };
            }

            var remoteVersion = ParseVersionFromTag(release.TagName);
            if (remoteVersion is null)
            {
                return new DesktopUpdateCheckResponse
                {
                    Result = DesktopUpdateCheckResult.Error,
                    ErrorMessage = $"Tag de release inválida: {release.TagName}",
                };
            }

            if (!IsRemoteNewer(remoteVersion, CurrentVersion))
            {
                return new DesktopUpdateCheckResponse { Result = DesktopUpdateCheckResult.UpToDate };
            }

            var asset = PickInstallerAsset(release.Assets);
            if (asset is null)
            {
                return new DesktopUpdateCheckResponse
                {
                    Result = DesktopUpdateCheckResult.Error,
                    ErrorMessage = "Release sem instalador MSI/EXE.",
                };
            }

            return new DesktopUpdateCheckResponse
            {
                Result = DesktopUpdateCheckResult.UpdateAvailable,
                Update = new DesktopUpdateInfo
                {
                    TagName = release.TagName,
                    Version = remoteVersion,
                    DownloadUrl = asset.BrowserDownloadUrl,
                    AssetName = asset.Name,
                    ReleaseNotes = release.Body,
                },
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao verificar actualizações GitHub");
            return new DesktopUpdateCheckResponse
            {
                Result = DesktopUpdateCheckResult.Error,
                ErrorMessage = ex.Message,
            };
        }
    }

    public async Task<bool> DownloadAndInstallAsync(
        DesktopUpdateInfo update,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "MUFUTU-update");
        Directory.CreateDirectory(tempDir);
        var targetPath = Path.Combine(tempDir, update.AssetName);

        _logger.LogInformation("A descarregar actualização {Asset}…", update.AssetName);

        using (var response = await _http.GetAsync(update.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
        {
            response.EnsureSuccessStatusCode();
            var total = response.Content.Headers.ContentLength ?? -1;
            await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var output = File.Create(targetPath);

            var buffer = new byte[81920];
            long read = 0;
            int count;
            while ((count = await input.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await output.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
                read += count;
                if (total > 0)
                {
                    progress?.Report((int)(read * 100 / total));
                }
            }
        }

        _logger.LogInformation("Instalador guardado em {Path}", targetPath);

        var extension = Path.GetExtension(targetPath).ToLowerInvariant();
        var startInfo = new ProcessStartInfo
        {
            FileName = targetPath,
            UseShellExecute = true,
        };

        if (extension == ".msi")
        {
            startInfo.FileName = "msiexec.exe";
            startInfo.Arguments = $"/i \"{targetPath}\"";
        }

        Process.Start(startInfo);
        return true;
    }

    private async Task<GitHubRelease?> FetchLatestWindowsReleaseAsync(CancellationToken cancellationToken)
    {
        var url =
            $"https://api.github.com/repos/{_options.GitHubOwner}/{_options.GitHubRepo}/releases?per_page=30";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("User-Agent", "MUFUTU-Desktop-Windows");
        request.Headers.TryAddWithoutValidation("Accept", "application/vnd.github+json");

        var response = await _http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var releases = await response.Content.ReadFromJsonAsync<List<GitHubRelease>>(cancellationToken: cancellationToken);
        return releases?
            .Where(r => !r.Draft && !r.Prerelease)
            .Where(r =>
                r.TagName.StartsWith(_options.GitHubWindowsTagPrefix, StringComparison.OrdinalIgnoreCase)
                || r.TagName.StartsWith("desktop-win/", StringComparison.OrdinalIgnoreCase)
                || r.TagName.StartsWith("desktop/", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(r =>
            {
                var v = ParseVersionFromTag(r.TagName);
                return Version.TryParse(NormalizeVersion(v ?? "0"), out var ver) ? ver : new Version(0, 0);
            })
            .FirstOrDefault();
    }

    private static GitHubReleaseAsset? PickInstallerAsset(IReadOnlyList<GitHubReleaseAsset>? assets)
    {
        if (assets is null || assets.Count == 0)
        {
            return null;
        }

        return assets.FirstOrDefault(a => a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            ?? assets.FirstOrDefault(a => a.Name.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
            ?? assets.FirstOrDefault(a => a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
    }

    private static string? ParseVersionFromTag(string tag)
    {
        if (tag.StartsWith('v') || tag.StartsWith('V'))
        {
            return tag[1..].TrimStart('v', 'V');
        }

        foreach (var prefix in new[] { "desktop-win/v", "desktop/v" })
        {
            if (tag.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return tag[prefix.Length..].TrimStart('v', 'V');
            }
        }

        return null;
    }

    private static bool IsRemoteNewer(string remote, string current)
    {
        if (Version.TryParse(NormalizeVersion(remote), out var remoteVersion)
            && Version.TryParse(NormalizeVersion(current), out var currentVersion))
        {
            return remoteVersion > currentVersion;
        }

        return string.Compare(remote, current, StringComparison.OrdinalIgnoreCase) > 0;
    }

    private static string NormalizeVersion(string value) =>
        value.Split('-')[0];

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;

        [JsonPropertyName("body")]
        public string? Body { get; set; }

        [JsonPropertyName("draft")]
        public bool Draft { get; set; }

        [JsonPropertyName("prerelease")]
        public bool Prerelease { get; set; }

        [JsonPropertyName("assets")]
        public List<GitHubReleaseAsset> Assets { get; set; } = [];
    }

    private sealed class GitHubReleaseAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = string.Empty;
    }
}
