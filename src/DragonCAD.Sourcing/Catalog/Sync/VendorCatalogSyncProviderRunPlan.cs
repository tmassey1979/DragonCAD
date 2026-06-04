namespace DragonCAD.Sourcing.Catalog.Sync;

public sealed record VendorCatalogSyncProviderRunPlan(
    string ProviderId,
    string DisplayName,
    VendorCatalogSyncCredentialReadiness CredentialReadiness,
    string RateLimitNotes,
    TimeSpan FreshnessWindow,
    IReadOnlyList<string> RequestedSearchTerms,
    CatalogProviderCapabilities Capabilities,
    IReadOnlyList<VendorCatalogSyncPlanDiagnostic> Blockers,
    IReadOnlyList<VendorCatalogSyncPlanDiagnostic> Warnings)
{
    public bool IsBlocked => Blockers.Count > 0;
}
