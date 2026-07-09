using System.Text.Json;
using Microsoft.Extensions.Options;
using Mufutu.Mobile.Core.Api;
using Mufutu.Mobile.Core.Configuration;
using Mufutu.Mobile.Core.Models;

namespace Mufutu.Mobile.Core.Offline;

public interface ICampoSyncEngine
{
    event EventHandler<SyncProgressEventArgs>? ProgressChanged;
    Task<SyncResult> ProcessQueueAsync(CancellationToken ct = default);
    Task<SyncResult> SyncAllAsync(CancellationToken ct = default);
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

    /// <summary>Registos novos ou alterados recebidos do servidor.</summary>
    public int Pulled { get; init; }

    /// <summary>Registos que já estavam iguais na cache — não reescritos.</summary>
    public int Unchanged { get; init; }

    /// <summary>Registos removidos localmente por já não existirem no servidor.</summary>
    public int Removed { get; init; }

    /// <summary>False quando o download falhou (o push pode ter corrido na mesma).</summary>
    public bool PullOk { get; init; } = true;
}

public sealed class CampoSyncEngine : ICampoSyncEngine
{
    private const int MaxRetries = 5;
    private const int BatchSize = 10;

    // Sobreposição do cursor delta: protege contra relógios dessincronizados
    private const int CursorOverlapMinutes = 5;

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private readonly ICampoOfflineStore _store;
    private readonly MufutuApiClient _api;
    private readonly MobileClientOptions _options;

    public event EventHandler<SyncProgressEventArgs>? ProgressChanged;

    public CampoSyncEngine(ICampoOfflineStore store, MufutuApiClient api, IOptions<MobileClientOptions> options)
    {
        _store = store;
        _api = api;
        _options = options.Value;
    }

    public Task<int> GetPendingCountAsync() => _store.GetPendingCountAsync();

    /// <summary>
    /// Ciclo completo do armazenamento geral local: primeiro envia as alterações
    /// feitas offline (push), depois descarrega do servidor só o que mudou (pull).
    /// </summary>
    public async Task<SyncResult> SyncAllAsync(CancellationToken ct = default)
    {
        var push = await ProcessQueueAsync(ct);

        try
        {
            var pull = await PullWorkOrdersAsync(ct);
            return new SyncResult
            {
                Processed = push.Processed,
                Errors = push.Errors,
                Remaining = push.Remaining,
                Pulled = pull.Added + pull.Updated,
                Unchanged = pull.Unchanged + pull.PreservedLocal,
                Removed = pull.Removed,
                PullOk = true,
            };
        }
        catch
        {
            // Sem rede/erro do servidor: a cache local continua válida
            return new SyncResult
            {
                Processed = push.Processed,
                Errors = push.Errors,
                Remaining = push.Remaining,
                PullOk = false,
            };
        }
    }

    private async Task<PullStats> PullWorkOrdersAsync(CancellationToken ct)
    {
        await _store.EnsureInitializedAsync();
        var pullStarted = DateTimeOffset.UtcNow;

        string? updatedSince = null;
        if (_options.ServerSupportsDeltaSync)
        {
            var stored = await _store.GetSyncCursorAsync(OfflineEntityTypes.WorkOrder);
            if (DateTimeOffset.TryParse(stored, out var cursor))
            {
                updatedSince = cursor.AddMinutes(-CursorOverlapMinutes).UtcDateTime.ToString("O");
            }
        }

        var items = await _api.GetMyWorkOrdersAsync(updatedSince, ct);
        var stats = await _store.UpsertWorkOrdersAsync(items);

        if (updatedSince == null)
        {
            // Pull completo é autoritativo: remover o que já não veio do servidor.
            // Em pulls delta (parciais) nunca varremos.
            var keep = items
                .Where(i => !string.IsNullOrWhiteSpace(i.Id))
                .Select(i => i.Id!)
                .ToList();
            stats.Removed = await _store.PruneWorkOrdersNotInAsync(keep);
        }

        await _store.SetSyncCursorAsync(OfflineEntityTypes.WorkOrder, pullStarted.ToString("O"));
        return stats;
    }

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
