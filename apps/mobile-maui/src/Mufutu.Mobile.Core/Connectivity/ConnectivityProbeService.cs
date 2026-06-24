using System.Diagnostics;
using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using Mufutu.Mobile.Core.Configuration;

namespace Mufutu.Mobile.Core.Connectivity;

public sealed class ConnectivityProbeService
{
    private readonly HttpClient _http;
    private readonly INetworkStatusProvider _network;
    private readonly MobileClientOptions _options;

    public ConnectivityProbeService(
        HttpClient http,
        INetworkStatusProvider network,
        IOptions<MobileClientOptions> options)
    {
        _http = http;
        _network = network;
        _options = options.Value;
    }

    public async Task<ConnectivityProbeResult> ProbeHealthAsync(
        CancellationToken cancellationToken = default,
        string? apiBaseUrlOverride = null)
    {
        var sw = Stopwatch.StartNew();
        var networkDesc = _network.NetworkDescription;
        var isWifi = _network.IsWifi;

        if (!_network.IsInternetAvailable)
        {
            return new ConnectivityProbeResult(
                ConnectivityProbeStatus.NoNetwork,
                null,
                null,
                "Sem ligação à Internet ou Wi‑Fi",
                networkDesc,
                isWifi,
                sw.ElapsedMilliseconds);
        }

        var baseUrl = (apiBaseUrlOverride ?? _options.ApiBaseUrl).TrimEnd('/');
        var healthUrl = $"{baseUrl}/health";

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(_options.HealthProbeTimeoutSeconds));

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, healthUrl);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.TryAddWithoutValidation("X-Site-Id", _options.SiteCode);

            using var response = await _http.SendAsync(request, cts.Token).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);
            sw.Stop();

            if (!response.IsSuccessStatusCode)
            {
                return new ConnectivityProbeResult(
                    ConnectivityProbeStatus.HttpError,
                    (int)response.StatusCode,
                    body,
                    $"HTTP {(int)response.StatusCode}",
                    networkDesc,
                    isWifi,
                    sw.ElapsedMilliseconds);
            }

            return new ConnectivityProbeResult(
                ConnectivityProbeStatus.Success,
                (int)response.StatusCode,
                body,
                null,
                networkDesc,
                isWifi,
                sw.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            sw.Stop();
            return new ConnectivityProbeResult(
                ConnectivityProbeStatus.Timeout,
                null,
                null,
                $"Timeout após {_options.HealthProbeTimeoutSeconds}s",
                networkDesc,
                isWifi,
                sw.ElapsedMilliseconds);
        }
        catch (HttpRequestException ex)
        {
            sw.Stop();
            return new ConnectivityProbeResult(
                ConnectivityProbeStatus.DnsOrTlsError,
                null,
                null,
                ex.Message,
                networkDesc,
                isWifi,
                sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new ConnectivityProbeResult(
                ConnectivityProbeStatus.Unknown,
                null,
                null,
                ex.Message,
                networkDesc,
                isWifi,
                sw.ElapsedMilliseconds);
        }
    }
}
