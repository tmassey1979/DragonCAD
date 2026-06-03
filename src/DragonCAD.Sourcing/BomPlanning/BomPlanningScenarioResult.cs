namespace DragonCAD.Sourcing.BomPlanning;

public sealed record BomPlanningScenarioResult(
    string Name,
    int BuildQuantity,
    Money TotalCost,
    IReadOnlyList<BomPlanningPurchaseLine> PurchaseLines);
