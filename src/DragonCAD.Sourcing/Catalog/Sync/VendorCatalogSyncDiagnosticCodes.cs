namespace DragonCAD.Sourcing.Catalog.Sync;

public static class VendorCatalogSyncDiagnosticCodes
{
    public const string ProviderUnavailable = "vendor-sync.provider-unavailable";

    public const string MissingQuery = "vendor-sync.missing-query";

    public const string MissingCredential = "vendor-sync.missing-credential";

    public const string ManualFeedRequired = "vendor-sync.manual-feed-required";

    public const string StaleCache = "vendor-sync.stale-cache";

    public const string UnsupportedCapability = "vendor-sync.unsupported-capability";
}
