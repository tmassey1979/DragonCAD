using DragonCAD.Sourcing.Catalog;
using DragonCAD.Sourcing.Marketplace;
using DragonCAD.Sourcing.Vendors.ApiBacked;

namespace DragonCAD.Sourcing.Tests.Vendors.ApiBacked;

public sealed class ApiBackedCatalogAdapterTests
{
    [Fact]
    public void DigiKeyMapsSearchAndDetailFixturesToCandidateCatalogRecords()
    {
        var adapter = new DigiKeyApiBackedCatalogAdapter();

        var searchResult = adapter.MapSearchFixture(ReadFixture("digikey-product-search.json"));
        var detailResult = adapter.MapDetailFixture(ReadFixture("digikey-product-detail.json"));

        Assert.Empty(searchResult.Diagnostics);
        Assert.Empty(detailResult.Diagnostics);

        var searchRecord = Assert.Single(searchResult.Records);
        Assert.Equal("Digi-Key", searchRecord.ProviderName);
        Assert.Equal("296-12345-1-ND", searchRecord.VendorSku);
        Assert.Equal("LM7805CT/NOPB", searchRecord.ManufacturerPartNumber);
        Assert.Equal("Texas Instruments", searchRecord.Manufacturer);
        Assert.Equal(1234, searchRecord.StockQuantity);
        Assert.Equal(CatalogProviderCapabilities.Api, searchRecord.SourceCapabilities);
        Assert.Equal("Digi-Key Product Information V4", searchRecord.Fields["SourceApi"]);
        Assert.Equal("Tube", searchRecord.Fields["PackageType"]);
        Assert.Equal(Money.Usd(0.51m), searchRecord.PriceBreaks[1].UnitPrice);

        var detailRecord = Assert.Single(detailResult.Records);
        Assert.Equal("296-1389-5-ND", detailRecord.VendorSku);
        Assert.Equal("NE555P", detailRecord.ManufacturerPartNumber);
        Assert.Equal("Clock and Timing", detailRecord.Fields["Category"]);
    }

    [Fact]
    public void MouserMapsSearchAndDetailFixturesToCandidateCatalogRecords()
    {
        var adapter = new MouserApiBackedCatalogAdapter();

        var searchResult = adapter.MapSearchFixture(ReadFixture("mouser-part-search.json"));
        var detailResult = adapter.MapDetailFixture(ReadFixture("mouser-part-detail.json"));

        Assert.Empty(searchResult.Diagnostics);
        Assert.Empty(detailResult.Diagnostics);

        var searchRecord = Assert.Single(searchResult.Records);
        Assert.Equal("Mouser", searchRecord.ProviderName);
        Assert.Equal("595-LM7805CT", searchRecord.VendorSku);
        Assert.Equal("LM7805CT/NOPB", searchRecord.ManufacturerPartNumber);
        Assert.Equal("Texas Instruments", searchRecord.Manufacturer);
        Assert.Equal(1234, searchRecord.StockQuantity);
        Assert.Equal(CatalogProviderCapabilities.Api, searchRecord.SourceCapabilities);
        Assert.Equal("Mouser Search API", searchRecord.Fields["SourceApi"]);
        Assert.Equal("Power Management ICs", searchRecord.Fields["Category"]);
        Assert.Equal(Money.Usd(0.51m), searchRecord.PriceBreaks[1].UnitPrice);

        var detailRecord = Assert.Single(detailResult.Records);
        Assert.Equal("595-NE555P", detailRecord.VendorSku);
        Assert.Equal("NE555P", detailRecord.ManufacturerPartNumber);
        Assert.Equal("Tube", detailRecord.Fields["Packaging"]);
    }

    [Fact]
    public void AdafruitMapsProductApiFixturesToCandidateCatalogRecords()
    {
        var adapter = new AdafruitApiBackedCatalogAdapter();

        var result = adapter.MapProductFixture(
            ReadFixture("adafruit-products.json"),
            DateTimeOffset.Parse("2026-06-03T12:00:00Z"));

        var record = Assert.Single(result.Records);
        Assert.Empty(result.Diagnostics);
        Assert.Equal("Adafruit", record.ProviderName);
        Assert.Equal("ID-50", record.VendorSku);
        Assert.Equal("NE555P", record.ManufacturerPartNumber);
        Assert.Equal("Texas Instruments", record.Manufacturer);
        Assert.Equal("555 Timer Chip", record.Description);
        Assert.Equal(278, record.StockQuantity);
        Assert.Equal(CatalogProviderCapabilities.Api, record.SourceCapabilities);
        Assert.Equal(Money.Usd(1.95m), Assert.Single(record.PriceBreaks).UnitPrice);
        Assert.Equal("50", record.Fields["ProductId"]);
        Assert.Equal("2026-06-03T12:00:00.0000000+00:00", record.Fields["RetrievedAt"]);
    }

    [Fact]
    public void AdaptersExposeRateLimitCredentialAndTermsDiagnostics()
    {
        IApiBackedCatalogAdapter[] adapters =
        [
            new DigiKeyApiBackedCatalogAdapter(),
            new MouserApiBackedCatalogAdapter(),
            new AdafruitApiBackedCatalogAdapter(),
        ];

        foreach (var adapter in adapters)
        {
            Assert.False(string.IsNullOrWhiteSpace(adapter.Diagnostics.ProviderName));
            Assert.NotNull(adapter.Diagnostics.RateLimit);
            Assert.NotEmpty(adapter.Diagnostics.CredentialKeys);
            Assert.NotEqual(MarketplaceProviderTerms.None, adapter.Diagnostics.Terms);
        }

        Assert.Contains("client_id", adapters[0].Diagnostics.CredentialKeys);
        Assert.Contains("api_key", adapters[1].Diagnostics.CredentialKeys);
        Assert.Contains("api_key", adapters[2].Diagnostics.CredentialKeys);
        Assert.True(adapters[0].Diagnostics.Terms.HasFlag(MarketplaceProviderTerms.RequiresAttribution));
        Assert.True(adapters[1].Diagnostics.Terms.HasFlag(MarketplaceProviderTerms.RequiresSourceUrl));
        Assert.True(adapters[2].Diagnostics.Terms.HasFlag(MarketplaceProviderTerms.RequiresAttribution));
    }

    private static string ReadFixture(string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var path = Path.Combine(directory.FullName, "Vendors", "ApiBacked", "Fixtures", fileName);
            if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find ApiBacked fixture '{fileName}'.");
    }
}
