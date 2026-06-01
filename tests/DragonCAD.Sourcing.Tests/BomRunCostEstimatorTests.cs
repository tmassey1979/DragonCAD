using DragonCAD.Sourcing;

namespace DragonCAD.Sourcing.Tests;

public sealed class BomRunCostEstimatorTests
{
    [Fact]
    public void EstimateChoosesBestVendorQuoteForEachBomLineAndTotalsTheRunCost()
    {
        var bomLines = new[]
        {
            new SourcingBomLine("R1,R2", "RC0603FR-0710KL", quantityPerAssembly: 2),
            new SourcingBomLine("C1", "CL10B104KB8NNNC", quantityPerAssembly: 1),
        };
        var quotes = new[]
        {
            Offer("Mouser", "RC0603FR-0710KL", quantityAvailable: 500, unitPrice: 0.03m),
            Offer("Digi-Key", "RC0603FR-0710KL", quantityAvailable: 500, unitPrice: 0.02m),
            Offer("Digi-Key", "CL10B104KB8NNNC", quantityAvailable: 300, unitPrice: 0.04m),
        };

        var estimate = BomRunCostEstimator.Estimate(bomLines, quotes, buildQuantity: 25);

        Assert.True(estimate.IsComplete);
        Assert.Empty(estimate.MissingQuoteDiagnostics);
        Assert.Equal(Money.Usd(2.00m), estimate.TotalEstimatedCost);
        Assert.Equal(["Digi-Key", "Digi-Key"], estimate.Lines.Select(line => line.SelectedQuote.Quote.VendorName));
        Assert.Equal([50, 25], estimate.Lines.Select(line => line.RequiredQuantity));
    }

    [Fact]
    public void EstimateReportsMissingQuoteDiagnosticsWithoutBlockingPricedLines()
    {
        var bomLines = new[]
        {
            new SourcingBomLine("U1", "LM7805CT", quantityPerAssembly: 1),
            new SourcingBomLine("J1", "USB-C-16P", quantityPerAssembly: 1),
        };
        var quotes = new[]
        {
            Offer("Jameco", "LM7805CT", quantityAvailable: 25, unitPrice: 0.40m),
        };

        var estimate = BomRunCostEstimator.Estimate(bomLines, quotes, buildQuantity: 10);

        Assert.False(estimate.IsComplete);
        Assert.Equal(Money.Usd(4.00m), estimate.TotalEstimatedCost);
        Assert.Single(estimate.Lines);
        var diagnostic = Assert.Single(estimate.MissingQuoteDiagnostics);
        Assert.Equal("J1", diagnostic.BomLineId);
        Assert.Equal("USB-C-16P", diagnostic.ManufacturerPartNumber);
        Assert.Equal(10, diagnostic.RequiredQuantity);
        Assert.Contains("No vendor quote", diagnostic.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void EstimateRejectsNonPositiveBuildQuantities(int buildQuantity)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            BomRunCostEstimator.Estimate([], [], buildQuantity));

        Assert.Contains("Build quantity must be greater than zero.", exception.Message);
    }

    private static VendorQuoteOffer Offer(
        string vendorName,
        string manufacturerPartNumber,
        int quantityAvailable,
        decimal unitPrice)
    {
        return new VendorQuoteOffer(
            new NormalizedVendorQuote(
                VendorName: vendorName,
                VendorPartNumber: $"{vendorName}-{manufacturerPartNumber}",
                ManufacturerPartNumber: manufacturerPartNumber,
                UnitPrice: Money.Usd(unitPrice),
                QuantityAvailable: quantityAvailable,
                MinimumOrderQuantity: 1,
                LeadTimeDays: quantityAvailable > 0 ? 0 : 14),
            PriceLadder.Normalize([new QuantityPriceBreak(1, Money.Usd(unitPrice))]));
    }
}
