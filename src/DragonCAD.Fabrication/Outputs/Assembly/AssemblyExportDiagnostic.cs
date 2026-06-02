namespace DragonCAD.Fabrication.Outputs.Assembly;

public sealed record AssemblyExportDiagnostic(
    AssemblyExportDiagnosticCode Code,
    string Reference,
    string Message);
