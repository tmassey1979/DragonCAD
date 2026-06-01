using DragonCAD.App.Datasheets;

namespace DragonCAD.App.Datasheets.Promotion.Commands;

public sealed class DatasheetPromotionQueueCommands
{
    public DatasheetReviewRow? SelectedRow { get; set; }

    public string TargetLibraryId { get; set; } = "";

    public string ReviewNote { get; set; } = "";

    public string StatusMessage { get; private set; } = "";

    public DatasheetPromotionCommandResult ApproveSelectedForPromotion()
    {
        if (SelectedRow is null)
        {
            return SetStatus(DatasheetPromotionCommandResult.Failed("Select a datasheet review row."));
        }

        string normalizedTargetLibraryId = Normalize(TargetLibraryId);
        if (string.IsNullOrWhiteSpace(normalizedTargetLibraryId))
        {
            return SetStatus(DatasheetPromotionCommandResult.Failed("Target library is required before datasheet promotion."));
        }

        if (!SelectedRow.Approve())
        {
            DatasheetPromotionPlan blockedPlan = DatasheetPromotionPlanner.CreatePlan(
                SelectedRow,
                normalizedTargetLibraryId,
                ReviewNote);

            return SetStatus(DatasheetPromotionCommandResult.Failed(
                FormatBlockedStatus(blockedPlan),
                blockedPlan));
        }

        DatasheetPromotionPlan plan = DatasheetPromotionPlanner.CreatePlan(
            SelectedRow,
            normalizedTargetLibraryId,
            ReviewNote);

        return SetStatus(plan.CanPromote
            ? DatasheetPromotionCommandResult.Passed(
                $"{SelectedRow.ComponentName} approved for promotion into {plan.TargetLibraryId}.",
                plan)
            : DatasheetPromotionCommandResult.Failed(FormatBlockedStatus(plan), plan));
    }

    public DatasheetPromotionCommandResult RejectSelected(string reason)
    {
        if (SelectedRow is null)
        {
            return SetStatus(DatasheetPromotionCommandResult.Failed("Select a datasheet review row."));
        }

        SelectedRow.Reject(reason);
        string rejectReason = SelectedRow.RejectReason.TrimEnd('.');
        return SetStatus(DatasheetPromotionCommandResult.Passed(
            $"{SelectedRow.ComponentName} rejected: {rejectReason}.",
            promotionPlan: null));
    }

    public DatasheetPromotionCommandResult CreatePromotionPlanForSelected()
    {
        if (SelectedRow is null)
        {
            return SetStatus(DatasheetPromotionCommandResult.Failed("Select a datasheet review row."));
        }

        DatasheetPromotionPlan plan = DatasheetPromotionPlanner.CreatePlan(
            SelectedRow,
            TargetLibraryId,
            ReviewNote);

        return SetStatus(plan.CanPromote
            ? DatasheetPromotionCommandResult.Passed(plan.Summary, plan)
            : DatasheetPromotionCommandResult.Failed(FormatBlockedStatus(plan), plan));
    }

    private DatasheetPromotionCommandResult SetStatus(DatasheetPromotionCommandResult result)
    {
        StatusMessage = result.StatusMessage;
        return result;
    }

    private static string FormatBlockedStatus(DatasheetPromotionPlan plan)
    {
        string details = plan.Diagnostics.Count == 0
            ? "No promotion diagnostics were provided."
            : string.Join("; ", plan.Diagnostics.Select(diagnostic => diagnostic.Message));

        return $"Promotion blocked for {plan.ComponentName}: {details}";
    }

    private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
}

public sealed record DatasheetPromotionCommandResult(
    bool Succeeded,
    string StatusMessage,
    DatasheetPromotionPlan? PromotionPlan)
{
    public static DatasheetPromotionCommandResult Passed(
        string statusMessage,
        DatasheetPromotionPlan? promotionPlan) =>
        new(Succeeded: true, statusMessage, promotionPlan);

    public static DatasheetPromotionCommandResult Failed(
        string statusMessage,
        DatasheetPromotionPlan? promotionPlan = null) =>
        new(Succeeded: false, statusMessage, promotionPlan);
}
