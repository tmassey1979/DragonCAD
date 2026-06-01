namespace DragonCAD.Sourcing.Catalog;

public sealed record CatalogImportResult(
    IReadOnlyList<NormalizedCatalogListing> Listings,
    IReadOnlyList<CatalogImportDiagnostic> Diagnostics);
