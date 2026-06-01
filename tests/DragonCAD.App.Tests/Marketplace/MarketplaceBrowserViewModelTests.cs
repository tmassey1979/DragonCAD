using DragonCAD.App.Marketplace;

namespace DragonCAD.App.Tests.Marketplace;

public sealed class MarketplaceBrowserViewModelTests
{
    [Fact]
    public void ProviderFilterOptionsExposeMajorComponentVendors()
    {
        MarketplaceBrowserViewModel viewModel = MarketplaceBrowserViewModel.FromRows([]);

        Assert.Equal(
            ["All", "SparkFun", "Adafruit", "Digi-Key", "Mouser", "Jameco"],
            viewModel.ProviderFilterOptions);
    }

    [Fact]
    public void SelectedProviderFilterNarrowsRowsByProvider()
    {
        MarketplaceBrowserViewModel viewModel = MarketplaceBrowserViewModel.FromRows(
        [
            Row("SparkFun", "Module", "ESP32 Thing Plus", "ESP32-THING", canonicalComponentId: "dragon:esp32-devkit"),
            Row("Digi-Key", "IC", "NE555 Timer", "NE555P", canonicalComponentId: "dragon:ne555")
        ]);

        viewModel.SelectedProviderFilter = "Digi-Key";

        MarketplaceComponentRow row = Assert.Single(viewModel.Components);
        Assert.Equal("Digi-Key", row.Provider);
        Assert.Equal("NE555 Timer", row.DisplayName);
    }

    [Fact]
    public void TypeFilterOptionsAndSelectedCategoryFilterNarrowRowsByCategory()
    {
        MarketplaceBrowserViewModel viewModel = MarketplaceBrowserViewModel.FromRows(
        [
            Row("Mouser", "Voltage Regulator", "7805 Regulator", "LM7805CT", canonicalComponentId: "dragon:lm7805"),
            Row("Jameco", "Passive", "10k Resistor", "CF1/4W103JRC", canonicalComponentId: "dragon:resistor")
        ]);

        Assert.Equal(["All", "Passive", "Voltage Regulator"], viewModel.CategoryFilterOptions);

        viewModel.SelectedCategoryFilter = "Voltage Regulator";

        MarketplaceComponentRow row = Assert.Single(viewModel.Components);
        Assert.Equal("7805 Regulator", row.DisplayName);
    }

    [Fact]
    public void SearchProviderAndCategoryFiltersCompose()
    {
        MarketplaceBrowserViewModel viewModel = MarketplaceBrowserViewModel.FromRows(
        [
            Row("Digi-Key", "IC", "NE555 Timer", "NE555P", canonicalComponentId: "dragon:ne555"),
            Row("Mouser", "IC", "TLC555 Timer", "TLC555CP", canonicalComponentId: "dragon:tlc555"),
            Row("Digi-Key", "Module", "ESP32 DevKit", "ESP32-DEVKITC", canonicalComponentId: "dragon:esp32-devkit")
        ]);

        viewModel.SelectedProviderFilter = "Digi-Key";
        viewModel.SelectedCategoryFilter = "IC";
        viewModel.SearchText = "555";

        MarketplaceComponentRow row = Assert.Single(viewModel.Components);
        Assert.Equal("NE555 Timer", row.DisplayName);
    }

    [Fact]
    public void DisplaySearchStateTracksVisibleAndTotalComponentCounts()
    {
        MarketplaceBrowserViewModel viewModel = MarketplaceBrowserViewModel.FromRows(
        [
            Row("Digi-Key", "IC", "NE555 Timer", "NE555P", canonicalComponentId: "dragon:ne555"),
            Row("Mouser", "IC", "TLC555 Timer", "TLC555CP", canonicalComponentId: "dragon:tlc555"),
            Row("Digi-Key", "Module", "ESP32 DevKit", "ESP32-DEVKITC", canonicalComponentId: "dragon:esp32-devkit")
        ]);

        Assert.Equal(3, viewModel.VisibleComponentCount);
        Assert.Equal(3, viewModel.TotalComponentCount);
        Assert.Equal("Showing 3 of 3 components", viewModel.SearchSummary);
        Assert.Equal("", viewModel.EmptyStateMessage);

        viewModel.SelectedProviderFilter = "Digi-Key";

        Assert.Equal(2, viewModel.VisibleComponentCount);
        Assert.Equal(3, viewModel.TotalComponentCount);
        Assert.Equal("Showing 2 of 3 components", viewModel.SearchSummary);
        Assert.Equal("", viewModel.EmptyStateMessage);

        viewModel.SelectedCategoryFilter = "IC";

        Assert.Equal(1, viewModel.VisibleComponentCount);
        Assert.Equal(3, viewModel.TotalComponentCount);
        Assert.Equal("Showing 1 of 3 components", viewModel.SearchSummary);
        Assert.Equal("", viewModel.EmptyStateMessage);

        viewModel.SearchText = "missing";

        Assert.Equal(0, viewModel.VisibleComponentCount);
        Assert.Equal(3, viewModel.TotalComponentCount);
        Assert.Equal("No components match the current filters", viewModel.SearchSummary);
        Assert.Equal("No marketplace components match your search and filters.", viewModel.EmptyStateMessage);
    }

    [Fact]
    public void RowsExposeCanonicalAndDuplicateBadges()
    {
        MarketplaceBrowserViewModel viewModel = MarketplaceBrowserViewModel.FromRows(
        [
            Row("Digi-Key", "Voltage Regulator", "LM7805", "LM7805CT", canonicalComponentId: "dragon:lm7805"),
            Row("Mouser", "Voltage Regulator", "L7805CV", "L7805CV", canonicalComponentId: "dragon:lm7805", duplicateOfComponentId: "dragon:lm7805")
        ]);

        Assert.Equal("Canonical", viewModel.Components[0].CanonicalBadge);
        Assert.True(viewModel.Components[0].IsCanonical);
        Assert.Equal("Duplicate of dragon:lm7805", viewModel.Components[1].CanonicalBadge);
        Assert.False(viewModel.Components[1].IsCanonical);
    }

    [Fact]
    public void RowsExposeDatasheetStatus()
    {
        MarketplaceBrowserViewModel viewModel = MarketplaceBrowserViewModel.FromRows(
        [
            Row("Adafruit", "Module", "Feather ESP32", "3405", canonicalComponentId: "dragon:feather-esp32", datasheetUrl: "https://example.test/feather.pdf"),
            Row("SparkFun", "Connector", "USB-C Breakout", "BOB-15100", canonicalComponentId: "dragon:usb-c-breakout")
        ]);

        Assert.Equal("Datasheet ready", viewModel.Components[0].DatasheetStatus);
        Assert.True(viewModel.Components[0].HasDatasheet);
        Assert.Equal("Datasheet needed", viewModel.Components[1].DatasheetStatus);
        Assert.False(viewModel.Components[1].HasDatasheet);
    }

    [Fact]
    public void RowsExposeStockPriceSummaryAndBomActionState()
    {
        MarketplaceBrowserViewModel viewModel = MarketplaceBrowserViewModel.FromRows(
        [
            Row("Digi-Key", "IC", "ATmega328P", "ATMEGA328P-PU", canonicalComponentId: "dragon:atmega328p", stockQuantity: 2450, minimumUnitPriceUsd: 2.89m),
            Row("Jameco", "IC", "Obsolete Timer", "OLD555", canonicalComponentId: "dragon:old555", stockQuantity: 0, minimumUnitPriceUsd: null)
        ]);

        Assert.Equal("2,450 in stock from $2.89", viewModel.Components[0].StockPriceSummary);
        Assert.Equal("Add to BOM", viewModel.Components[0].BomActionLabel);
        Assert.True(viewModel.Components[0].CanAddToBom);

        Assert.Equal("Out of stock / price unavailable", viewModel.Components[1].StockPriceSummary);
        Assert.Equal("Unavailable for BOM", viewModel.Components[1].BomActionLabel);
        Assert.False(viewModel.Components[1].CanAddToBom);
    }

    [Fact]
    public void SelectedComponentSummaryExposesDetailsPanelText()
    {
        MarketplaceBrowserViewModel viewModel = MarketplaceBrowserViewModel.FromRows(
        [
            Row("Adafruit", "Module", "Feather ESP32", "3405", canonicalComponentId: "dragon:feather-esp32", stockQuantity: 42, minimumUnitPriceUsd: 19.95m)
        ]);

        Assert.True(viewModel.HasSelectedComponent);
        Assert.Equal("Feather ESP32 (3405)", viewModel.SelectedComponentTitle);
        Assert.Equal("Adafruit / Dragon Test", viewModel.SelectedComponentVendorSummary);
        Assert.Equal("42 in stock from $19.95", viewModel.SelectedComponentAvailabilitySummary);
        Assert.Equal("", viewModel.SelectedComponentEmptySelectionText);
    }

    [Fact]
    public void SelectedComponentSummaryExposesEmptySelectionText()
    {
        MarketplaceBrowserViewModel viewModel = MarketplaceBrowserViewModel.FromRows(
        [
            Row("Digi-Key", "IC", "NE555 Timer", "NE555P", canonicalComponentId: "dragon:ne555")
        ]);

        viewModel.SearchText = "missing";

        Assert.False(viewModel.HasSelectedComponent);
        Assert.Equal("", viewModel.SelectedComponentTitle);
        Assert.Equal("", viewModel.SelectedComponentVendorSummary);
        Assert.Equal("", viewModel.SelectedComponentAvailabilitySummary);
        Assert.Equal("Select a marketplace component to view supplier and availability details.", viewModel.SelectedComponentEmptySelectionText);
    }

    private static MarketplaceComponentRow Row(
        string provider,
        string category,
        string displayName,
        string manufacturerPartNumber,
        string canonicalComponentId,
        string duplicateOfComponentId = "",
        string datasheetUrl = "",
        int stockQuantity = 100,
        decimal? minimumUnitPriceUsd = 1.25m) =>
        new(
            Provider: provider,
            Category: category,
            DisplayName: displayName,
            Manufacturer: "Dragon Test",
            ManufacturerPartNumber: manufacturerPartNumber,
            CanonicalComponentId: canonicalComponentId,
            DuplicateOfComponentId: duplicateOfComponentId,
            DatasheetUrl: datasheetUrl,
            StockQuantity: stockQuantity,
            MinimumUnitPriceUsd: minimumUnitPriceUsd);
}
