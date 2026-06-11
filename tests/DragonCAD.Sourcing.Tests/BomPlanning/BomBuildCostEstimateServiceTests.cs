using DragonCAD.Sourcing;
using DragonCAD.Sourcing.BomPlanning;

namespace DragonCAD.Sourcing.Tests.BomPlanning;

public sealed class BomBuildCostEstimateServiceTests
{
    private static readonly DateTimeOffset EstimateTimestamp = new(2026, 6, 10, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void EstimateCalculatesSingleAndMultiBuildTotalsFromPriceBreaks()
    {
        var estimate = BomBuildCostEstimateService.Estimate(
            [Component("R1", quantityPerBuild: 2, selectedPart: "RC0603FR-0710KL")],
            [
                Quote(
                    "Digi-Key",
                    "DK-R-10K",
                    "RC0603FR-0710KL",
                    stock: 1000,
                    priceBreaks:
                    [
                        new QuantityPriceBreak(1, Money.Usd(0.10m)),
                        new QuantityPriceBreak(100, Money.Usd(0.03m)),
                    ]),
            ],
            [1, 100],
            Options());

        Assert.Equal(Money.Usd(0.20m), estimate.Scenarios.Single(scenario => scenario.BuildQuantity == 1).ExtendedBuildTotal);
        Assert.Equal(Money.Usd(6.00m), estimate.Scenarios.Single(scenario => scenario.BuildQuantity == 100).ExtendedBuildTotal);
        Assert.Equal("$6.00", estimate.Scenarios.Single(scenario => scenario.BuildQuantity == 100).FormattedExtendedBuildTotal);
    }

    [Fact]
    public void EstimateAccountsForMoqAndReportsPriceBreakDiagnostics()
    {
        var estimate = BomBuildCostEstimateService.Estimate(
            [Component("U1", "timer", "555", "DIP-8", quantityPerBuild: 1, selectedPart: "NE555P")],
            [
                Quote(
                    "Jameco",
                    "JAM-555",
                    "NE555P",
                    stock: 100,
                    unitPrice: 0.25m,
                    minimumOrderQuantity: 10,
                    orderMultiple: 6,
                    canonicalIdentity: "timer",
                    selectedValue: "555",
                    package: "DIP-8"),
            ],
            [1],
            Options());

        var line = Assert.Single(estimate.Scenarios.Single().Lines);
        Assert.Equal(1, line.RequiredQuantity);
        Assert.Equal(12, line.PurchaseQuantity);
        Assert.Equal(Money.Usd(3.00m), line.LineTotal);
        Assert.Contains(line.Diagnostics, diagnostic => diagnostic.Code == "PriceBreakApplied");
    }

    [Fact]
    public void EstimateSelectsAvailableAlternateWhenPrimaryCannotFillBuild()
    {
        var estimate = BomBuildCostEstimateService.Estimate(
            [
                Component(
                    "C1",
                    "capacitor",
                    "100nF",
                    "0603",
                    quantityPerBuild: 1,
                    selectedPart: "CL10B104KB8NNNC",
                    alternates: ["C0603C104K5RACTU"]),
            ],
            [
                Quote("Mouser", "MOU-C-PRIMARY", "CL10B104KB8NNNC", stock: 0, unitPrice: 0.04m, canonicalIdentity: "capacitor", selectedValue: "100nF"),
                Quote("Digi-Key", "DK-C-ALT", "C0603C104K5RACTU", stock: 100, unitPrice: 0.05m, canonicalIdentity: "capacitor", selectedValue: "100nF"),
            ],
            [10],
            Options());

        var line = Assert.Single(estimate.Scenarios.Single().Lines);
        Assert.True(line.IsAlternate);
        Assert.Equal("C0603C104K5RACTU", line.ManufacturerPartNumber);
        Assert.Equal(Money.Usd(0.50m), line.LineTotal);
        Assert.Empty(estimate.UnavailableLines);
    }

    [Fact]
    public void EstimateReportsStaleLifecyclePreferredVendorAndShortageDiagnostics()
    {
        var estimate = BomBuildCostEstimateService.Estimate(
            [
                Component("U1", "module", "esp32", "devkit", quantityPerBuild: 1, selectedPart: "ESP32-DEVKITC-32E"),
                Component("U2", "regulator", "3v3", "SOT-223", quantityPerBuild: 1, selectedPart: "LD1117V33"),
            ],
            [
                Quote(
                    "Adafruit",
                    "ADA-ESP32",
                    "ESP32-DEVKITC-32E",
                    stock: 10,
                    unitPrice: 12.50m,
                    isPreferredVendor: true,
                    lifecycle: BomPartLifecycle.NotRecommendedForNewDesigns,
                    capturedAt: EstimateTimestamp.AddDays(-40),
                    canonicalIdentity: "module",
                    selectedValue: "esp32",
                    package: "devkit"),
                Quote(
                    "Digi-Key",
                    "DK-LDO",
                    "LD1117V33",
                    stock: 2,
                    unitPrice: 0.40m,
                    canonicalIdentity: "regulator",
                    selectedValue: "3v3",
                    package: "SOT-223"),
            ],
            [10],
            Options(maxQuoteAge: TimeSpan.FromDays(30)));

        var scenario = estimate.Scenarios.Single();
        var sourcedLine = Assert.Single(scenario.Lines);
        Assert.Equal("Preferred vendor selected: Adafruit.", sourcedLine.PreferredVendorNote);
        Assert.Contains(sourcedLine.Diagnostics, diagnostic => diagnostic.Code == "StaleQuote");
        Assert.Contains(sourcedLine.Diagnostics, diagnostic => diagnostic.Code == "LifecycleWarning");

        var unavailable = Assert.Single(estimate.UnavailableLines);
        Assert.Equal("regulator|3v3|sot-223", unavailable.GroupKey);
        Assert.Equal(10, unavailable.RequiredQuantity);
        Assert.Equal(2, unavailable.AvailableQuantity);
        Assert.Contains(estimate.Diagnostics, diagnostic => diagnostic.Code == "Shortage");
    }

    [Fact]
    public void EstimateProducesDeterministicOfferSelectionAndLineOrdering()
    {
        var first = BomBuildCostEstimateService.Estimate(
            [
                Component("C1", "capacitor", "100nF", "0603", quantityPerBuild: 1, selectedPart: "CAP-A"),
                Component("R1", "resistor", "10k", "0603", quantityPerBuild: 1, selectedPart: "RES-A"),
            ],
            [
                Quote("Vendor B", "VB-RES", "RES-A", stock: 10, unitPrice: 0.02m, canonicalIdentity: "resistor", selectedValue: "10k"),
                Quote("Vendor A", "VA-RES", "RES-A", stock: 10, unitPrice: 0.02m, canonicalIdentity: "resistor", selectedValue: "10k"),
                Quote("Vendor C", "VC-CAP", "CAP-A", stock: 10, unitPrice: 0.03m, canonicalIdentity: "capacitor", selectedValue: "100nF"),
            ],
            [1],
            Options());

        var second = BomBuildCostEstimateService.Estimate(
            [
                Component("R1", "resistor", "10k", "0603", quantityPerBuild: 1, selectedPart: "RES-A"),
                Component("C1", "capacitor", "100nF", "0603", quantityPerBuild: 1, selectedPart: "CAP-A"),
            ],
            [
                Quote("Vendor C", "VC-CAP", "CAP-A", stock: 10, unitPrice: 0.03m, canonicalIdentity: "capacitor", selectedValue: "100nF"),
                Quote("Vendor A", "VA-RES", "RES-A", stock: 10, unitPrice: 0.02m, canonicalIdentity: "resistor", selectedValue: "10k"),
                Quote("Vendor B", "VB-RES", "RES-A", stock: 10, unitPrice: 0.02m, canonicalIdentity: "resistor", selectedValue: "10k"),
            ],
            [1],
            Options());

        Assert.Equal(
            first.Scenarios.Single().Lines.Select(line => $"{line.GroupKey}:{line.VendorName}:{line.LineTotal.Amount}"),
            second.Scenarios.Single().Lines.Select(line => $"{line.GroupKey}:{line.VendorName}:{line.LineTotal.Amount}"));
        Assert.Equal("Vendor A", first.Scenarios.Single().Lines.Single(line => line.GroupKey == "resistor|10k|0603").VendorName);
    }

    private static BomBuildCostEstimateOptions Options(TimeSpan? maxQuoteAge = null)
    {
        return new BomBuildCostEstimateOptions("USD", "en-US", EstimateTimestamp, maxQuoteAge ?? TimeSpan.FromDays(30));
    }

    private static BomPlanningComponent Component(
        string designator,
        string canonicalIdentity = "resistor",
        string selectedValue = "10k",
        string package = "0603",
        int quantityPerBuild = 1,
        string selectedPart = "RC0603FR-0710KL",
        bool doNotSubstitute = false,
        IReadOnlyList<string>? alternates = null)
    {
        return new BomPlanningComponent(
            designator,
            canonicalIdentity,
            selectedValue,
            package,
            quantityPerBuild,
            selectedPart,
            doNotSubstitute,
            alternates ?? []);
    }

    private static BomBuildCostVendorQuote Quote(
        string vendor,
        string vendorPartNumber,
        string manufacturerPartNumber,
        int stock,
        decimal? unitPrice = null,
        int minimumOrderQuantity = 1,
        int orderMultiple = 1,
        int leadTimeDays = 0,
        BomPartLifecycle lifecycle = BomPartLifecycle.Active,
        bool isPreferredVendor = false,
        DateTimeOffset? capturedAt = null,
        IReadOnlyList<QuantityPriceBreak>? priceBreaks = null,
        string canonicalIdentity = "resistor",
        string selectedValue = "10k",
        string package = "0603")
    {
        return new BomBuildCostVendorQuote(
            canonicalIdentity,
            selectedValue,
            package,
            manufacturerPartNumber,
            vendor,
            vendorPartNumber,
            isPreferredVendor,
            stock,
            minimumOrderQuantity,
            orderMultiple,
            leadTimeDays,
            lifecycle,
            capturedAt ?? EstimateTimestamp,
            PriceLadder.Normalize(priceBreaks ?? [new QuantityPriceBreak(1, Money.Usd(unitPrice ?? 0.01m))]));
    }
}
