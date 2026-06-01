using DragonCAD.App.Marketplace.TrustedLibrary;
using DragonCAD.Core.Components.Identity;
using DragonCAD.Sourcing.TrustedLibrary;

namespace DragonCAD.App.Tests.Marketplace.TrustedLibrary;

public sealed class TrustedLibraryPromotionQueueViewModelTests
{
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
}
