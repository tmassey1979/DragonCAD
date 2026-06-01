using DragonCAD.App.Marketplace.TrustedLibrary;
using DragonCAD.Core.Components.Identity;
using DragonCAD.Sourcing.TrustedLibrary;

namespace DragonCAD.App.Tests.Marketplace.TrustedLibrary;

public sealed class TrustedLibraryPromotionQueueViewModelTests
{
    [Fact]
    public void FromReviewedCandidatesBuildsQueueThroughPlannerWithoutMutatingCoreLibrary()
    {
        TrustedLibraryPromotionQueueViewModel viewModel = TrustedLibraryPromotionQueueViewModel.FromReviewedCandidates(
        [
            Candidate(
                reviewState: TrustedLibraryMatchReviewState.Rejected,
                provider: "Mouser",
                sku: "595-NE555P",
                manufacturerPartNumber: "NE555P",
                componentId: "core:timer:ne555p",
                artifacts:
                [
                    new TrustedLibraryReviewedArtifactCandidate("datasheet", "artifacts/vendor/mouser/ne555p.pdf", "sha256:datasheet"),
                ],
                warnings: ["Duplicate manufacturer part number requires steward review."]),
            Candidate(
                reviewState: TrustedLibraryMatchReviewState.Approved,
                provider: "Digi-Key",
                sku: "296-LM7805CT-ND",
                manufacturerPartNumber: "LM7805CT/NOPB",
                componentId: "core:regulator:lm7805ct",
                artifacts:
                [
                    new TrustedLibraryReviewedArtifactCandidate("symbol", "artifacts/generated/lm7805ct/symbol.dcad-symbol.json", "sha256:symbol"),
                    new TrustedLibraryReviewedArtifactCandidate("datasheet", "artifacts/vendor/digikey/lm7805ct.pdf", "sha256:datasheet"),
                ],
                warnings: ["Verify TO-220 footprint before promotion."]),
            Candidate(
                reviewState: TrustedLibraryMatchReviewState.PendingReview,
                provider: "Jameco",
                sku: "51262",
                manufacturerPartNumber: "7805",
                componentId: "core:regulator:7805",
                artifacts:
                [
                    new TrustedLibraryReviewedArtifactCandidate("datasheet", "artifacts/manual/jameco/51262.pdf", null),
                ],
                warnings: ["Manual feed requires reviewer confirmation."]),
        ]);

        Assert.False(viewModel.MutatesCoreLibrary);
        Assert.Equal("3 promotion candidates, 1 ready to stage", viewModel.QueueSummary);
        Assert.Equal(
            ["Digi-Key:296-LM7805CT-ND", "Jameco:51262", "Mouser:595-NE555P"],
            viewModel.Rows.Select(row => $"{row.Provider}:{row.VendorSku}"));

        TrustedLibraryPromotionRow approvedRow = viewModel.Rows[0];
        Assert.Equal("core:regulator:lm7805ct", approvedRow.TargetComponentId);
        Assert.Equal(TrustedLibraryMatchReviewState.Approved, approvedRow.ReviewState);
        Assert.True(approvedRow.CanStage);
        Assert.Equal("Ready to stage", approvedRow.StageReadiness);
        Assert.Equal("Verify TO-220 footprint before promotion.", approvedRow.WarningSummary);
        Assert.Equal(
            "datasheet:artifacts/vendor/digikey/lm7805ct.pdf; symbol:artifacts/generated/lm7805ct/symbol.dcad-symbol.json",
            approvedRow.ArtifactPathSummary);

        TrustedLibraryPromotionRow pendingRow = viewModel.Rows[1];
        Assert.Equal("core:regulator:7805", pendingRow.TargetComponentId);
        Assert.Equal(TrustedLibraryMatchReviewState.PendingReview, pendingRow.ReviewState);
        Assert.False(pendingRow.CanStage);
        Assert.Equal("Blocked until approved", pendingRow.StageReadiness);

        TrustedLibraryPromotionRow rejectedRow = viewModel.Rows[2];
        Assert.Equal("core:timer:ne555p", rejectedRow.TargetComponentId);
        Assert.Equal(TrustedLibraryMatchReviewState.Rejected, rejectedRow.ReviewState);
        Assert.False(rejectedRow.CanStage);
        Assert.Equal("Rejected", rejectedRow.StageReadiness);
    }

    [Fact]
    public void FromPlanExposesPromotionRowsWithReviewReadinessWarningsAndArtifacts()
    {
        TrustedLibraryPromotionQueueViewModel viewModel = TrustedLibraryPromotionQueueViewModel.FromPlan(
            new TrustedLibraryVendorMatchPromotionPlan(
            [
                PromotionRecord(
                    reviewState: TrustedLibraryMatchReviewState.PendingReview,
                    provider: "Jameco",
                    vendorSku: "51262",
                    manufacturerPartNumber: "7805",
                    targetComponentId: "core:regulator:7805",
                    artifacts:
                    [
                        new TrustedLibraryArtifactPath("datasheet", "artifacts/manual/jameco/51262.pdf", null),
                    ],
                    warnings:
                    [
                        "Manual feed requires reviewer confirmation.",
                    ]),
                PromotionRecord(
                    reviewState: TrustedLibraryMatchReviewState.Approved,
                    provider: "Digi-Key",
                    vendorSku: "296-LM7805CT-ND",
                    manufacturerPartNumber: "LM7805CT/NOPB",
                    targetComponentId: "core:regulator:lm7805ct",
                    artifacts:
                    [
                        new TrustedLibraryArtifactPath("symbol", "artifacts/generated/lm7805ct/symbol.dcad-symbol.json", "sha256:symbol"),
                        new TrustedLibraryArtifactPath("datasheet", "artifacts/vendor/digikey/lm7805ct.pdf", "sha256:datasheet"),
                    ],
                    warnings:
                    [
                        "Verify TO-220 footprint before promotion.",
                    ]),
            ]));

        Assert.False(viewModel.MutatesCoreLibrary);
        Assert.Equal("2 promotion candidates, 1 ready to stage", viewModel.QueueSummary);

        TrustedLibraryPromotionRow pendingRow = viewModel.Rows[0];
        Assert.Equal("core:regulator:7805", pendingRow.TargetComponentId);
        Assert.Equal("Jameco", pendingRow.Provider);
        Assert.Equal("51262", pendingRow.VendorSku);
        Assert.Equal("7805", pendingRow.ManufacturerPartNumber);
        Assert.Equal(TrustedLibraryMatchReviewState.PendingReview, pendingRow.ReviewState);
        Assert.Equal("Pending review", pendingRow.ReviewStateLabel);
        Assert.Equal("Blocked until approved", pendingRow.StageReadiness);
        Assert.False(pendingRow.CanStage);
        Assert.Equal("Manual feed requires reviewer confirmation.", pendingRow.WarningSummary);
        Assert.Equal("datasheet:artifacts/manual/jameco/51262.pdf", pendingRow.ArtifactPathSummary);

        TrustedLibraryPromotionRow approvedRow = viewModel.Rows[1];
        Assert.Equal("core:regulator:lm7805ct", approvedRow.TargetComponentId);
        Assert.Equal("Digi-Key", approvedRow.Provider);
        Assert.Equal("296-LM7805CT-ND", approvedRow.VendorSku);
        Assert.Equal("LM7805CT/NOPB", approvedRow.ManufacturerPartNumber);
        Assert.Equal("Approved", approvedRow.ReviewStateLabel);
        Assert.Equal("Ready to stage", approvedRow.StageReadiness);
        Assert.True(approvedRow.CanStage);
        Assert.Equal("Verify TO-220 footprint before promotion.", approvedRow.WarningSummary);
        Assert.Equal(
            "datasheet:artifacts/vendor/digikey/lm7805ct.pdf; symbol:artifacts/generated/lm7805ct/symbol.dcad-symbol.json",
            approvedRow.ArtifactPathSummary);
    }

    [Fact]
    public void FromPlanExposesQueueStatusCountsAndLabelsForLibraryMarketDisplay()
    {
        TrustedLibraryPromotionQueueViewModel viewModel = TrustedLibraryPromotionQueueViewModel.FromPlan(
            new TrustedLibraryVendorMatchPromotionPlan(
            [
                PromotionRecord(
                    reviewState: TrustedLibraryMatchReviewState.Approved,
                    provider: "Digi-Key",
                    vendorSku: "296-LM7805CT-ND",
                    manufacturerPartNumber: "LM7805CT/NOPB",
                    targetComponentId: "core:regulator:lm7805ct",
                    artifacts: [],
                    warnings: []),
                PromotionRecord(
                    reviewState: TrustedLibraryMatchReviewState.PendingReview,
                    provider: "Jameco",
                    vendorSku: "51262",
                    manufacturerPartNumber: "7805",
                    targetComponentId: "core:regulator:7805",
                    artifacts: [],
                    warnings: []),
                PromotionRecord(
                    reviewState: TrustedLibraryMatchReviewState.Rejected,
                    provider: "Mouser",
                    vendorSku: "595-NE555P",
                    manufacturerPartNumber: "NE555P",
                    targetComponentId: "core:timer:ne555p",
                    artifacts: [],
                    warnings: []),
            ]));

        Assert.Equal(1, viewModel.ReadyCount);
        Assert.Equal(1, viewModel.PendingCount);
        Assert.Equal(1, viewModel.BlockedCount);
        Assert.Equal("1 ready", viewModel.ReadyStatusLabel);
        Assert.Equal("1 pending", viewModel.PendingStatusLabel);
        Assert.Equal("1 blocked", viewModel.BlockedStatusLabel);
        Assert.Equal("1 ready / 1 pending / 1 blocked", viewModel.QueueStatusSummary);
        Assert.Equal("Stage 1 ready", viewModel.NextActionStatusLabel);
    }

    [Theory]
    [InlineData(0, 0, 0, "No promotion candidates queued.", "Add reviewed vendor matches to build the trusted-library promotion queue.", false)]
    [InlineData(0, 1, 0, "0 of 1 promotion candidate ready to stage; 1 pending review; 0 blocked.", "Review 1 pending candidate before staging.", false)]
    [InlineData(0, 2, 1, "0 of 3 promotion candidates ready to stage; 2 pending review; 1 blocked.", "Review 2 pending candidates before staging.", false)]
    [InlineData(0, 0, 1, "0 of 1 promotion candidate ready to stage; 0 pending review; 1 blocked.", "Resolve 1 blocked candidate before staging.", false)]
    [InlineData(1, 2, 1, "1 of 4 promotion candidates ready to stage; 2 pending review; 1 blocked.", "Stage 1 approved candidate to the trusted library.", true)]
    [InlineData(3, 0, 0, "3 of 3 promotion candidates ready to stage; 0 pending review; 0 blocked.", "Stage 3 approved candidates to the trusted library.", true)]
    public void FromPlanExposesPromotionReadinessAndActionSummaryForQueueSurface(
        int readyCount,
        int pendingCount,
        int blockedCount,
        string expectedReadinessSummary,
        string expectedActionSummary,
        bool expectedCanStagePromotions)
    {
        TrustedLibraryPromotionQueueViewModel viewModel = TrustedLibraryPromotionQueueViewModel.FromPlan(
            new TrustedLibraryVendorMatchPromotionPlan(
                BuildPromotionRecords(
                    readyCount,
                    pendingCount,
                    blockedCount)));

        Assert.Equal(expectedReadinessSummary, viewModel.PromotionReadinessSummary);
        Assert.Equal(expectedActionSummary, viewModel.PromotionActionSummary);
        Assert.Equal(expectedCanStagePromotions, viewModel.CanStagePromotions);
    }

    [Fact]
    public void RowCommandsNotifyQueueStatusCountsLabelsAndPromotionSummaries()
    {
        TrustedLibraryPromotionQueueViewModel viewModel = TrustedLibraryPromotionQueueViewModel.FromPlan(
            new TrustedLibraryVendorMatchPromotionPlan(
            [
                PromotionRecord(
                    reviewState: TrustedLibraryMatchReviewState.PendingReview,
                    provider: "Mouser",
                    vendorSku: "595-NE555P",
                    manufacturerPartNumber: "NE555P",
                    targetComponentId: "core:timer:ne555p",
                    artifacts: [],
                    warnings: []),
            ]));
        TrustedLibraryPromotionRow row = Assert.Single(viewModel.Rows);
        List<string?> changedProperties = [];
        viewModel.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

        row.MarkApprovedCommand.Execute(null);

        Assert.Equal(1, viewModel.ReadyCount);
        Assert.Equal(0, viewModel.PendingCount);
        Assert.Equal(0, viewModel.BlockedCount);
        Assert.Equal("1 ready / 0 pending / 0 blocked", viewModel.QueueStatusSummary);
        Assert.Equal("Stage 1 ready", viewModel.NextActionStatusLabel);
        Assert.Equal("1 of 1 promotion candidate ready to stage; 0 pending review; 0 blocked.", viewModel.PromotionReadinessSummary);
        Assert.Equal("Stage 1 approved candidate to the trusted library.", viewModel.PromotionActionSummary);
        Assert.True(viewModel.CanStagePromotions);
        Assert.Contains(nameof(TrustedLibraryPromotionQueueViewModel.ReadyCount), changedProperties);
        Assert.Contains(nameof(TrustedLibraryPromotionQueueViewModel.PendingCount), changedProperties);
        Assert.Contains(nameof(TrustedLibraryPromotionQueueViewModel.BlockedCount), changedProperties);
        Assert.Contains(nameof(TrustedLibraryPromotionQueueViewModel.ReadyStatusLabel), changedProperties);
        Assert.Contains(nameof(TrustedLibraryPromotionQueueViewModel.PendingStatusLabel), changedProperties);
        Assert.Contains(nameof(TrustedLibraryPromotionQueueViewModel.BlockedStatusLabel), changedProperties);
        Assert.Contains(nameof(TrustedLibraryPromotionQueueViewModel.QueueStatusSummary), changedProperties);
        Assert.Contains(nameof(TrustedLibraryPromotionQueueViewModel.NextActionStatusLabel), changedProperties);
        Assert.Contains(nameof(TrustedLibraryPromotionQueueViewModel.PromotionReadinessSummary), changedProperties);
        Assert.Contains(nameof(TrustedLibraryPromotionQueueViewModel.PromotionActionSummary), changedProperties);
        Assert.Contains(nameof(TrustedLibraryPromotionQueueViewModel.CanStagePromotions), changedProperties);
    }

    [Theory]
    [InlineData(0, 0, 0, "No promotions queued")]
    [InlineData(0, 1, 0, "Review 1 pending")]
    [InlineData(0, 2, 1, "Review 2 pending")]
    [InlineData(0, 0, 1, "1 blocked")]
    [InlineData(1, 2, 1, "Stage 1 ready")]
    [InlineData(3, 0, 0, "Stage 3 ready")]
    public void FromPlanExposesCompactNextActionStatusLabelForQueueCard(
        int readyCount,
        int pendingCount,
        int blockedCount,
        string expectedLabel)
    {
        TrustedLibraryPromotionQueueViewModel viewModel = TrustedLibraryPromotionQueueViewModel.FromPlan(
            new TrustedLibraryVendorMatchPromotionPlan(
                BuildPromotionRecords(
                    readyCount,
                    pendingCount,
                    blockedCount)));

        Assert.Equal(expectedLabel, viewModel.NextActionStatusLabel);
    }

    [Fact]
    public void RowCommandsUpdateReviewStateInMemoryWithoutChangingSourcePlan()
    {
        TrustedLibraryVendorMatchPromotionRecord sourceRecord = PromotionRecord(
            reviewState: TrustedLibraryMatchReviewState.PendingReview,
            provider: "Mouser",
            vendorSku: "595-NE555P",
            manufacturerPartNumber: "NE555P",
            targetComponentId: "core:timer:ne555p",
            artifacts:
            [
                new TrustedLibraryArtifactPath("datasheet", "artifacts/vendor/mouser/ne555p.pdf", "sha256:datasheet"),
            ],
            warnings: []);

        TrustedLibraryPromotionQueueViewModel viewModel = TrustedLibraryPromotionQueueViewModel.FromPlan(
            new TrustedLibraryVendorMatchPromotionPlan([sourceRecord]));

        TrustedLibraryPromotionRow row = Assert.Single(viewModel.Rows);

        row.MarkApprovedCommand.Execute(null);

        Assert.Equal(TrustedLibraryMatchReviewState.Approved, row.ReviewState);
        Assert.Equal("Ready to stage", row.StageReadiness);
        Assert.Equal(TrustedLibraryMatchReviewState.PendingReview, sourceRecord.ReviewState);

        row.MarkRejectedCommand.Execute(null);

        Assert.Equal(TrustedLibraryMatchReviewState.Rejected, row.ReviewState);
        Assert.Equal("Rejected", row.StageReadiness);
        Assert.False(row.CanStage);
        Assert.Equal(TrustedLibraryMatchReviewState.PendingReview, sourceRecord.ReviewState);

        row.MarkPendingCommand.Execute(null);

        Assert.Equal(TrustedLibraryMatchReviewState.PendingReview, row.ReviewState);
        Assert.Equal("Blocked until approved", row.StageReadiness);
        Assert.Equal(TrustedLibraryMatchReviewState.PendingReview, sourceRecord.ReviewState);
    }

    private static TrustedLibraryVendorMatchPromotionRecord PromotionRecord(
        TrustedLibraryMatchReviewState reviewState,
        string provider,
        string vendorSku,
        string manufacturerPartNumber,
        string targetComponentId,
        IReadOnlyList<TrustedLibraryArtifactPath> artifacts,
        IReadOnlyList<string> warnings) =>
        new(
            reviewState,
            provider,
            vendorSku,
            manufacturerPartNumber,
            new ComponentId(targetComponentId),
            artifacts,
            warnings);

    private static IReadOnlyList<TrustedLibraryVendorMatchPromotionRecord> BuildPromotionRecords(
        int readyCount,
        int pendingCount,
        int blockedCount) =>
        Enumerable
            .Range(0, readyCount)
            .Select(index => PromotionRecord(
                TrustedLibraryMatchReviewState.Approved,
                provider: "Digi-Key",
                vendorSku: $"READY-{index}",
                manufacturerPartNumber: $"READY-MPN-{index}",
                targetComponentId: $"core:test:ready-{index}",
                artifacts: [],
                warnings: []))
            .Concat(Enumerable
                .Range(0, pendingCount)
                .Select(index => PromotionRecord(
                    TrustedLibraryMatchReviewState.PendingReview,
                    provider: "Jameco",
                    vendorSku: $"PENDING-{index}",
                    manufacturerPartNumber: $"PENDING-MPN-{index}",
                    targetComponentId: $"core:test:pending-{index}",
                    artifacts: [],
                    warnings: [])))
            .Concat(Enumerable
                .Range(0, blockedCount)
                .Select(index => PromotionRecord(
                    TrustedLibraryMatchReviewState.Rejected,
                    provider: "Mouser",
                    vendorSku: $"BLOCKED-{index}",
                    manufacturerPartNumber: $"BLOCKED-MPN-{index}",
                    targetComponentId: $"core:test:blocked-{index}",
                    artifacts: [],
                    warnings: [])))
            .ToArray();

    private static TrustedLibraryReviewedCandidate Candidate(
        TrustedLibraryMatchReviewState reviewState,
        string provider,
        string sku,
        string manufacturerPartNumber,
        string componentId,
        IReadOnlyList<TrustedLibraryReviewedArtifactCandidate> artifacts,
        IReadOnlyList<string> warnings) =>
        new(componentId, provider, sku, manufacturerPartNumber, reviewState, artifacts, warnings);
}
