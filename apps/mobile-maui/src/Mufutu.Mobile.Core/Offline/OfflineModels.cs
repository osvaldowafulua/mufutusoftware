using System.Text.Json;
using Mufutu.Mobile.Core.Models;

namespace Mufutu.Mobile.Core.Offline;

public sealed class SyncQueueRecord
{
    public long Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public int Priority { get; set; } = 2;
    public int RetryCount { get; set; }
    public string? Error { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class CachedWorkOrder
{
    public string Id { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string Json { get; set; } = string.Empty;
    public string SyncStatus { get; set; } = "synced";
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}

public sealed class CachedNotification
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? DataJson { get; set; }
    public string Timestamp { get; set; } = string.Empty;
    public bool Read { get; set; }
}

public static class OfflineEntityTypes
{
    public const string WorkOrder = "workOrder";
    public const string MaintenanceRequest = "maintenanceRequest";
}

public static class OfflineOperations
{
    public const string StatusChange = "statusChange";
    public const string Create = "create";
}

/// <summary>
/// Resultado de um pull para o armazenamento local: quantas linhas entraram,
/// mudaram, já estavam iguais (não reescritas) ou foram preservadas por terem
/// alterações locais pendentes.
/// </summary>
public sealed class PullStats
{
    public int Added { get; set; }
    public int Updated { get; set; }
    public int Unchanged { get; set; }
    public int PreservedLocal { get; set; }
    public int Removed { get; set; }
}
