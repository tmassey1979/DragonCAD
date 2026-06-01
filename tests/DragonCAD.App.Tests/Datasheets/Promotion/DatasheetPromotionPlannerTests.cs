using DragonCAD.App.Datasheets;
using DragonCAD.App.Datasheets.Promotion;

namespace DragonCAD.App.Tests.Datasheets.Promotion;

public sealed class DatasheetPromotionPlannerTests
{
    [Fact]
    public void ApprovedRowCreatesPendingPromotionPlanWithoutMutatingLibrary()
    {
        DatasheetReviewRow row = Row(componentName: "LM7805");
        Assert.True(row.Approve());

        DatasheetPromotionPlan plan = DatasheetPromotionPlanner.CreatePlan(
            row,
            targetLibraryId: "core-analog",
            reviewNote: "Verified TO-220 package against datasheet.");

        Assert.True(plan.CanPromote);
        Assert.Equal(DatasheetPromotionPlanState.PendingLibraryPromotion, plan.State);
        Assert.Equal("LM7805", plan.ComponentName);
        Assert.Equal("core-analog", plan.TargetLibraryId);
        Assert.Equal("Verified TO-220 package against datasheet.", plan.ReviewNote);
        Assert.Equal("Promotion pending for LM7805 into core-analog.", plan.Summary);
        Assert.Empty(plan.Diagnostics);
        Assert.False(plan.MutatesLibrary);
    }

    [Theory]
    [InlineData(DatasheetReviewState.Pending, "DATASHEET_PROMOTION_REVIEW_NOT_APPROVED")]
    [InlineData(DatasheetReviewState.Rejected, "DATASHEET_PROMOTION_REVIEW_REJECTED")]
    public void PendingAndRejectedRowsAreBlocked(DatasheetReviewState reviewState, string expectedDiagnosticCode)
    {
        DatasheetReviewRow row = Row();
        if (reviewState == DatasheetReviewState.Rejected)
        {
            row.Reject("Wrong package.");
        }

        DatasheetPromotionPlan plan = DatasheetPromotionPlanner.CreatePlan(
            row,
            targetLibraryId: "core-mixed-signal",
            reviewNote: "Needs approval.");

        Assert.False(plan.CanPromote);
        Assert.Equal(DatasheetPromotionPlanState.Blocked, plan.State);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == expectedDiagnosticCode);
    }

    [Fact]
    public void CriticalWarningsBlockPromotion()
    {
        DatasheetReviewRow row = Row(
            warnings:
            [
                new DatasheetReviewWarning(DatasheetReviewWarningSeverity.Critical, "Pin count mismatch")
            ]);

        DatasheetPromotionPlan plan = DatasheetPromotionPlanner.CreatePlan(
            row,
            targetLibraryId: "core-mixed-signal",
            reviewNote: "Critical mismatch must be resolved.");

        Assert.False(plan.CanPromote);
        Assert.Equal(DatasheetPromotionPlanState.Blocked, plan.State);
        Assert.Contains(
            plan.Diagnostics,
            diagnostic => diagnostic.Code == "DATASHEET_PROMOTION_CRITICAL_WARNING"
                && diagnostic.Message == "Pin count mismatch");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TargetLibraryIdIsRequired(string targetLibraryId)
    {
        DatasheetReviewRow row = Row();
        Assert.True(row.Approve());

        DatasheetPromotionPlan plan = DatasheetPromotionPlanner.CreatePlan(
            row,
            targetLibraryId,
            reviewNote: "Verified.");

        Assert.False(plan.CanPromote);
        Assert.Equal(DatasheetPromotionPlanState.Blocked, plan.State);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "DATASHEET_PROMOTION_TARGET_LIBRARY_REQUIRED");
    }

    [Fact]
    public void GeneratedAssetStatusesAreSummarized()
    {
        DatasheetReviewRow row = Row(
            symbolStatus: DatasheetProposalStatus.Ready,
            footprintStatus: DatasheetProposalStatus.NeedsReview,
            threeDimensionalModelStatus: DatasheetProposalStatus.Placeholder);
        Assert.True(row.Approve());

        DatasheetPromotionPlan plan = DatasheetPromotionPlanner.CreatePlan(
            row,
            targetLibraryId: "core-regulators",
            reviewNote: "Use placeholder 3D body until STEP model is available.");

        Assert.Equal(
            ["Symbol: ready", "Footprint: needs review", "3D model: placeholder"],
            plan.AssetStatusSummaries);
    }

    private static DatasheetReviewRow Row(
        string componentName = "NE555",
        DatasheetProposalStatus symbolStatus = DatasheetProposalStatus.Ready,
        DatasheetProposalStatus footprintStatus = DatasheetProposalStatus.Ready,
        DatasheetProposalStatus threeDimensionalModelStatus = DatasheetProposalStatus.Ready,
        IReadOnlyList<DatasheetReviewWarning>? warnings = null) =>
        new(
            componentName: componentName,
            datasheetSource: $"https://datasheets.example/{componentName}.pdf",
            extractedPinCount: 8,
            symbolStatus: symbolStatus,
            footprintStatus: footprintStatus,
            threeDimensionalModelStatus: threeDimensionalModelStatus,
            confidence: DatasheetReviewConfidence.High,
            warnings: warnings ?? []);
}
