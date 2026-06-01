using DragonCAD.Sourcing;
using DragonCAD.Sourcing.Bom;
using DragonCAD.Sourcing.Catalog;

namespace DragonCAD.Sourcing.Tests.Bom;

public sealed class BomCostRollupCalculatorTests
{
    [Fact]
    public void RollUpSelectsBestProviderOfferForEachComponentAndSummarizesSelectedProviders()
    {
        var components = new[]
        {
            new BomComponentQuantity("R1,R2", "RC0603FR-0710KL", quantity: 12),
            new BomComponentQuantity("C1", "CL10B104KB8NNNC", quantity: 5),
        };
        var listings = new[]
        {
            Listing("Mouser", "MOU-R-1", "RC0603FR-0710KL", stockQuantity: 1_000, [(1, 0.06m), (10, 0.04m)]),
            Listing("Digi-Key", "DK-R-1", "rc0603fr-0710kl", stockQuantity: 1_000, [(1, 0.05m), (10, 0.03m)]),
            Listing("Jameco", "JAM-C-1", "CL10B104KB8NNNC", stockQuantity: 5, [(1, 0.10m)]),
            Listing("Mouser", "MOU-C-1", "CL10B104KB8NNNC", stockQuantity: 50, [(1, 0.12m), (5, 0.08m)]),
        };

        var rollup = BomCostRollupCalculator.RollUp(components, listings);

        Assert.True(rollup.IsComplete);
        Assert.Empty(rollup.Diagnostics);
        Assert.Equal(Money.Usd(0.76m), rollup.TotalEstimatedCost);
        Assert.Equal(["R1,R2", "C1"], rollup.Lines.Select(line => line.Component.Reference));
        Assert.Equal(["Digi-Key", "Mouser"], rollup.Lines.Select(line => line.SelectedOffer?.ProviderName));
        Assert.Equal([10, 5], rollup.Lines.Select(line => line.SelectedOffer?.SelectedPriceBreakQuantity));
        Assert.Equal([Money.Usd(0.36m), Money.Usd(0.40m)], rollup.Lines.Select(line => line.SelectedOffer?.ExtendedCost));

        var resistorLine = rollup.Lines[0];
        Assert.Equal(["Digi-Key", "Mouser"], resistorLine.ProviderOffers.Select(offer => offer.ProviderName));
        Assert.Equal([Money.Usd(0.36m), Money.Usd(0.48m)], resistorLine.ProviderOffers.Select(offer => offer.ExtendedCost));

        Assert.Equal(["Digi-Key", "Mouser"], rollup.ProviderSummaries.Select(summary => summary.ProviderName));
        Assert.Equal([1, 1], rollup.ProviderSummaries.Select(summary => summary.SelectedLineCount));
        Assert.Equal([Money.Usd(0.36m), Money.Usd(0.40m)], rollup.ProviderSummaries.Select(summary => summary.TotalEstimatedCost));
    }

    [Fact]
    public void RollUpReportsMissingSourceDiagnosticsWithoutBlockingPricedComponents()
    {
        var components = new[]
        {
            new BomComponentQuantity("U1", "LM7805CT", quantity: 2),
            new BomComponentQuantity("J1", "USB-C-16P", quantity: 4),
        };
        var listings = new[]
        {
            Listing("Jameco", "JAM-U-1", "LM7805CT", stockQuantity: 20, [(1, 0.45m)]),
        };

        var rollup = BomCostRollupCalculator.RollUp(components, listings);

        Assert.False(rollup.IsComplete);
        Assert.Equal(Money.Usd(0.90m), rollup.TotalEstimatedCost);
        Assert.Equal(["U1", "J1"], rollup.Lines.Select(line => line.Component.Reference));
        Assert.NotNull(rollup.Lines[0].SelectedOffer);
        Assert.Null(rollup.Lines[1].SelectedOffer);

        var diagnostic = Assert.Single(rollup.Diagnostics);
        Assert.Equal(BomCostRollupDiagnosticCode.MissingCatalogSource, diagnostic.Code);
        Assert.Equal("J1", diagnostic.Reference);
        Assert.Equal("USB-C-16P", diagnostic.ManufacturerPartNumber);
        Assert.Equal(4, diagnostic.RequiredQuantity);
        Assert.Contains("No normalized catalog listing", diagnostic.Message);

        var summary = Assert.Single(rollup.ProviderSummaries);
        Assert.Equal("Jameco", summary.ProviderName);
        Assert.Equal(1, summary.SelectedLineCount);
        Assert.Equal(Money.Usd(0.90m), summary.TotalEstimatedCost);
    }

    [Fact]
    public void RollUpUsesDeterministicTieBreaksForEquivalentProviderOffers()
    {
        var components = new[]
        {
            new BomComponentQuantity("R1", "RC0603FR-0710KL", quantity: 10),
        };
        var listings = new[]
        {
            Listing("Mouser", "MOU-Z", "RC0603FR-0710KL", stockQuantity: 100, [(1, 0.04m)]),
            Listing("Digi-Key", "DK-A", "RC0603FR-0710KL", stockQuantity: 100, [(1, 0.04m)]),
            Listing("Digi-Key", "DK-B", "RC0603FR-0710KL", stockQuantity: 100, [(1, 0.04m)]),
        };

        var rollup = BomCostRollupCalculator.RollUp(components, listings);

        var line = Assert.Single(rollup.Lines);
        Assert.Equal("Digi-Key", line.SelectedOffer?.ProviderName);
        Assert.Equal("DK-A", line.SelectedOffer?.VendorSku);
        Assert.Equal(["Digi-Key", "Mouser"], line.ProviderOffers.Select(offer => offer.ProviderName));
    }

    private static NormalizedCatalogListing Listing(
        string providerName,
        string vendorSku,
        string manufacturerPartNumber,
        int? stockQuantity,
        IEnumerable<(int Quantity, decimal UnitPrice)> priceBreaks)
    {
        return new NormalizedCatalogListing(
            providerName,
            vendorSku,
            manufacturerPartNumber,
            manufacturer: "Yageo",
            description: "Test component",
            PriceLadder.Normalize(priceBreaks.Select(priceBreak => new QuantityPriceBreak(
                priceBreak.Quantity,
                Money.Usd(priceBreak.UnitPrice)))),
            stockQuantity,
            datasheetUrl: null,
            productUrl: null,
            fields: new Dictionary<string, string>(),
            CatalogProviderCapabilities.Feed);
    }
}
