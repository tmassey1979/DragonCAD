namespace DragonCAD.Sourcing.BomPlanning;

public sealed record BomPlanningDiagnostic(
    string Code,
    string ScenarioName,
    string GroupKey,
    string Message,
    int UnfilledQuantity)
{
    public static BomPlanningDiagnostic MissingStock(string scenarioName, BomPlanningGroup group, int unfilledQuantity)
    {
        return new BomPlanningDiagnostic(
            "MissingStock",
            scenarioName,
            group.GroupKey,
            $"Unable to source {unfilledQuantity} units for {group.GroupKey}.",
            unfilledQuantity);
    }
}
