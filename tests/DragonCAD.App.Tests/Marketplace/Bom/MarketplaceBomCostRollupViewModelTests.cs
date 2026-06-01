using DragonCAD.App.Marketplace.Bom;
using DragonCAD.Sourcing;
using DragonCAD.Sourcing.Bom;

namespace DragonCAD.App.Tests.Marketplace.Bom;

public sealed class MarketplaceBomCostRollupViewModelTests
{
    [Fact]
    public void FromRollupMapsSelectedOffersAlternativeOffersProviderSummariesAndTotal()
    {
        var rollup = new BomCostRollup(
            Money.Usd(0.76m),
            [
                new BomCostRollupLine(
                    new BomComponentQuantity("R1,R2", "RC0603FR-0710KL", quantity: 12),
                    [
                        Offer("Digi-Key", "DK-R-1", "RC0603FR-0710KL", requiredQuantity: 12, priceBreak: 10, unitPrice: 0.03m, extendedCost: 0.36m, stockQuantity: 1_000),
                        Offer("Mouser", "MOU-R-1", "RC0603FR-0710KL", requiredQuantity: 12, priceBreak: 10, unitPrice: 0.04m, extendedCost: 0.48m, stockQuantity: 1_000)
                    ],
                    Offer("Digi-Key", "DK-R-1", "RC0603FR-0710KL", requiredQuantity: 12, priceBreak: 10, unitPrice: 0.03m, extendedCost: 0.36m, stockQuantity: 1_000)),
                new BomCostRollupLine(
                    new BomComponentQuantity("C1", "CL10B104KB8NNNC", quantity: 5),
                    [
                        Offer("Mouser", "MOU-C-1", "CL10B104KB8NNNC", requiredQuantity: 5, priceBreak: 5, unitPrice: 0.08m, extendedCost: 0.40m, stockQuantity: 50)
                    ],
                    Offer("Mouser", "MOU-C-1", "CL10B104KB8NNNC", requiredQuantity: 5, priceBreak: 5, unitPrice: 0.08m, extendedCost: 0.40m, stockQuantity: 50))
            ],
            [],
            [
                new BomProviderSummary("Digi-Key", 1, Money.Usd(0.36m)),
                new BomProviderSummary("Mouser", 1, Money.Usd(0.40m))
            ]);

        MarketplaceBomCostRollupViewModel viewModel = MarketplaceBomCostRollupViewModel.FromRollup(rollup);

        Assert.Equal("Total: $0.76 across 2 components", viewModel.TotalSummary);
        Assert.True(viewModel.IsComplete);
        Assert.Equal(["R1,R2", "C1"], viewModel.Rows.Select(row => row.ComponentId));
        Assert.Equal(["RC0603FR-0710KL", "CL10B104KB8NNNC"], viewModel.Rows.Select(row => row.ComponentName));

        MarketplaceBomCostRollupRow resistor = viewModel.Rows[0];
        Assert.Equal(12, resistor.Quantity);
        Assert.Equal("Digi-Key", resistor.SelectedProvider);
        Assert.Equal("DK-R-1", resistor.SelectedSku);
        Assert.Equal("$0.03 ea @ 10+", resistor.SelectedUnitCost);
        Assert.Equal("$0.36", resistor.SelectedExtendedCost);
        MarketplaceBomAlternativeOfferRow alternative = Assert.Single(resistor.AlternativeOffers);
        Assert.Equal("Mouser", alternative.Provider);
        Assert.Equal("MOU-R-1", alternative.Sku);
        Assert.Equal("$0.04 ea @ 10+", alternative.UnitCost);
        Assert.Equal("$0.48", alternative.ExtendedCost);
        Assert.Equal("1,000 in stock", alternative.Availability);

        Assert.Equal(["Digi-Key: 1 line, $0.36", "Mouser: 1 line, $0.40"], viewModel.ProviderSummaries.Select(summary => summary.Summary));
    }

    [Fact]
    public void FromRollupMapsMissingOfferDiagnosticsOntoRowsAndDiagnosticList()
    {
        var rollup = new BomCostRollup(
            Money.Usd(0.90m),
            [
                new BomCostRollupLine(
                    new BomComponentQuantity("U1", "LM7805CT", quantity: 2),
                    [Offer("Jameco", "JAM-U-1", "LM7805CT", requiredQuantity: 2, priceBreak: 1, unitPrice: 0.45m, extendedCost: 0.90m, stockQuantity: 20)],
                    Offer("Jameco", "JAM-U-1", "LM7805CT", requiredQuantity: 2, priceBreak: 1, unitPrice: 0.45m, extendedCost: 0.90m, stockQuantity: 20)),
                new BomCostRollupLine(
                    new BomComponentQuantity("J1", "USB-C-16P", quantity: 4),
                    [],
                    null)
            ],
            [
                new BomCostRollupDiagnostic(
                    BomCostRollupDiagnosticCode.MissingCatalogSource,
                    "J1",
                    "USB-C-16P",
                    4,
                    "No normalized catalog listing matched USB-C-16P.")
            ],
            [new BomProviderSummary("Jameco", 1, Money.Usd(0.90m))]);

        MarketplaceBomCostRollupViewModel viewModel = MarketplaceBomCostRollupViewModel.FromRollup(rollup);

        Assert.False(viewModel.IsComplete);
        Assert.Equal("Total: $0.90 across 2 components, 1 diagnostic", viewModel.TotalSummary);

        MarketplaceBomCostRollupRow missing = viewModel.Rows[1];
        Assert.Equal("Unpriced", missing.SelectedProvider);
        Assert.Equal("", missing.SelectedSku);
        Assert.Equal("$0.00", missing.SelectedExtendedCost);
        Assert.Equal(["MissingCatalogSource: No normalized catalog listing matched USB-C-16P."], missing.Diagnostics);

        MarketplaceBomDiagnosticRow diagnostic = Assert.Single(viewModel.Diagnostics);
        Assert.Equal("MissingCatalogSource", diagnostic.Code);
        Assert.Equal("J1", diagnostic.ComponentId);
        Assert.Equal("USB-C-16P", diagnostic.ComponentName);
        Assert.Equal(4, diagnostic.Quantity);
        Assert.Equal("No normalized catalog listing matched USB-C-16P.", diagnostic.Message);
    }

    private static BomProviderOffer Offer(
        string provider,
        string sku,
        string manufacturerPartNumber,
        int requiredQuantity,
        int priceBreak,
        decimal unitPrice,
        decimal extendedCost,
        int? stockQuantity) =>
        new(
            provider,
            sku,
            manufacturerPartNumber,
            requiredQuantity,
            priceBreak,
            Money.Usd(unitPrice),
            Money.Usd(extendedCost),
            stockQuantity);
}
