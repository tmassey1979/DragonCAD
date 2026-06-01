using DragonCAD.App.Datasheets;
using DragonCAD.App.Datasheets.Promotion;
using DragonCAD.App.Datasheets.Promotion.Commands;

namespace DragonCAD.App.Tests.Datasheets.Promotion.Commands;

public sealed class DatasheetPromotionQueueCommandsTests
{
    [Fact]
    public void ApproveSelectedRowCreatesPromotionPlan()
    {
        DatasheetReviewRow row = Row(componentName: "LM7805");
        DatasheetPromotionQueueCommands commands = new()
        {
            SelectedRow = row,
            TargetLibraryId = "core-regulators",
            ReviewNote = "Verified symbol, TO-220 footprint, and regulator pinout."
        };

        DatasheetPromotionCommandResult result = commands.ApproveSelectedForPromotion();

        Assert.True(result.Succeeded);
        Assert.Equal(DatasheetReviewState.Approved, row.ReviewState);
        Assert.NotNull(result.PromotionPlan);
        Assert.True(result.PromotionPlan.CanPromote);
        Assert.Equal("core-regulators", result.PromotionPlan.TargetLibraryId);
        Assert.Equal("Verified symbol, TO-220 footprint, and regulator pinout.", result.PromotionPlan.ReviewNote);
        Assert.Equal("LM7805 approved for promotion into core-regulators.", result.StatusMessage);
        Assert.Equal(result.StatusMessage, commands.StatusMessage);
    }

    [Fact]
    public void RejectSelectedRowCapturesReason()
    {
        DatasheetReviewRow row = Row(componentName: "NE555");
        DatasheetPromotionQueueCommands commands = new()
        {
            SelectedRow = row
        };

        DatasheetPromotionCommandResult result = commands.RejectSelected("Pin numbering does not match datasheet.");

        Assert.True(result.Succeeded);
        Assert.Null(result.PromotionPlan);
        Assert.Equal(DatasheetReviewState.Rejected, row.ReviewState);
        Assert.Equal("Pin numbering does not match datasheet.", row.RejectReason);
        Assert.Equal("NE555 rejected: Pin numbering does not match datasheet.", result.StatusMessage);
        Assert.Equal(result.StatusMessage, commands.StatusMessage);
    }

    [Fact]
    public void CreatePromotionPlanBlocksWithoutApproval()
    {
        DatasheetReviewRow row = Row(componentName: "ESP32-WROOM");
        DatasheetPromotionQueueCommands commands = new()
        {
            SelectedRow = row,
            TargetLibraryId = "core-wireless",
            ReviewNote = "Review queued."
        };

        DatasheetPromotionCommandResult result = commands.CreatePromotionPlanForSelected();

        Assert.False(result.Succeeded);
        Assert.Equal(DatasheetReviewState.Pending, row.ReviewState);
        Assert.NotNull(result.PromotionPlan);
        Assert.False(result.PromotionPlan.CanPromote);
        Assert.Contains(
            result.PromotionPlan.Diagnostics,
            diagnostic => diagnostic.Code == "DATASHEET_PROMOTION_REVIEW_NOT_APPROVED");
        Assert.Equal("Promotion blocked for ESP32-WROOM: Datasheet review must be approved before promotion.", result.StatusMessage);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ApproveRequiresTargetLibrary(string targetLibraryId)
    {
        DatasheetReviewRow row = Row(componentName: "MCP1700");
        DatasheetPromotionQueueCommands commands = new()
        {
            SelectedRow = row,
            TargetLibraryId = targetLibraryId,
            ReviewNote = "Ready."
        };

        DatasheetPromotionCommandResult result = commands.ApproveSelectedForPromotion();

        Assert.False(result.Succeeded);
        Assert.Null(result.PromotionPlan);
        Assert.Equal(DatasheetReviewState.Pending, row.ReviewState);
        Assert.Equal("Target library is required before datasheet promotion.", result.StatusMessage);
    }

    [Fact]
    public void CommandsReportMissingSelection()
    {
        DatasheetPromotionQueueCommands commands = new()
        {
            TargetLibraryId = "core-logic"
        };

        DatasheetPromotionCommandResult approveResult = commands.ApproveSelectedForPromotion();
        DatasheetPromotionCommandResult rejectResult = commands.RejectSelected("Not needed.");
        DatasheetPromotionCommandResult planResult = commands.CreatePromotionPlanForSelected();

        Assert.False(approveResult.Succeeded);
        Assert.False(rejectResult.Succeeded);
        Assert.False(planResult.Succeeded);
        Assert.Equal("Select a datasheet review row.", approveResult.StatusMessage);
        Assert.Equal("Select a datasheet review row.", rejectResult.StatusMessage);
        Assert.Equal("Select a datasheet review row.", planResult.StatusMessage);
    }

    private static DatasheetReviewRow Row(string componentName) =>
        new(
            componentName: componentName,
            datasheetSource: $"https://datasheets.example/{componentName}.pdf",
            extractedPinCount: 8,
            symbolStatus: DatasheetProposalStatus.Ready,
            footprintStatus: DatasheetProposalStatus.Ready,
            threeDimensionalModelStatus: DatasheetProposalStatus.Ready,
            confidence: DatasheetReviewConfidence.High,
            warnings: []);
}
