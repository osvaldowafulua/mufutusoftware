using System.Text.Json.Serialization;

namespace Mufutu.Desktop.Core.Api.Models;

public sealed class LoginRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public sealed class RefreshTokenRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}

public sealed class AuthUser
{
    public string Id { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
}

public sealed class TenantSummary
{
    public string Id { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}

public sealed class LoginResponse
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; init; } = string.Empty;

    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; init; } = string.Empty;

    public AuthUser? User { get; init; }
    public TenantSummary? Tenant { get; init; }
}

public sealed class PagedResponse<T>
{
    public IReadOnlyList<T> Data { get; init; } = Array.Empty<T>();
    public int Total { get; init; }
    public int Page { get; init; }
    public int Limit { get; init; }
}

public sealed class WorkOrderDto
{
    public string Id { get; init; } = string.Empty;
    public string Number { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public string? AssetName { get; init; }
}

public sealed class AssetDto
{
    public string Id { get; init; } = string.Empty;
    public string GlobalId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? SiteCode { get; init; }
}

public sealed class DashboardSummaryDto
{
    public int TotalAssets { get; init; }
    public int OpenWorkOrders { get; init; }
    public int PendingRequests { get; init; }

    public static DashboardSummaryDto FromApiJson(System.Text.Json.JsonElement root)
    {
        var totalAssets = 0;
        if (root.TryGetProperty("equipment", out var eq)
            && eq.TryGetProperty("assets", out var assets)
            && assets.TryGetProperty("total", out var ta))
        {
            totalAssets = ta.GetInt32();
        }

        var openWo = 0;
        if (root.TryGetProperty("maintenance", out var maint)
            && maint.TryGetProperty("byStatus", out var byStatus))
        {
            foreach (var prop in byStatus.EnumerateObject())
            {
                if (prop.Name is "completed" or "cancelled")
                {
                    continue;
                }

                openWo += prop.Value.GetInt32();
            }
        }

        return new DashboardSummaryDto
        {
            TotalAssets = totalAssets,
            OpenWorkOrders = openWo,
            PendingRequests = 0,
        };
    }
}
