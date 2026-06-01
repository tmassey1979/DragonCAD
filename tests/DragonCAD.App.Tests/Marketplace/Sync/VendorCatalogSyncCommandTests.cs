using DragonCAD.App.Marketplace.Sync;
using DragonCAD.Sourcing;
using DragonCAD.Sourcing.Catalog;
using DragonCAD.Sourcing.Catalog.Sync;

namespace DragonCAD.App.Tests.Marketplace.Sync;

public sealed class VendorCatalogSyncCommandTests
{
    [Fact]
    public async Task RunVendorCatalogSyncUpdatesResultRowsAndStatus()
    {
        var service = new FakeVendorCatalogSyncSearchService(new VendorCatalogSyncRunResult(
            "Mouser",
            "NE555",
            VendorCatalogSyncRunStatus.Completed,
            [
                new NormalizedCatalogListing(
                    "Mouser",
                    "595-NE555P",
                    "NE555P",
                    "Texas Instruments",
                    "Precision timer",
                    PriceLadder.Normalize([new QuantityPriceBreak(1, Money.Usd(0.44m))]),
                    500,
                    new Uri("https://example.test/ne555.pdf"),
                    new Uri("https://example.test/ne555"),
                    new Dictionary<string, string> { ["Packaging"] = "Tube" },
                    CatalogProviderCapabilities.Api)
            ],
            []));
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 3,
            vendorCatalogSyncSearchService: service);
        viewModel.SelectedVendorCatalogSyncProviderName = "Mouser";
        viewModel.VendorCatalogSyncSearchText = "NE555";

        await viewModel.RunVendorCatalogSyncAsync();

        Assert.Equal("Mouser", service.ProviderName);
        Assert.Equal("NE555", service.Query);
        Assert.Equal("Mouser API sync completed: 1 catalog candidate from Mouser for 'NE555' with 0 diagnostics.", viewModel.VendorCatalogSyncStatusText);
        Assert.Equal("NE555P", Assert.Single(viewModel.VendorCatalogSyncResult.ResultRows).ManufacturerPartNumber);
        Assert.False(viewModel.IsVendorCatalogSyncRunning);
    }

    [Fact]
    public async Task RunVendorCatalogSyncShowsBlockedResultForBlankQueryWithoutCallingService()
    {
        var service = new FakeVendorCatalogSyncSearchService(new VendorCatalogSyncRunResult("Mouser", "NE555", VendorCatalogSyncRunStatus.Completed, [], []));
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 3,
            vendorCatalogSyncSearchService: service);
        viewModel.VendorCatalogSyncSearchText = " ";

        await viewModel.RunVendorCatalogSyncAsync();

        Assert.Equal(0, service.CallCount);
        Assert.Equal("Enter a part number or keyword before running vendor sync.", viewModel.VendorCatalogSyncStatusText);
        Assert.Equal(VendorCatalogSyncRunStatus.Blocked, viewModel.ActiveVendorCatalogSyncRunStatus);
        Assert.Empty(viewModel.VendorCatalogSyncResult.ResultRows);
        Assert.Single(viewModel.VendorCatalogSyncResult.Diagnostics);
    }

    [Fact]
    public async Task RunInUseVendorCatalogSyncRefreshesPlacedPartAcrossApiProviders()
    {
        string artifactDirectory = CreateTempArtifactDirectory();
        var service = new FakeVendorCatalogSyncSearchService();
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 20,
            datasheetPromotionArtifactDirectory: artifactDirectory,
            vendorCatalogSyncSearchService: service);
        var sourcedComponent = Assert.Single(
            viewModel.ComponentManager.Components,
            row => !string.IsNullOrWhiteSpace(row.ManufacturerPartNumber));
        viewModel.ComponentManager.SelectedComponent = sourcedComponent;
        viewModel.PlaceSelectedComponentCommand.Execute(null);
        viewModel.PlaceArmedComponentOnSchematicAt(default);

        await viewModel.RunInUseVendorCatalogSyncAsync();

        Assert.Equal(
            [("Digi-Key", sourcedComponent.ManufacturerPartNumber), ("Mouser", sourcedComponent.ManufacturerPartNumber)],
            service.Calls);
        Assert.Equal("In-use vendor sync completed: 2 requests, 2 catalog candidates.", viewModel.VendorCatalogSyncStatusText);
        Assert.Equal(2, viewModel.VendorCatalogSyncResult.ResultRows.Count);
        Assert.All(viewModel.VendorCatalogSyncResult.ResultRows, row => Assert.Equal(sourcedComponent.ManufacturerPartNumber, row.ManufacturerPartNumber));
    }

    [Fact]
    public async Task RunInUseVendorCatalogSyncDoesNotRepeatFreshProviderRequests()
    {
        string artifactDirectory = CreateTempArtifactDirectory();
        var service = new FakeVendorCatalogSyncSearchService();
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 20,
            datasheetPromotionArtifactDirectory: artifactDirectory,
            vendorCatalogSyncSearchService: service);
        var sourcedComponent = Assert.Single(
            viewModel.ComponentManager.Components,
            row => !string.IsNullOrWhiteSpace(row.ManufacturerPartNumber));
        viewModel.ComponentManager.SelectedComponent = sourcedComponent;
        viewModel.PlaceSelectedComponentCommand.Execute(null);
        viewModel.PlaceArmedComponentOnSchematicAt(default);

        await viewModel.RunInUseVendorCatalogSyncAsync();
        await viewModel.RunInUseVendorCatalogSyncAsync();

        Assert.Equal(2, service.CallCount);
        Assert.All(viewModel.InUseVendorCatalogSyncQueue, request =>
        {
            Assert.False(request.IsDue);
            Assert.Equal("Fresh", request.ActionLabel);
            Assert.StartsWith("Synced ", request.SyncStateLabel, StringComparison.Ordinal);
        });
        Assert.Equal("In-use vendor sync skipped: all 2 requests are fresh.", viewModel.VendorCatalogSyncStatusText);
    }

    [Fact]
    public async Task RunInUseVendorCatalogSyncPersistsFreshStateForNextSession()
    {
        string artifactDirectory = CreateTempArtifactDirectory();
        var firstService = new FakeVendorCatalogSyncSearchService();
        MainWindowViewModel firstSession = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 20,
            datasheetPromotionArtifactDirectory: artifactDirectory,
            vendorCatalogSyncSearchService: firstService);
        var sourcedComponent = Assert.Single(
            firstSession.ComponentManager.Components,
            row => !string.IsNullOrWhiteSpace(row.ManufacturerPartNumber));
        firstSession.ComponentManager.SelectedComponent = sourcedComponent;
        firstSession.PlaceSelectedComponentCommand.Execute(null);
        firstSession.PlaceArmedComponentOnSchematicAt(default);

        await firstSession.RunInUseVendorCatalogSyncAsync();

        var secondService = new FakeVendorCatalogSyncSearchService();
        MainWindowViewModel secondSession = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 20,
            datasheetPromotionArtifactDirectory: artifactDirectory,
            vendorCatalogSyncSearchService: secondService);
        secondSession.ComponentManager.SelectedComponent = sourcedComponent;
        secondSession.PlaceSelectedComponentCommand.Execute(null);
        secondSession.PlaceArmedComponentOnSchematicAt(default);

        await secondSession.RunInUseVendorCatalogSyncAsync();

        Assert.Equal(2, firstService.CallCount);
        Assert.Equal(0, secondService.CallCount);
        Assert.Equal("In-use vendor sync skipped: all 2 requests are fresh.", secondSession.VendorCatalogSyncStatusText);
    }

    [Fact]
    public async Task ForceInUseVendorCatalogSyncRepeatsFreshProviderRequests()
    {
        string artifactDirectory = CreateTempArtifactDirectory();
        var service = new FakeVendorCatalogSyncSearchService();
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 20,
            datasheetPromotionArtifactDirectory: artifactDirectory,
            vendorCatalogSyncSearchService: service);
        var sourcedComponent = Assert.Single(
            viewModel.ComponentManager.Components,
            row => !string.IsNullOrWhiteSpace(row.ManufacturerPartNumber));
        viewModel.ComponentManager.SelectedComponent = sourcedComponent;
        viewModel.PlaceSelectedComponentCommand.Execute(null);
        viewModel.PlaceArmedComponentOnSchematicAt(default);

        await viewModel.RunInUseVendorCatalogSyncAsync();
        await viewModel.ForceInUseVendorCatalogSyncAsync();

        Assert.Equal(
            [
                ("Digi-Key", sourcedComponent.ManufacturerPartNumber),
                ("Mouser", sourcedComponent.ManufacturerPartNumber),
                ("Digi-Key", sourcedComponent.ManufacturerPartNumber),
                ("Mouser", sourcedComponent.ManufacturerPartNumber)
            ],
            service.Calls);
        Assert.Equal("Forced in-use vendor sync completed: 2 requests, 2 catalog candidates.", viewModel.VendorCatalogSyncStatusText);
    }

    [Fact]
    public async Task ClearInUseVendorCatalogSyncStateMarksPlacedPartsDueAgainAndPersistsEmptyState()
    {
        string artifactDirectory = CreateTempArtifactDirectory();
        var firstService = new FakeVendorCatalogSyncSearchService();
        MainWindowViewModel firstSession = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 20,
            datasheetPromotionArtifactDirectory: artifactDirectory,
            vendorCatalogSyncSearchService: firstService);
        var sourcedComponent = Assert.Single(
            firstSession.ComponentManager.Components,
            row => !string.IsNullOrWhiteSpace(row.ManufacturerPartNumber));
        firstSession.ComponentManager.SelectedComponent = sourcedComponent;
        firstSession.PlaceSelectedComponentCommand.Execute(null);
        firstSession.PlaceArmedComponentOnSchematicAt(default);

        await firstSession.RunInUseVendorCatalogSyncAsync();
        Assert.All(firstSession.InUseVendorCatalogSyncQueue, request => Assert.False(request.IsDue));

        firstSession.ClearInUseVendorCatalogSyncStateCommand.Execute(null);

        Assert.Equal("In-use vendor sync state cleared.", firstSession.VendorCatalogSyncStatusText);
        Assert.All(firstSession.InUseVendorCatalogSyncQueue, request =>
        {
            Assert.True(request.IsDue);
            Assert.Equal("Never synced", request.SyncStateLabel);
            Assert.Equal("Sync now", request.ActionLabel);
        });

        MainWindowViewModel secondSession = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 20,
            datasheetPromotionArtifactDirectory: artifactDirectory,
            vendorCatalogSyncSearchService: new FakeVendorCatalogSyncSearchService());
        secondSession.ComponentManager.SelectedComponent = sourcedComponent;
        secondSession.PlaceSelectedComponentCommand.Execute(null);
        secondSession.PlaceArmedComponentOnSchematicAt(default);

        Assert.All(secondSession.InUseVendorCatalogSyncQueue, request =>
        {
            Assert.True(request.IsDue);
            Assert.Equal("Never synced", request.SyncStateLabel);
        });
    }

    [Fact]
    public void ChangingInUseVendorFreshnessHoursPersistsForNextSession()
    {
        string artifactDirectory = CreateTempArtifactDirectory();
        MainWindowViewModel firstSession = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 20,
            datasheetPromotionArtifactDirectory: artifactDirectory,
            vendorCatalogSyncSearchService: new FakeVendorCatalogSyncSearchService());

        firstSession.DigiKeyInUseVendorFreshnessHours = "6";
        firstSession.MouserInUseVendorFreshnessHours = "18";

        MainWindowViewModel secondSession = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 20,
            datasheetPromotionArtifactDirectory: artifactDirectory,
            vendorCatalogSyncSearchService: new FakeVendorCatalogSyncSearchService());

        Assert.Equal("6", secondSession.DigiKeyInUseVendorFreshnessHours);
        Assert.Equal("18", secondSession.MouserInUseVendorFreshnessHours);
        Assert.Equal("Freshness: Digi-Key 6h, Mouser 18h", secondSession.InUseVendorCatalogFreshnessPolicySummary);
    }

    [Fact]
    public void ResetInUseVendorFreshnessPolicyRestoresAndPersistsDefaults()
    {
        string artifactDirectory = CreateTempArtifactDirectory();
        MainWindowViewModel firstSession = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 20,
            datasheetPromotionArtifactDirectory: artifactDirectory,
            vendorCatalogSyncSearchService: new FakeVendorCatalogSyncSearchService());
        firstSession.DigiKeyInUseVendorFreshnessHours = "6";
        firstSession.MouserInUseVendorFreshnessHours = "18";

        firstSession.ResetInUseVendorFreshnessPolicyCommand.Execute(null);

        Assert.Equal("12", firstSession.DigiKeyInUseVendorFreshnessHours);
        Assert.Equal("24", firstSession.MouserInUseVendorFreshnessHours);
        Assert.Equal("Freshness: Digi-Key 12h, Mouser 24h", firstSession.InUseVendorCatalogFreshnessPolicySummary);

        MainWindowViewModel secondSession = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 20,
            datasheetPromotionArtifactDirectory: artifactDirectory,
            vendorCatalogSyncSearchService: new FakeVendorCatalogSyncSearchService());
        Assert.Equal("12", secondSession.DigiKeyInUseVendorFreshnessHours);
        Assert.Equal("24", secondSession.MouserInUseVendorFreshnessHours);
    }

    [Fact]
    public void InvalidInUseVendorFreshnessHoursShowsValidationWithoutPersisting()
    {
        string artifactDirectory = CreateTempArtifactDirectory();
        MainWindowViewModel firstSession = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 20,
            datasheetPromotionArtifactDirectory: artifactDirectory,
            vendorCatalogSyncSearchService: new FakeVendorCatalogSyncSearchService());

        firstSession.DigiKeyInUseVendorFreshnessHours = "abc";
        firstSession.MouserInUseVendorFreshnessHours = "-3";

        Assert.Equal("12", firstSession.DigiKeyInUseVendorFreshnessHours);
        Assert.Equal("24", firstSession.MouserInUseVendorFreshnessHours);
        Assert.Equal("Freshness hours must be a positive number.", firstSession.InUseVendorFreshnessValidationStatus);

        MainWindowViewModel secondSession = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 20,
            datasheetPromotionArtifactDirectory: artifactDirectory,
            vendorCatalogSyncSearchService: new FakeVendorCatalogSyncSearchService());
        Assert.Equal("12", secondSession.DigiKeyInUseVendorFreshnessHours);
        Assert.Equal("24", secondSession.MouserInUseVendorFreshnessHours);
    }

    private static string CreateTempArtifactDirectory() =>
        Path.Combine(Path.GetTempPath(), "dragoncad-in-use-sync-session-tests", Guid.NewGuid().ToString("N"));

    private sealed class FakeVendorCatalogSyncSearchService : IVendorCatalogSyncSearchService
    {
        private readonly VendorCatalogSyncRunResult? result;

        public FakeVendorCatalogSyncSearchService(VendorCatalogSyncRunResult? result = null)
        {
            this.result = result;
        }

        public int CallCount { get; private set; }

        public string ProviderName { get; private set; } = string.Empty;

        public string Query { get; private set; } = string.Empty;

        public List<(string ProviderName, string Query)> Calls { get; } = [];

        public Task<VendorCatalogSyncRunResult> SearchAsync(
            string providerName,
            string query,
            int limit,
            CancellationToken cancellationToken)
        {
            CallCount++;
            ProviderName = providerName;
            Query = query;
            Calls.Add((providerName, query));
            return Task.FromResult(result ?? CreateResult(providerName, query));
        }

        private static VendorCatalogSyncRunResult CreateResult(string providerName, string query) =>
            new(
                providerName,
                query,
                VendorCatalogSyncRunStatus.Completed,
                [
                    new NormalizedCatalogListing(
                        providerName,
                        $"{providerName}-{query}",
                        query,
                        "Texas Instruments",
                        "Matched in-use part",
                        PriceLadder.Normalize([new QuantityPriceBreak(1, Money.Usd(1.23m))]),
                        10,
                        null,
                        null,
                        new Dictionary<string, string>(),
                        CatalogProviderCapabilities.Api)
                ],
                []);
    }
}
