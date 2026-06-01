using DragonCAD.App.Marketplace.Deduplication;
using DragonCAD.App.Marketplace;
using DragonCAD.Sourcing.Deduplication;

namespace DragonCAD.App.Tests.Marketplace.Deduplication;

public sealed class ComponentDeduplicationReviewViewModelTests
{
    [Fact]
    public void FromMarketplaceRowsGroupsRowsByCanonicalAliasesAndPreservesProviderSourceKeys()
    {
        MarketplaceComponentRow[] rows =
        [
            Row(
                provider: "Digi-Key",
                category: "Voltage Regulator",
                displayName: "LM7805 5V Linear Regulator",
                manufacturer: "Texas Instruments",
                manufacturerPartNumber: "LM7805CT/NOPB",
                canonicalComponentId: "dragon:lm7805"),
            Row(
                provider: "Mouser",
                category: "Voltage Regulator",
                displayName: "L7805CV 5V Linear Regulator",
                manufacturer: "STMicroelectronics",
                manufacturerPartNumber: "L7805CV",
                canonicalComponentId: "dragon:lm7805",
                duplicateOfComponentId: "dragon:lm7805")
        ];

        ComponentDeduplicationReviewViewModel viewModel = ComponentDeduplicationReviewViewModel.FromMarketplaceRows(rows);

        ComponentDeduplicationReviewRow row = Assert.Single(viewModel.Rows);
        Assert.Equal("LM7805CT/NOPB", row.ManufacturerPartNumber);
        Assert.Contains("dragon:lm7805", row.AliasSummary);
        Assert.Equal("1 warning", row.WarningBadge);
        Assert.Equal("Manufacturer disagreement: STMicroelectronics, Texas Instruments", row.ConflictSummary);
        Assert.Collection(
            row.VendorListings,
            listing =>
            {
                Assert.Equal("Digi-Key", listing.ProviderName);
                Assert.Equal(["LM7805CT/NOPB"], listing.VendorSkus);
            },
            listing =>
            {
                Assert.Equal("Mouser", listing.ProviderName);
                Assert.Equal(["L7805CV"], listing.VendorSkus);
            });
    }

    [Fact]
    public void FromMarketplaceRowsGroupsDuplicateManufacturerPartNumbersAcrossProviders()
    {
        MarketplaceComponentRow[] rows =
        [
            Row("Digi-Key", "IC", "NE555 Timer", "Texas Instruments", "NE555P", "dragon:ne555"),
            Row("Mouser", "IC", "NE555 Timer", "TI", "NE555P", "dragon:ne555")
        ];

        ComponentDeduplicationReviewViewModel viewModel = ComponentDeduplicationReviewViewModel.FromMarketplaceRows(rows);

        ComponentDeduplicationReviewRow row = Assert.Single(viewModel.Rows);
        Assert.Equal("NE555P", row.ManufacturerPartNumber);
        Assert.Collection(
            row.VendorListings,
            listing =>
            {
                Assert.Equal("Digi-Key", listing.ProviderName);
                Assert.Equal(["NE555P"], listing.VendorSkus);
            },
            listing =>
            {
                Assert.Equal("Mouser", listing.ProviderName);
                Assert.Equal(["NE555P"], listing.VendorSkus);
            });
    }

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

    private static MarketplaceComponentRow Row(
        string provider,
        string category,
        string displayName,
        string manufacturer,
        string manufacturerPartNumber,
        string canonicalComponentId,
        string duplicateOfComponentId = "") =>
        new(
            Provider: provider,
            Category: category,
            DisplayName: displayName,
            Manufacturer: manufacturer,
            ManufacturerPartNumber: manufacturerPartNumber,
            CanonicalComponentId: canonicalComponentId,
            DuplicateOfComponentId: duplicateOfComponentId,
            DatasheetUrl: "",
            StockQuantity: 100,
            MinimumUnitPriceUsd: 1.25m);
}
