using System.Text.Json.Serialization;

namespace Mufutu.Mobile.Core.Models;

public sealed class LoginRequest
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

public sealed class LoginResponse
{
    [JsonPropertyName("accessToken")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("refreshToken")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("user")]
    public UserDto? User { get; set; }
}

public sealed class UserDto
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }
}

public sealed class WorkOrderDto
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("number")]
    public string? Number { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("priority")]
    public string? Priority { get; set; }

    [JsonPropertyName("asset")]
    public AssetRefDto? Asset { get; set; }
}

public sealed class AssetRefDto
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }
}

public sealed class PagedWorkOrdersResponse
{
    [JsonPropertyName("data")]
    public List<WorkOrderDto>? Data { get; set; }

    [JsonPropertyName("workOrders")]
    public List<WorkOrderDto>? WorkOrders { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    public IEnumerable<WorkOrderDto> Items => Data ?? WorkOrders ?? [];
}

public sealed class MaintenancePhotoDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("name")]
    public string Name { get; set; } = "avaria.jpg";

    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; } = "image/jpeg";

    [JsonPropertyName("dataUrl")]
    public string DataUrl { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("O");
}

public sealed class NotificationDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public Dictionary<string, object>? Data { get; set; }

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    [JsonPropertyName("read")]
    public bool Read { get; set; }
}

public sealed class NotificationsPageResponse
{
    [JsonPropertyName("data")]
    public List<NotificationDto>? Data { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("unreadCount")]
    public int UnreadCount { get; set; }
}

public sealed class RefreshTokenResponse
{
    [JsonPropertyName("accessToken")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("refreshToken")]
    public string? RefreshToken { get; set; }
}

public sealed class CreateMaintenanceRequestPayload
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("symptom")]
    public string Symptom { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    public string Priority { get; set; } = "medium";

    [JsonPropertyName("assetId")]
    public string? AssetId { get; set; }

    [JsonPropertyName("photos")]
    public List<MaintenancePhotoDto>? Photos { get; set; }
}
