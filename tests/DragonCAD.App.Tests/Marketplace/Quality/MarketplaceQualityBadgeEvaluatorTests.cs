using DragonCAD.App.Marketplace;
using DragonCAD.App.Marketplace.Quality;
using DragonCAD.Core.Components.Marketplace;
using DragonCAD.Core.Components.Marketplace.Provenance;

namespace DragonCAD.App.Tests.Marketplace.Quality;

public sealed class MarketplaceQualityBadgeEvaluatorTests
{
    [Fact]
    public void CleanCanonicalStockedDatasheetBackedRowHasNoQualityBadges()
    {
        MarketplaceComponentRow row = Row(datasheetUrl: "https://example.test/ne555.pdf");

        IReadOnlyList<MarketplaceQualityBadge> badges = MarketplaceQualityBadgeEvaluator.Evaluate(row, []);

        Assert.Empty(badges);
    }

    [Fact]
    public void MissingDatasheetProducesWarningBadge()
    {
        MarketplaceComponentRow row = Row(datasheetUrl: "");

        MarketplaceQualityBadge badge = Assert.Single(MarketplaceQualityBadgeEvaluator.Evaluate(row, []));

        Assert.Equal(MarketplaceQualityBadgeCode.DatasheetMissing, badge.Code);
        Assert.Equal(MarketplaceQualitySeverity.Warning, badge.Severity);
        Assert.Equal("Datasheet missing", badge.Label);
    }

    [Fact]
    public void DuplicateCanonicalLinkProducesInfoBadge()
    {
        MarketplaceComponentRow row = Row(
            canonicalComponentId: "dragon:lm7805",
            duplicateOfComponentId: "dragon:lm7805",
            datasheetUrl: "https://example.test/lm7805.pdf");

        MarketplaceQualityBadge badge = Assert.Single(MarketplaceQualityBadgeEvaluator.Evaluate(row, []));

        Assert.Equal(MarketplaceQualityBadgeCode.DuplicateCanonicalLink, badge.Code);
        Assert.Equal(MarketplaceQualitySeverity.Info, badge.Severity);
        Assert.Equal("Duplicate linked to dragon:lm7805", badge.Label);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void LowOrMissingStockProducesStockRiskBadge(int stockQuantity)
    {
        MarketplaceComponentRow row = Row(
            datasheetUrl: "https://example.test/ne555.pdf",
            stockQuantity: stockQuantity);

        MarketplaceQualityBadge badge = Assert.Single(MarketplaceQualityBadgeEvaluator.Evaluate(row, []));

        Assert.Equal(MarketplaceQualityBadgeCode.StockRisk, badge.Code);
        Assert.Equal(MarketplaceQualitySeverity.Warning, badge.Severity);
        Assert.Equal(stockQuantity <= 0 ? "Out of stock" : "Low stock", badge.Label);
    }

    [Fact]
    public void MissingPriceProducesPriceUnavailableBadge()
    {
        MarketplaceComponentRow row = Row(
            datasheetUrl: "https://example.test/ne555.pdf",
            minimumUnitPriceUsd: null);

        MarketplaceQualityBadge badge = Assert.Single(MarketplaceQualityBadgeEvaluator.Evaluate(row, []));

        Assert.Equal(MarketplaceQualityBadgeCode.PriceUnavailable, badge.Code);
        Assert.Equal(MarketplaceQualitySeverity.Warning, badge.Severity);
        Assert.Equal("Price unavailable", badge.Label);
    }

    [Fact]
    public void GeneratedPendingReviewProvenanceProducesCriticalReviewBadge()
    {
        MarketplaceComponentRow row = Row(datasheetUrl: "https://example.test/ne555.pdf");
        MarketplaceComponentProvenance provenance = MarketplaceComponentProvenance.DatasheetGenerated(
            new CanonicalComponentKey(row.CanonicalComponentId),
            sourceVendor: "Digi-Key",
            productUrl: "https://example.test/product",
            datasheetUrl: "https://example.test/ne555.pdf",
            datasheetChecksum: "sha256:abc123",
            generatorName: "Codex",
            MarketplaceReviewState.PendingReview,
            new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero));

        MarketplaceQualityBadge badge = Assert.Single(MarketplaceQualityBadgeEvaluator.Evaluate(row, [provenance]));

        Assert.Equal(MarketplaceQualityBadgeCode.GeneratedNeedsReview, badge.Code);
        Assert.Equal(MarketplaceQualitySeverity.Critical, badge.Severity);
        Assert.Equal("Generated component needs review", badge.Label);
    }

    [Fact]
    public void VendorImportProvenanceProducesTrustedVendorImportBadge()
    {
        MarketplaceComponentRow row = Row(datasheetUrl: "https://example.test/ne555.pdf");
        MarketplaceComponentProvenance provenance = MarketplaceComponentProvenance.VendorImport(
            new CanonicalComponentKey(row.CanonicalComponentId),
            sourceVendor: "Mouser",
            productUrl: "https://example.test/product",
            datasheetUrl: "https://example.test/ne555.pdf",
            datasheetChecksum: "sha256:def456",
            new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero));

        MarketplaceQualityBadge badge = Assert.Single(MarketplaceQualityBadgeEvaluator.Evaluate(row, [provenance]));

        Assert.Equal(MarketplaceQualityBadgeCode.TrustedVendorImport, badge.Code);
        Assert.Equal(MarketplaceQualitySeverity.Info, badge.Severity);
        Assert.Equal("Trusted vendor import", badge.Label);
    }

    [Fact]
    public void BadgesAreSeverityOrderedThenDeterministic()
    {
        MarketplaceComponentRow row = Row(
            canonicalComponentId: "dragon:lm7805",
            duplicateOfComponentId: "dragon:lm7805",
            datasheetUrl: "",
            stockQuantity: 0,
            minimumUnitPriceUsd: null);
        MarketplaceComponentProvenance generated = MarketplaceComponentProvenance.DatasheetGenerated(
            new CanonicalComponentKey(row.CanonicalComponentId),
            sourceVendor: "Digi-Key",
            productUrl: "https://example.test/generated",
            datasheetUrl: "https://example.test/generated.pdf",
            datasheetChecksum: "sha256:generated",
            generatorName: "Ollama",
            MarketplaceReviewState.PendingReview,
            new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero));
        MarketplaceComponentProvenance trusted = MarketplaceComponentProvenance.VendorImport(
            new CanonicalComponentKey(row.CanonicalComponentId),
            sourceVendor: "Mouser",
            productUrl: "https://example.test/vendor",
            datasheetUrl: "https://example.test/vendor.pdf",
            datasheetChecksum: "sha256:vendor",
            new DateTimeOffset(2026, 5, 31, 13, 0, 0, TimeSpan.Zero));

        IReadOnlyList<MarketplaceQualityBadge> badges = MarketplaceQualityBadgeEvaluator.Evaluate(row, [trusted, generated]);

        Assert.Equal(
            [
                MarketplaceQualityBadgeCode.GeneratedNeedsReview,
                MarketplaceQualityBadgeCode.DatasheetMissing,
                MarketplaceQualityBadgeCode.StockRisk,
                MarketplaceQualityBadgeCode.PriceUnavailable,
                MarketplaceQualityBadgeCode.DuplicateCanonicalLink,
                MarketplaceQualityBadgeCode.TrustedVendorImport
            ],
            badges.Select(badge => badge.Code));
    }

    private static MarketplaceComponentRow Row(
        string canonicalComponentId = "dragon:ne555",
        string duplicateOfComponentId = "",
        string datasheetUrl = "https://example.test/ne555.pdf",
        int stockQuantity = 100,
        decimal? minimumUnitPriceUsd = 0.19m) =>
        new(
            Provider: "Digi-Key",
            Category: "Timer",
            DisplayName: "NE555 Timer",
            Manufacturer: "Texas Instruments",
            ManufacturerPartNumber: "NE555P",
            CanonicalComponentId: canonicalComponentId,
            DuplicateOfComponentId: duplicateOfComponentId,
            DatasheetUrl: datasheetUrl,
            StockQuantity: stockQuantity,
            MinimumUnitPriceUsd: minimumUnitPriceUsd);
}
