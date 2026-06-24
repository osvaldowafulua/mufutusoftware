using Mufutu.Mobile.Core.Offline;
using Xunit;

namespace Mufutu.Mobile.Tests;

public sealed class TestDatabasePathProvider : IDatabasePathProvider
{
    private readonly string _path;

    public TestDatabasePathProvider() =>
        _path = Path.Combine(Path.GetTempPath(), $"mufutu_test_{Guid.NewGuid():N}.db");

    public string GetDatabasePath() => _path;
}

public class CampoOfflineStoreTests
{
    [Fact]
    public async Task Enqueue_and_pending_count_works()
    {
        var store = new CampoOfflineStore(new TestDatabasePathProvider());
        await store.EnsureInitializedAsync();

        Assert.Equal(0, await store.GetPendingCountAsync());

        await store.EnqueueAsync(new SyncQueueRecord
        {
            EntityType = OfflineEntityTypes.WorkOrder,
            EntityId = "wo-1",
            Operation = OfflineOperations.StatusChange,
            Priority = 1,
            PayloadJson = """{"workOrderId":"wo-1","status":"in_progress"}""",
        });

        Assert.Equal(1, await store.GetPendingCountAsync());
    }

    [Fact]
    public async Task Notifications_cache_unread_count()
    {
        var store = new CampoOfflineStore(new TestDatabasePathProvider());
        await store.EnsureInitializedAsync();

        await store.UpsertNotificationsAsync(
        [
            new CachedNotification { Id = "n1", Title = "OT", Message = "Nova", Type = "work_order", Timestamp = DateTime.UtcNow.ToString("O"), Read = false },
            new CachedNotification { Id = "n2", Title = "Stock", Message = "Baixo", Type = "stock_warning", Timestamp = DateTime.UtcNow.ToString("O"), Read = true },
        ]);

        Assert.Equal(1, await store.GetUnreadNotificationCountAsync());
        var list = await store.GetNotificationsAsync();
        Assert.Equal(2, list.Count);
    }
}
