namespace DragonCAD.Sourcing;

public sealed record BomRunCostEstimate(
    int BuildQuantity,
    Money TotalEstimatedCost,
    IReadOnlyList<BomCostEstimateLine> Lines,
    IReadOnlyList<MissingQuoteDiagnostic> MissingQuoteDiagnostics)
{
    public bool IsComplete => MissingQuoteDiagnostics.Count == 0;
}
