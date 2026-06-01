using DragonCAD.App.Marketplace.Audit;
using DragonCAD.Core.Components.Marketplace;
using DragonCAD.Core.Components.Marketplace.Provenance;

namespace DragonCAD.App.Tests.Marketplace.Audit;

public sealed class MarketplaceAuditTimelineViewModelTests
{
    [Fact]
    public void VendorImportRowExposesUiFriendlySourceDetails()
    {
        MarketplaceAuditTimelineViewModel viewModel = MarketplaceAuditTimelineViewModel.FromRecords(
        [
            VendorImport("LM7805CT", "Digi-Key", new DateTimeOffset(2026, 5, 31, 14, 30, 0, TimeSpan.Zero))
        ]);

        MarketplaceAuditTimelineRow row = Assert.Single(viewModel.Rows);
        Assert.Equal("PART:LM7805CT", row.ComponentKey);
        Assert.Equal("Vendor Import", row.SourceType);
        Assert.Equal("Digi-Key", row.Vendor);
        Assert.Equal("https://example.invalid/lm7805ct.pdf", row.DatasheetUrl);
        Assert.Equal("manual", row.Generator);
        Assert.Equal("Imported", row.ReviewState);
        Assert.Equal("No reviewer note", row.Note);
        Assert.Equal("2026-05-31 14:30 UTC", row.TimestampDisplay);
    }

    [Fact]
    public void DatasheetGeneratedRowExposesGeneratorAndPendingReviewState()
    {
        MarketplaceAuditTimelineViewModel viewModel = MarketplaceAuditTimelineViewModel.FromRecords(
        [
            DatasheetGenerated("NE555P", "Mouser", "Codex", MarketplaceReviewState.PendingReview, new DateTimeOffset(2026, 5, 31, 15, 0, 0, TimeSpan.Zero))
        ]);

        MarketplaceAuditTimelineRow row = Assert.Single(viewModel.Rows);
        Assert.Equal("Datasheet Generated", row.SourceType);
        Assert.Equal("Mouser", row.Vendor);
        Assert.Equal("Codex", row.Generator);
        Assert.Equal("Pending Review", row.ReviewState);
        Assert.Equal("No reviewer note", row.Note);
    }

    [Fact]
    public void ManualOverrideRowExposesReviewerNote()
    {
        MarketplaceComponentProvenance imported = VendorImport("L7805CV", "Jameco", new DateTimeOffset(2026, 5, 31, 16, 0, 0, TimeSpan.Zero));
        MarketplaceComponentProvenance reviewed = imported.WithManualOverride(
            MarketplaceReviewState.Approved,
            "Matched to canonical 7805 regulator after package review.",
            new DateTimeOffset(2026, 5, 31, 17, 0, 0, TimeSpan.Zero));

        MarketplaceAuditTimelineViewModel viewModel = MarketplaceAuditTimelineViewModel.FromRecords([reviewed]);

        MarketplaceAuditTimelineRow row = Assert.Single(viewModel.Rows);
        Assert.Equal("Manual Override", row.SourceType);
        Assert.Equal("Approved", row.ReviewState);
        Assert.Equal("Matched to canonical 7805 regulator after package review.", row.Note);
    }

    [Fact]
    public void ReviewStateFilterNarrowsRows()
    {
        MarketplaceAuditTimelineViewModel viewModel = MarketplaceAuditTimelineViewModel.FromRecords(
        [
            VendorImport("LM7805CT", "Digi-Key", new DateTimeOffset(2026, 5, 31, 14, 30, 0, TimeSpan.Zero)),
            DatasheetGenerated("NE555P", "Mouser", "Codex", MarketplaceReviewState.PendingReview, new DateTimeOffset(2026, 5, 31, 15, 0, 0, TimeSpan.Zero))
        ]);

        viewModel.SelectedReviewStateFilter = "Pending Review";

        MarketplaceAuditTimelineRow row = Assert.Single(viewModel.Rows);
        Assert.Equal("PART:NE555P", row.ComponentKey);
        Assert.Equal("Pending Review", row.ReviewState);
    }

    [Fact]
    public void SourceFilterNarrowsRowsBySourceType()
    {
        MarketplaceAuditTimelineViewModel viewModel = MarketplaceAuditTimelineViewModel.FromRecords(
        [
            VendorImport("LM7805CT", "Digi-Key", new DateTimeOffset(2026, 5, 31, 14, 30, 0, TimeSpan.Zero)),
            DatasheetGenerated("NE555P", "Mouser", "Codex", MarketplaceReviewState.PendingReview, new DateTimeOffset(2026, 5, 31, 15, 0, 0, TimeSpan.Zero))
        ]);

        Assert.Equal(["All", "Datasheet Generated", "Vendor Import"], viewModel.SourceFilterOptions);

        viewModel.SelectedSourceFilter = "Datasheet Generated";

        MarketplaceAuditTimelineRow row = Assert.Single(viewModel.Rows);
        Assert.Equal("PART:NE555P", row.ComponentKey);
        Assert.Equal("Datasheet Generated", row.SourceType);
    }

    [Fact]
    public void RowsAreOrderedNewestFirstDeterministically()
    {
        MarketplaceComponentProvenance sameTimeA = VendorImport("ATMEGA328P-PU", "Digi-Key", new DateTimeOffset(2026, 5, 31, 18, 0, 0, TimeSpan.Zero));
        MarketplaceComponentProvenance sameTimeB = VendorImport("ESP32-DEVKITC", "Adafruit", new DateTimeOffset(2026, 5, 31, 18, 0, 0, TimeSpan.Zero));
        MarketplaceComponentProvenance newest = DatasheetGenerated("NE555P", "Mouser", "Codex", MarketplaceReviewState.PendingReview, new DateTimeOffset(2026, 5, 31, 19, 0, 0, TimeSpan.Zero));
        MarketplaceComponentProvenance oldest = VendorImport("LM7805CT", "Digi-Key", new DateTimeOffset(2026, 5, 31, 14, 30, 0, TimeSpan.Zero));

        MarketplaceAuditTimelineViewModel viewModel = MarketplaceAuditTimelineViewModel.FromRecords([oldest, sameTimeB, newest, sameTimeA]);

        Assert.Equal(
            ["PART:NE555P", "PART:ATMEGA328PPU", "PART:ESP32DEVKITC", "PART:LM7805CT"],
            viewModel.Rows.Select(row => row.ComponentKey));
    }

    private static MarketplaceComponentProvenance VendorImport(
        string manufacturerPartNumber,
        string vendor,
        DateTimeOffset timestamp) =>
        MarketplaceComponentProvenance.VendorImport(
            CanonicalComponentKey.FromPartNumber(manufacturerPartNumber),
            vendor,
            $"https://example.invalid/{manufacturerPartNumber.ToLowerInvariant()}",
            $"https://example.invalid/{manufacturerPartNumber.ToLowerInvariant()}.pdf",
            "sha256:0123456789abcdef",
            timestamp);

    private static MarketplaceComponentProvenance DatasheetGenerated(
        string manufacturerPartNumber,
        string vendor,
        string generatorName,
        MarketplaceReviewState reviewState,
        DateTimeOffset timestamp) =>
        MarketplaceComponentProvenance.DatasheetGenerated(
            CanonicalComponentKey.FromPartNumber(manufacturerPartNumber),
            vendor,
            $"https://example.invalid/{manufacturerPartNumber.ToLowerInvariant()}",
            $"https://example.invalid/{manufacturerPartNumber.ToLowerInvariant()}.pdf",
            "sha256:abcdef0123456789",
            generatorName,
            reviewState,
            timestamp);
}
