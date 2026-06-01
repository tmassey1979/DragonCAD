namespace DragonCAD.Sourcing.Bom;

public sealed record BomCostRollupDiagnostic(
    BomCostRollupDiagnosticCode Code,
    string Reference,
    string ManufacturerPartNumber,
    int RequiredQuantity,
    string Message);
