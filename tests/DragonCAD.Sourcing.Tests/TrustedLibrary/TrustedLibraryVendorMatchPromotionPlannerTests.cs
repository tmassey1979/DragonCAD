using DragonCAD.Core.Components.Identity;
using DragonCAD.Sourcing.TrustedLibrary;

namespace DragonCAD.Sourcing.Tests.TrustedLibrary;

public sealed class TrustedLibraryVendorMatchPromotionPlannerTests
{
    [Fact]
    public void PlanStagesApprovedReviewedMatchesDeterministicallyWithoutMutatingTheLibrary()
    {
        var plan = TrustedLibraryVendorMatchPromotionPlanner.Plan(
        [
            new ReviewedVendorCatalogMatch(
                reviewState: TrustedLibraryMatchReviewState.Approved,
                sourceProvider: "Mouser",
                vendorSku: "595-NE555P",
                manufacturerPartNumber: "NE555P",
                targetComponentId: new ComponentId("core:timer:ne555p"),
                artifactPaths:
                [
                    new TrustedLibraryArtifactPath("datasheet", "artifacts/vendor/mouser/ne555p.pdf", "sha256:datasheet"),
                ],
                warnings: ["Datasheet checksum is from vendor metadata."]),
            new ReviewedVendorCatalogMatch(
                reviewState: TrustedLibraryMatchReviewState.Approved,
                sourceProvider: "Digi-Key",
                vendorSku: "296-LM7805CT-ND",
                manufacturerPartNumber: "LM7805CT/NOPB",
                targetComponentId: new ComponentId("core:regulator:lm7805ct"),
                artifactPaths:
                [
                    new TrustedLibraryArtifactPath("symbol", "artifacts/generated/lm7805ct/symbol.dcad-symbol.json", "sha256:symbol"),
                    new TrustedLibraryArtifactPath("datasheet", "artifacts/vendor/digikey/lm7805ct.pdf", "sha256:datasheet"),
                ],
                warnings: ["Verify TO-220 footprint before promotion."]),
        ]);

        Assert.False(plan.MutatesCoreLibrary);
        Assert.Equal(2, plan.Records.Count);
        Assert.Equal(
            ["Digi-Key:296-LM7805CT-ND->core:regulator:lm7805ct", "Mouser:595-NE555P->core:timer:ne555p"],
            plan.Records.Select(record => record.SourceKey));

        TrustedLibraryVendorMatchPromotionRecord first = plan.Records[0];
        Assert.Equal(TrustedLibraryMatchReviewState.Approved, first.ReviewState);
        Assert.Equal("Digi-Key", first.SourceProvider);
        Assert.Equal("296-LM7805CT-ND", first.VendorSku);
        Assert.Equal("LM7805CT/NOPB", first.ManufacturerPartNumber);
        Assert.Equal("core:regulator:lm7805ct", first.TargetComponentId.Value);
        Assert.True(first.CanStage);
        Assert.Equal(["Verify TO-220 footprint before promotion."], first.Warnings);
        Assert.Equal(
            ["datasheet:artifacts/vendor/digikey/lm7805ct.pdf", "symbol:artifacts/generated/lm7805ct/symbol.dcad-symbol.json"],
            first.ArtifactPaths.Select(artifact => artifact.Summary));
    }

    [Fact]
    public void PlanKeepsPendingReviewMatchesBlockedWithWarningsAndArtifactMetadata()
    {
        var plan = TrustedLibraryVendorMatchPromotionPlanner.Plan(
        [
            new ReviewedVendorCatalogMatch(
                reviewState: TrustedLibraryMatchReviewState.PendingReview,
                sourceProvider: "Jameco",
                vendorSku: "51262",
                manufacturerPartNumber: "7805",
                targetComponentId: new ComponentId("core:regulator:7805"),
                artifactPaths:
                [
                    new TrustedLibraryArtifactPath("datasheet", "artifacts/manual/jameco/51262.pdf", null),
                ],
                warnings: ["Manual feed requires reviewer confirmation."]),
        ]);

        TrustedLibraryVendorMatchPromotionRecord record = Assert.Single(plan.Records);
        Assert.False(record.CanStage);
        Assert.Equal(TrustedLibraryMatchReviewState.PendingReview, record.ReviewState);
        Assert.Equal(["Manual feed requires reviewer confirmation."], record.Warnings);
        Assert.Equal("datasheet", Assert.Single(record.ArtifactPaths).Kind);
        Assert.Equal("artifacts/manual/jameco/51262.pdf", Assert.Single(record.ArtifactPaths).Path);
        Assert.Null(Assert.Single(record.ArtifactPaths).Checksum);
    }
}
