using System.Text.Json;
using Mufutu.Mobile.Core.Api;
using Mufutu.Mobile.Core.Models;

namespace Mufutu.Mobile.Core.Offline;

public interface ICampoSyncEngine
{
    event EventHandler<SyncProgressEventArgs>? ProgressChanged;
    Task<SyncResult> ProcessQueueAsync(CancellationToken ct = default);
    Task<int> GetPendingCountAsync();
}

public sealed class SyncProgressEventArgs : EventArgs
{
    public int PendingCount { get; init; }
    public int ProcessedCount { get; init; }
    public int ErrorCount { get; init; }
    public string? CurrentOperation { get; init; }
}

public sealed class SyncResult
{
    public int Processed { get; init; }
    public int Errors { get; init; }
    public int Remaining { get; init; }
}

public sealed class CampoSyncEngine : ICampoSyncEngine
{
    private const int MaxRetries = 5;
    private const int BatchSize = 10;

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private readonly ICampoOfflineStore _store;
    private readonly MufutuApiClient _api;

    public event EventHandler<SyncProgressEventArgs>? ProgressChanged;

    public CampoSyncEngine(ICampoOfflineStore store, MufutuApiClient api)
    {
        _store = store;
        _api = api;
    }

    public Task<int> GetPendingCountAsync() => _store.GetPendingCountAsync();

    public async Task<SyncResult> ProcessQueueAsync(CancellationToken ct = default)
    {
        await _store.EnsureInitializedAsync();
        var batch = await _store.GetPendingQueueAsync(BatchSize);
        var processed = 0;
        var errors = 0;

        foreach (var item in batch)
        {
            if (item.RetryCount >= MaxRetries)
            {
                errors++;
                continue;
            }

            RaiseProgress(processed, errors, $"{item.EntityType}:{item.Operation}");
            try
            {
                await ProcessItemAsync(item, ct);
                await _store.RemoveQueueItemAsync(item.Id);
                processed++;
            }
            catch (Exception ex)
            {
                errors++;
                await _store.MarkQueueErrorAsync(item.Id, ex.Message, item.RetryCount + 1);
            }
        }

        var remaining = await _store.GetPendingCountAsync();
        RaiseProgress(processed, errors, null);
        return new SyncResult { Processed = processed, Errors = errors, Remaining = remaining };
    }

    private async Task ProcessItemAsync(SyncQueueRecord item, CancellationToken ct)
    {
        if (item.EntityType == OfflineEntityTypes.WorkOrder && item.Operation == OfflineOperations.StatusChange)
        {
            var payload = JsonSerializer.Deserialize<WorkOrderStatusPayload>(item.PayloadJson, JsonOpts)
                ?? throw new InvalidOperationException("Payload OT inválido");
            await _api.ChangeWorkOrderStatusAsync(payload.WorkOrderId, payload.Status, ct);
            await _store.MarkWorkOrderSyncedAsync(payload.WorkOrderId, payload.Status);
            return;
        }

        if (item.EntityType == OfflineEntityTypes.MaintenanceRequest && item.Operation == OfflineOperations.Create)
        {
            var payload = JsonSerializer.Deserialize<CreateMaintenanceRequestPayload>(item.PayloadJson, JsonOpts)
                ?? throw new InvalidOperationException("Payload PT inválido");
            await _api.CreateMaintenanceRequestAsync(payload, ct);
            return;
        }

        throw new NotSupportedException($"Operação não suportada: {item.EntityType}/{item.Operation}");
    }

    private void RaiseProgress(int processed, int errors, string? op)
    {
        ProgressChanged?.Invoke(this, new SyncProgressEventArgs
        {
            ProcessedCount = processed,
            ErrorCount = errors,
            CurrentOperation = op,
        });
    }
}

public sealed class WorkOrderStatusPayload
{
    public string WorkOrderId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
