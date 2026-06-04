using DragonCAD.Sourcing;
using DragonCAD.Sourcing.QuoteComparison;

namespace DragonCAD.Sourcing.Tests.QuoteComparison;

public sealed class QuoteLadderComparerTests
{
    private static readonly DateOnly EvaluationDate = new(2026, 6, 3);

    [Fact]
    public void CompareSelectsApplicablePriceBreakForRequestedQuantityTier()
    {
        var offer = Offer(
            "Digi-Key",
            quantityAvailable: 500,
            priceBreaks:
            [
                Break(1, 0.20m),
                Break(100, 0.12m),
                Break(250, 0.09m),
            ]);

        var comparison = QuoteLadderComparer.Compare(Request(125, offer));

        var result = Assert.Single(comparison.Results);
        Assert.Equal(100, result.SelectedPriceBreakQuantity);
        Assert.Equal(125, result.PurchaseQuantity);
        Assert.Equal(Money.Usd(0.12m), result.UnitPrice);
        Assert.Equal(Money.Usd(15.00m), result.ExtendedCost);
        Assert.Empty(comparison.Diagnostics);
    }

    [Fact]
    public void CompareUsesMoqPurchaseQuantityAndReportsMismatch()
    {
        var offer = Offer(
            "Mouser",
            quantityAvailable: 250,
            minimumOrderQuantity: 25,
            priceBreaks: [Break(1, 0.40m), Break(25, 0.32m)]);

        var comparison = QuoteLadderComparer.Compare(Request(10, offer));

        var result = Assert.Single(comparison.Results);
        Assert.Equal(25, result.PurchaseQuantity);
        Assert.Equal(25, result.SelectedPriceBreakQuantity);
        Assert.Equal(Money.Usd(8.00m), result.ExtendedCost);
        Assert.Contains(
            comparison.Diagnostics,
            diagnostic => diagnostic.Code == QuoteComparisonDiagnosticCode.MoqMismatch
                && diagnostic.VendorName == "Mouser");
    }

    [Fact]
    public void CompareReportsOutOfStockAndInsufficientStock()
    {
        var outOfStock = Offer("BackorderVendor", quantityAvailable: 0, priceBreaks: [Break(1, 0.05m)]);
        var partialStock = Offer("PartialVendor", quantityAvailable: 40, priceBreaks: [Break(1, 0.06m)]);

        var comparison = QuoteLadderComparer.Compare(Request(100, outOfStock, partialStock));

        Assert.Contains(
            comparison.Diagnostics,
            diagnostic => diagnostic.Code == QuoteComparisonDiagnosticCode.InsufficientStock
                && diagnostic.VendorName == "BackorderVendor");
        Assert.Contains(
            comparison.Diagnostics,
            diagnostic => diagnostic.Code == QuoteComparisonDiagnosticCode.InsufficientStock
                && diagnostic.VendorName == "PartialVendor");
    }

    [Fact]
    public void CompareReportsStaleQuoteAndLifecycleWarning()
    {
        var stale = Offer(
            "StaleVendor",
            quantityAvailable: 100,
            lastUpdated: EvaluationDate.AddDays(-91),
            priceBreaks: [Break(1, 0.10m)]);
        var lifecycleWarning = Offer(
            "LifecycleVendor",
            quantityAvailable: 100,
            lifecycleRisk: QuoteLifecycleRisk.NotRecommendedForNewDesigns,
            priceBreaks: [Break(1, 0.11m)]);

        var comparison = QuoteLadderComparer.Compare(Request(50, stale, lifecycleWarning));

        Assert.Contains(
            comparison.Diagnostics,
            diagnostic => diagnostic.Code == QuoteComparisonDiagnosticCode.StaleOffer
                && diagnostic.VendorName == "StaleVendor");
        Assert.Contains(
            comparison.Diagnostics,
            diagnostic => diagnostic.Code == QuoteComparisonDiagnosticCode.LifecycleWarning
                && diagnostic.VendorName == "LifecycleVendor");
    }

    [Fact]
    public void CompareReportsMissingPriceBreakWhenNoTierApplies()
    {
        var offer = Offer("BulkOnlyVendor", quantityAvailable: 100, priceBreaks: [Break(50, 0.02m)]);

        var comparison = QuoteLadderComparer.Compare(Request(10, offer));

        Assert.Empty(comparison.Results);
        Assert.Contains(
            comparison.Diagnostics,
            diagnostic => diagnostic.Code == QuoteComparisonDiagnosticCode.MissingPriceBreak
                && diagnostic.VendorName == "BulkOnlyVendor");
    }

    [Fact]
    public void CompareSortsByExtendedCostAvailabilityLifecycleRiskPreferredVendorAndStableVendorKeys()
    {
        var offers = new[]
        {
            Offer("Beta", "B-2", quantityAvailable: 100, priceBreaks: [Break(1, 0.08m)]),
            Offer("Preferred", "P-1", quantityAvailable: 100, preferredVendor: true, priceBreaks: [Break(1, 0.10m)]),
            Offer("NonPreferred", "N-1", quantityAvailable: 100, priceBreaks: [Break(1, 0.10m)]),
            Offer("Partial", "PA-1", quantityAvailable: 50, priceBreaks: [Break(1, 0.08m)]),
            Offer("LifecycleRisk", "L-1", quantityAvailable: 100, lifecycleRisk: QuoteLifecycleRisk.Obsolete, priceBreaks: [Break(1, 0.10m)]),
            Offer("Alpha", "A-1", quantityAvailable: 100, priceBreaks: [Break(1, 0.08m)]),
        };

        var comparison = QuoteLadderComparer.Compare(Request(100, offers));

        Assert.Equal(
            ["Alpha", "Beta", "Partial", "Preferred", "NonPreferred", "LifecycleRisk"],
            comparison.Results.Select(result => result.Offer.VendorName));
    }

    private static QuoteLadderComparisonRequest Request(int requestedBuildQuantity, params QuoteLadderOffer[] offers)
    {
        return new QuoteLadderComparisonRequest(
            ManufacturerPartNumber: "LM7805CT",
            RequestedBuildQuantity: requestedBuildQuantity,
            EvaluationDate: EvaluationDate,
            Offers: offers);
    }

    private static QuoteLadderOffer Offer(
        string vendorName,
        int quantityAvailable,
        IReadOnlyList<QuantityPriceBreak> priceBreaks,
        int minimumOrderQuantity = 1,
        DateOnly? lastUpdated = null,
        QuoteLifecycleRisk lifecycleRisk = QuoteLifecycleRisk.Active,
        bool preferredVendor = false)
    {
        return Offer(
            vendorName,
            $"{vendorName}-LM7805",
            quantityAvailable,
            priceBreaks,
            minimumOrderQuantity,
            lastUpdated,
            lifecycleRisk,
            preferredVendor);
    }

    private static QuoteLadderOffer Offer(
        string vendorName,
        string vendorPartNumber,
        int quantityAvailable,
        IReadOnlyList<QuantityPriceBreak> priceBreaks,
        int minimumOrderQuantity = 1,
        DateOnly? lastUpdated = null,
        QuoteLifecycleRisk lifecycleRisk = QuoteLifecycleRisk.Active,
        bool preferredVendor = false)
    {
        return new QuoteLadderOffer(
            VendorName: vendorName,
            VendorPartNumber: vendorPartNumber,
            ManufacturerPartNumber: "LM7805CT",
            QuantityAvailable: quantityAvailable,
            MinimumOrderQuantity: minimumOrderQuantity,
            PriceBreaks: priceBreaks,
            LastUpdated: lastUpdated ?? EvaluationDate,
            LifecycleRisk: lifecycleRisk,
            IsPreferredVendor: preferredVendor);
    }

    private static QuantityPriceBreak Break(int quantity, decimal unitPrice)
    {
        return new QuantityPriceBreak(quantity, Money.Usd(unitPrice));
    }
}
