using DragonCAD.App.Marketplace;
using DragonCAD.App.Marketplace.Cart;

namespace DragonCAD.App.Tests.Marketplace.Cart;

public sealed class MarketplaceCartViewModelTests
{
    [Fact]
    public void EmptyCartExposesCompactStripEmptyState()
    {
        MarketplaceCartViewModel viewModel = new();

        Assert.False(viewModel.HasLines);
        Assert.Equal(0, viewModel.LineCount);
        Assert.Equal(0, viewModel.UnitCount);
        Assert.Equal("$0.00", viewModel.CartSummary);
        Assert.Equal("Add marketplace components to build a BOM cart.", viewModel.EmptyStateMessage);
    }

    [Fact]
    public void AddItemAddsAvailableMarketplaceRowToBomCart()
    {
        MarketplaceCartViewModel viewModel = new();

        viewModel.AddItem(Row("Digi-Key", "ATmega328P", "ATMEGA328P-PU", stockQuantity: 25, price: 2.89m), quantity: 3);

        MarketplaceCartLine line = Assert.Single(viewModel.Lines);
        Assert.Equal("Digi-Key", line.Provider);
        Assert.Equal("ATmega328P", line.DisplayName);
        Assert.Equal(3, line.Quantity);
        Assert.Equal(8.67m, line.SubtotalUsd);
        Assert.Equal(8.67m, viewModel.TotalUsd);
        Assert.Empty(viewModel.Diagnostics);
    }

    [Fact]
    public void CartLineExposesOrderStripReadinessLabels()
    {
        MarketplaceCartViewModel viewModel = new();

        viewModel.AddItem(Row("Digi-Key", "ATmega328P", "ATMEGA328P-PU", stockQuantity: 25, price: 2.89m), quantity: 3);

        MarketplaceCartLine line = Assert.Single(viewModel.Lines);
        Assert.Equal("Digi-Key source: ATMEGA328P-PU", line.ProviderSourceSummary);
        Assert.Equal("Review Digi-Key order", line.NextActionLabel);
    }

    [Fact]
    public void CompactStripSummaryAndNotificationsRefreshAfterAddUpdateAndRemove()
    {
        MarketplaceCartViewModel viewModel = new();
        List<string> changedProperties = [];
        viewModel.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName ?? "");

        viewModel.AddItem(Row("Digi-Key", "ATmega328P", "ATMEGA328P-PU", stockQuantity: 25, price: 2.89m), quantity: 3);

        Assert.True(viewModel.HasLines);
        Assert.Equal(1, viewModel.LineCount);
        Assert.Equal(3, viewModel.UnitCount);
        Assert.Equal("1 line, 3 units, $8.67", viewModel.CartSummary);
        Assert.Contains(nameof(MarketplaceCartViewModel.HasLines), changedProperties);
        Assert.Contains(nameof(MarketplaceCartViewModel.LineCount), changedProperties);
        Assert.Contains(nameof(MarketplaceCartViewModel.UnitCount), changedProperties);
        Assert.Contains(nameof(MarketplaceCartViewModel.CartSummary), changedProperties);

        changedProperties.Clear();
        viewModel.UpdateQuantity(viewModel.Lines[0].LineId, quantity: 4);

        Assert.Equal(1, viewModel.LineCount);
        Assert.Equal(4, viewModel.UnitCount);
        Assert.Equal("1 line, 4 units, $11.56", viewModel.CartSummary);
        Assert.Contains(nameof(MarketplaceCartViewModel.UnitCount), changedProperties);
        Assert.Contains(nameof(MarketplaceCartViewModel.CartSummary), changedProperties);

        changedProperties.Clear();
        viewModel.RemoveLine(viewModel.Lines[0].LineId);

        Assert.False(viewModel.HasLines);
        Assert.Equal(0, viewModel.LineCount);
        Assert.Equal(0, viewModel.UnitCount);
        Assert.Equal("$0.00", viewModel.CartSummary);
        Assert.Contains(nameof(MarketplaceCartViewModel.HasLines), changedProperties);
        Assert.Contains(nameof(MarketplaceCartViewModel.LineCount), changedProperties);
        Assert.Contains(nameof(MarketplaceCartViewModel.UnitCount), changedProperties);
        Assert.Contains(nameof(MarketplaceCartViewModel.CartSummary), changedProperties);
    }

    [Fact]
    public void AddItemCombinesDuplicateRowsByProviderAndManufacturerPartNumber()
    {
        MarketplaceCartViewModel viewModel = new();
        MarketplaceComponentRow row = Row("Mouser", "NE555 Timer", "NE555P", stockQuantity: 100, price: 0.42m);

        viewModel.AddItem(row, quantity: 2);
        viewModel.AddItem(row, quantity: 5);

        MarketplaceCartLine line = Assert.Single(viewModel.Lines);
        Assert.Equal(7, line.Quantity);
        Assert.Equal(2.94m, line.SubtotalUsd);
    }

    [Fact]
    public void UpdateQuantityRecalculatesLineVendorAndCartTotals()
    {
        MarketplaceCartViewModel viewModel = new();
        viewModel.AddItem(Row("SparkFun", "USB-C Breakout", "BOB-15100", stockQuantity: 8, price: 5.95m), quantity: 1);

        viewModel.UpdateQuantity(viewModel.Lines[0].LineId, quantity: 4);

        Assert.Equal(23.80m, viewModel.Lines[0].SubtotalUsd);
        Assert.Equal(23.80m, viewModel.VendorGroups[0].SubtotalUsd);
        Assert.Equal(23.80m, viewModel.TotalUsd);
    }

    [Fact]
    public void VendorGroupsExposeDeterministicOrderingAndSubtotals()
    {
        MarketplaceCartViewModel viewModel = new();

        viewModel.AddItem(Row("Mouser", "LM7805", "LM7805CT", stockQuantity: 20, price: 0.81m), quantity: 2);
        viewModel.AddItem(Row("Digi-Key", "NE555 Timer", "NE555P", stockQuantity: 20, price: 0.48m), quantity: 5);
        viewModel.AddItem(Row("Digi-Key", "10k Resistor", "CF14JT10K0", stockQuantity: 500, price: 0.02m), quantity: 10);

        Assert.Equal(["Digi-Key", "Mouser"], viewModel.VendorGroups.Select(group => group.Provider));
        Assert.Equal(["10k Resistor", "NE555 Timer"], viewModel.VendorGroups[0].Lines.Select(line => line.DisplayName));
        Assert.Equal(2.60m, viewModel.VendorGroups[0].SubtotalUsd);
        Assert.Equal(1.62m, viewModel.VendorGroups[1].SubtotalUsd);
        Assert.Equal(4.22m, viewModel.TotalUsd);
    }

    [Fact]
    public void UnavailableItemsAreRejectedAndReportedAsDiagnostics()
    {
        MarketplaceCartViewModel viewModel = new();

        viewModel.AddItem(Row("Jameco", "Obsolete Timer", "OLD555", stockQuantity: 0, price: null), quantity: 1);

        Assert.Empty(viewModel.Lines);
        MarketplaceCartDiagnostic diagnostic = Assert.Single(viewModel.Diagnostics);
        Assert.Equal("Unavailable", diagnostic.Code);
        Assert.Contains("Obsolete Timer", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RemoveLineDeletesLineAndRefreshesTotals()
    {
        MarketplaceCartViewModel viewModel = new();
        viewModel.AddItem(Row("Adafruit", "Feather ESP32", "3405", stockQuantity: 3, price: 19.95m), quantity: 1);
        viewModel.AddItem(Row("SparkFun", "ESP32 Thing Plus", "WRL-15663", stockQuantity: 5, price: 21.50m), quantity: 2);

        viewModel.RemoveLine(viewModel.Lines.Single(line => line.Provider == "Adafruit").LineId);

        MarketplaceCartLine line = Assert.Single(viewModel.Lines);
        Assert.Equal("SparkFun", line.Provider);
        Assert.Equal(43.00m, viewModel.TotalUsd);
        Assert.Single(viewModel.VendorGroups);
    }

    private static MarketplaceComponentRow Row(
        string provider,
        string displayName,
        string manufacturerPartNumber,
        int stockQuantity,
        decimal? price) =>
        new(
            Provider: provider,
            Category: "IC",
            DisplayName: displayName,
            Manufacturer: "Dragon Test",
            ManufacturerPartNumber: manufacturerPartNumber,
            CanonicalComponentId: $"dragon:{manufacturerPartNumber.ToLowerInvariant()}",
            DuplicateOfComponentId: "",
            DatasheetUrl: "",
            StockQuantity: stockQuantity,
            MinimumUnitPriceUsd: price);
}
