using DragonCAD.App.Marketplace.Sync.Results;
using DragonCAD.Sourcing;
using DragonCAD.Sourcing.Catalog;
using DragonCAD.Sourcing.Catalog.Sync;

namespace DragonCAD.App.Tests.Marketplace.Sync.Results;

public sealed class VendorCatalogSyncResultViewModelTests
{
    [Fact]
    public void FromRunResultMapsListingsAndDiagnosticsForDisplay()
    {
        var runResult = new VendorCatalogSyncRunResult(
            "Digi-Key",
            "LM7805",
            VendorCatalogSyncRunStatus.Completed,
            [
                new NormalizedCatalogListing(
                    "Digi-Key",
                    "296-12345-1-ND",
                    "LM7805CT/NOPB",
                    "Texas Instruments",
                    "Linear regulator",
                    PriceLadder.Normalize([new QuantityPriceBreak(1, Money.Usd(0.72m))]),
                    1234,
                    new Uri("https://example.test/lm7805.pdf"),
                    new Uri("https://example.test/product"),
                    new Dictionary<string, string> { ["PackageType"] = "Tube" },
                    CatalogProviderCapabilities.Api)
            ],
            [new CatalogImportDiagnostic(CatalogDiagnosticSeverity.Warning, "dk.warning", "Review package", "Digi-Key", "296-12345-1-ND")]);

        VendorCatalogSyncResultViewModel viewModel = VendorCatalogSyncResultViewModel.FromRunResult(runResult);

        Assert.Equal("2 total rows: 1 result, 1 diagnostic", viewModel.Summary);
        Assert.Equal("Digi-Key", viewModel.ProviderName);
        Assert.Equal("LM7805", viewModel.Query);
        VendorCatalogSyncResultRow resultRow = Assert.Single(viewModel.ResultRows);
        Assert.Equal("296-12345-1-ND", resultRow.VendorSku);
        Assert.Equal("LM7805CT/NOPB", resultRow.ManufacturerPartNumber);
        Assert.Equal("1,234 in stock from $0.72", resultRow.StockPriceSummary);
        Assert.Equal("Tube", resultRow.PackageSummary);
        Assert.True(resultRow.HasDatasheet);
        VendorCatalogSyncDiagnosticRow diagnosticRow = Assert.Single(viewModel.Diagnostics);
        Assert.Equal("Warning", diagnosticRow.Severity);
        Assert.Equal("dk.warning", diagnosticRow.Code);
    }
}
