namespace DragonCAD.Sourcing.Catalog.Smoke;

public sealed record VendorLiveSmokeRunResult(
    string ProviderName,
    VendorLiveSmokeRunStatus Status,
    int ListingCount,
    IReadOnlyList<CatalogImportDiagnostic> Diagnostics)
{
    public static VendorLiveSmokeRunResult Disabled(string providerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);

        return new VendorLiveSmokeRunResult(
            providerName,
            VendorLiveSmokeRunStatus.Disabled,
            0,
            []);
    }

    public static VendorLiveSmokeRunResult Failed(
        string providerName,
        IReadOnlyList<CatalogImportDiagnostic> diagnostics)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);
        ArgumentNullException.ThrowIfNull(diagnostics);

        return new VendorLiveSmokeRunResult(
            providerName,
            VendorLiveSmokeRunStatus.Failed,
            0,
            diagnostics);
    }

    public static VendorLiveSmokeRunResult FromCatalogResult(
        string providerName,
        CatalogImportResult catalogResult)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);
        ArgumentNullException.ThrowIfNull(catalogResult);

        var status = catalogResult.Diagnostics.Any(diagnostic => diagnostic.Severity == CatalogDiagnosticSeverity.Error)
            ? VendorLiveSmokeRunStatus.Failed
            : VendorLiveSmokeRunStatus.Succeeded;

        return new VendorLiveSmokeRunResult(
            providerName,
            status,
            catalogResult.Listings.Count,
            catalogResult.Diagnostics);
    }
}
