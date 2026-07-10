using System.Text.Json;
using Mufutu.Desktop.Core.Api;
using Mufutu.Desktop.Core.Api.Models;

namespace Mufutu.Desktop.Core.Offline;

/// <summary>
/// Armazenamento geral local do desktop: descarrega os dados do servidor para a
/// base local encriptada e serve sempre a partir dela — igual online e offline.
/// Só reescreve o que mudou (hash de conteúdo) e remove o que deixou de existir.
/// </summary>
public interface IDesktopSyncService
{
    Task<DesktopSyncResult> SyncAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<WorkOrderDto>> GetWorkOrdersAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AssetDto>> GetAssetsAsync(CancellationToken ct = default);
    Task<DashboardSummaryDto?> GetDashboardSummaryAsync(CancellationToken ct = default);
}

public sealed class DesktopSyncResult
{
    public int Pulled { get; init; }
    public int Unchanged { get; init; }
    public int Removed { get; init; }
    public bool PullOk { get; init; } = true;
}

public sealed class DesktopSyncService : IDesktopSyncService
{
    public const string EntityWorkOrder = "workOrder";
    public const string EntityAsset = "asset";
    public const string EntityDashboard = "dashboard";

    // Uma página grande por sync: suficiente para operação de mina; paginação
    // completa entra com o delta updatedSince (CMMS ≥ 1.2).
    private const int PullPageSize = 200;

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private readonly IMufutuApiClient _api;
    private readonly OfflineStore _store;

    public DesktopSyncService(IMufutuApiClient api, OfflineStore store)
    {
        _api = api;
        _store = store;
    }

    public async Task<DesktopSyncResult> SyncAllAsync(CancellationToken ct = default)
    {
        try
        {
            var pullStarted = DateTimeOffset.UtcNow;

            var workOrders = await _api.GetWorkOrdersAsync(page: 1, limit: PullPageSize, ct);
            var woStats = await UpsertAndPruneAsync(
                EntityWorkOrder,
                workOrders.Data.Select(wo => (wo.Id, JsonSerializer.Serialize(wo))).ToList(),
                ct);

            var assets = await _api.GetAssetsAsync(page: 1, limit: PullPageSize, ct);
            var assetStats = await UpsertAndPruneAsync(
                EntityAsset,
                assets.Data.Select(a => (a.Id, JsonSerializer.Serialize(a))).ToList(),
                ct);

            var dashboard = await _api.GetDashboardSummaryAsync(ct);
            var dashStats = await _store.UpsertEntitiesAsync(
                EntityDashboard,
                [("summary", JsonSerializer.Serialize(dashboard))],
                ct);

            await _store.SetSyncCursorAsync(EntityWorkOrder, pullStarted.ToString("O"), ct);
            await _store.SetSyncCursorAsync(EntityAsset, pullStarted.ToString("O"), ct);

            return new DesktopSyncResult
            {
                Pulled = woStats.Added + woStats.Updated + assetStats.Added + assetStats.Updated + dashStats.Added + dashStats.Updated,
                Unchanged = woStats.Unchanged + assetStats.Unchanged + dashStats.Unchanged,
                Removed = woStats.Removed + assetStats.Removed,
                PullOk = true,
            };
        }
        catch
        {
            // Sem rede/erro do servidor — a cache local continua a servir
            return new DesktopSyncResult { PullOk = false };
        }
    }

    public async Task<IReadOnlyList<WorkOrderDto>> GetWorkOrdersAsync(CancellationToken ct = default)
    {
        await SyncAllAsync(ct);
        return await ReadEntitiesAsync<WorkOrderDto>(EntityWorkOrder, ct);
    }

    public async Task<IReadOnlyList<AssetDto>> GetAssetsAsync(CancellationToken ct = default)
    {
        await SyncAllAsync(ct);
        return await ReadEntitiesAsync<AssetDto>(EntityAsset, ct);
    }

    public async Task<DashboardSummaryDto?> GetDashboardSummaryAsync(CancellationToken ct = default)
    {
        await SyncAllAsync(ct);
        var cached = await ReadEntitiesAsync<DashboardSummaryDto>(EntityDashboard, ct);
        return cached.Count > 0 ? cached[0] : null;
    }

    private async Task<EntityPullStats> UpsertAndPruneAsync(
        string type,
        IReadOnlyCollection<(string Id, string Json)> items,
        CancellationToken ct)
    {
        var stats = await _store.UpsertEntitiesAsync(type, items, ct);

        // Pull completo é autoritativo — remove localmente o que já não existe
        var keep = items.Select(i => i.Id).Where(id => !string.IsNullOrWhiteSpace(id)).ToList();
        stats.Removed = await _store.PruneEntitiesNotInAsync(type, keep, ct);
        return stats;
    }

    private async Task<IReadOnlyList<T>> ReadEntitiesAsync<T>(string type, CancellationToken ct)
    {
        var rows = await _store.GetEntitiesAsync(type, ct);
        var list = new List<T>(rows.Count);
        foreach (var json in rows)
        {
            try
            {
                var item = JsonSerializer.Deserialize<T>(json, JsonOpts);
                if (item != null)
                {
                    list.Add(item);
                }
            }
            catch
            {
                // linha corrompida — ignorar em vez de falhar a listagem
            }
        }

        return list;
    }
}
