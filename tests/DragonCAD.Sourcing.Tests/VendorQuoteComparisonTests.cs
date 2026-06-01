using DragonCAD.Sourcing;

namespace DragonCAD.Sourcing.Tests;

public sealed class VendorQuoteComparisonTests
{
    [Fact]
    public void EvaluateCalculatesExtendedCostFromBestPriceBreakForBuildQuantity()
    {
        var offer = new VendorQuoteOffer(
            Quote("Digi-Key", quantityAvailable: 500, minimumOrderQuantity: 1),
            PriceLadder.Normalize(
            [
                new QuantityPriceBreak(1, Money.Usd(0.20m)),
                new QuantityPriceBreak(100, Money.Usd(0.12m)),
            ]));

        var evaluated = VendorQuoteComparison.Evaluate(offer, requestedBuildQuantity: 125);

        Assert.Equal(125, evaluated.RequestedBuildQuantity);
        Assert.Equal(125, evaluated.PurchaseQuantity);
        Assert.True(evaluated.IsFullyAvailable);
        Assert.Equal(Money.Usd(0.12m), evaluated.UnitPrice);
        Assert.Equal(Money.Usd(15.00m), evaluated.ExtendedCost);
    }

    [Fact]
    public void EvaluateHonorsMinimumOrderQuantityWhenCalculatingPurchaseQuantity()
    {
        var offer = new VendorQuoteOffer(
            Quote("Mouser", quantityAvailable: 500, minimumOrderQuantity: 25),
            PriceLadder.Normalize([new QuantityPriceBreak(1, Money.Usd(0.40m))]));

        var evaluated = VendorQuoteComparison.Evaluate(offer, requestedBuildQuantity: 10);

        Assert.Equal(25, evaluated.PurchaseQuantity);
        Assert.Equal(Money.Usd(10.00m), evaluated.ExtendedCost);
    }

    [Fact]
    public void SortBestFirstPrioritizesFullyAvailableStockThenAvailableQuantityThenTotalCost()
    {
        var offers = new[]
        {
            Offer("LowCostBackorder", quantityAvailable: 0, unitPrice: 0.01m),
            Offer("PartialStock", quantityAvailable: 75, unitPrice: 0.02m),
            Offer("ExpensiveFullStock", quantityAvailable: 200, unitPrice: 0.20m),
            Offer("CheapFullStock", quantityAvailable: 125, unitPrice: 0.10m),
            Offer("DeepStock", quantityAvailable: 250, unitPrice: 0.15m),
        };

        var sorted = VendorQuoteComparison.SortBestFirst(offers, requestedBuildQuantity: 100).ToArray();

        Assert.Equal(
            ["CheapFullStock", "DeepStock", "ExpensiveFullStock", "PartialStock", "LowCostBackorder"],
            sorted.Select(evaluated => evaluated.Quote.VendorName));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void EvaluateRejectsNonPositiveBuildQuantities(int requestedBuildQuantity)
    {
        var offer = Offer("Digi-Key", quantityAvailable: 10, unitPrice: 0.10m);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => VendorQuoteComparison.Evaluate(offer, requestedBuildQuantity));

        Assert.Contains("Requested build quantity must be greater than zero.", exception.Message);
    }

    private static VendorQuoteOffer Offer(string vendorName, int quantityAvailable, decimal unitPrice)
    {
        return new VendorQuoteOffer(
            Quote(vendorName, quantityAvailable, minimumOrderQuantity: 1),
            PriceLadder.Normalize([new QuantityPriceBreak(1, Money.Usd(unitPrice))]));
    }

    private static NormalizedVendorQuote Quote(string vendorName, int quantityAvailable, int minimumOrderQuantity)
    {
        return new NormalizedVendorQuote(
            VendorName: vendorName,
            VendorPartNumber: $"{vendorName}-LM7805",
            ManufacturerPartNumber: "LM7805CT",
            UnitPrice: Money.Usd(0.99m),
            QuantityAvailable: quantityAvailable,
            MinimumOrderQuantity: minimumOrderQuantity,
            LeadTimeDays: quantityAvailable > 0 ? 0 : 21);
    }
}
