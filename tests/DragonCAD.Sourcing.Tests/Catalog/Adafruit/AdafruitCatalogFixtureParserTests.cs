using DragonCAD.Sourcing;
using DragonCAD.Sourcing.Catalog.Adafruit;

namespace DragonCAD.Sourcing.Tests.Catalog.Adafruit;

public sealed class AdafruitCatalogFixtureParserTests
{
    [Fact]
    public void ParseMapsValidProductJsonToNormalizedListingWithProvenance()
    {
        var retrievedAt = new DateTimeOffset(2026, 5, 31, 14, 45, 0, TimeSpan.Zero);
        const string json = """
        {
          "id": 50,
          "sku": "ID-50",
          "title": "555 Timer Chip",
          "mpn": "NE555P",
          "manufacturer": "Texas Instruments",
          "description": "Classic DIP-8 timer.",
          "url": "https://www.adafruit.com/product/50",
          "datasheet_url": "https://cdn-shop.adafruit.com/datasheets/ne555.pdf",
          "learn_url": "https://learn.adafruit.com/555-timer",
          "stock_quantity": 278,
          "price": 1.95
        }
        """;

        var result = AdafruitCatalogFixtureParser.Parse(json, retrievedAt);

        var listing = Assert.Single(result.Listings);
        Assert.Empty(result.Diagnostics);
        Assert.Equal("Adafruit", listing.ProviderName);
        Assert.Equal("ID-50", listing.VendorSku);
        Assert.Equal("NE555P", listing.ManufacturerPartNumber);
        Assert.Equal("Texas Instruments", listing.Manufacturer);
        Assert.Equal("555 Timer Chip", listing.Description);
        Assert.Equal(278, listing.StockQuantity);
        Assert.Equal(Money.Usd(1.95m), listing.PriceLadder.FindBestBreakFor(1).UnitPrice);
        Assert.Equal("https://www.adafruit.com/product/50", listing.ProductUrl?.ToString());
        Assert.Equal("https://cdn-shop.adafruit.com/datasheets/ne555.pdf", listing.DatasheetUrl?.ToString());
        Assert.Equal("50", listing.Fields["ProductId"]);
        Assert.Equal("https://learn.adafruit.com/555-timer", listing.Fields["LearnUrl"]);
        Assert.Equal("2026-05-31T14:45:00.0000000+00:00", listing.Fields["RetrievedAt"]);
    }

    [Fact]
    public void ParseAcceptsMissingOptionalDatasheetUrl()
    {
        var result = AdafruitCatalogFixtureParser.Parse(
            """
            {
              "id": 6100,
              "title": "Feather Board",
              "mpn": "ADA-6100",
              "url": "https://www.adafruit.com/product/6100",
              "stock_quantity": 12,
              "price": 9.95
            }
            """,
            DateTimeOffset.Parse("2026-05-31T15:00:00Z"));

        var listing = Assert.Single(result.Listings);
        Assert.Empty(result.Diagnostics);
        Assert.Equal("ID-6100", listing.VendorSku);
        Assert.Null(listing.DatasheetUrl);
    }

    [Fact]
    public void ParsePreservesUnavailableStateForOutOfStockProduct()
    {
        var result = AdafruitCatalogFixtureParser.Parse(
            """
            {
              "id": 9999,
              "title": "Retired Sensor",
              "mpn": "ADA-9999",
              "url": "https://www.adafruit.com/product/9999",
              "availability": "discontinued",
              "in_stock": false,
              "price": 2.50
            }
            """,
            DateTimeOffset.Parse("2026-05-31T15:30:00Z"));

        var listing = Assert.Single(result.Listings);
        Assert.Empty(result.Diagnostics);
        Assert.Null(listing.StockQuantity);
        Assert.Equal(Money.Usd(2.50m), listing.PriceLadder.FindBestBreakFor(1).UnitPrice);
        Assert.Equal("discontinued", listing.Fields["Availability"]);
        Assert.Equal("false", listing.Fields["InStock"]);
    }

    [Fact]
    public void ParseReturnsDiagnosticForMalformedJson()
    {
        var result = AdafruitCatalogFixtureParser.Parse(
            """{ "id": 50, "title": """,
            DateTimeOffset.Parse("2026-05-31T16:00:00Z"));

        Assert.Empty(result.Listings);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(AdafruitCatalogDiagnosticCodes.MalformedJson, diagnostic.Code);
        Assert.Equal("Adafruit", diagnostic.ProviderName);
        Assert.Contains("malformed", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }
}
