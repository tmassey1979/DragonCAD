using DragonCAD.App.Marketplace.Smoke;
using DragonCAD.Sourcing.Catalog;
using DragonCAD.Sourcing.Catalog.Smoke;

namespace DragonCAD.App.Tests.Marketplace.Smoke;

public sealed class VendorLiveSmokeViewModelTests
{
    [Fact]
    public void DisabledGateShowsProviderRowsAndSafeDefaultMessaging()
    {
        var harness = new FakeVendorLiveSmokeHarness(isEnabled: false);

        VendorLiveSmokeViewModel viewModel = new(harness);

        Assert.False(viewModel.IsGateEnabled);
        Assert.Equal(VendorLiveSmokeHarness.GateEnvironmentVariable, viewModel.GateEnvironmentVariable);
        Assert.Equal("Live vendor smoke is disabled", viewModel.GateStatus);
        Assert.Equal("Set DRAGONCAD_VENDOR_LIVE_SMOKE=1 to enable real Digi-Key and Mouser calls.", viewModel.DisabledMessage);
        AssertSmokeCommandsCanExecute(viewModel, expected: false);
        Assert.Equal(["Digi-Key", "Mouser"], viewModel.Providers.Select(row => row.ProviderName));
        Assert.All(viewModel.Providers, row =>
        {
            Assert.Equal("Disabled", row.Status);
            Assert.Equal("Enable live smoke gate", row.ActionLabel);
            Assert.False(row.CanRun);
            Assert.Equal("Not run", row.LastResultSummary);
        });
    }

    [Fact]
    public async Task RunProviderUpdatesLastResultProviderRowAndDiagnostics()
    {
        var diagnostic = new CatalogImportDiagnostic(
            CatalogDiagnosticSeverity.Error,
            "Mouser.Http",
            "Mouser returned 401.",
            "Mouser",
            "595-NE555P");
        var harness = new FakeVendorLiveSmokeHarness(
            isEnabled: true,
            mouserResult: VendorLiveSmokeRunResult.Failed("Mouser", [diagnostic]));
        VendorLiveSmokeViewModel viewModel = new(harness)
        {
            QueryText = "NE555",
            ResultLimit = 5
        };

        await viewModel.RunMouserAsync();

        Assert.Equal([("Mouser", "NE555", 5)], harness.Calls);
        Assert.Equal("Mouser live smoke failed: 0 listings, 1 diagnostic.", viewModel.LastRunSummary);
        Assert.Equal("Failed", viewModel.LastRunStatus);

        VendorLiveSmokeProviderRow mouser = Assert.Single(viewModel.Providers, row => row.ProviderName == "Mouser");
        Assert.Equal("Failed", mouser.Status);
        Assert.Equal("0 listings, 1 diagnostic", mouser.LastResultSummary);
        Assert.Equal("Run Mouser smoke", mouser.ActionLabel);
        Assert.True(mouser.CanRun);

        VendorLiveSmokeDiagnosticRow row = Assert.Single(viewModel.Diagnostics);
        Assert.Equal("Error", row.Severity);
        Assert.Equal("Mouser.Http", row.Code);
        Assert.Equal("Mouser returned 401.", row.Message);
        Assert.Equal("Mouser", row.ProviderName);
        Assert.Equal("595-NE555P", row.VendorSku);
    }

    [Fact]
    public async Task RunAllCallsDigiKeyAndMouserWithCurrentQueryAndAggregatesResults()
    {
        var harness = new FakeVendorLiveSmokeHarness(
            isEnabled: true,
            digiKeyResult: new VendorLiveSmokeRunResult("Digi-Key", VendorLiveSmokeRunStatus.Succeeded, 2, []),
            mouserResult: new VendorLiveSmokeRunResult("Mouser", VendorLiveSmokeRunStatus.Succeeded, 3, []));
        VendorLiveSmokeViewModel viewModel = new(harness)
        {
            QueryText = "LM7805",
            ResultLimit = 4
        };

        await viewModel.RunAllAsync();

        Assert.Equal([("Digi-Key", "LM7805", 4), ("Mouser", "LM7805", 4)], harness.Calls);
        Assert.Equal("Live smoke completed: 2 providers, 5 listings, 0 diagnostics.", viewModel.LastRunSummary);
        Assert.Equal("Succeeded", viewModel.LastRunStatus);
        Assert.Empty(viewModel.Diagnostics);
        Assert.Equal("2 listings, 0 diagnostics", viewModel.Providers.Single(row => row.ProviderName == "Digi-Key").LastResultSummary);
        Assert.Equal("3 listings, 0 diagnostics", viewModel.Providers.Single(row => row.ProviderName == "Mouser").LastResultSummary);
    }

    [Fact]
    public async Task BlankQueryIsBlockedWithoutCallingHarness()
    {
        var harness = new FakeVendorLiveSmokeHarness(isEnabled: true);
        VendorLiveSmokeViewModel viewModel = new(harness)
        {
            QueryText = " "
        };

        await viewModel.RunDigiKeyAsync();

        Assert.Empty(harness.Calls);
        Assert.Equal("Enter a keyword before running live vendor smoke.", viewModel.LastRunSummary);
        Assert.Equal("Blocked", viewModel.LastRunStatus);
        VendorLiveSmokeDiagnosticRow diagnostic = Assert.Single(viewModel.Diagnostics);
        Assert.Equal("Blocked", diagnostic.Severity);
        Assert.Equal("DragonCAD.LiveSmoke.QueryRequired", diagnostic.Code);
    }

    [Fact]
    public void RefreshStatusEnablesProviderRowsAndCommandsWithoutCallingHarnessRuns()
    {
        var harness = new FakeVendorLiveSmokeHarness(isEnabled: false);
        VendorLiveSmokeViewModel viewModel = new(harness);

        harness.IsEnabled = true;

        viewModel.RefreshStatus();

        Assert.Empty(harness.Calls);
        Assert.Equal("Live vendor smoke is enabled", viewModel.GateStatus);
        AssertSmokeCommandsCanExecute(viewModel, expected: true);
        Assert.All(viewModel.Providers, row =>
        {
            Assert.Equal("Not run", row.Status);
            Assert.StartsWith("Run ", row.ActionLabel, StringComparison.Ordinal);
            Assert.True(row.CanRun);
            Assert.Equal("Not run", row.LastResultSummary);
        });
    }

    [Fact]
    public async Task RefreshStatusDisablesProviderRowsAndCommandsWithoutCallingHarnessRuns()
    {
        var harness = new FakeVendorLiveSmokeHarness(isEnabled: true);
        VendorLiveSmokeViewModel viewModel = new(harness)
        {
            QueryText = "NE555"
        };
        await viewModel.RunAllAsync();
        harness.Calls.Clear();

        harness.IsEnabled = false;

        viewModel.RefreshStatus();

        Assert.Empty(harness.Calls);
        Assert.Equal("Live vendor smoke is disabled", viewModel.GateStatus);
        AssertSmokeCommandsCanExecute(viewModel, expected: false);
        Assert.All(viewModel.Providers, row =>
        {
            Assert.Equal("Disabled", row.Status);
            Assert.Equal("Enable live smoke gate", row.ActionLabel);
            Assert.False(row.CanRun);
        });
    }

    private static void AssertSmokeCommandsCanExecute(VendorLiveSmokeViewModel viewModel, bool expected)
    {
        Assert.Equal(expected, viewModel.RunDigiKeyCommand.CanExecute(null));
        Assert.Equal(expected, viewModel.RunMouserCommand.CanExecute(null));
        Assert.Equal(expected, viewModel.RunAllCommand.CanExecute(null));
    }

    private sealed class FakeVendorLiveSmokeHarness : IVendorLiveSmokeHarness
    {
        private readonly VendorLiveSmokeRunResult digiKeyResult;
        private readonly VendorLiveSmokeRunResult mouserResult;

        public FakeVendorLiveSmokeHarness(
            bool isEnabled,
            VendorLiveSmokeRunResult? digiKeyResult = null,
            VendorLiveSmokeRunResult? mouserResult = null)
        {
            IsEnabled = isEnabled;
            this.digiKeyResult = digiKeyResult ?? new VendorLiveSmokeRunResult("Digi-Key", VendorLiveSmokeRunStatus.Succeeded, 1, []);
            this.mouserResult = mouserResult ?? new VendorLiveSmokeRunResult("Mouser", VendorLiveSmokeRunStatus.Succeeded, 1, []);
        }

        public List<(string ProviderName, string Query, int Limit)> Calls { get; } = [];

        public bool IsEnabled { get; set; }

        bool IVendorLiveSmokeHarness.IsEnabled() => IsEnabled;

        public Task<VendorLiveSmokeRunResult> RunDigiKeyKeywordSearchAsync(
            string keyword,
            int limit,
            CancellationToken cancellationToken)
        {
            Calls.Add(("Digi-Key", keyword, limit));
            return Task.FromResult(digiKeyResult);
        }

        public Task<VendorLiveSmokeRunResult> RunMouserKeywordSearchAsync(
            string keyword,
            int limit,
            CancellationToken cancellationToken)
        {
            Calls.Add(("Mouser", keyword, limit));
            return Task.FromResult(mouserResult);
        }
    }
}
