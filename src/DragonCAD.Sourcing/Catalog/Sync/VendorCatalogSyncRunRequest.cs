namespace DragonCAD.Sourcing.Catalog.Sync;

public sealed record VendorCatalogSyncRunRequest(
    IReadOnlyList<string> RequestedSearchTerms,
    DateTimeOffset PlannedAtUtc,
    TimeSpan FreshnessWindow,
    IReadOnlyDictionary<string, IReadOnlySet<string>> CredentialKeysByProviderId,
    IReadOnlyDictionary<string, DateTimeOffset> CacheRetrievedAtUtcByProviderId,
    CatalogProviderCapabilities RequiredCapabilities);
