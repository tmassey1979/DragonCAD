using DragonCAD.Sourcing;
using DragonCAD.Sourcing.Catalog;
using DragonCAD.Sourcing.Catalog.Jameco;

namespace DragonCAD.Sourcing.Tests.Catalog.Jameco;

public sealed class JamecoCsvManualFeedParserTests
{
    private static readonly DateTimeOffset RetrievedAt = new(2026, 6, 1, 14, 30, 0, TimeSpan.Zero);

    [Fact]
    public void ParseMapsValidCsvRowsToManualFeedCatalogItems()
    {
        var csv = """
            Jameco SKU,Title,Manufacturer Part Number,Manufacturer,Product URL,Datasheet URL,Stock,Unit Price
            51262,5V Regulator TO-220,LM7805CT,Texas Instruments,https://www.jameco.com/z/LM7805CT.html,https://example.test/lm7805.pdf,1234,0.59
            """;

        var batch = JamecoCsvManualFeedParser.Parse(csv, RetrievedAt);

        var item = Assert.Single(batch.Items);
        Assert.Empty(batch.Diagnostics);
        Assert.Equal("Jameco", batch.ProviderName);
        Assert.Equal(CatalogProviderCapabilities.Feed | CatalogProviderCapabilities.Manual | CatalogProviderCapabilities.ScrapeRestricted, batch.SourceCapabilities);
        Assert.Equal("51262", item.VendorSku);
        Assert.Equal("LM7805CT", item.ManufacturerPartNumber);
        Assert.Equal("Texas Instruments", item.Manufacturer);
        Assert.Equal("5V Regulator TO-220", item.Description);
        Assert.Equal(1234, item.StockQuantity);
        Assert.Equal("https://www.jameco.com/z/LM7805CT.html", item.ProductUrl?.ToString());
        Assert.Equal("https://example.test/lm7805.pdf", item.DatasheetUrl?.ToString());
        Assert.Equal(new QuantityPriceBreak(1, Money.Usd(0.59m)), Assert.Single(item.PriceBreaks));
        Assert.Equal(RetrievedAt.UtcDateTime.ToString("O"), item.Fields["RetrievedAtUtc"]);
        Assert.Equal("51262", item.Fields["JamecoProductId"]);
    }

    [Fact]
    public void ParseAllowsMissingOptionalDatasheetUrl()
    {
        var csv = """
            Product ID,Description,Manufacturer Part Number,Manufacturer,Product URL,Datasheet URL,Stock Quantity,Unit Price
            2219001,Microcontroller breakout,DEV-2219001,Jameco ValuePro,https://www.jameco.com/z/DEV-2219001.html,,42,12.95
            """;

        var batch = JamecoCsvManualFeedParser.Parse(csv, RetrievedAt);

        var item = Assert.Single(batch.Items);
        Assert.Empty(batch.Diagnostics);
        Assert.Null(item.DatasheetUrl);
        Assert.Equal("2219001", item.VendorSku);
    }

    [Fact]
    public void ParseReportsInvalidNumericFieldsDeterministically()
    {
        var csv = """
            SKU,Title,Manufacturer Part Number,Stock,Unit Price
            1001,Bad stock row,ABC-1,many,1.25
            1002,Bad price row,ABC-2,12,free
            """;

        var batch = JamecoCsvManualFeedParser.Parse(csv, RetrievedAt);

        Assert.Empty(batch.Items);
        Assert.Collection(
            batch.Diagnostics,
            diagnostic =>
            {
                Assert.Equal(CatalogDiagnosticSeverity.Error, diagnostic.Severity);
                Assert.Equal(JamecoCatalogDiagnosticCodes.InvalidStockQuantity, diagnostic.Code);
                Assert.Equal("1001", diagnostic.VendorSku);
            },
            diagnostic =>
            {
                Assert.Equal(CatalogDiagnosticSeverity.Error, diagnostic.Severity);
                Assert.Equal(JamecoCatalogDiagnosticCodes.InvalidUnitPrice, diagnostic.Code);
                Assert.Equal("1002", diagnostic.VendorSku);
            });
    }

    [Fact]
    public void ParseReportsDuplicateSkuAndKeepsFirstRow()
    {
        var csv = """
            SKU,Title,Manufacturer Part Number,Stock,Unit Price
            51262,First listing,ABC-1,10,0.50
            51262,Duplicate listing,ABC-2,12,0.75
            """;

        var batch = JamecoCsvManualFeedParser.Parse(csv, RetrievedAt);

        var item = Assert.Single(batch.Items);
        var diagnostic = Assert.Single(batch.Diagnostics);
        Assert.Equal("First listing", item.Description);
        Assert.Equal(JamecoCatalogDiagnosticCodes.DuplicateSku, diagnostic.Code);
        Assert.Equal("51262", diagnostic.VendorSku);
    }

    [Fact]
    public void ParseReportsMissingRequiredSkuAndTitle()
    {
        var csv = """
            SKU,Title,Manufacturer Part Number,Stock,Unit Price
            ,No SKU,ABC-1,10,0.50
            51262,,ABC-2,12,0.75
            """;

        var batch = JamecoCsvManualFeedParser.Parse(csv, RetrievedAt);

        Assert.Empty(batch.Items);
        Assert.Collection(
            batch.Diagnostics,
            diagnostic => Assert.Equal(JamecoCatalogDiagnosticCodes.MissingSku, diagnostic.Code),
            diagnostic =>
            {
                Assert.Equal(JamecoCatalogDiagnosticCodes.MissingTitle, diagnostic.Code);
                Assert.Equal("51262", diagnostic.VendorSku);
            });
    }
}
