using DragonCAD.Fabrication.Outputs;

namespace DragonCAD.Fabrication.Ordering;

public sealed record FabricationPackageDiagnostic(
    FabricationPackageDiagnosticSeverity Severity,
    string Code,
    string Message,
    ManufacturingFileRole? FileRole);
