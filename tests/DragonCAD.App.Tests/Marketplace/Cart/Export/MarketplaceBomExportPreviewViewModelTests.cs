using DragonCAD.App.Marketplace;
using DragonCAD.App.Marketplace.Cart;
using DragonCAD.App.Marketplace.Cart.Export;

namespace DragonCAD.App.Tests.Marketplace.Cart.Export;

public sealed class MarketplaceBomExportPreviewViewModelTests
{
    [Fact]
    public void EmptyCartProducesHeaderOnlyPreviewAndZeroTotal()
    {
        MarketplaceCartViewModel cart = new();

        MarketplaceBomExportPreviewViewModel preview = MarketplaceBomExportPreviewViewModel.FromCart(cart);

        Assert.Empty(preview.Rows);
        Assert.Empty(preview.Diagnostics);
        Assert.Equal("$0.00", preview.TotalSummary);
        Assert.Equal(
            "Vendor,MPN,Manufacturer,Component,Quantity,Unit Price,Subtotal,Canonical Id",
            preview.Header);
        Assert.Equal([preview.Header], preview.CsvLines);
    }

    [Fact]
    public void SingleVendorCartProducesCsvLikeRows()
    {
        MarketplaceCartViewModel cart = new();
        cart.AddItem(Row("Digi-Key", "7805 Regulator", "Texas Instruments", "LM7805CT", "dragon:7805", price: 0.8125m), quantity: 4);

        MarketplaceBomExportPreviewViewModel preview = MarketplaceBomExportPreviewViewModel.FromCart(cart);

        MarketplaceBomExportPreviewRow row = Assert.Single(preview.Rows);
        Assert.Equal("Digi-Key", row.Vendor);
        Assert.Equal("LM7805CT", row.ManufacturerPartNumber);
        Assert.Equal("Texas Instruments", row.Manufacturer);
        Assert.Equal("7805 Regulator", row.Component);
        Assert.Equal("4", row.Quantity);
        Assert.Equal("$0.8125", row.UnitPrice);
        Assert.Equal("$3.25", row.Subtotal);
        Assert.Equal("dragon:7805", row.CanonicalComponentId);
        Assert.Equal("$3.25", preview.TotalSummary);
        Assert.Equal("Digi-Key,LM7805CT,Texas Instruments,7805 Regulator,4,$0.8125,$3.25,dragon:7805", row.CsvLine);
        Assert.Equal(
            [
                preview.Header,
                "Digi-Key,LM7805CT,Texas Instruments,7805 Regulator,4,$0.8125,$3.25,dragon:7805"
            ],
            preview.CsvLines);
    }

    [Fact]
    public void MultiVendorCartUsesCartDeterministicOrdering()
    {
        MarketplaceCartViewModel cart = new();
        cart.AddItem(Row("Mouser", "555 Timer", "ST", "NE555N", "dragon:555", price: 0.51m), quantity: 2);
        cart.AddItem(Row("Digi-Key", "10k Resistor", "Yageo", "CFR-25JB-52-10K", "dragon:r-10k", price: 0.021m), quantity: 10);
        cart.AddItem(Row("Adafruit", "ESP32 Feather", "Espressif", "HUZZAH32", "dragon:esp32-feather", price: 19.95m), quantity: 1);

        MarketplaceBomExportPreviewViewModel preview = MarketplaceBomExportPreviewViewModel.FromCart(cart);

        Assert.Equal(["Adafruit", "Digi-Key", "Mouser"], preview.Rows.Select(row => row.Vendor));
        Assert.Equal(["HUZZAH32", "CFR-25JB-52-10K", "NE555N"], preview.Rows.Select(row => row.ManufacturerPartNumber));
        Assert.Equal(["Vendor,MPN,Manufacturer,Component,Quantity,Unit Price,Subtotal,Canonical Id", "Adafruit,HUZZAH32,Espressif,ESP32 Feather,1,$19.95,$19.95,dragon:esp32-feather"], preview.CsvLines.Take(2));
        Assert.Equal("$21.18", preview.TotalSummary);
    }

    [Fact]
    public void CurrencyFormattingPreservesFractionalCatalogPricesAndRoundsSubtotals()
    {
        MarketplaceCartViewModel cart = new();
        cart.AddItem(Row("Jameco", "Ceramic Capacitor", "Kemet", "C315C104M5U5TA", "dragon:c-100nf", stockQuantity: 500, price: 0.0067m), quantity: 150);

        MarketplaceBomExportPreviewRow row = Assert.Single(MarketplaceBomExportPreviewViewModel.FromCart(cart).Rows);

        Assert.Equal("$0.0067", row.UnitPrice);
        Assert.Equal("$1.005", row.Subtotal);
    }

    [Fact]
    public void CartDiagnosticsAreIncludedInPreview()
    {
        MarketplaceCartViewModel cart = new();
        cart.AddItem(Row("Jameco", "Obsolete Timer", "Dragon Test", "OLD555", "dragon:old555", stockQuantity: 0, price: null), quantity: 1);

        MarketplaceBomExportPreviewViewModel preview = MarketplaceBomExportPreviewViewModel.FromCart(cart);

        MarketplaceBomExportDiagnostic diagnostic = Assert.Single(preview.Diagnostics);
        Assert.Equal("Unavailable", diagnostic.Code);
        Assert.Equal("Jameco", diagnostic.Vendor);
        Assert.Equal("OLD555", diagnostic.ManufacturerPartNumber);
        Assert.Contains("Obsolete Timer", diagnostic.Message, StringComparison.Ordinal);
    }

    private static MarketplaceComponentRow Row(
        string provider,
        string displayName,
        string manufacturer,
        string manufacturerPartNumber,
        string canonicalComponentId,
        int stockQuantity = 100,
        decimal? price = 1.00m) =>
        new(
            Provider: provider,
            Category: "IC",
            DisplayName: displayName,
            Manufacturer: manufacturer,
            ManufacturerPartNumber: manufacturerPartNumber,
            CanonicalComponentId: canonicalComponentId,
            DuplicateOfComponentId: "",
            DatasheetUrl: "",
            StockQuantity: stockQuantity,
            MinimumUnitPriceUsd: price);
}
