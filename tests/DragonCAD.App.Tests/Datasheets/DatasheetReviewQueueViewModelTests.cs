using DragonCAD.App.Datasheets;

namespace DragonCAD.App.Tests.Datasheets;

public sealed class DatasheetReviewQueueViewModelTests
{
    [Fact]
    public void RowDisplaysDatasheetSourceExtractionSummaryProposalStatusAndWarnings()
    {
        DatasheetReviewQueueViewModel viewModel = DatasheetReviewQueueViewModel.FromRows(
        [
            Row(
                source: "https://vendor.example/7805.pdf",
                pinCount: 3,
                symbolStatus: DatasheetProposalStatus.Ready,
                footprintStatus: DatasheetProposalStatus.NeedsReview,
                threeDimensionalModelStatus: DatasheetProposalStatus.Missing,
                confidence: DatasheetReviewConfidence.Medium,
                warnings:
                [
                    new DatasheetReviewWarning(DatasheetReviewWarningSeverity.Warning, "Missing package height")
                ])
        ]);

        DatasheetReviewRow row = Assert.Single(viewModel.Rows);

        Assert.Equal("vendor.example/7805.pdf", row.SourceDisplay);
        Assert.Equal("3 pins extracted", row.ExtractedPinsSummary);
        Assert.Equal("Symbol ready", row.SymbolStatusDisplay);
        Assert.Equal("Footprint needs review", row.FootprintStatusDisplay);
        Assert.Equal("3D model missing", row.ThreeDimensionalModelStatusDisplay);
        Assert.Equal("Medium confidence", row.ConfidenceDisplay);
        Assert.Equal("Missing package height", row.WarningDisplay);
    }

    [Fact]
    public void RowsAreNeverAutoApproved()
    {
        DatasheetReviewQueueViewModel viewModel = DatasheetReviewQueueViewModel.FromRows(
        [
            Row(confidence: DatasheetReviewConfidence.High)
        ]);

        DatasheetReviewRow row = Assert.Single(viewModel.Rows);

        Assert.Equal(DatasheetReviewState.Pending, row.ReviewState);
        Assert.False(row.IsApproved);
        Assert.Equal("Approve", row.ApproveLabel);
        Assert.Equal("Reject", row.RejectLabel);
    }

    [Fact]
    public void ApproveEnabledOnlyForHighConfidenceRowsWithoutCriticalWarnings()
    {
        DatasheetReviewQueueViewModel viewModel = DatasheetReviewQueueViewModel.FromRows(
        [
            Row(componentName: "LM7805", confidence: DatasheetReviewConfidence.High),
            Row(componentName: "NE555", confidence: DatasheetReviewConfidence.Medium),
            Row(
                componentName: "ESP32",
                confidence: DatasheetReviewConfidence.High,
                warnings:
                [
                    new DatasheetReviewWarning(DatasheetReviewWarningSeverity.Critical, "Pin count mismatch")
                ])
        ]);

        Assert.True(viewModel.Rows[0].CanApprove);
        Assert.False(viewModel.Rows[1].CanApprove);
        Assert.False(viewModel.Rows[2].CanApprove);
    }

    [Fact]
    public void ApproveChangesStateOnlyWhenApprovalRulesPass()
    {
        DatasheetReviewQueueViewModel viewModel = DatasheetReviewQueueViewModel.FromRows(
        [
            Row(componentName: "LM7805", confidence: DatasheetReviewConfidence.High),
            Row(componentName: "NE555", confidence: DatasheetReviewConfidence.Low)
        ]);

        Assert.True(viewModel.Rows[0].Approve());
        Assert.Equal(DatasheetReviewState.Approved, viewModel.Rows[0].ReviewState);
        Assert.True(viewModel.Rows[0].IsApproved);

        Assert.False(viewModel.Rows[1].Approve());
        Assert.Equal(DatasheetReviewState.Pending, viewModel.Rows[1].ReviewState);
        Assert.False(viewModel.Rows[1].IsApproved);
    }

    [Fact]
    public void RejectCapturesReasonAndDisablesApproval()
    {
        DatasheetReviewQueueViewModel viewModel = DatasheetReviewQueueViewModel.FromRows(
        [
            Row(confidence: DatasheetReviewConfidence.High)
        ]);

        DatasheetReviewRow row = Assert.Single(viewModel.Rows);

        row.Reject("Footprint pads do not match datasheet drawing.");

        Assert.Equal(DatasheetReviewState.Rejected, row.ReviewState);
        Assert.Equal("Footprint pads do not match datasheet drawing.", row.RejectReason);
        Assert.False(row.CanApprove);
        Assert.False(row.IsApproved);
    }

    [Fact]
    public void SelectedReviewStateFilterNarrowsVisibleRows()
    {
        DatasheetReviewQueueViewModel viewModel = DatasheetReviewQueueViewModel.FromRows(
        [
            Row(componentName: "LM7805", confidence: DatasheetReviewConfidence.High),
            Row(componentName: "NE555", confidence: DatasheetReviewConfidence.High),
            Row(componentName: "ESP32", confidence: DatasheetReviewConfidence.High)
        ]);
        viewModel.Rows[0].Approve();
        viewModel.Rows[1].Reject("Wrong package");

        viewModel.SelectedReviewStateFilter = DatasheetReviewStateFilter.Rejected;

        DatasheetReviewRow row = Assert.Single(viewModel.Rows);
        Assert.Equal("NE555", row.ComponentName);
        Assert.Equal(["All", "Pending", "Approved", "Rejected"], viewModel.ReviewStateFilterOptions);
    }

    private static DatasheetReviewRow Row(
        string componentName = "LM7805",
        string source = "https://vendor.example/lm7805.pdf",
        int pinCount = 3,
        DatasheetProposalStatus symbolStatus = DatasheetProposalStatus.Ready,
        DatasheetProposalStatus footprintStatus = DatasheetProposalStatus.Ready,
        DatasheetProposalStatus threeDimensionalModelStatus = DatasheetProposalStatus.Placeholder,
        DatasheetReviewConfidence confidence = DatasheetReviewConfidence.High,
        IReadOnlyList<DatasheetReviewWarning>? warnings = null) =>
        new(
            componentName: componentName,
            datasheetSource: source,
            extractedPinCount: pinCount,
            symbolStatus: symbolStatus,
            footprintStatus: footprintStatus,
            threeDimensionalModelStatus: threeDimensionalModelStatus,
            confidence: confidence,
            warnings: warnings ?? []);
}
