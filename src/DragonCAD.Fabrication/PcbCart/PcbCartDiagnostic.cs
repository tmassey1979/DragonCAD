namespace DragonCAD.Fabrication.PcbCart;

public sealed record PcbCartDiagnostic(
    PcbCartDiagnosticSeverity Severity,
    string Code,
    string Message);
