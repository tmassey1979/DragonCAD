namespace DragonCAD.Sourcing.Catalog.Sync;

public sealed record VendorCatalogSyncRunResult(
    string ProviderName,
    string Query,
    VendorCatalogSyncRunStatus Status,
    IReadOnlyList<NormalizedCatalogListing> Listings,
    IReadOnlyList<CatalogImportDiagnostic> Diagnostics)
{
    public int ImportedCount => Listings.Count;

    public int WarningCount => Diagnostics.Count(diagnostic => diagnostic.Severity == CatalogDiagnosticSeverity.Warning);

    public int ErrorCount => Diagnostics.Count(diagnostic => diagnostic.Severity == CatalogDiagnosticSeverity.Error);

    public string Summary
    {
        get
        {
            if (Status == VendorCatalogSyncRunStatus.Blocked)
            {
                return Diagnostics.Count == 0
                    ? $"{ProviderName} catalog sync is blocked."
                    : Diagnostics[0].Message;
            }

            string candidateLabel = ImportedCount == 1 ? "catalog candidate" : "catalog candidates";
            string diagnosticLabel = Diagnostics.Count == 1 ? "diagnostic" : "diagnostics";
            return $"{ImportedCount:N0} {candidateLabel} from {ProviderName} for '{Query}' with {Diagnostics.Count:N0} {diagnosticLabel}.";
        }
    }
}
