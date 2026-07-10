using System.Text.Json;
using Microsoft.Extensions.Options;
using Mufutu.Mobile.Core.Configuration;

namespace Mufutu.Mobile.Core.Updates;

/// <summary>
/// Bloqueia o arranque quando a versão instalada está abaixo da mínima aceite
/// (<see cref="MobileClientOptions.UpdateGateManifestUrl"/>). Nunca bloqueia por
/// falha de rede — só quando confirma positivamente que a versão está desactualizada,
/// para não quebrar o uso offline-first no terreno.
/// </summary>
public interface IUpdateGateService
{
    Task<UpdateGateResult> CheckAsync(string currentVersion, CancellationToken ct = default);
}

public sealed class UpdateGateService : IUpdateGateService
{
    private readonly HttpClient _http;
    private readonly MobileClientOptions _options;

    public UpdateGateService(HttpClient http, IOptions<MobileClientOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<UpdateGateResult> CheckAsync(string currentVersion, CancellationToken ct = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(_options.UpdateGateTimeoutSeconds));

            using var response = await _http.GetAsync(_options.UpdateGateManifestUrl, cts.Token);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cts.Token);

            if (!doc.RootElement.TryGetProperty("platforms", out var platforms)
                || !platforms.TryGetProperty(_options.UpdateGatePlatformKey, out var platform))
            {
                return new UpdateGateResult { Status = UpdateGateStatus.CheckFailed };
            }

            var latest = platform.TryGetProperty("latestVersion", out var l) ? l.GetString() : null;
            var minimum = platform.TryGetProperty("minimumVersion", out var m) ? m.GetString() : null;
            var downloadUrl = platform.TryGetProperty("downloadUrl", out var d) ? d.GetString() : null;
            var notes = platform.TryGetProperty("note", out var n) ? n.GetString() : null;

            if (!string.IsNullOrWhiteSpace(minimum) && SemVerCompare.IsOlder(currentVersion, minimum))
            {
                return new UpdateGateResult
                {
                    Status = UpdateGateStatus.UpdateRequired,
                    LatestVersion = latest,
                    MinimumVersion = minimum,
                    DownloadUrl = downloadUrl,
                    Notes = notes,
                };
            }

            if (!string.IsNullOrWhiteSpace(latest) && SemVerCompare.IsNewer(latest, currentVersion))
            {
                return new UpdateGateResult
                {
                    Status = UpdateGateStatus.UpdateAvailable,
                    LatestVersion = latest,
                    MinimumVersion = minimum,
                    DownloadUrl = downloadUrl,
                    Notes = notes,
                };
            }

            return new UpdateGateResult { Status = UpdateGateStatus.UpToDate, LatestVersion = latest, MinimumVersion = minimum };
        }
        catch
        {
            // Sem rede / manifesto indisponível — nunca bloqueia o arranque offline
            return new UpdateGateResult { Status = UpdateGateStatus.CheckFailed };
        }
    }
}
