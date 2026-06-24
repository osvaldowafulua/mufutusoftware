using System.Text.Json;
using Mufutu.Mobile.Core.Api;
using Mufutu.Mobile.Core.Connectivity;
using Mufutu.Mobile.Core.Models;
using Mufutu.Mobile.Core.Offline;
using Mufutu.Mobile.Core.Services;
using SocketIOClient;
using SocketIOClient.Transport;

namespace Mufutu.Mobile.Core.Notifications;

public interface INotificationPresenter
{
    Task ShowAsync(NotificationDto notification, CancellationToken ct = default);
    Task RequestPermissionAsync(CancellationToken ct = default);
}

public interface ICampoNotificationService
{
    event EventHandler? NotificationsChanged;
    bool IsSocketConnected { get; }
    Task StartAsync(CancellationToken ct = default);
    Task StopAsync();
    Task RefreshAsync(CancellationToken ct = default);
    Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync();
    Task<int> GetUnreadCountAsync();
    Task MarkReadAsync(string id, CancellationToken ct = default);
    Task MarkAllReadAsync(CancellationToken ct = default);
}

public sealed class CampoNotificationService : ICampoNotificationService, IDisposable
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private readonly MufutuApiClient _api;
    private readonly ICampoOfflineStore _store;
    private readonly IAuthSessionStore _session;
    private readonly INetworkStatusProvider _network;
    private readonly INotificationPresenter? _presenter;

    private global::SocketIOClient.SocketIO? _socket;
    private readonly SemaphoreSlim _startLock = new(1, 1);

    public event EventHandler? NotificationsChanged;

    public bool IsSocketConnected => _socket?.Connected == true;

    public CampoNotificationService(
        MufutuApiClient api,
        ICampoOfflineStore store,
        IAuthSessionStore session,
        INetworkStatusProvider network,
        INotificationPresenter? presenter = null)
    {
        _api = api;
        _store = store;
        _session = session;
        _network = network;
        _presenter = presenter;
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        await _startLock.WaitAsync(ct);
        try
        {
            if (!await _session.HasSessionAsync())
            {
                return;
            }

            await _store.EnsureInitializedAsync();
            await RefreshAsync(ct);
            await ConnectSocketAsync(ct);
        }
        finally
        {
            _startLock.Release();
        }
    }

    public async Task StopAsync()
    {
        if (_socket != null)
        {
            await _socket.DisconnectAsync();
            _socket.Dispose();
            _socket = null;
        }
    }

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        await _store.EnsureInitializedAsync();

        if (!_network.IsInternetAvailable)
        {
            NotificationsChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        try
        {
            var page = await _api.GetNotificationsAsync(50, ct);
            var items = page.Data ?? [];
            await _store.UpsertNotificationsAsync(items.Select(ToCached));
            NotificationsChanged?.Invoke(this, EventArgs.Empty);
        }
        catch
        {
            NotificationsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public async Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync()
    {
        await _store.EnsureInitializedAsync();
        var cached = await _store.GetNotificationsAsync(50);
        return cached.Select(FromCached).ToList();
    }

    public Task<int> GetUnreadCountAsync() => _store.GetUnreadNotificationCountAsync();

    public async Task MarkReadAsync(string id, CancellationToken ct = default)
    {
        await _store.MarkNotificationReadLocalAsync(id);
        NotificationsChanged?.Invoke(this, EventArgs.Empty);

        if (_network.IsInternetAvailable)
        {
            try
            {
                await _api.MarkNotificationReadAsync(id, ct);
            }
            catch
            {
                // local state kept
            }
        }
    }

    public async Task MarkAllReadAsync(CancellationToken ct = default)
    {
        await _store.MarkAllNotificationsReadLocalAsync();
        NotificationsChanged?.Invoke(this, EventArgs.Empty);

        if (_network.IsInternetAvailable)
        {
            try
            {
                await _api.MarkAllNotificationsReadAsync(ct);
            }
            catch
            {
                // local state kept
            }
        }
    }

    private async Task ConnectSocketAsync(CancellationToken ct)
    {
        if (!_network.IsInternetAvailable)
        {
            return;
        }

        var token = await _session.GetAccessTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        await StopAsync();

        var url = _api.GetNotificationsSocketUrl();
        _socket = new global::SocketIOClient.SocketIO(url, new global::SocketIOClient.SocketIOOptions
        {
            Transport = TransportProtocol.WebSocket,
            Reconnection = true,
            ReconnectionDelay = 2000,
            Auth = new Dictionary<string, string> { ["token"] = token },
        });

        _socket.OnConnected += async (_, _) =>
        {
            NotificationsChanged?.Invoke(this, EventArgs.Empty);
            await RefreshAsync();
        };

        _socket.OnDisconnected += async (_, _) =>
        {
            NotificationsChanged?.Invoke(this, EventArgs.Empty);
            await RefreshAsync();
        };

        _socket.On("notification", async response =>
        {
            try
            {
                var dto = response.GetValue<NotificationDto>(0);
                if (dto == null || string.IsNullOrWhiteSpace(dto.Id))
                {
                    return;
                }

                await _store.UpsertNotificationsAsync([ToCached(dto)]);
                NotificationsChanged?.Invoke(this, EventArgs.Empty);

                if (_presenter != null)
                {
                    await _presenter.ShowAsync(dto, ct);
                }
            }
            catch
            {
                // ignore malformed payloads
            }
        });

        await _socket.ConnectAsync();
    }

    private static CachedNotification ToCached(NotificationDto n) => new()
    {
        Id = n.Id,
        Type = n.Type,
        Title = n.Title,
        Message = n.Message,
        DataJson = n.Data == null ? null : JsonSerializer.Serialize(n.Data),
        Timestamp = n.Timestamp,
        Read = n.Read,
    };

    private static NotificationDto FromCached(CachedNotification n) => new()
    {
        Id = n.Id,
        Type = n.Type,
        Title = n.Title,
        Message = n.Message,
        Data = string.IsNullOrWhiteSpace(n.DataJson)
            ? null
            : JsonSerializer.Deserialize<Dictionary<string, object>>(n.DataJson),
        Timestamp = n.Timestamp,
        Read = n.Read,
    };

    public void Dispose()
    {
        _ = StopAsync();
        _startLock.Dispose();
    }
}
