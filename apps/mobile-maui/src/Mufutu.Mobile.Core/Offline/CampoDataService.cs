using System.Text.Json;
using Mufutu.Mobile.Core.Api;
using Mufutu.Mobile.Core.Connectivity;
using Mufutu.Mobile.Core.Models;

namespace Mufutu.Mobile.Core.Offline;

public interface ICampoDataService
{
    Task<IReadOnlyList<WorkOrderDto>> GetMyWorkOrdersAsync(CancellationToken ct = default);
    Task ChangeWorkOrderStatusAsync(string id, string status, CancellationToken ct = default);
    Task<SubmitResult> CreateMaintenanceRequestAsync(CreateMaintenanceRequestPayload payload, CancellationToken ct = default);
    Task<SyncResult> SyncNowAsync(CancellationToken ct = default);
    Task<int> GetPendingCountAsync();
    bool IsOnline { get; }
}

public sealed class SubmitResult
{
    public bool Queued { get; init; }
    public string Message { get; init; } = string.Empty;
}

public sealed class CampoDataService : ICampoDataService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private readonly MufutuApiClient _api;
    private readonly ICampoOfflineStore _store;
    private readonly ICampoSyncEngine _sync;
    private readonly INetworkStatusProvider _network;

    public CampoDataService(
        MufutuApiClient api,
        ICampoOfflineStore store,
        ICampoSyncEngine sync,
        INetworkStatusProvider network)
    {
        _api = api;
        _store = store;
        _sync = sync;
        _network = network;
    }

    public bool IsOnline => _network.IsInternetAvailable;

    public async Task<IReadOnlyList<WorkOrderDto>> GetMyWorkOrdersAsync(CancellationToken ct = default)
    {
        await _store.EnsureInitializedAsync();

        // Armazenamento geral local: a cache é a fonte de verdade. Online,
        // sincroniza primeiro (push + pull com dedupe) e serve da cache —
        // offline serve directamente da cache. Comportamento idêntico nos dois modos.
        if (IsOnline)
        {
            try
            {
                await _sync.SyncAllAsync(ct);
            }
            catch
            {
                // cache local continua válida
            }
        }

        var cached = await _store.GetWorkOrdersAsync();
        return cached.Select(c =>
        {
            try
            {
                return JsonSerializer.Deserialize<WorkOrderDto>(c.Json, JsonOpts)!;
            }
            catch
            {
                return new WorkOrderDto
                {
                    Id = c.Id,
                    Number = c.Number,
                    Title = c.Title,
                    Status = c.Status,
                    Priority = c.Priority,
                    Asset = new AssetRefDto { Name = c.AssetName },
                };
            }
        }).ToList();
    }

    public async Task ChangeWorkOrderStatusAsync(string id, string status, CancellationToken ct = default)
    {
        await _store.EnsureInitializedAsync();
        await _store.UpdateWorkOrderStatusLocalAsync(id, status);

        if (IsOnline)
        {
            try
            {
                await _api.ChangeWorkOrderStatusAsync(id, status, ct);
                return;
            }
            catch
            {
                // queue below
            }
        }

        await _store.EnqueueAsync(new SyncQueueRecord
        {
            EntityType = OfflineEntityTypes.WorkOrder,
            EntityId = id,
            Operation = OfflineOperations.StatusChange,
            Priority = 1,
            PayloadJson = JsonSerializer.Serialize(new WorkOrderStatusPayload
            {
                WorkOrderId = id,
                Status = status,
            }),
        });
    }

    public async Task<SubmitResult> CreateMaintenanceRequestAsync(
        CreateMaintenanceRequestPayload payload,
        CancellationToken ct = default)
    {
        await _store.EnsureInitializedAsync();

        if (IsOnline)
        {
            try
            {
                await _api.CreateMaintenanceRequestAsync(payload, ct);
                return new SubmitResult { Queued = false, Message = "Enviado" };
            }
            catch
            {
                // queue below
            }
        }

        var tempId = $"pt-offline-{DateTime.UtcNow.Ticks}";
        await _store.EnqueueAsync(new SyncQueueRecord
        {
            EntityType = OfflineEntityTypes.MaintenanceRequest,
            EntityId = tempId,
            Operation = OfflineOperations.Create,
            Priority = 1,
            PayloadJson = JsonSerializer.Serialize(payload, JsonOpts),
        });

        return new SubmitResult
        {
            Queued = true,
            Message = IsOnline ? "Guardado — vai reenviar" : "Sem rede — guardado para enviar",
        };
    }

    public Task<SyncResult> SyncNowAsync(CancellationToken ct = default) =>
        _sync.SyncAllAsync(ct);

    public Task<int> GetPendingCountAsync() => _store.GetPendingCountAsync();
}
