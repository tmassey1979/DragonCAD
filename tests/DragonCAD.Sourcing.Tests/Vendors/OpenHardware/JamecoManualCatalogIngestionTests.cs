using DragonCAD.Sourcing.Catalog;
using DragonCAD.Sourcing.Catalog.Jameco;
using DragonCAD.Sourcing.Vendors.OpenHardware;

namespace DragonCAD.Sourcing.Tests.Vendors.OpenHardware;

public sealed class JamecoManualCatalogIngestionTests
{
    private static readonly DateTimeOffset RetrievedAt = new(2026, 6, 1, 14, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset RefreshAfter = new(2026, 6, 8, 14, 30, 0, TimeSpan.Zero);

    [Fact]
    public void ParseAddsManifestProvenanceAndRefreshMetadataToManualCsvItems()
    {
        var source = JamecoSource();
        var csv = """
            Jameco SKU,Title,Manufacturer Part Number,Manufacturer,Product URL,Datasheet URL,Stock,Unit Price
            51262,5V Regulator TO-220,LM7805CT,Texas Instruments,https://www.jameco.com/z/LM7805CT.html,https://example.test/lm7805.pdf,1234,0.59
            """;

        var batch = JamecoManualCatalogIngestion.Parse(csv, source);

        var item = Assert.Single(batch.Items);
        Assert.Empty(batch.Diagnostics);
        Assert.Equal("Jameco", batch.ProviderName);
        Assert.Equal(CatalogProviderCapabilities.Feed | CatalogProviderCapabilities.Manual | CatalogProviderCapabilities.ScrapeRestricted, batch.SourceCapabilities);
        Assert.Equal("51262", item.VendorSku);
        Assert.Equal("jameco-curated-csv", item.Fields["SourceId"]);
        Assert.Equal("Curated MKT-006 CSV", item.Fields["ManualFeedName"]);
        Assert.Equal("manual-csv", item.Fields["Provenance"]);
        Assert.Equal(RetrievedAt.UtcDateTime.ToString("O"), item.Fields["RetrievedAtUtc"]);
        Assert.Equal(RefreshAfter.UtcDateTime.ToString("O"), item.Fields["RefreshAfterUtc"]);
    }

    [Fact]
    public void ParseKeepsJamecoCsvDiagnosticsForDuplicateManualRows()
    {
        var csv = """
            SKU,Title,Manufacturer Part Number,Stock,Unit Price
            51262,First listing,ABC-1,10,0.50
            51262,Duplicate listing,ABC-2,12,0.75
            """;

        var batch = JamecoManualCatalogIngestion.Parse(csv, JamecoSource());

        var item = Assert.Single(batch.Items);
        var diagnostic = Assert.Single(batch.Diagnostics);
        Assert.Equal("First listing", item.Description);
        Assert.Equal(JamecoCatalogDiagnosticCodes.DuplicateSku, diagnostic.Code);
        Assert.Equal("51262", diagnostic.VendorSku);
    }

    [Fact]
    public void ParseBlocksNonManualJamecoSourceModes()
    {
        var source = JamecoSource(OpenHardwareSourceMode.Scrape);

        var batch = JamecoManualCatalogIngestion.Parse("SKU,Title,Stock,Unit Price", source);

        Assert.Empty(batch.Items);
        var diagnostic = Assert.Single(batch.Diagnostics);
        Assert.Equal(OpenHardwareSourceManifestDiagnosticCodes.UnsupportedSourceMode, diagnostic.Code);
        Assert.Equal("Jameco", diagnostic.ProviderName);
        Assert.Contains("manual CSV", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static OpenHardwareSourceEntry JamecoSource(OpenHardwareSourceMode mode = OpenHardwareSourceMode.ManualCsvFeed)
    {
        return new OpenHardwareSourceEntry(
            ProviderName: "Jameco",
            SourceId: "jameco-curated-csv",
            Mode: mode,
            RepositoryUrl: null,
            LocalPath: "catalog/jameco/mkt-006.csv",
            CacheKey: null,
            LibraryPaths: [],
            ManualFeedName: "Curated MKT-006 CSV",
            RetrievedAtUtc: RetrievedAt,
            RefreshAfterUtc: RefreshAfter,
            AllowsScraping: false);
    }
}
