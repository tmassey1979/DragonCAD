using System.Text.Json;
using DragonCAD.Sourcing;
using DragonCAD.Sourcing.BomPlanning;

namespace DragonCAD.Sourcing.Tests.BomPlanning;

public sealed class BomPurchasePlannerTests
{
    private static readonly DateTimeOffset PlanTimestamp = new(2026, 6, 3, 14, 30, 0, TimeSpan.Zero);

    [Fact]
    public void PlanGroupsComponentsByCanonicalIdentityValueAndPackage()
    {
        var plan = BomPurchasePlanner.Plan(
            [
                Component("R1", "resistor", "10k", "0603", quantityPerBuild: 1, selectedPart: "RC0603FR-0710KL"),
                Component("R2", " RESISTOR ", "10K", "0603", quantityPerBuild: 1, selectedPart: "RC0603FR-0710KL"),
            ],
            [
                Quote("Digi-Key", "DK-R", "RC0603FR-0710KL", stock: 100, unitPrice: 0.02m),
            ],
            [new BomBuildScenario("per-build", 1)],
            "USD",
            PlanTimestamp);

        var group = Assert.Single(plan.Groups);
        Assert.Equal("resistor", group.CanonicalIdentity);
        Assert.Equal("10k", group.SelectedValue);
        Assert.Equal("0603", group.Package);
        Assert.Equal(2, group.QuantityPerBuild);
        Assert.Equal(["R1", "R2"], group.Designators);
    }

    [Fact]
    public void PlanCalculatesOneOffPrototypeAndProductionTotalsWithPriceBreaks()
    {
        var plan = BomPurchasePlanner.Plan(
            [Component("R1", "resistor", "10k", "0603", quantityPerBuild: 2, selectedPart: "RC0603FR-0710KL")],
            [
                Quote(
                    "Digi-Key",
                    "DK-R",
                    "RC0603FR-0710KL",
                    stock: 1000,
                    priceBreaks:
                    [
                        new QuantityPriceBreak(1, Money.Usd(0.10m)),
                        new QuantityPriceBreak(100, Money.Usd(0.03m)),
                    ]),
            ],
            [
                new BomBuildScenario("per-build", 1),
                new BomBuildScenario("prototype-10", 10),
                new BomBuildScenario("production-100", 100),
            ],
            "USD",
            PlanTimestamp);

        Assert.Equal(Money.Usd(0.20m), plan.Scenarios.Single(scenario => scenario.Name == "per-build").TotalCost);
        Assert.Equal(Money.Usd(2.00m), plan.Scenarios.Single(scenario => scenario.Name == "prototype-10").TotalCost);
        Assert.Equal(Money.Usd(6.00m), plan.Scenarios.Single(scenario => scenario.Name == "production-100").TotalCost);
    }

    [Fact]
    public void PlanRoundsPurchasesForMoqAndOrderMultiple()
    {
        var plan = BomPurchasePlanner.Plan(
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
            [new BomBuildScenario("per-build", 1)],
            "USD",
            PlanTimestamp);

        var line = plan.Scenarios.Single().PurchaseLines.Single();
        Assert.Equal(1, line.RequiredQuantity);
        Assert.Equal(12, line.PurchaseQuantity);
        Assert.Equal(Money.Usd(3.00m), line.ExtendedCost);
    }

    [Fact]
    public void PlanReportsMissingStockWithoutInventingPurchases()
    {
        var plan = BomPurchasePlanner.Plan(
            [Component("U1", "module", "esp32", "devkit", quantityPerBuild: 1, selectedPart: "ESP32-DEVKITC-32E")],
            [
                Quote(
                    "Adafruit",
                    "ADA-ESP32",
                    "ESP32-DEVKITC-32E",
                    stock: 2,
                    unitPrice: 12.50m,
                    canonicalIdentity: "module",
                    selectedValue: "esp32",
                    package: "devkit"),
            ],
            [new BomBuildScenario("prototype-10", 10)],
            "USD",
            PlanTimestamp);

        Assert.False(plan.IsComplete);
        Assert.Equal(2, plan.Scenarios.Single().PurchaseLines.Single().RequiredQuantity);
        var diagnostic = Assert.Single(plan.Diagnostics);
        Assert.Equal("MissingStock", diagnostic.Code);
        Assert.Equal("module|esp32|devkit", diagnostic.GroupKey);
        Assert.Equal(8, diagnostic.UnfilledQuantity);
    }

    [Fact]
    public void PlanUsesAlternateSubstitutionWhenSelectedPartCannotFillDemand()
    {
        var plan = BomPurchasePlanner.Plan(
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
            [new BomBuildScenario("prototype-10", 10)],
            "USD",
            PlanTimestamp);

        var line = plan.Scenarios.Single().PurchaseLines.Single();
        Assert.True(line.IsSubstitution);
        Assert.Equal("C0603C104K5RACTU", line.ManufacturerPartNumber);
        Assert.Equal(Money.Usd(0.50m), plan.Scenarios.Single().TotalCost);
        Assert.Empty(plan.Diagnostics);
    }

    [Fact]
    public void PlanHonorsDoNotSubstituteEvenWhenAlternateHasStock()
    {
        var plan = BomPurchasePlanner.Plan(
            [
                Component(
                    "C1",
                    "capacitor",
                    "100nF",
                    "0603",
                    quantityPerBuild: 1,
                    selectedPart: "CL10B104KB8NNNC",
                    doNotSubstitute: true,
                    alternates: ["C0603C104K5RACTU"]),
            ],
            [
                Quote("Mouser", "MOU-C-PRIMARY", "CL10B104KB8NNNC", stock: 0, unitPrice: 0.04m, canonicalIdentity: "capacitor", selectedValue: "100nF"),
                Quote("Digi-Key", "DK-C-ALT", "C0603C104K5RACTU", stock: 100, unitPrice: 0.05m, canonicalIdentity: "capacitor", selectedValue: "100nF"),
            ],
            [new BomBuildScenario("prototype-10", 10)],
            "USD",
            PlanTimestamp);

        Assert.Empty(plan.Scenarios.Single().PurchaseLines);
        Assert.Equal(10, Assert.Single(plan.Diagnostics).UnfilledQuantity);
    }

    [Fact]
    public void PlanPrefersPreferredVendorWhenCostIsOtherwiseComparable()
    {
        var plan = BomPurchasePlanner.Plan(
            [Component("R1", "resistor", "10k", "0603", quantityPerBuild: 1, selectedPart: "RC0603FR-0710KL")],
            [
                Quote("Mouser", "MOU-R", "RC0603FR-0710KL", stock: 100, unitPrice: 0.02m),
                Quote("Digi-Key", "DK-R", "RC0603FR-0710KL", stock: 100, unitPrice: 0.02m, isPreferredVendor: true),
            ],
            [new BomBuildScenario("per-build", 1)],
            "USD",
            PlanTimestamp);

        Assert.Equal("Digi-Key", plan.Scenarios.Single().PurchaseLines.Single().VendorName);
    }

    [Fact]
    public void ExportProducesBomCsvAndReviewableOrderPlanJsonWithExplicitCurrencyAndTimestamp()
    {
        var plan = BomPurchasePlanner.Plan(
            [Component("R1", "resistor", "10k", "0603", quantityPerBuild: 1, selectedPart: "RC0603FR-0710KL")],
            [Quote("Digi-Key", "DK-R", "RC0603FR-0710KL", stock: 100, unitPrice: 0.02m, leadTimeDays: 14, lifecycle: BomPartLifecycle.Active)],
            [new BomBuildScenario("per-build", 1)],
            "USD",
            PlanTimestamp);

        var csv = BomPlanExporter.ExportBomCsv(plan);
        Assert.Contains("GroupKey,Designators,CanonicalIdentity,SelectedValue,Package,QuantityPerBuild,SelectedManufacturerPartNumber,Alternates,DoNotSubstitute", csv);
        Assert.Contains("resistor|10k|0603,R1,resistor,10k,0603,1,RC0603FR-0710KL,,False", csv);

        using var json = JsonDocument.Parse(BomPlanExporter.ExportOrderPlanJson(plan));
        Assert.Equal("USD", json.RootElement.GetProperty("currencyCode").GetString());
        Assert.Equal("2026-06-03T14:30:00+00:00", json.RootElement.GetProperty("createdAt").GetString());
        Assert.Equal("Digi-Key", json.RootElement.GetProperty("scenarios")[0].GetProperty("purchaseLines")[0].GetProperty("vendorName").GetString());
        Assert.Equal(14, json.RootElement.GetProperty("scenarios")[0].GetProperty("purchaseLines")[0].GetProperty("leadTimeDays").GetInt32());
        Assert.Equal("Active", json.RootElement.GetProperty("scenarios")[0].GetProperty("purchaseLines")[0].GetProperty("lifecycle").GetString());
    }

    private static BomPlanningComponent Component(
        string designator,
        string canonicalIdentity,
        string selectedValue,
        string package,
        int quantityPerBuild,
        string selectedPart,
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

    private static BomPlanningVendorQuote Quote(
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
        IReadOnlyList<QuantityPriceBreak>? priceBreaks = null,
        string canonicalIdentity = "resistor",
        string selectedValue = "10k",
        string package = "0603")
    {
        return new BomPlanningVendorQuote(
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
            PriceLadder.Normalize(priceBreaks ?? [new QuantityPriceBreak(1, Money.Usd(unitPrice ?? 0.01m))]));
    }
}
