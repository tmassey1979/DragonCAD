using DragonCAD.App.Marketplace.Bom;
using DragonCAD.App.Marketplace;
using DragonCAD.App.Marketplace.Cart;
using DragonCAD.Sourcing;
using DragonCAD.Sourcing.Bom;

namespace DragonCAD.App.Tests.Marketplace.Bom;

public sealed class MarketplaceBomCostRollupViewModelTests
{
    [Fact]
    public void FromCartExposesEmptyCartStateForDisplay()
    {
        MarketplaceCartViewModel cart = new();

        MarketplaceBomCostRollupViewModel viewModel = MarketplaceBomCostRollupFactory.FromCart(cart, []);

        Assert.True(viewModel.IsEmpty);
        Assert.Equal("Add parts to the marketplace cart to build a BOM rollup.", viewModel.EmptyStateMessage);
        Assert.Equal("No BOM lines ready", viewModel.ReadyStateSummary);
        Assert.Equal("No BOM lines ready for procurement.", viewModel.ProcurementReadinessSummary);
        Assert.Equal("Add parts to the marketplace cart", viewModel.ProcurementActionSummary);
    }

    [Fact]
    public void FromRollupExposesReadyStateSummaryForCompletePricedBom()
    {
        var rollup = new BomCostRollup(
            Money.Usd(2.55m),
            [
                new BomCostRollupLine(
                    new BomComponentQuantity("U1", "ATMEGA328P", quantity: 3),
                    [Offer("Digi-Key", "DK-U-1", "ATMEGA328P", requiredQuantity: 3, priceBreak: 1, unitPrice: 0.85m, extendedCost: 2.55m, stockQuantity: 10)],
                    Offer("Digi-Key", "DK-U-1", "ATMEGA328P", requiredQuantity: 3, priceBreak: 1, unitPrice: 0.85m, extendedCost: 2.55m, stockQuantity: 10))
            ],
            [],
            [new BomProviderSummary("Digi-Key", 1, Money.Usd(2.55m))]);

        MarketplaceBomCostRollupViewModel viewModel = MarketplaceBomCostRollupViewModel.FromRollup(rollup);

        Assert.False(viewModel.IsEmpty);
        Assert.Equal("", viewModel.EmptyStateMessage);
        Assert.Equal("Ready to source 1 component from 1 provider", viewModel.ReadyStateSummary);
        Assert.Equal("Ready for procurement review: 1 component priced across 1 provider.", viewModel.ProcurementReadinessSummary);
        Assert.Equal("Review 1 provider order", viewModel.ProcurementActionSummary);
    }

    [Fact]
    public void FromCartAggregatesCartQuantitiesAndPricesFromMarketplaceCatalogRows()
    {
        MarketplaceComponentRow digiKeyRow = Row(
            provider: "Digi-Key",
            displayName: "NE555 Timer",
            manufacturerPartNumber: "NE555P",
            canonicalComponentId: "dragon:ne555p",
            stockQuantity: 100,
            price: 0.42m);
        MarketplaceComponentRow mouserRow = Row(
            provider: "Mouser",
            displayName: "NE555 Timer",
            manufacturerPartNumber: "NE555P",
            canonicalComponentId: "dragon:ne555p",
            stockQuantity: 75,
            price: 0.37m);
        MarketplaceCartViewModel cart = new();
        cart.AddItem(digiKeyRow, quantity: 3);
        cart.AddItem(mouserRow, quantity: 2);

        MarketplaceBomCostRollupViewModel viewModel = MarketplaceBomCostRollupFactory.FromCart(
            cart,
            [mouserRow, digiKeyRow]);

        MarketplaceBomCostRollupRow row = Assert.Single(viewModel.Rows);
        Assert.Equal("dragon:ne555p", row.ComponentId);
        Assert.Equal("NE555P", row.ComponentName);
        Assert.Equal(5, row.Quantity);
        Assert.Equal("Mouser", row.SelectedProvider);
        Assert.Equal("NE555P", row.SelectedSku);
        Assert.Equal("$0.37 ea @ 1+", row.SelectedUnitCost);
        Assert.Equal("$1.85", row.SelectedExtendedCost);

        MarketplaceBomAlternativeOfferRow alternative = Assert.Single(row.AlternativeOffers);
        Assert.Equal("Digi-Key", alternative.Provider);
        Assert.Equal("$0.42 ea @ 1+", alternative.UnitCost);
        Assert.Equal("$2.10", alternative.ExtendedCost);
        Assert.Equal("100 in stock", alternative.Availability);

        Assert.True(viewModel.IsComplete);
        Assert.Empty(viewModel.Diagnostics);
        MarketplaceBomProviderSummaryRow providerSummary = Assert.Single(viewModel.ProviderSummaries);
        Assert.Equal("Mouser: 1 line, $1.85", providerSummary.Summary);
        Assert.Equal("Ready", providerSummary.ProcurementStatus);
        Assert.Equal("Review Mouser procurement: 1 line, $1.85 ready for checkout setup.", providerSummary.ProcurementActionSummary);
        Assert.Equal("Total: $1.85 across 1 component", viewModel.TotalSummary);
    }

    [Fact]
    public void FromCartReportsMissingSourceWhenCartLineHasNoPricedCatalogRow()
    {
        MarketplaceComponentRow row = Row(
            provider: "SparkFun",
            displayName: "USB-C Breakout",
            manufacturerPartNumber: "BOB-15100",
            canonicalComponentId: "dragon:bob-15100",
            stockQuantity: 8,
            price: 5.95m);
        MarketplaceCartViewModel cart = new();
        cart.AddItem(row, quantity: 2);

        MarketplaceBomCostRollupViewModel viewModel = MarketplaceBomCostRollupFactory.FromCart(cart, []);

        MarketplaceBomCostRollupRow rollupRow = Assert.Single(viewModel.Rows);
        Assert.Equal("dragon:bob-15100", rollupRow.ComponentId);
        Assert.Equal("BOB-15100", rollupRow.ComponentName);
        Assert.Equal(2, rollupRow.Quantity);
        Assert.Equal("Unpriced", rollupRow.SelectedProvider);
        Assert.Equal(["MissingCatalogSource: No normalized catalog listing found for BOB-15100."], rollupRow.Diagnostics);

        MarketplaceBomDiagnosticRow diagnostic = Assert.Single(viewModel.Diagnostics);
        Assert.Equal("MissingCatalogSource", diagnostic.Code);
        Assert.Equal("dragon:bob-15100", diagnostic.ComponentId);
        Assert.Equal("BOB-15100", diagnostic.ComponentName);
        Assert.Equal(2, diagnostic.Quantity);
        Assert.False(viewModel.IsComplete);
        Assert.Equal("Total: $0.00 across 1 component, 1 diagnostic", viewModel.TotalSummary);
        Assert.Equal("Blocked: 1 component needs sourcing attention before procurement.", viewModel.ProcurementReadinessSummary);
        Assert.Equal("Resolve 1 BOM diagnostic", viewModel.ProcurementActionSummary);
    }

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
        Assert.Equal(["Ready", "Ready"], viewModel.ProviderSummaries.Select(summary => summary.ProcurementStatus));
        Assert.Equal(
            [
                "Review Digi-Key procurement: 1 line, $0.36 ready for checkout setup.",
                "Review Mouser procurement: 1 line, $0.40 ready for checkout setup."
            ],
            viewModel.ProviderSummaries.Select(summary => summary.ProcurementActionSummary));
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
        Assert.Equal("Blocked: 1 component needs sourcing attention before procurement.", viewModel.ProcurementReadinessSummary);
        Assert.Equal("Resolve 1 BOM diagnostic", viewModel.ProcurementActionSummary);
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

    private static MarketplaceComponentRow Row(
        string provider,
        string displayName,
        string manufacturerPartNumber,
        string canonicalComponentId,
        int stockQuantity,
        decimal? price) =>
        new(
            Provider: provider,
            Category: "IC",
            DisplayName: displayName,
            Manufacturer: "Dragon Test",
            ManufacturerPartNumber: manufacturerPartNumber,
            CanonicalComponentId: canonicalComponentId,
            DuplicateOfComponentId: "",
            DatasheetUrl: "",
            StockQuantity: stockQuantity,
            MinimumUnitPriceUsd: price);
}
