namespace DragonCAD.Import.Eagle.Assembly;

public sealed record EagleImportAssemblyDiagnostic(
    string Code,
    EagleImportAssemblyDiagnosticSeverity Severity,
    string Message);
