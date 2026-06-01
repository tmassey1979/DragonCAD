using DragonCAD.App.Fabrication.Handoff;

namespace DragonCAD.App.Fabrication.Export;

public sealed record FabricationManufacturingPackageActionSummary(
    string Status,
    string ActionLabel,
    string ActionTarget,
    bool CanRunAction,
    int ReadyFileCount,
    int MissingFileCount,
    string SummaryText)
{
    public static FabricationManufacturingPackageActionSummary FromPlan(
        FabricationHandoffActionPlan plan,
        IEnumerable<FabricationChecklistRow> rows)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(rows);

        FabricationChecklistRow[] rowArray = rows.ToArray();
        int missingFileCount = rowArray.Count(row => string.Equals(row.Status, "Missing", StringComparison.Ordinal));
        int readyFileCount = rowArray.Length - missingFileCount;
        string status = plan.IsReady ? "Ready" : "Blocked";
        string actionLabel = plan.Action?.Label ?? "Resolve missing files";
        string actionTarget = plan.Action?.Target ?? string.Empty;
        string summaryText = plan.IsReady
            ? $"Ready to hand off {readyFileCount} {PluralizeFile(readyFileCount)}. {actionLabel}."
            : $"Blocked by {missingFileCount} missing {PluralizeFile(missingFileCount)}. {actionLabel}.";

        return new FabricationManufacturingPackageActionSummary(
            status,
            actionLabel,
            actionTarget,
            plan.Action is not null,
            readyFileCount,
            missingFileCount,
            summaryText);
    }

    private static string PluralizeFile(int count) => count == 1 ? "file" : "files";
}
