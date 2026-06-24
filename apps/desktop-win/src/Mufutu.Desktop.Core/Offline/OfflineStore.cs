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
            """;
        cmd.ExecuteNonQuery();
    }
}

public sealed record SyncQueueItem(long Id, string Entity, string PayloadJson, string CreatedAt);
