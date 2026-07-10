using System.Text.Json;
using Microsoft.Data.Sqlite;
using Mufutu.Mobile.Core.Models;

namespace Mufutu.Mobile.Core.Offline;

public interface ICampoOfflineStore
{
    Task EnsureInitializedAsync();
    Task<int> GetPendingCountAsync();
    Task<PullStats> UpsertWorkOrdersAsync(IEnumerable<WorkOrderDto> items);
    Task<int> PruneWorkOrdersNotInAsync(IReadOnlyCollection<string> keepIds);
    Task<string?> GetSyncCursorAsync(string entityType);
    Task SetSyncCursorAsync(string entityType, string cursor);
    Task<IReadOnlyList<CachedWorkOrder>> GetWorkOrdersAsync();
    Task UpdateWorkOrderStatusLocalAsync(string id, string status);
    Task MarkWorkOrderSyncedAsync(string id, string status);
    Task<long> EnqueueAsync(SyncQueueRecord item);
    Task<IReadOnlyList<SyncQueueRecord>> GetPendingQueueAsync(int limit = 10);
    Task RemoveQueueItemAsync(long id);
    Task MarkQueueErrorAsync(long id, string error, int retryCount);
    Task UpsertNotificationsAsync(IEnumerable<CachedNotification> items);
    Task<IReadOnlyList<CachedNotification>> GetNotificationsAsync(int limit = 50);
    Task<int> GetUnreadNotificationCountAsync();
    Task MarkNotificationReadLocalAsync(string id);
    Task MarkAllNotificationsReadLocalAsync();
    Task ClearAllAsync();
}

public sealed class CampoOfflineStore : ICampoOfflineStore, IDisposable
{
    private readonly IDatabasePathProvider _paths;
    private SqliteConnection? _connection;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public CampoOfflineStore(IDatabasePathProvider paths)
    {
        _paths = paths;
    }

    public async Task EnsureInitializedAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (_connection != null)
            {
                return;
            }

            var path = _paths.GetDatabasePath();
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            _connection = new SqliteConnection($"Data Source={path}");
            await _connection.OpenAsync();
            await ExecuteAsync("""
                CREATE TABLE IF NOT EXISTS work_orders (
                    id TEXT PRIMARY KEY,
                    number TEXT,
                    title TEXT,
                    status TEXT,
                    priority TEXT,
                    asset_name TEXT,
                    json TEXT,
                    sync_status TEXT,
                    last_modified TEXT
                );
                CREATE TABLE IF NOT EXISTS sync_queue (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    entity_type TEXT NOT NULL,
                    entity_id TEXT NOT NULL,
                    operation TEXT NOT NULL,
                    payload_json TEXT NOT NULL,
                    priority INTEGER NOT NULL,
                    retry_count INTEGER NOT NULL DEFAULT 0,
                    error TEXT,
                    created_at TEXT NOT NULL
                );
                CREATE TABLE IF NOT EXISTS notifications (
                    id TEXT PRIMARY KEY,
                    type TEXT,
                    title TEXT,
                    message TEXT,
                    data_json TEXT,
                    timestamp TEXT,
                    read INTEGER NOT NULL DEFAULT 0
                );
                CREATE TABLE IF NOT EXISTS sync_cursors (
                    entity_type TEXT PRIMARY KEY,
                    cursor TEXT NOT NULL,
                    last_synced_at TEXT NOT NULL
                );
                """);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<int> GetPendingCountAsync()
    {
        await EnsureInitializedAsync();
        await using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sync_queue";
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task<PullStats> UpsertWorkOrdersAsync(IEnumerable<WorkOrderDto> items)
    {
        await EnsureInitializedAsync();
        var stats = new PullStats();

        await using var tx = (SqliteTransaction)await _connection!.BeginTransactionAsync();
        foreach (var wo in items)
        {
            if (string.IsNullOrWhiteSpace(wo.Id))
            {
                continue;
            }

            var json = JsonSerializer.Serialize(wo);

            // Dedupe: só escreve o que mudou; alterações locais pendentes nunca
            // são esmagadas por dados do servidor (local-wins até o push aceitar).
            string? existingJson = null;
            string? existingSyncStatus = null;
            await using (var check = _connection.CreateCommand())
            {
                check.Transaction = tx;
                check.CommandText = "SELECT json, sync_status FROM work_orders WHERE id=$id";
                check.Parameters.AddWithValue("$id", wo.Id);
                await using var reader = await check.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    existingJson = reader.GetString(0);
                    existingSyncStatus = reader.GetString(1);
                }
            }

            if (existingSyncStatus == "pending")
            {
                stats.PreservedLocal++;
                continue;
            }

            if (existingJson == json)
            {
                stats.Unchanged++;
                continue;
            }

            var cached = new CachedWorkOrder
            {
                Id = wo.Id,
                Number = wo.Number ?? "—",
                Title = wo.Title ?? "Trabalho",
                Status = wo.Status ?? "approved",
                Priority = wo.Priority ?? "medium",
                AssetName = wo.Asset?.Name ?? wo.Asset?.Code ?? "Equipamento",
                Json = json,
                SyncStatus = "synced",
                LastModified = DateTime.UtcNow,
            };
            await UpsertWorkOrderAsync(cached, tx);

            if (existingJson == null)
            {
                stats.Added++;
            }
            else
            {
                stats.Updated++;
            }
        }

        await tx.CommitAsync();
        return stats;
    }

    public async Task<int> PruneWorkOrdersNotInAsync(IReadOnlyCollection<string> keepIds)
    {
        await EnsureInitializedAsync();

        // Varre só linhas sincronizadas — pendentes locais nunca são removidas.
        await using var cmd = _connection!.CreateCommand();
        if (keepIds.Count == 0)
        {
            cmd.CommandText = "DELETE FROM work_orders WHERE sync_status='synced'";
        }
        else
        {
            var placeholders = string.Join(",", keepIds.Select((_, i) => $"$p{i}"));
            cmd.CommandText = $"DELETE FROM work_orders WHERE sync_status='synced' AND id NOT IN ({placeholders})";
            var index = 0;
            foreach (var id in keepIds)
            {
                cmd.Parameters.AddWithValue($"$p{index++}", id);
            }
        }

        return await cmd.ExecuteNonQueryAsync();
    }

    public async Task<string?> GetSyncCursorAsync(string entityType)
    {
        await EnsureInitializedAsync();
        await using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT cursor FROM sync_cursors WHERE entity_type=$type";
        cmd.Parameters.AddWithValue("$type", entityType);
        return await cmd.ExecuteScalarAsync() as string;
    }

    public async Task SetSyncCursorAsync(string entityType, string cursor)
    {
        await EnsureInitializedAsync();
        await using var cmd = _connection!.CreateCommand();
        cmd.CommandText = """
            INSERT INTO sync_cursors (entity_type, cursor, last_synced_at)
            VALUES ($type, $cursor, $now)
            ON CONFLICT(entity_type) DO UPDATE SET cursor=$cursor, last_synced_at=$now
            """;
        cmd.Parameters.AddWithValue("$type", entityType);
        cmd.Parameters.AddWithValue("$cursor", cursor);
        cmd.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToString("O"));
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task UpsertWorkOrderAsync(CachedWorkOrder wo, SqliteTransaction tx)
    {
        await using var cmd = _connection!.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            INSERT INTO work_orders (id, number, title, status, priority, asset_name, json, sync_status, last_modified)
            VALUES ($id, $number, $title, $status, $priority, $asset, $json, $sync, $mod)
            ON CONFLICT(id) DO UPDATE SET
                number=$number, title=$title, status=$status, priority=$priority,
                asset_name=$asset, json=$json, sync_status=$sync, last_modified=$mod
            """;
        cmd.Parameters.AddWithValue("$id", wo.Id);
        cmd.Parameters.AddWithValue("$number", wo.Number);
        cmd.Parameters.AddWithValue("$title", wo.Title);
        cmd.Parameters.AddWithValue("$status", wo.Status);
        cmd.Parameters.AddWithValue("$priority", wo.Priority);
        cmd.Parameters.AddWithValue("$asset", wo.AssetName);
        cmd.Parameters.AddWithValue("$json", wo.Json);
        cmd.Parameters.AddWithValue("$sync", wo.SyncStatus);
        cmd.Parameters.AddWithValue("$mod", wo.LastModified.ToString("O"));
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<IReadOnlyList<CachedWorkOrder>> GetWorkOrdersAsync()
    {
        await EnsureInitializedAsync();
        var list = new List<CachedWorkOrder>();
        await using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT id, number, title, status, priority, asset_name, json, sync_status, last_modified FROM work_orders ORDER BY number";
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new CachedWorkOrder
            {
                Id = reader.GetString(0),
                Number = reader.GetString(1),
                Title = reader.GetString(2),
                Status = reader.GetString(3),
                Priority = reader.GetString(4),
                AssetName = reader.GetString(5),
                Json = reader.GetString(6),
                SyncStatus = reader.GetString(7),
                LastModified = DateTime.Parse(reader.GetString(8)),
            });
        }

        return list
            .Where(wo => wo.Status is "approved" or "in_progress")
            .OrderByDescending(wo => wo.Status == "in_progress")
            .ThenBy(wo => wo.Number)
            .ToList();
    }

    public async Task UpdateWorkOrderStatusLocalAsync(string id, string status)
    {
        await EnsureInitializedAsync();
        await using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "UPDATE work_orders SET status=$status, sync_status='pending', last_modified=$mod WHERE id=$id";
        cmd.Parameters.AddWithValue("$status", status);
        cmd.Parameters.AddWithValue("$mod", DateTime.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("$id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task MarkWorkOrderSyncedAsync(string id, string status)
    {
        await EnsureInitializedAsync();
        await using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "UPDATE work_orders SET status=$status, sync_status='synced', last_modified=$mod WHERE id=$id";
        cmd.Parameters.AddWithValue("$status", status);
        cmd.Parameters.AddWithValue("$mod", DateTime.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("$id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<long> EnqueueAsync(SyncQueueRecord item)
    {
        await EnsureInitializedAsync();
        await using var cmd = _connection!.CreateCommand();
        cmd.CommandText = """
            INSERT INTO sync_queue (entity_type, entity_id, operation, payload_json, priority, retry_count, error, created_at)
            VALUES ($type, $eid, $op, $payload, $pri, $retry, $err, $created)
            """;
        cmd.Parameters.AddWithValue("$type", item.EntityType);
        cmd.Parameters.AddWithValue("$eid", item.EntityId);
        cmd.Parameters.AddWithValue("$op", item.Operation);
        cmd.Parameters.AddWithValue("$payload", item.PayloadJson);
        cmd.Parameters.AddWithValue("$pri", item.Priority);
        cmd.Parameters.AddWithValue("$retry", item.RetryCount);
        cmd.Parameters.AddWithValue("$err", (object?)item.Error ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$created", item.CreatedAt.ToString("O"));
        await cmd.ExecuteNonQueryAsync();
        return (long)(await new SqliteCommand("SELECT last_insert_rowid()", _connection).ExecuteScalarAsync() ?? 0L);
    }

    public async Task<IReadOnlyList<SyncQueueRecord>> GetPendingQueueAsync(int limit = 10)
    {
        await EnsureInitializedAsync();
        var list = new List<SyncQueueRecord>();
        await using var cmd = _connection!.CreateCommand();
        cmd.CommandText = """
            SELECT id, entity_type, entity_id, operation, payload_json, priority, retry_count, error, created_at
            FROM sync_queue ORDER BY priority ASC, created_at ASC LIMIT $limit
            """;
        cmd.Parameters.AddWithValue("$limit", limit);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new SyncQueueRecord
            {
                Id = reader.GetInt64(0),
                EntityType = reader.GetString(1),
                EntityId = reader.GetString(2),
                Operation = reader.GetString(3),
                PayloadJson = reader.GetString(4),
                Priority = reader.GetInt32(5),
                RetryCount = reader.GetInt32(6),
                Error = reader.IsDBNull(7) ? null : reader.GetString(7),
                CreatedAt = DateTime.Parse(reader.GetString(8)),
            });
        }
        return list;
    }

    public async Task RemoveQueueItemAsync(long id)
    {
        await EnsureInitializedAsync();
        await using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "DELETE FROM sync_queue WHERE id=$id";
        cmd.Parameters.AddWithValue("$id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task MarkQueueErrorAsync(long id, string error, int retryCount)
    {
        await EnsureInitializedAsync();
        await using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "UPDATE sync_queue SET error=$err, retry_count=$retry WHERE id=$id";
        cmd.Parameters.AddWithValue("$err", error.Length > 500 ? error[..500] : error);
        cmd.Parameters.AddWithValue("$retry", retryCount);
        cmd.Parameters.AddWithValue("$id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpsertNotificationsAsync(IEnumerable<CachedNotification> items)
    {
        await EnsureInitializedAsync();
        foreach (var n in items)
        {
            await using var cmd = _connection!.CreateCommand();
            cmd.CommandText = """
                INSERT INTO notifications (id, type, title, message, data_json, timestamp, read)
                VALUES ($id, $type, $title, $msg, $data, $ts, $read)
                ON CONFLICT(id) DO UPDATE SET
                    type=$type, title=$title, message=$msg, data_json=$data, timestamp=$ts, read=$read
                """;
            cmd.Parameters.AddWithValue("$id", n.Id);
            cmd.Parameters.AddWithValue("$type", n.Type);
            cmd.Parameters.AddWithValue("$title", n.Title);
            cmd.Parameters.AddWithValue("$msg", n.Message);
            cmd.Parameters.AddWithValue("$data", (object?)n.DataJson ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$ts", n.Timestamp);
            cmd.Parameters.AddWithValue("$read", n.Read ? 1 : 0);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    public async Task<IReadOnlyList<CachedNotification>> GetNotificationsAsync(int limit = 50)
    {
        await EnsureInitializedAsync();
        var list = new List<CachedNotification>();
        await using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT id, type, title, message, data_json, timestamp, read FROM notifications ORDER BY timestamp DESC LIMIT $limit";
        cmd.Parameters.AddWithValue("$limit", limit);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new CachedNotification
            {
                Id = reader.GetString(0),
                Type = reader.GetString(1),
                Title = reader.GetString(2),
                Message = reader.GetString(3),
                DataJson = reader.IsDBNull(4) ? null : reader.GetString(4),
                Timestamp = reader.GetString(5),
                Read = reader.GetInt32(6) == 1,
            });
        }
        return list;
    }

    public async Task<int> GetUnreadNotificationCountAsync()
    {
        await EnsureInitializedAsync();
        await using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM notifications WHERE read=0";
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task MarkNotificationReadLocalAsync(string id)
    {
        await EnsureInitializedAsync();
        await using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "UPDATE notifications SET read=1 WHERE id=$id";
        cmd.Parameters.AddWithValue("$id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task MarkAllNotificationsReadLocalAsync()
    {
        await EnsureInitializedAsync();
        await using var cmd = _connection!.CreateCommand();
        cmd.CommandText = "UPDATE notifications SET read=1";
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task ClearAllAsync()
    {
        await EnsureInitializedAsync();
        await ExecuteAsync("DELETE FROM work_orders; DELETE FROM sync_queue; DELETE FROM notifications; DELETE FROM sync_cursors;");
    }

    private async Task ExecuteAsync(string sql)
    {
        await using var cmd = _connection!.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }

    public void Dispose()
    {
        _connection?.Dispose();
        _lock.Dispose();
    }
}
