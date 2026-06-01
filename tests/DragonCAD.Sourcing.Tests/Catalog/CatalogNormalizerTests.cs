using DragonCAD.Sourcing;
using DragonCAD.Sourcing.Catalog;

namespace DragonCAD.Sourcing.Tests.Catalog;

public sealed class CatalogNormalizerTests
{
    [Fact]
    public void NormalizerMapsDigiKeyAndMouserStylePriceBreaks()
    {
        var item = new VendorCatalogItem(
            providerName: "Digi-Key",
            vendorSku: "296-12345-1-ND",
            manufacturerPartNumber: " LM7805CT/NOPB ",
            manufacturer: " Texas Instruments ",
            description: "Linear voltage regulator",
            priceBreaks:
            [
                new QuantityPriceBreak(100, Money.Usd(0.44m)),
                new QuantityPriceBreak(1, Money.Usd(0.72m)),
                new QuantityPriceBreak(10, Money.Usd(0.51m)),
            ],
            stockQuantity: 12_345,
            datasheetUrl: new Uri("https://example.test/lm7805.pdf"),
            productUrl: new Uri("https://example.test/products/296-12345-1-ND"),
            fields: new Dictionary<string, string>
            {
                ["Packaging"] = "Tube",
            });

        var listing = CatalogNormalizer.Normalize(item);

        Assert.Equal("Digi-Key", listing.ProviderName);
        Assert.Equal("296-12345-1-ND", listing.VendorSku);
        Assert.Equal("LM7805CT/NOPB", listing.ManufacturerPartNumber);
        Assert.Equal("Texas Instruments", listing.Manufacturer);
        Assert.Equal(12_345, listing.StockQuantity);
        Assert.Equal("https://example.test/lm7805.pdf", listing.DatasheetUrl?.ToString());
        Assert.Equal([1, 10, 100], listing.PriceLadder.Breaks.Select(priceBreak => priceBreak.Quantity));
        Assert.Equal(Money.Usd(0.51m), listing.PriceLadder.FindBestBreakFor(25).UnitPrice);
        Assert.Equal("Tube", listing.Fields["Packaging"]);
    }

    [Fact]
    public void NormalizerMapsAdafruitProductStyleFields()
    {
        var item = new VendorCatalogItem(
            providerName: "Adafruit",
            vendorSku: "ID-50",
            manufacturerPartNumber: "NE555P",
            manufacturer: "TI",
            description: "555 timer DIP-8",
            priceBreaks: [new QuantityPriceBreak(1, Money.Usd(1.95m))],
            stockQuantity: 278,
            datasheetUrl: null,
            productUrl: new Uri("https://www.adafruit.com/product/50"),
            fields: new Dictionary<string, string>
            {
                ["ProductId"] = "50",
                ["Category"] = "Integrated Circuits",
                ["Permalink"] = "https://www.adafruit.com/product/50",
            });

        var listing = CatalogNormalizer.Normalize(item);

        Assert.Equal("Adafruit", listing.ProviderName);
        Assert.Equal("ID-50", listing.VendorSku);
        Assert.Equal("NE555P", listing.ManufacturerPartNumber);
        Assert.Equal("TI", listing.Manufacturer);
        Assert.Equal("https://www.adafruit.com/product/50", listing.ProductUrl?.ToString());
        Assert.Equal("Integrated Circuits", listing.Fields["Category"]);
    }

    [Fact]
    public void NormalizerMapsSparkFunOpenHardwareSourceFields()
    {
        var item = new VendorCatalogItem(
            providerName: "SparkFun",
            vendorSku: "DEV-13975",
            manufacturerPartNumber: "ESP32-THING",
            manufacturer: "SparkFun",
            description: "ESP32 Thing open hardware board",
            priceBreaks: [new QuantityPriceBreak(1, Money.Usd(24.95m))],
            stockQuantity: null,
            datasheetUrl: new Uri("https://example.test/esp32-thing.pdf"),
            productUrl: new Uri("https://www.sparkfun.com/products/13975"),
            fields: new Dictionary<string, string>
            {
                ["RepositoryUrl"] = "https://github.com/sparkfun/ESP32_Thing",
                ["License"] = "CC BY-SA 4.0",
                ["DesignFilesUrl"] = "https://github.com/sparkfun/ESP32_Thing/tree/main/Hardware",
            },
            sourceCapabilities: CatalogProviderCapabilities.Feed);

        var listing = CatalogNormalizer.Normalize(item);

        Assert.Equal(CatalogProviderCapabilities.Feed, listing.SourceCapabilities);
        Assert.Null(listing.StockQuantity);
        Assert.Equal("https://github.com/sparkfun/ESP32_Thing", listing.Fields["RepositoryUrl"]);
        Assert.Equal("CC BY-SA 4.0", listing.Fields["License"]);
    }

    [Fact]
    public void BatchNormalizationAddsDiagnosticsForJamecoManualFeedFallback()
    {
        var batch = new CatalogImportBatch(
            providerName: "Jameco",
            sourceCapabilities: CatalogProviderCapabilities.Feed | CatalogProviderCapabilities.Manual | CatalogProviderCapabilities.ScrapeRestricted,
            items:
            [
                new VendorCatalogItem(
                    providerName: "Jameco",
                    vendorSku: "51262",
                    manufacturerPartNumber: "7805",
                    manufacturer: "Major Brands",
                    description: "5V regulator TO-220",
                    priceBreaks: [new QuantityPriceBreak(1, Money.Usd(0.59m))],
                    stockQuantity: null,
                    datasheetUrl: null,
                    productUrl: new Uri("https://www.jameco.com/z/7805-voltage-regulator.html"),
                    sourceCapabilities: CatalogProviderCapabilities.Feed | CatalogProviderCapabilities.Manual | CatalogProviderCapabilities.ScrapeRestricted)
            ],
            diagnostics: []);

        var result = CatalogNormalizer.Normalize(batch);

        Assert.Single(result.Listings);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == CatalogDiagnosticCodes.ManualReviewRequired);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == CatalogDiagnosticCodes.ScrapeRestricted);
        Assert.All(result.Diagnostics, diagnostic => Assert.Equal("Jameco", diagnostic.ProviderName));
    }
}
