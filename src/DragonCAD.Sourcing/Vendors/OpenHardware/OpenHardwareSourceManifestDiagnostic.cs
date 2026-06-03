using DragonCAD.Sourcing.Catalog;

namespace DragonCAD.Sourcing.Vendors.OpenHardware;

public sealed record OpenHardwareSourceManifestDiagnostic(
    CatalogDiagnosticSeverity Severity,
    string Code,
    string Message,
    string ProviderName,
    string SourceId);
