namespace DragonCAD.Fabrication.Outputs.Assembly;

public sealed record AssemblyExportPackage(
    string BomCsv,
    string PickAndPlaceCsv,
    IReadOnlyList<AssemblyExportDiagnostic> Diagnostics);
