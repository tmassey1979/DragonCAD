using DragonCAD.Sourcing;
using DragonCAD.Sourcing.BomOrdering;

namespace DragonCAD.Sourcing.Tests.BomOrdering;

public sealed class BomOrderPlannerTests
{
    [Fact]
    public void PlanGroupsPurchasesByVendorAndCalculatesDeterministicTotalCost()
    {
        var plan = BomOrderPlanner.Plan(
            [
                new BomOrderLine("R1,R2", "RC0603FR-0710KL", quantityPerAssembly: 2),
                new BomOrderLine("C1", "CL10B104KB8NNNC", quantityPerAssembly: 1),
            ],
            [
                Offer("Mouser", "MOU-R", "RC0603FR-0710KL", stock: 1000, moq: 1, multiple: 1, unitPrice: 0.03m),
                Offer("Digi-Key", "DK-R", "RC0603FR-0710KL", stock: 1000, moq: 1, multiple: 1, unitPrice: 0.02m),
                Offer("Digi-Key", "DK-C", "CL10B104KB8NNNC", stock: 1000, moq: 1, multiple: 1, unitPrice: 0.04m),
            ],
            buildQuantity: 25);

        Assert.True(plan.IsComplete);
        Assert.Empty(plan.Diagnostics);
        Assert.Equal(Money.Usd(2.00m), plan.TotalCost);
        Assert.Equal(["Digi-Key"], plan.VendorOrders.Select(order => order.VendorName));
        Assert.Equal(["DK-C", "DK-R"], plan.VendorOrders.Single().Lines.Select(line => line.VendorPartNumber));
    }

    [Fact]
    public void PlanRoundsPurchaseQuantityUpToMinimumOrderQuantity()
    {
        var plan = BomOrderPlanner.Plan(
            [new BomOrderLine("U1", "NE555P", quantityPerAssembly: 1)],
            [Offer("Jameco", "JAM-555", "NE555P", stock: 100, moq: 10, multiple: 1, unitPrice: 0.25m)],
            buildQuantity: 3);

        var line = plan.VendorOrders.Single().Lines.Single();
        Assert.Equal(3, line.RequiredQuantity);
        Assert.Equal(10, line.PurchaseQuantity);
        Assert.Equal(Money.Usd(2.50m), line.ExtendedCost);
    }

    [Fact]
    public void PlanRoundsPurchaseQuantityUpToOrderMultipleAfterMinimumOrderQuantity()
    {
        var plan = BomOrderPlanner.Plan(
            [new BomOrderLine("J1", "USB-C-16P", quantityPerAssembly: 1)],
            [Offer("SparkFun", "SPK-USBC", "USB-C-16P", stock: 100, moq: 5, multiple: 4, unitPrice: 0.80m)],
            buildQuantity: 6);

        var line = plan.VendorOrders.Single().Lines.Single();
        Assert.Equal(6, line.RequiredQuantity);
        Assert.Equal(8, line.PurchaseQuantity);
        Assert.Equal(Money.Usd(6.40m), line.ExtendedCost);
    }

    [Fact]
    public void PlanSplitsAcrossVendorsWhenLowestPriceVendorHasInsufficientStock()
    {
        var plan = BomOrderPlanner.Plan(
            [new BomOrderLine("U1", "LM7805CT", quantityPerAssembly: 1)],
            [
                Offer("Digi-Key", "DK-7805", "LM7805CT", stock: 8, moq: 1, multiple: 1, unitPrice: 0.40m),
                Offer("Mouser", "MOU-7805", "LM7805CT", stock: 20, moq: 1, multiple: 1, unitPrice: 0.45m),
            ],
            buildQuantity: 12);

        Assert.True(plan.IsComplete);
        Assert.Equal(Money.Usd(5.00m), plan.TotalCost);
        Assert.Equal(["Digi-Key", "Mouser"], plan.VendorOrders.Select(order => order.VendorName));
        Assert.Equal([8, 4], plan.VendorOrders.SelectMany(order => order.Lines).Select(line => line.PurchaseQuantity));
    }

    [Fact]
    public void PlanReportsUnavailableDiagnosticsForUnfilledRequiredQuantity()
    {
        var plan = BomOrderPlanner.Plan(
            [new BomOrderLine("U1", "ESP32-DEVKITC-32E", quantityPerAssembly: 1)],
            [
                Offer("Adafruit", "ADA-ESP32", "ESP32-DEVKITC-32E", stock: 2, moq: 1, multiple: 1, unitPrice: 12.50m),
            ],
            buildQuantity: 5);

        Assert.False(plan.IsComplete);
        Assert.Equal(Money.Usd(25.00m), plan.TotalCost);
        var diagnostic = Assert.Single(plan.Diagnostics);
        Assert.Equal("U1", diagnostic.BomLineId);
        Assert.Equal("ESP32-DEVKITC-32E", diagnostic.ManufacturerPartNumber);
        Assert.Equal(3, diagnostic.UnfilledQuantity);
        Assert.Contains("Unable to source", diagnostic.Message);
    }

    private static BomOrderVendorOffer Offer(
        string vendor,
        string vendorPartNumber,
        string manufacturerPartNumber,
        int stock,
        int moq,
        int multiple,
        decimal unitPrice)
    {
        return new BomOrderVendorOffer(
            vendor,
            vendorPartNumber,
            manufacturerPartNumber,
            stock,
            moq,
            multiple,
            PriceLadder.Normalize([new QuantityPriceBreak(1, Money.Usd(unitPrice))]));
    }
}
