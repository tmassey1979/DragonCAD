namespace DragonCAD.Sourcing.Catalog.Sync;

public sealed record VendorCatalogSyncRunPlan(
    DateTimeOffset PlannedAtUtc,
    TimeSpan FreshnessWindow,
    IReadOnlyList<VendorCatalogSyncProviderRunPlan> Providers);
