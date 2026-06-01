using DragonCAD.App.Marketplace;
using DragonCAD.App.Marketplace.Filters;

namespace DragonCAD.App.Tests.Marketplace.Filters;

public sealed class MarketplaceSavedFilterPresetTests
{
    [Fact]
    public void SavePresetStoresNormalizedFilterValues()
    {
        MarketplaceSavedFilterPresetStore store = new();

        MarketplaceFilterPreset preset = store.Save(
            "  Regulators in stock  ",
            provider: " Digi-Key ",
            category: " Voltage Regulator ",
            searchText: " 7805 ",
            requiresDatasheet: true,
            inStockOnly: true);

        Assert.Equal("Regulators in stock", preset.Name);
        Assert.Equal("Digi-Key", preset.Provider);
        Assert.Equal("Voltage Regulator", preset.Category);
        Assert.Equal("7805", preset.SearchText);
        Assert.True(preset.RequiresDatasheet);
        Assert.True(preset.InStockOnly);
    }

    [Fact]
    public void ApplyPresetFiltersByVendorCategoryAndSearchWithoutMutatingSourceRows()
    {
        MarketplaceComponentRow[] rows =
        [
            Row("Digi-Key", "Voltage Regulator", "LM7805 Regulator", "LM7805CT"),
            Row("Mouser", "Voltage Regulator", "L7805 Regulator", "L7805CV"),
            Row("Digi-Key", "IC", "NE555 Timer", "NE555P")
        ];
        MarketplaceFilterPreset preset = new(
            Name: "Digi-Key 7805 regulators",
            Provider: "Digi-Key",
            Category: "Voltage Regulator",
            SearchText: "7805",
            RequiresDatasheet: false,
            InStockOnly: false);

        IReadOnlyList<MarketplaceComponentRow> filtered = MarketplaceFilterPresetApplicator.Apply(rows, preset);

        MarketplaceComponentRow row = Assert.Single(filtered);
        Assert.Equal("LM7805 Regulator", row.DisplayName);
        Assert.Equal(3, rows.Length);
    }

    [Fact]
    public void ApplyPresetCanRequireInStockRows()
    {
        MarketplaceFilterPreset preset = MarketplaceFilterPreset.All with { InStockOnly = true };

        IReadOnlyList<MarketplaceComponentRow> filtered = MarketplaceFilterPresetApplicator.Apply(
        [
            Row("Digi-Key", "IC", "In Stock Timer", "NE555P", stockQuantity: 25),
            Row("Mouser", "IC", "Out Of Stock Timer", "LM555CN", stockQuantity: 0)
        ], preset);

        MarketplaceComponentRow row = Assert.Single(filtered);
        Assert.Equal("In Stock Timer", row.DisplayName);
    }

    [Fact]
    public void ApplyPresetCanRequireDatasheetRows()
    {
        MarketplaceFilterPreset preset = MarketplaceFilterPreset.All with { RequiresDatasheet = true };

        IReadOnlyList<MarketplaceComponentRow> filtered = MarketplaceFilterPresetApplicator.Apply(
        [
            Row("Adafruit", "Module", "Feather ESP32", "3405", datasheetUrl: "https://example.test/3405.pdf"),
            Row("SparkFun", "Module", "ESP32 Thing", "WRL-15663")
        ], preset);

        MarketplaceComponentRow row = Assert.Single(filtered);
        Assert.Equal("Feather ESP32", row.DisplayName);
    }

    [Fact]
    public void PresetsAreReturnedInDeterministicNameOrder()
    {
        MarketplaceSavedFilterPresetStore store = new();

        store.Save("Vendor modules", provider: "SparkFun", category: "Module", searchText: "", requiresDatasheet: false, inStockOnly: false);
        store.Save("All stocked", provider: "All", category: "All", searchText: "", requiresDatasheet: false, inStockOnly: true);
        store.Save("Datasheets", provider: "All", category: "All", searchText: "", requiresDatasheet: true, inStockOnly: false);

        Assert.Equal(["All stocked", "Datasheets", "Vendor modules"], store.Presets.Select(preset => preset.Name));
    }

    private static MarketplaceComponentRow Row(
        string provider,
        string category,
        string displayName,
        string manufacturerPartNumber,
        string datasheetUrl = "",
        int stockQuantity = 100) =>
        new(
            Provider: provider,
            Category: category,
            DisplayName: displayName,
            Manufacturer: "Dragon Test",
            ManufacturerPartNumber: manufacturerPartNumber,
            CanonicalComponentId: $"dragon:{manufacturerPartNumber.ToLowerInvariant()}",
            DuplicateOfComponentId: "",
            DatasheetUrl: datasheetUrl,
            StockQuantity: stockQuantity,
            MinimumUnitPriceUsd: stockQuantity > 0 ? 1.25m : null);
}
