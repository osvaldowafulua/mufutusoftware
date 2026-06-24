namespace Mufutu.Desktop.Licensing;

public static class LicenseConstants
{
    public const string Prefix = "MUFUTU-LIC-";
}

public enum LicenseType
{
    Trial,
    Subscription,
    Perpetual,
}

public sealed class LicenseClaims
{
    public int V { get; init; }
    public string Kid { get; init; } = string.Empty;
    public string TenantId { get; init; } = string.Empty;
    public string TenantSlug { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string PlanId { get; init; } = string.Empty;
    public string PlanLabel { get; init; } = string.Empty;
    public string? ValidUntil { get; init; }
    public IReadOnlyList<string> Modules { get; init; } = Array.Empty<string>();
    public int? MaxUsers { get; init; }
    public int? MaxSites { get; init; }
    public decimal? MaintenanceFeeAoa { get; init; }
    public string? MaintenanceDueAt { get; init; }
    public string IssuedAt { get; init; } = string.Empty;
    public string IssuedBy { get; init; } = string.Empty;
    public string Nonce { get; init; } = string.Empty;
}

public sealed class PublicKeyRecord
{
    public string Kid { get; init; } = string.Empty;
    public string PublicKey { get; init; } = string.Empty;
}
