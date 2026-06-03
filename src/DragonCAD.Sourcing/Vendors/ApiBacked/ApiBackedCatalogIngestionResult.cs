using DragonCAD.Sourcing.Catalog;

namespace DragonCAD.Sourcing.Vendors.ApiBacked;

public sealed record ApiBackedCatalogIngestionResult(
    IReadOnlyList<VendorCatalogItem> Records,
    IReadOnlyList<CatalogImportDiagnostic> Diagnostics);
