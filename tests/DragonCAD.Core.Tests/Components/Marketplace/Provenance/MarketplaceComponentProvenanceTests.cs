using DragonCAD.Core.Components.Marketplace;
using DragonCAD.Core.Components.Marketplace.Provenance;

namespace DragonCAD.Core.Tests.Components.Marketplace.Provenance;

public sealed class MarketplaceComponentProvenanceTests
{
    [Fact]
    public void VendorImportProvenanceCapturesSourceLinksAndInjectedTimestamp()
    {
        DateTimeOffset importedAt = new(2026, 5, 31, 14, 30, 0, TimeSpan.Zero);

        MarketplaceComponentProvenance provenance = MarketplaceComponentProvenance.VendorImport(
            CanonicalComponentKey.FromPartNumber("LM7805CT"),
            sourceVendor: "Digi-Key",
            productUrl: "https://www.digikey.com/en/products/detail/texas-instruments/LM7805CT/390192",
            datasheetUrl: "https://example.invalid/lm7805ct.pdf",
            datasheetChecksum: "sha256:0123456789abcdef",
            importedAt);

        Assert.Equal(CanonicalComponentKey.FromPartNumber("LM7805CT"), provenance.ComponentKey);
        Assert.Equal(MarketplaceProvenanceKind.VendorImport, provenance.Kind);
        Assert.Equal("Digi-Key", provenance.SourceVendor);
        Assert.Equal("https://www.digikey.com/en/products/detail/texas-instruments/LM7805CT/390192", provenance.ProductUrl);
        Assert.Equal("https://example.invalid/lm7805ct.pdf", provenance.DatasheetUrl);
        Assert.Equal("sha256:0123456789abcdef", provenance.DatasheetChecksum);
        Assert.Equal("manual", provenance.GeneratorName);
        Assert.Equal(MarketplaceReviewState.Imported, provenance.ReviewState);
        Assert.Equal(importedAt, provenance.Timestamp);
    }

    [Fact]
    public void DatasheetGeneratedProvenanceCapturesGeneratorAndReviewState()
    {
        DateTimeOffset generatedAt = new(2026, 5, 31, 15, 0, 0, TimeSpan.Zero);

        MarketplaceComponentProvenance provenance = MarketplaceComponentProvenance.DatasheetGenerated(
            CanonicalComponentKey.FromPartNumber("NE555P"),
            sourceVendor: "Mouser",
            productUrl: "https://www.mouser.com/ProductDetail/Texas-Instruments/NE555P",
            datasheetUrl: "https://example.invalid/ne555p.pdf",
            datasheetChecksum: "sha256:abcdef0123456789",
            generatorName: "Codex",
            reviewState: MarketplaceReviewState.PendingReview,
            timestamp: generatedAt);

        Assert.Equal(MarketplaceProvenanceKind.DatasheetGenerated, provenance.Kind);
        Assert.Equal("Codex", provenance.GeneratorName);
        Assert.Equal(MarketplaceReviewState.PendingReview, provenance.ReviewState);
        Assert.Equal(generatedAt, provenance.Timestamp);
        Assert.Empty(provenance.ReviewerNote);
    }

    [Fact]
    public void ManualOverrideRecordsReviewerNoteWithoutChangingSourceIdentity()
    {
        MarketplaceComponentProvenance imported = MarketplaceComponentProvenance.VendorImport(
            CanonicalComponentKey.FromPartNumber("L7805CV"),
            sourceVendor: "Jameco",
            productUrl: "https://www.jameco.com/example/l7805cv",
            datasheetUrl: "https://example.invalid/l7805cv.pdf",
            datasheetChecksum: "sha256:feedface",
            timestamp: new DateTimeOffset(2026, 5, 31, 16, 0, 0, TimeSpan.Zero));

        MarketplaceComponentProvenance reviewed = imported.WithManualOverride(
            reviewState: MarketplaceReviewState.Approved,
            reviewerNote: "Matched to canonical 7805 regulator after package review.",
            timestamp: new DateTimeOffset(2026, 5, 31, 17, 0, 0, TimeSpan.Zero));

        Assert.Equal(MarketplaceProvenanceKind.ManualOverride, reviewed.Kind);
        Assert.Equal("Jameco", reviewed.SourceVendor);
        Assert.Equal("manual", reviewed.GeneratorName);
        Assert.Equal(MarketplaceReviewState.Approved, reviewed.ReviewState);
        Assert.Equal("Matched to canonical 7805 regulator after package review.", reviewed.ReviewerNote);
        Assert.Equal(new DateTimeOffset(2026, 5, 31, 17, 0, 0, TimeSpan.Zero), reviewed.Timestamp);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void DatasheetAssetsRequireChecksum(string checksum)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            MarketplaceComponentProvenance.DatasheetGenerated(
                CanonicalComponentKey.FromPartNumber("ESP32-DEVKITC"),
                sourceVendor: "SparkFun",
                productUrl: "https://www.sparkfun.com/products/13907",
                datasheetUrl: "https://example.invalid/esp32.pdf",
                datasheetChecksum: checksum,
                generatorName: "Ollama",
                reviewState: MarketplaceReviewState.PendingReview,
                timestamp: new DateTimeOffset(2026, 5, 31, 18, 0, 0, TimeSpan.Zero)));

        Assert.Contains("checksum", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AuditSummaryOrderingIsDeterministic()
    {
        MarketplaceComponentProvenance newer = MarketplaceComponentProvenance.VendorImport(
            CanonicalComponentKey.FromPartNumber("NE555P"),
            sourceVendor: "Mouser",
            productUrl: "https://www.mouser.com/example/ne555p",
            datasheetUrl: "https://example.invalid/ne555p.pdf",
            datasheetChecksum: "sha256:2222",
            timestamp: new DateTimeOffset(2026, 5, 31, 19, 0, 0, TimeSpan.Zero));
        MarketplaceComponentProvenance older = MarketplaceComponentProvenance.DatasheetGenerated(
            CanonicalComponentKey.FromPartNumber("LM7805CT"),
            sourceVendor: "Digi-Key",
            productUrl: "https://www.digikey.com/example/lm7805ct",
            datasheetUrl: "https://example.invalid/lm7805ct.pdf",
            datasheetChecksum: "sha256:1111",
            generatorName: "Codex",
            reviewState: MarketplaceReviewState.PendingReview,
            timestamp: new DateTimeOffset(2026, 5, 31, 18, 0, 0, TimeSpan.Zero));

        MarketplaceProvenanceAudit audit = MarketplaceProvenanceAudit.Create([newer, older]);

        Assert.Equal(
            [
                "PART:LM7805CT | DatasheetGenerated | Digi-Key | Codex | PendingReview | 2026-05-31T18:00:00.0000000+00:00",
                "PART:NE555P | VendorImport | Mouser | manual | Imported | 2026-05-31T19:00:00.0000000+00:00",
            ],
            audit.SummaryLines);
        Assert.Equal(string.Join(Environment.NewLine, audit.SummaryLines), audit.ToString());
    }
}
