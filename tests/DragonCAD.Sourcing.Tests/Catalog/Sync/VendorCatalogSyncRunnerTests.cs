using DragonCAD.Sourcing.Catalog;
using DragonCAD.Sourcing.Catalog.Sync;

namespace DragonCAD.Sourcing.Tests.Catalog.Sync;

public sealed class VendorCatalogSyncRunnerTests
{
    [Fact]
    public async Task SearchProviderReturnsListingsAndSummaryForConfiguredProvider()
    {
        var provider = new FakeVendorCatalogSearchProvider(
            "Digi-Key",
            new CatalogImportResult(
                [
                    Listing("Digi-Key", "296-12345-1-ND", "LM7805CT/NOPB"),
                    Listing("Digi-Key", "296-22222-1-ND", "NE555P")
                ],
                [new CatalogImportDiagnostic(CatalogDiagnosticSeverity.Warning, "dk.warning", "One warning", "Digi-Key", null)]));
        var runner = new VendorCatalogSyncRunner([provider]);

        VendorCatalogSyncRunResult result = await runner.SearchProviderAsync(
            "Digi-Key",
            "LM7805",
            limit: 10,
            CancellationToken.None);

        Assert.Equal(VendorCatalogSyncRunStatus.Completed, result.Status);
        Assert.Equal("Digi-Key", result.ProviderName);
        Assert.Equal("LM7805", result.Query);
        Assert.Equal(2, result.ImportedCount);
        Assert.Equal(1, result.WarningCount);
        Assert.Equal("2 catalog candidates from Digi-Key for 'LM7805' with 1 diagnostic.", result.Summary);
        Assert.Equal(["LM7805"], provider.Queries);
    }

    [Fact]
    public async Task SearchProviderReturnsBlockedResultForUnknownProvider()
    {
        var runner = new VendorCatalogSyncRunner([]);

        VendorCatalogSyncRunResult result = await runner.SearchProviderAsync(
            "Jameco",
            "LM7805",
            limit: 10,
            CancellationToken.None);

        Assert.Equal(VendorCatalogSyncRunStatus.Blocked, result.Status);
        Assert.Empty(result.Listings);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(VendorCatalogSyncDiagnosticCodes.ProviderUnavailable, diagnostic.Code);
        Assert.Equal("Jameco", diagnostic.ProviderName);
    }

    [Fact]
    public async Task SearchProviderReturnsBlockedResultForBlankQuery()
    {
        var runner = new VendorCatalogSyncRunner([new FakeVendorCatalogSearchProvider("Mouser", new CatalogImportResult([], []))]);

        VendorCatalogSyncRunResult result = await runner.SearchProviderAsync(
            "Mouser",
            " ",
            limit: 10,
            CancellationToken.None);

        Assert.Equal(VendorCatalogSyncRunStatus.Blocked, result.Status);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(VendorCatalogSyncDiagnosticCodes.MissingQuery, diagnostic.Code);
    }

    private static NormalizedCatalogListing Listing(
        string provider,
        string vendorSku,
        string manufacturerPartNumber) =>
        new(
            provider,
            vendorSku,
            manufacturerPartNumber,
            "Texas Instruments",
            "Linear regulator",
            PriceLadder.Normalize([new QuantityPriceBreak(1, Money.Usd(0.72m))]),
            100,
            new Uri("https://example.test/datasheet.pdf"),
            new Uri("https://example.test/product"),
            new Dictionary<string, string>(),
            CatalogProviderCapabilities.Api);

    private sealed class FakeVendorCatalogSearchProvider : IVendorCatalogSearchProvider
    {
        private readonly CatalogImportResult result;

        public FakeVendorCatalogSearchProvider(string providerName, CatalogImportResult result)
        {
            ProviderName = providerName;
            this.result = result;
        }

        public string ProviderName { get; }

        public List<string> Queries { get; } = [];

        public Task<CatalogImportResult> SearchAsync(string query, int limit, CancellationToken cancellationToken)
        {
            Queries.Add(query);
            return Task.FromResult(result);
        }
    }
}
