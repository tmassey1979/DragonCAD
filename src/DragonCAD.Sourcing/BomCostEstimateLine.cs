namespace DragonCAD.Sourcing;

public sealed record BomCostEstimateLine(
    SourcingBomLine BomLine,
    int RequiredQuantity,
    EvaluatedVendorQuote SelectedQuote);
