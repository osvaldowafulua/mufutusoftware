using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using Mufutu.Desktop.Core.Crypto;

namespace Mufutu.Desktop.Core.Offline;

public sealed class OfflineStore : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AesGcmCipher _cipher = new();
    private readonly byte[] _key;

    public OfflineStore(string dbPath, byte[] encryptionKey)
    {
        _key = encryptionKey;
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        _connection = new SqliteConnection($"Data Source={dbPath}");
        _connection.Open();
        Initialize();
    }

    public async Task EnqueueSyncAsync(string entity, string payloadJson, CancellationToken ct = default)
    {
        var encrypted = _cipher.EncryptToBase64(payloadJson, _key);
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            INSERT INTO sync_queue (entity, payload_enc, created_at, priority)
            VALUES ($entity, $payload, $created, $priority);
            """;
        cmd.Parameters.AddWithValue("$entity", entity);
        cmd.Parameters.AddWithValue("$payload", encrypted);
        cmd.Parameters.AddWithValue("$created", DateTimeOffset.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("$priority", entity == "work_order" ? 1 : 0);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<SyncQueueItem>> PeekQueueAsync(int limit = 50, CancellationToken ct = default)
    {
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT id, entity, payload_enc, created_at
            FROM sync_queue
            ORDER BY priority DESC, id ASC
            LIMIT $limit;
            """;
        cmd.Parameters.AddWithValue("$limit", limit);

        var items = new List<SyncQueueItem>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var encrypted = reader.GetString(2);
            var json = _cipher.DecryptFromBase64(encrypted, _key);
            items.Add(new SyncQueueItem(
                reader.GetInt64(0),
                reader.GetString(1),
                json,
                reader.GetString(3)));
        }

        return items;
    }

    /// <summary>
    /// Armazenamento geral local: guarda entidades (JSON encriptado em repouso)
    /// e só reescreve as que mudaram — comparação por hash do conteúdo, porque
    /// o AES-GCM produz ciphertext diferente a cada escrita.
    /// </summary>
    public async Task<EntityPullStats> UpsertEntitiesAsync(
        string type,
        IReadOnlyCollection<(string Id, string Json)> items,
        CancellationToken ct = default)
    {
        var stats = new EntityPullStats();
        await using var tx = (SqliteTransaction)await _connection.BeginTransactionAsync(ct);

        foreach (var (id, json) in items)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));

            string? existingHash = null;
            await using (var check = _connection.CreateCommand())
            {
                check.Transaction = tx;
                check.CommandText = "SELECT content_hash FROM entities WHERE type=$type AND id=$id";
                check.Parameters.AddWithValue("$type", type);
                check.Parameters.AddWithValue("$id", id);
                existingHash = await check.ExecuteScalarAsync(ct) as string;
            }

            if (existingHash == hash)
            {
                stats.Unchanged++;
                continue;
            }

            await using var cmd = _connection.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText =
                """
                INSERT INTO entities (type, id, json_enc, content_hash, updated_at)
                VALUES ($type, $id, $json, $hash, $now)
                ON CONFLICT(type, id) DO UPDATE SET
                    json_enc=$json, content_hash=$hash, updated_at=$now
                """;
            cmd.Parameters.AddWithValue("$type", type);
            cmd.Parameters.AddWithValue("$id", id);
            cmd.Parameters.AddWithValue("$json", _cipher.EncryptToBase64(json, _key));
            cmd.Parameters.AddWithValue("$hash", hash);
            cmd.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToString("O"));
            await cmd.ExecuteNonQueryAsync(ct);

            if (existingHash == null)
            {
                stats.Added++;
            }
            else
            {
                stats.Updated++;
            }
        }

        await tx.CommitAsync(ct);
        return stats;
    }

    public async Task<IReadOnlyList<string>> GetEntitiesAsync(string type, CancellationToken ct = default)
    {
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT json_enc FROM entities WHERE type=$type ORDER BY id";
        cmd.Parameters.AddWithValue("$type", type);

        var items = new List<string>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            items.Add(_cipher.DecryptFromBase64(reader.GetString(0), _key));
        }

        return items;
    }

    public async Task<int> PruneEntitiesNotInAsync(
        string type,
        IReadOnlyCollection<string> keepIds,
        CancellationToken ct = default)
    {
        await using var cmd = _connection.CreateCommand();
        if (keepIds.Count == 0)
        {
            cmd.CommandText = "DELETE FROM entities WHERE type=$type";
            cmd.Parameters.AddWithValue("$type", type);
        }
        else
        {
            var placeholders = string.Join(",", keepIds.Select((_, i) => $"$p{i}"));
            cmd.CommandText = $"DELETE FROM entities WHERE type=$type AND id NOT IN ({placeholders})";
            cmd.Parameters.AddWithValue("$type", type);
            var index = 0;
            foreach (var id in keepIds)
            {
                cmd.Parameters.AddWithValue($"$p{index++}", id);
            }
        }

        return await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<string?> GetSyncCursorAsync(string entityType, CancellationToken ct = default)
    {
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT cursor FROM sync_cursors WHERE entity_type=$type";
        cmd.Parameters.AddWithValue("$type", entityType);
        return await cmd.ExecuteScalarAsync(ct) as string;
    }

    public async Task SetSyncCursorAsync(string entityType, string cursor, CancellationToken ct = default)
    {
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            INSERT INTO sync_cursors (entity_type, cursor, last_synced_at)
            VALUES ($type, $cursor, $now)
            ON CONFLICT(entity_type) DO UPDATE SET cursor=$cursor, last_synced_at=$now
            """;
        cmd.Parameters.AddWithValue("$type", entityType);
        cmd.Parameters.AddWithValue("$cursor", cursor);
        cmd.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToString("O"));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    private void Initialize()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            CREATE TABLE IF NOT EXISTS sync_queue (
              id INTEGER PRIMARY KEY AUTOINCREMENT,
              entity TEXT NOT NULL,
              payload_enc TEXT NOT NULL,
              created_at TEXT NOT NULL,
              priority INTEGER NOT NULL DEFAULT 0
            );
            CREATE TABLE IF NOT EXISTS entities (
              type TEXT NOT NULL,
              id TEXT NOT NULL,
              json_enc TEXT NOT NULL,
              content_hash TEXT NOT NULL,
              updated_at TEXT NOT NULL,
              PRIMARY KEY (type, id)
            );
            CREATE TABLE IF NOT EXISTS sync_cursors (
              entity_type TEXT PRIMARY KEY,
              cursor TEXT NOT NULL,
              last_synced_at TEXT NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();
    }
}

public sealed record SyncQueueItem(long Id, string Entity, string PayloadJson, string CreatedAt);

/// <summary>Resultado de um pull para o armazenamento local.</summary>
public sealed class EntityPullStats
{
    public int Added { get; set; }
    public int Updated { get; set; }
    public int Unchanged { get; set; }
    public int Removed { get; set; }
}
