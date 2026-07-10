using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mufutu.Desktop.Core.Configuration;

namespace Mufutu.Desktop.Core.Updates;

/// <summary>
/// Bloqueia o arranque quando a versão instalada está abaixo da mínima aceite
/// (<see cref="MufutuClientOptions.VersionGateManifestUrl"/>). Nunca bloqueia por
/// falha de rede — só quando confirma positivamente que a versão está desactualizada,
/// para não quebrar o uso offline-first no terreno.
/// </summary>
public interface IVersionGateService
{
    string CurrentVersion { get; }
    Task<VersionGateResult> CheckAsync(CancellationToken ct = default);
}

public sealed class VersionGateService : IVersionGateService
{
    private readonly HttpClient _http;
    private readonly MufutuClientOptions _options;
    private readonly ILogger<VersionGateService> _logger;
    private readonly Func<string> _currentVersionProvider;

    public VersionGateService(HttpClient http, IOptions<MufutuClientOptions> options, ILogger<VersionGateService> logger)
        : this(http, options, logger, DefaultCurrentVersion)
    {
    }

    /// <summary>Injecta a versão "instalada" sem depender do assembly de arranque (usado em testes).</summary>
    public VersionGateService(
        HttpClient http,
        IOptions<MufutuClientOptions> options,
        ILogger<VersionGateService> logger,
        Func<string> currentVersionProvider)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
        _currentVersionProvider = currentVersionProvider;
    }

    public string CurrentVersion => _currentVersionProvider();

    private static string DefaultCurrentVersion() =>
        Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "1.0.0";

    public async Task<VersionGateResult> CheckAsync(CancellationToken ct = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(_options.VersionGateTimeout);

            using var response = await _http.GetAsync(_options.VersionGateManifestUrl, cts.Token);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cts.Token);

            if (!doc.RootElement.TryGetProperty("platforms", out var platforms)
                || !platforms.TryGetProperty(_options.VersionGatePlatformKey, out var platform))
            {
                return new VersionGateResult { Status = VersionGateStatus.CheckFailed };
            }

            var latest = platform.TryGetProperty("latestVersion", out var l) ? l.GetString() : null;
            var minimum = platform.TryGetProperty("minimumVersion", out var m) ? m.GetString() : null;
            var downloadUrl = platform.TryGetProperty("downloadUrl", out var d) ? d.GetString() : null;
            var notes = platform.TryGetProperty("note", out var n) ? n.GetString() : null;

            var current = CurrentVersion;

            if (!string.IsNullOrWhiteSpace(minimum) && SemVerCompare.IsOlder(current, minimum))
            {
                return new VersionGateResult
                {
                    Status = VersionGateStatus.UpdateRequired,
                    LatestVersion = latest,
                    MinimumVersion = minimum,
                    DownloadUrl = downloadUrl,
                    Notes = notes,
                };
            }

            if (!string.IsNullOrWhiteSpace(latest) && SemVerCompare.IsNewer(latest, current))
            {
                return new VersionGateResult
                {
                    Status = VersionGateStatus.UpdateAvailable,
                    LatestVersion = latest,
                    MinimumVersion = minimum,
                    DownloadUrl = downloadUrl,
                    Notes = notes,
                };
            }

            return new VersionGateResult { Status = VersionGateStatus.UpToDate, LatestVersion = latest, MinimumVersion = minimum };
        }
        catch (Exception ex)
        {
            // Sem rede / manifesto indisponível — nunca bloqueia o arranque offline
            _logger.LogWarning(ex, "Version gate: não foi possível verificar o manifesto");
            return new VersionGateResult { Status = VersionGateStatus.CheckFailed };
        }
    }
}
