namespace DragonCAD.Sourcing.BomPlanning;

public sealed record BomPlanningResult(
    DateTimeOffset CreatedAt,
    string CurrencyCode,
    IReadOnlyList<BomPlanningGroup> Groups,
    IReadOnlyList<BomPlanningScenarioResult> Scenarios,
    IReadOnlyList<BomPlanningDiagnostic> Diagnostics)
{
    public bool IsComplete => Diagnostics.Count == 0;
}
