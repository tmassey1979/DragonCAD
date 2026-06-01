namespace DragonCAD.Sourcing.Catalog.SparkFun;

public sealed record SparkFunSourceManifestDiagnostic(
    CatalogDiagnosticSeverity Severity,
    string Code,
    string Message,
    string ProviderName,
    string SourceId);
