namespace DragonCAD.Sourcing.Providers;

public sealed record VendorRateLimitMetadata(
    int? RequestsPerMinute,
    int? RequestsPerDay,
    bool RequiresManualRefresh,
    string Notes);
