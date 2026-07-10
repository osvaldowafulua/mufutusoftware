using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Mufutu.Mobile.Core.Configuration;
using Mufutu.Mobile.Core.Models;
using Mufutu.Mobile.Core.Services;

namespace Mufutu.Mobile.Core.Api;

public sealed class MufutuApiClient
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http;
    private readonly IAuthSessionStore _session;
    private readonly IApiSettings _settings;

    public MufutuApiClient(
        HttpClient http,
        IAuthSessionStore session,
        IApiSettings settings)
    {
        _http = http;
        _session = session;
        _settings = settings;
    }

    private async Task ApplyAuthAsync()
    {
        var token = await _session.GetAccessTokenAsync();
        _http.DefaultRequestHeaders.Authorization = string.IsNullOrWhiteSpace(token)
            ? null
            : new AuthenticationHeaderValue("Bearer", token);
        _http.DefaultRequestHeaders.Remove("X-Site-Id");
        _http.DefaultRequestHeaders.Add("X-Site-Id", await _session.GetSiteCodeAsync());
    }

    private async Task<bool> TryRefreshTokenAsync(CancellationToken ct)
    {
        var refresh = await _session.GetRefreshTokenAsync();
        if (string.IsNullOrWhiteSpace(refresh))
        {
            return false;
        }

        var baseUrl = _settings.ApiBaseUrl.TrimEnd('/');
        using var res = await _http.PostAsJsonAsync($"{baseUrl}/auth/refresh", new { refreshToken = refresh }, ct);
        if (!res.IsSuccessStatusCode)
        {
            return false;
        }

        var payload = await res.Content.ReadFromJsonAsync<RefreshTokenResponse>(JsonOpts, ct);
        if (string.IsNullOrWhiteSpace(payload?.AccessToken))
        {
            return false;
        }

        await _session.UpdateAccessTokenAsync(payload.AccessToken, payload.RefreshToken);
        return true;
    }

    private async Task<HttpResponseMessage> SendAuthorizedAsync(
        Func<Task<HttpResponseMessage>> send,
        CancellationToken ct)
    {
        await ApplyAuthAsync();
        var res = await send();
        if (res.StatusCode != HttpStatusCode.Unauthorized)
        {
            return res;
        }

        res.Dispose();
        if (!await TryRefreshTokenAsync(ct))
        {
            throw new HttpRequestException("Sessão expirada — volte a entrar.");
        }

        await ApplyAuthAsync();
        return await send();
    }

    public async Task<LoginResponse> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var baseUrl = _settings.ApiBaseUrl.TrimEnd('/');
        using var res = await _http.PostAsJsonAsync(
            $"{baseUrl}/auth/login",
            new LoginRequest { Email = email.Trim(), Password = password },
            ct);

        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"Login falhou ({(int)res.StatusCode}): {body}");
        }

        var payload = await res.Content.ReadFromJsonAsync<LoginResponse>(JsonOpts, ct)
            ?? throw new InvalidOperationException("Resposta de login inválida");
        await _session.SaveAsync(payload, _settings.SiteCode);
        return payload;
    }

    public Task<IReadOnlyList<WorkOrderDto>> GetMyWorkOrdersAsync(CancellationToken ct = default) =>
        GetMyWorkOrdersAsync(null, ct);

    public async Task<IReadOnlyList<WorkOrderDto>> GetMyWorkOrdersAsync(string? updatedSince, CancellationToken ct = default)
    {
        var userId = await _session.GetUserIdAsync();
        var baseUrl = _settings.ApiBaseUrl.TrimEnd('/');
        var url = $"{baseUrl}/work-orders?limit=50&page=1";
        if (!string.IsNullOrWhiteSpace(userId))
        {
            url += $"&assignedTechnicianId={Uri.EscapeDataString(userId)}";
        }
        if (!string.IsNullOrWhiteSpace(updatedSince))
        {
            // Delta sync (CMMS ≥ 1.2); versões anteriores ignoram o parâmetro
            url += $"&updatedSince={Uri.EscapeDataString(updatedSince)}";
        }

        using var res = await SendAuthorizedAsync(() => _http.GetAsync(url, ct), ct);
        res.EnsureSuccessStatusCode();
        var page = await res.Content.ReadFromJsonAsync<PagedWorkOrdersResponse>(JsonOpts, ct)
            ?? new PagedWorkOrdersResponse();

        return page.Items
            .Where(wo => wo.Status is "approved" or "in_progress")
            .OrderByDescending(wo => wo.Status == "in_progress")
            .ThenBy(wo => wo.Number)
            .ToList();
    }

    public async Task ChangeWorkOrderStatusAsync(string id, string status, CancellationToken ct = default)
    {
        var baseUrl = _settings.ApiBaseUrl.TrimEnd('/');
        using var res = await SendAuthorizedAsync(
            () => _http.PatchAsync($"{baseUrl}/work-orders/{id}/status/{status}", null, ct),
            ct);
        res.EnsureSuccessStatusCode();
    }

    public async Task CreateMaintenanceRequestAsync(
        CreateMaintenanceRequestPayload payload,
        CancellationToken ct = default)
    {
        var baseUrl = _settings.ApiBaseUrl.TrimEnd('/');
        using var res = await SendAuthorizedAsync(
            () => _http.PostAsJsonAsync($"{baseUrl}/maintenance-requests", payload, ct),
            ct);
        res.EnsureSuccessStatusCode();
    }

    public async Task<NotificationsPageResponse> GetNotificationsAsync(int limit = 50, CancellationToken ct = default)
    {
        var baseUrl = _settings.ApiBaseUrl.TrimEnd('/');
        using var res = await SendAuthorizedAsync(
            () => _http.GetAsync($"{baseUrl}/notifications?limit={limit}", ct),
            ct);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<NotificationsPageResponse>(JsonOpts, ct)
            ?? new NotificationsPageResponse();
    }

    public async Task MarkNotificationReadAsync(string id, CancellationToken ct = default)
    {
        var baseUrl = _settings.ApiBaseUrl.TrimEnd('/');
        using var res = await SendAuthorizedAsync(
            () => _http.PatchAsync($"{baseUrl}/notifications/{id}/read", null, ct),
            ct);
        res.EnsureSuccessStatusCode();
    }

    public async Task MarkAllNotificationsReadAsync(CancellationToken ct = default)
    {
        var baseUrl = _settings.ApiBaseUrl.TrimEnd('/');
        using var res = await SendAuthorizedAsync(
            () => _http.PatchAsync($"{baseUrl}/notifications/mark-all-read", null, ct),
            ct);
        res.EnsureSuccessStatusCode();
    }

    public string GetNotificationsSocketUrl()
    {
        var root = _settings.ApiBaseUrl.TrimEnd('/');
        if (root.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
        {
            root = root[..^4];
        }
        return $"{root}/notifications";
    }
}
