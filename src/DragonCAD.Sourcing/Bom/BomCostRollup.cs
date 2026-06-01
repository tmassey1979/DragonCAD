namespace DragonCAD.Sourcing.Bom;

public sealed record BomCostRollup(
    Money TotalEstimatedCost,
    IReadOnlyList<BomCostRollupLine> Lines,
    IReadOnlyList<BomCostRollupDiagnostic> Diagnostics,
    IReadOnlyList<BomProviderSummary> ProviderSummaries)
{
    public bool IsComplete => Diagnostics.Count == 0;
}
