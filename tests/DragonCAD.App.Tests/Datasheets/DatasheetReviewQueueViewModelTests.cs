using DragonCAD.App.Datasheets;

namespace DragonCAD.App.Tests.Datasheets;

public sealed class DatasheetReviewQueueViewModelTests
{
    [Fact]
    public void RowProjectionShowsDraftMetadataBlockersAndRecommendedAction()
    {
        DatasheetReviewQueueViewModel viewModel = DatasheetReviewQueueViewModel.FromRows(
        [
            Row(
                draftId: "draft:lm7805",
                componentName: "LM7805",
                manufacturerPartNumber: "LM7805CT",
                source: "https://vendor.example/7805.pdf",
                category: DatasheetReviewCategory.Blocked,
                confidence: DatasheetReviewConfidence.Medium,
                symbolStatus: DatasheetProposalStatus.Ready,
                footprintStatus: DatasheetProposalStatus.NeedsReview,
                threeDimensionalModelStatus: DatasheetProposalStatus.Missing,
                diagnostics:
                [
                    new DatasheetReviewDiagnostic(DatasheetReviewDiagnosticSeverity.Blocker, "Footprint pad pitch is ambiguous.")
                ])
        ]);

        DatasheetReviewRow row = Assert.Single(viewModel.Rows);

        Assert.Equal("draft:lm7805", row.DraftId);
        Assert.Equal("LM7805CT", row.ManufacturerPartNumber);
        Assert.Equal("vendor.example/7805.pdf", row.SourceDisplay);
        Assert.Equal("Medium confidence", row.ConfidenceDisplay);
        Assert.Equal("Symbol ready", row.SymbolStatusDisplay);
        Assert.Equal("Footprint needs review", row.FootprintStatusDisplay);
        Assert.Equal("3D model missing", row.ThreeDimensionalModelStatusDisplay);
        Assert.Equal("Footprint pad pitch is ambiguous.", row.BlockerDisplay);
        Assert.Equal("Resolve blockers", row.RecommendedAction);
    }

    [Fact]
    public void QueueFiltersByReadyBlockedDuplicateNeedsDataAndRejected()
    {
        DatasheetReviewQueueViewModel viewModel = DatasheetReviewQueueViewModel.FromRows(
        [
            Row(draftId: "draft:blocked", category: DatasheetReviewCategory.Blocked),
            Row(draftId: "draft:ready-b", category: DatasheetReviewCategory.Ready),
            Row(draftId: "draft:needs-data", category: DatasheetReviewCategory.NeedsData),
            Row(draftId: "draft:duplicate", category: DatasheetReviewCategory.Duplicate),
            Row(draftId: "draft:ready-a", category: DatasheetReviewCategory.Ready),
            Row(draftId: "draft:rejected", category: DatasheetReviewCategory.Ready)
        ]);
        viewModel.Rows.Single(row => row.DraftId == "draft:rejected").Reject("Not the requested package.");

        Assert.Equal(
            ["Ready", "Blocked", "Duplicate", "Needs Data", "Rejected"],
            viewModel.ReviewCategoryFilterOptions);
        Assert.Equal(
            ["draft:ready-a", "draft:ready-b", "draft:blocked", "draft:duplicate", "draft:needs-data", "draft:rejected"],
            viewModel.Rows.Select(row => row.DraftId));

        viewModel.SelectedReviewCategoryFilter = DatasheetReviewCategoryFilter.Ready;
        Assert.Equal(["draft:ready-a", "draft:ready-b"], viewModel.Rows.Select(row => row.DraftId));

        viewModel.SelectedReviewCategoryFilter = DatasheetReviewCategoryFilter.Blocked;
        Assert.Equal(["draft:blocked"], viewModel.Rows.Select(row => row.DraftId));

        viewModel.SelectedReviewCategoryFilter = DatasheetReviewCategoryFilter.Duplicate;
        Assert.Equal(["draft:duplicate"], viewModel.Rows.Select(row => row.DraftId));

        viewModel.SelectedReviewCategoryFilter = DatasheetReviewCategoryFilter.NeedsData;
        Assert.Equal(["draft:needs-data"], viewModel.Rows.Select(row => row.DraftId));

        viewModel.SelectedReviewCategoryFilter = DatasheetReviewCategoryFilter.Rejected;
        Assert.Equal(["draft:rejected"], viewModel.Rows.Select(row => row.DraftId));
    }

    [Fact]
    public void SelectingRowExposesReviewNotesAndRedactedProvenance()
    {
        DatasheetReviewQueueViewModel viewModel = DatasheetReviewQueueViewModel.FromRows(
        [
            Row(
                draftId: "draft:secret",
                reviewNotes: "Check thermal pad against the package drawing.",
                provenance:
                [
                    new DatasheetReviewProvenance("source", "vendor.example/7805.pdf", isSecret: false),
                    new DatasheetReviewProvenance("api-key", "sk-test-secret", isSecret: true)
                ]),
            Row(draftId: "draft:other")
        ]);

        viewModel.SelectedRow = viewModel.Rows.Single(row => row.DraftId == "draft:secret");

        Assert.NotNull(viewModel.SelectedRowDetails);
        Assert.Equal("Check thermal pad against the package drawing.", viewModel.SelectedRowDetails.ReviewNotes);
        Assert.Equal(
            ["source: vendor.example/7805.pdf", "api-key: [redacted]"],
            viewModel.SelectedRowDetails.ProvenanceDisplayLines);
        Assert.DoesNotContain("sk-test-secret", viewModel.SelectedRowDetails.ProvenanceDisplay);
    }

    [Fact]
    public void ReviewDecisionsAreLocalAndSupportApproveRejectAndDefer()
    {
        DatasheetReviewQueueViewModel viewModel = DatasheetReviewQueueViewModel.FromRows(
        [
            Row(draftId: "draft:ready", category: DatasheetReviewCategory.Ready, confidence: DatasheetReviewConfidence.High),
            Row(draftId: "draft:needs-data", category: DatasheetReviewCategory.NeedsData)
        ]);

        DatasheetReviewRow ready = viewModel.Rows.Single(row => row.DraftId == "draft:ready");
        DatasheetReviewRow needsData = viewModel.Rows.Single(row => row.DraftId == "draft:needs-data");

        Assert.True(ready.Approve("Symbols and footprint match."));
        Assert.Equal(DatasheetReviewState.Promoted, ready.ReviewState);
        Assert.False(ready.MutatedTrustedLibrary);
        Assert.Equal("Approved for promotion", ready.DecisionRecords.Single().Decision);

        needsData.Defer("Waiting for vendor package drawing.");
        Assert.Equal(DatasheetReviewState.Pending, needsData.ReviewState);
        Assert.Equal("Deferred", needsData.DecisionRecords.Single().Decision);

        needsData.Reject("Insufficient source data.");
        Assert.Equal(DatasheetReviewState.Rejected, needsData.ReviewState);
        Assert.Equal("Rejected", needsData.DecisionRecords.Last().Decision);
        Assert.False(needsData.MutatedTrustedLibrary);
    }

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
        Assert.Equal(DatasheetReviewState.Promoted, viewModel.Rows[0].ReviewState);
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
        Assert.Equal(["All", "Pending", "Promoted", "Rejected"], viewModel.ReviewStateFilterOptions);
    }

    private static DatasheetReviewRow Row(
        string componentName = "LM7805",
        string draftId = "",
        string manufacturerPartNumber = "",
        string source = "https://vendor.example/lm7805.pdf",
        int pinCount = 3,
        DatasheetProposalStatus symbolStatus = DatasheetProposalStatus.Ready,
        DatasheetProposalStatus footprintStatus = DatasheetProposalStatus.Ready,
        DatasheetProposalStatus threeDimensionalModelStatus = DatasheetProposalStatus.Placeholder,
        DatasheetReviewConfidence confidence = DatasheetReviewConfidence.High,
        DatasheetReviewCategory category = DatasheetReviewCategory.Ready,
        IReadOnlyList<DatasheetReviewWarning>? warnings = null,
        IReadOnlyList<DatasheetReviewDiagnostic>? diagnostics = null,
        IReadOnlyList<DatasheetReviewProvenance>? provenance = null,
        string reviewNotes = "") =>
        new(
            componentName: componentName,
            datasheetSource: source,
            extractedPinCount: pinCount,
            symbolStatus: symbolStatus,
            footprintStatus: footprintStatus,
            threeDimensionalModelStatus: threeDimensionalModelStatus,
            confidence: confidence,
            warnings: warnings ?? [],
            draftId: draftId,
            manufacturerPartNumber: manufacturerPartNumber,
            category: category,
            diagnostics: diagnostics,
            provenance: provenance,
            reviewNotes: reviewNotes);
}
