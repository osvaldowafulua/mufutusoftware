using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Security;
using System.Security.Authentication;
using System.Text.Json;
using Mufutu.Desktop.Core.Api.Models;
using Mufutu.Desktop.Core.Configuration;
using Mufutu.Desktop.Core.Network;
using Mufutu.Desktop.Core.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mufutu.Desktop.Core.Api;

public interface IMufutuApiClient
{
    Task<LoginResponse> LoginAsync(string email, string password, CancellationToken ct = default);
    Task<LoginResponse> RefreshAsync(CancellationToken ct = default);
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken ct = default);
    Task<PagedResponse<WorkOrderDto>> GetWorkOrdersAsync(int page = 1, int limit = 20, CancellationToken ct = default);
    Task<PagedResponse<AssetDto>> GetAssetsAsync(int page = 1, int limit = 20, CancellationToken ct = default);
    bool IsAuthenticated { get; }
}

public sealed class MufutuApiClient : IMufutuApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http;
    private readonly ISecureStorageService _storage;
    private readonly MufutuClientOptions _options;
    private readonly ILogger<MufutuApiClient> _logger;

    public MufutuApiClient(
        HttpClient http,
        ISecureStorageService storage,
        IOptions<MufutuClientOptions> options,
        ILogger<MufutuApiClient> logger)
    {
        _http = http;
        _storage = storage;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsAuthenticated => !string.IsNullOrEmpty(_storage.LoadSecret("access_token"));

    public async Task<LoginResponse> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        await EnsureNetworkAsync(ct);

        var response = await _http.PostAsJsonAsync(
            "auth/login",
            new LoginRequest { Email = email, Password = password },
            ct);

        await EnsureSuccessAsync(response, ct);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions, ct)
            ?? throw new InvalidOperationException("Resposta de login inválida");

        PersistTokens(result);
        return result;
    }

    public async Task<LoginResponse> RefreshAsync(CancellationToken ct = default)
    {
        var refresh = _storage.LoadSecret("refresh_token")
            ?? throw new UnauthorizedAccessException("Sem refresh token");

        var response = await _http.PostAsJsonAsync(
            "auth/refresh",
            new RefreshTokenRequest { RefreshToken = refresh },
            ct);

        await EnsureSuccessAsync(response, ct);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions, ct)
            ?? throw new InvalidOperationException("Resposta de refresh inválida");

        PersistTokens(result);
        return result;
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "reports/overview");
        ApplyAuth(request);
        var response = await SendWithRefreshAsync(request, ct);
        await EnsureSuccessAsync(response, ct);
        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await System.Text.Json.JsonDocument.ParseAsync(stream, cancellationToken: ct);
        return DashboardSummaryDto.FromApiJson(doc.RootElement);
    }

    public async Task<PagedResponse<WorkOrderDto>> GetWorkOrdersAsync(
        int page = 1,
        int limit = 20,
        CancellationToken ct = default)
    {
        return await GetAuthorizedAsync<PagedResponse<WorkOrderDto>>(
            $"work-orders?page={page}&limit={limit}",
            ct) ?? new PagedResponse<WorkOrderDto>();
    }

    public async Task<PagedResponse<AssetDto>> GetAssetsAsync(
        int page = 1,
        int limit = 20,
        CancellationToken ct = default)
    {
        return await GetAuthorizedAsync<PagedResponse<AssetDto>>(
            $"assets?page={page}&limit={limit}",
            ct) ?? new PagedResponse<AssetDto>();
    }

    private async Task<T?> GetAuthorizedAsync<T>(string path, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        ApplyAuth(request);
        var response = await SendWithRefreshAsync(request, ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
    }

    private async Task<HttpResponseMessage> SendWithRefreshAsync(
        HttpRequestMessage request,
        CancellationToken ct)
    {
        var response = await _http.SendAsync(request, ct);
        if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized)
        {
            return response;
        }

        response.Dispose();
        await RefreshAsync(ct);

        using var retry = CloneRequest(request);
        ApplyAuth(retry);
        return await _http.SendAsync(retry, ct);
    }

    private void PersistTokens(LoginResponse tokens)
    {
        _storage.SaveSecret("access_token", tokens.AccessToken);
        _storage.SaveSecret("refresh_token", tokens.RefreshToken);
    }

    private void ApplyAuth(HttpRequestMessage request)
    {
        var token = _storage.LoadSecret("access_token");
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    private static HttpRequestMessage CloneRequest(HttpRequestMessage original)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri);
        foreach (var header in original.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }

    private async Task EnsureNetworkAsync(CancellationToken ct)
    {
        if (!NetworkConnectivity.IsNetworkAvailable())
        {
            _logger.LogWarning("Sem interface de rede activa (Wi-Fi/Ethernet)");
            throw new HttpRequestException(
                "Sem ligação de rede. Verifique Wi-Fi ou Ethernet e tente novamente.");
        }

        if (_http.BaseAddress is null)
        {
            return;
        }

        var reachable = await NetworkConnectivity.CanReachApiAsync(_http.BaseAddress, ct);
        if (!reachable)
        {
            _logger.LogWarning(
                "API inacessível em {BaseAddress}",
                _http.BaseAddress);
            throw new HttpRequestException(
                $"Não foi possível contactar a API em {_http.BaseAddress}. Verifique internet e firewall.");
        }
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(ct);
        throw new HttpRequestException(
            $"API {(int)response.StatusCode}: {body}",
            null,
            response.StatusCode);
    }
}

public sealed class MufutuHttpClientFactory
{
    public static HttpClientHandler CreateHandler(MufutuClientOptions options)
    {
        var handler = new HttpClientHandler
        {
            // UseProxy=true (default) → proxy automático do Windows (Wi-Fi/Ethernet corporativo)
            UseProxy = true,
            DefaultProxyCredentials = CredentialCache.DefaultCredentials,
            AutomaticDecompression = DecompressionMethods.All,
            SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
            ServerCertificateCustomValidationCallback = (request, cert, chain, errors) =>
            {
                if (errors == SslPolicyErrors.None)
                {
                    return true;
                }

                var host = request.RequestUri?.Host ?? string.Empty;
                return options.PinnedHostSuffixes.Any(suffix =>
                    host.Equals(suffix, StringComparison.OrdinalIgnoreCase)
                    || host.EndsWith($".{suffix}", StringComparison.OrdinalIgnoreCase));
            },
        };
        return handler;
    }
}
