using DragonCAD.App.Datasheets;

namespace DragonCAD.App.Datasheets.Promotion;

public static class DatasheetPromotionPlanner
{
    public static DatasheetPromotionPlan CreatePlan(
        DatasheetReviewRow row,
        string targetLibraryId,
        string reviewNote)
    {
        ArgumentNullException.ThrowIfNull(row);

        string normalizedTargetLibraryId = targetLibraryId.Trim();
        string normalizedReviewNote = string.IsNullOrWhiteSpace(reviewNote) ? "" : reviewNote.Trim();
        IReadOnlyList<DatasheetPromotionDiagnostic> diagnostics = CreateDiagnostics(row, normalizedTargetLibraryId);

        DatasheetPromotionPlanState state = diagnostics.Count == 0
            ? DatasheetPromotionPlanState.PendingLibraryPromotion
            : DatasheetPromotionPlanState.Blocked;

        return new DatasheetPromotionPlan(
            ComponentName: row.ComponentName,
            DatasheetSource: row.DatasheetSource,
            TargetLibraryId: normalizedTargetLibraryId,
            ReviewNote: normalizedReviewNote,
            State: state,
            MutatesLibrary: false,
            AssetStatusSummaries:
            [
                FormatAssetStatus("Symbol", row.SymbolStatus),
                FormatAssetStatus("Footprint", row.FootprintStatus),
                FormatAssetStatus("3D model", row.ThreeDimensionalModelStatus)
            ],
            Diagnostics: diagnostics);
    }

    private static IReadOnlyList<DatasheetPromotionDiagnostic> CreateDiagnostics(
        DatasheetReviewRow row,
        string normalizedTargetLibraryId)
    {
        List<DatasheetPromotionDiagnostic> diagnostics = [];

        if (string.IsNullOrWhiteSpace(normalizedTargetLibraryId))
        {
            diagnostics.Add(new DatasheetPromotionDiagnostic(
                "DATASHEET_PROMOTION_TARGET_LIBRARY_REQUIRED",
                "Target library id is required."));
        }

        switch (row.ReviewState)
        {
            case DatasheetReviewState.Promoted:
                break;
            case DatasheetReviewState.Rejected:
                diagnostics.Add(new DatasheetPromotionDiagnostic(
                    "DATASHEET_PROMOTION_REVIEW_REJECTED",
                    "Rejected datasheet reviews cannot be promoted."));
                break;
            case DatasheetReviewState.Pending:
                diagnostics.Add(new DatasheetPromotionDiagnostic(
                    "DATASHEET_PROMOTION_REVIEW_NOT_APPROVED",
                    "Datasheet review must be approved before promotion."));
                break;
            default:
                throw new InvalidOperationException($"Unsupported datasheet review state {row.ReviewState}.");
        }

        foreach (DatasheetReviewWarning warning in row.Warnings.Where(warning => warning.Severity == DatasheetReviewWarningSeverity.Critical))
        {
            diagnostics.Add(new DatasheetPromotionDiagnostic(
                "DATASHEET_PROMOTION_CRITICAL_WARNING",
                warning.Message));
        }

        return diagnostics;
    }

    private static string FormatAssetStatus(string assetName, DatasheetProposalStatus status) =>
        status switch
        {
            DatasheetProposalStatus.Ready => $"{assetName}: ready",
            DatasheetProposalStatus.NeedsReview => $"{assetName}: needs review",
            DatasheetProposalStatus.Placeholder => $"{assetName}: placeholder",
            DatasheetProposalStatus.Missing => $"{assetName}: missing",
            _ => throw new InvalidOperationException($"Unsupported datasheet proposal status {status}.")
        };
}

public sealed record DatasheetPromotionPlan(
    string ComponentName,
    string DatasheetSource,
    string TargetLibraryId,
    string ReviewNote,
    DatasheetPromotionPlanState State,
    bool MutatesLibrary,
    IReadOnlyList<string> AssetStatusSummaries,
    IReadOnlyList<DatasheetPromotionDiagnostic> Diagnostics)
{
    public bool CanPromote => State == DatasheetPromotionPlanState.PendingLibraryPromotion && Diagnostics.Count == 0;

    public string Summary => CanPromote
        ? $"Promotion pending for {ComponentName} into {TargetLibraryId}."
        : $"Promotion blocked for {ComponentName}.";
}

public sealed record DatasheetPromotionDiagnostic(string Code, string Message);

public enum DatasheetPromotionPlanState
{
    Blocked,
    PendingLibraryPromotion,
}
