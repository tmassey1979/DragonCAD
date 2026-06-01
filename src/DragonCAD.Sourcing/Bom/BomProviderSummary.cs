namespace DragonCAD.Sourcing.Bom;

public sealed record BomProviderSummary(
    string ProviderName,
    int SelectedLineCount,
    Money TotalEstimatedCost);
