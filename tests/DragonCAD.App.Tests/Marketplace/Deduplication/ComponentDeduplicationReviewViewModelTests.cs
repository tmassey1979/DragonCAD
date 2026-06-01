using DragonCAD.App.Marketplace.Deduplication;
using DragonCAD.Sourcing.Deduplication;

namespace DragonCAD.App.Tests.Marketplace.Deduplication;

public sealed class ComponentDeduplicationReviewViewModelTests
{
    [Fact]
    public void FromCandidatesMapsCandidateIdentityWarningsAndGroupedVendorListings()
    {
        var candidate = new ComponentCandidate(
            "LM7805CT/NOPB",
            "Texas Instruments",
            "TO-220",
            "5 V",
            ["LM7805", "7805"],
            ["Digi-Key:296-12345-1-ND", "Digi-Key:296-99999-1-ND", "Mouser:595-LM7805CT"],
            [
                new ComponentMergeWarning(
                    ComponentMergeWarningKind.PackageDisagreement,
                    "Merged listings use different package signals.",
                    ["TO-220", "TO-263"],
                    ["Digi-Key:296-12345-1-ND", "Mouser:595-LM7805CT"])
            ]);

        ComponentDeduplicationReviewViewModel viewModel = ComponentDeduplicationReviewViewModel.FromCandidates([candidate]);

        ComponentDeduplicationReviewRow row = Assert.Single(viewModel.Rows);
        Assert.Equal("Texas Instruments LM7805CT/NOPB", row.CanonicalName);
        Assert.Equal("Texas Instruments", row.Manufacturer);
        Assert.Equal("LM7805CT/NOPB", row.ManufacturerPartNumber);
        Assert.Equal("TO-220 / 5 V", row.PackageValueSummary);
        Assert.Equal("LM7805, 7805", row.AliasSummary);
        Assert.Equal("1 warning", row.WarningBadge);
        Assert.Equal("Package disagreement: TO-220, TO-263", row.ConflictSummary);
        Assert.Equal("Pending Review", row.ReviewStateDisplay);

        Assert.Collection(
            row.VendorListings,
            listing =>
            {
                Assert.Equal("Digi-Key", listing.ProviderName);
                Assert.Equal(["296-12345-1-ND", "296-99999-1-ND"], listing.VendorSkus);
                Assert.Equal("Digi-Key: 296-12345-1-ND, 296-99999-1-ND", listing.DisplayText);
            },
            listing =>
            {
                Assert.Equal("Mouser", listing.ProviderName);
                Assert.Equal(["595-LM7805CT"], listing.VendorSkus);
                Assert.Equal("Mouser: 595-LM7805CT", listing.DisplayText);
            });
    }

    [Fact]
    public void ReviewCommandsUpdateRowStateInMemory()
    {
        ComponentDeduplicationReviewRow row = Assert.Single(ComponentDeduplicationReviewViewModel.FromCandidates(
            [
                new ComponentCandidate(
                    "NE555P",
                    "Texas Instruments",
                    "DIP-8",
                    null,
                    ["NE555"],
                    ["Mouser:595-NE555P"],
                    [])
            ]).Rows);

        Assert.True(row.ApproveCommand.CanExecute(null));
        row.ApproveCommand.Execute(null);

        Assert.Equal(ComponentDeduplicationReviewState.Approved, row.ReviewState);
        Assert.Equal("Approved", row.ReviewStateDisplay);
        Assert.Equal("Approved locally; merge write is still pending.", row.ReviewNote);
        Assert.False(row.ApproveCommand.CanExecute(null));
        Assert.False(row.RejectCommand.CanExecute(null));

        row.ResetCommand.Execute(null);
        row.RejectCommand.Execute(null);

        Assert.Equal(ComponentDeduplicationReviewState.Rejected, row.ReviewState);
        Assert.Equal("Rejected", row.ReviewStateDisplay);
        Assert.Equal("Rejected locally; candidate remains unchanged.", row.ReviewNote);
    }
}
