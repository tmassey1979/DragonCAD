namespace DragonCAD.Sourcing.Catalog;

public sealed record CatalogImportDiagnostic(
    CatalogDiagnosticSeverity Severity,
    string Code,
    string Message,
    string ProviderName,
    string? VendorSku = null);
