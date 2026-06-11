namespace DragonCAD.Fabrication.Packages;

public sealed record ManufacturingPackageDiagnostic(
    ManufacturingPackageDiagnosticSeverity Severity,
    string Code,
    string Message,
    ManufacturingPackageArtifactKind? ArtifactKind,
    ManufacturingPackageHandoffTarget HandoffTarget);
