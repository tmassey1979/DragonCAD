using DragonCAD.App.Marketplace.Sync;

namespace DragonCAD.App.Tests.Marketplace.Sync;

public sealed class VendorCatalogSyncDashboardViewModelTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 31, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void RowsAreSortedInDeterministicMarketplaceProviderOrder()
    {
        VendorCatalogSyncDashboardViewModel viewModel = VendorCatalogSyncDashboardViewModel.FromStatuses(
            Now,
            [
                Status("Jameco", CatalogCredentialState.NotSupported),
                Status("SparkFun", CatalogCredentialState.NotRequired),
                Status("Digi-Key", CatalogCredentialState.Configured),
                Status("Adafruit", CatalogCredentialState.NotRequired),
                Status("Mouser", CatalogCredentialState.Missing)
            ]);

        Assert.Equal(
            ["Digi-Key", "Mouser", "Adafruit", "SparkFun", "Jameco"],
            viewModel.Providers.Select(provider => provider.ProviderName));
    }

    [Fact]
    public void DigiKeyAndMouserRequireCredentialsBeforeSyncing()
    {
        VendorCatalogSyncDashboardViewModel viewModel = VendorCatalogSyncDashboardViewModel.FromStatuses(
            Now,
            [
                Status("Digi-Key", CatalogCredentialState.Missing),
                Status("Mouser", CatalogCredentialState.Configured, lastSync: Now.AddHours(-2), imported: 42, linked: 12)
            ]);

        VendorCatalogSyncProviderRow digiKey = viewModel.Providers[0];
        Assert.Equal("Credential missing", digiKey.CredentialStatus);
        Assert.Equal("Add API credentials", digiKey.NextActionLabel);
        Assert.Equal("Digi-Key catalog sync requires API credentials before product, price, and stock data can be refreshed.", digiKey.Warning);
        Assert.False(digiKey.CanSync);

        VendorCatalogSyncProviderRow mouser = viewModel.Providers[1];
        Assert.Equal("Credential configured", mouser.CredentialStatus);
        Assert.Equal("Sync now", mouser.NextActionLabel);
        Assert.Equal("", mouser.Warning);
        Assert.True(mouser.CanSync);
        Assert.Equal("42 imported, 12 linked, 0 warnings", mouser.ResultSummary);
    }

    [Fact]
    public void AdafruitAndSparkFunExposePublicSourceSyncLabels()
    {
        VendorCatalogSyncDashboardViewModel viewModel = VendorCatalogSyncDashboardViewModel.FromStatuses(
            Now,
            [
                Status("Adafruit", CatalogCredentialState.NotRequired),
                Status("SparkFun", CatalogCredentialState.NotRequired, lastSync: Now.AddMinutes(-20), imported: 20, linked: 18, warnings: 1)
            ]);

        VendorCatalogSyncProviderRow adafruit = Assert.Single(viewModel.Providers, row => row.ProviderName == "Adafruit");
        Assert.Equal("Public catalog", adafruit.CredentialStatus);
        Assert.Equal("Sync public catalog", adafruit.NextActionLabel);
        Assert.True(adafruit.CanSync);

        VendorCatalogSyncProviderRow sparkFun = Assert.Single(viewModel.Providers, row => row.ProviderName == "SparkFun");
        Assert.Equal("Public source", sparkFun.CredentialStatus);
        Assert.Equal("Refresh source libraries", sparkFun.NextActionLabel);
        Assert.Equal("20 imported, 18 linked, 1 warning", sparkFun.ResultSummary);
    }

    [Fact]
    public void JamecoUsesManualFeedFallbackWarning()
    {
        VendorCatalogSyncDashboardViewModel viewModel = VendorCatalogSyncDashboardViewModel.FromStatuses(
            Now,
            [Status("Jameco", CatalogCredentialState.NotSupported)]);

        VendorCatalogSyncProviderRow jameco = Assert.Single(viewModel.Providers);
        Assert.Equal("Manual feed", jameco.CredentialStatus);
        Assert.Equal("Import vendor feed", jameco.NextActionLabel);
        Assert.Equal("Jameco sync is limited to manual/feed imports until an official catalog API is configured.", jameco.Warning);
        Assert.False(jameco.CanSync);
    }

    [Fact]
    public void LastSyncStatusDetectsStaleAndNeverSyncedProviders()
    {
        VendorCatalogSyncDashboardViewModel viewModel = VendorCatalogSyncDashboardViewModel.FromStatuses(
            Now,
            [
                Status("Digi-Key", CatalogCredentialState.Configured, lastSync: Now.AddDays(-9)),
                Status("Mouser", CatalogCredentialState.Configured),
                Status("Adafruit", CatalogCredentialState.NotRequired, lastSync: Now.AddHours(-3))
            ]);

        Assert.Equal("Stale: 9 days ago", viewModel.Providers[0].LastSyncStatus);
        Assert.Equal("Never synced", viewModel.Providers[1].LastSyncStatus);
        Assert.Equal("Last synced 3 hours ago", viewModel.Providers[2].LastSyncStatus);
    }

    [Fact]
    public void DisabledProviderReportsDisabledAction()
    {
        VendorCatalogSyncDashboardViewModel viewModel = VendorCatalogSyncDashboardViewModel.FromStatuses(
            Now,
            [Status("Adafruit", CatalogCredentialState.NotRequired, isEnabled: false)]);

        VendorCatalogSyncProviderRow row = Assert.Single(viewModel.Providers);
        Assert.Equal("Disabled", row.NextActionLabel);
        Assert.False(row.CanSync);
    }

    private static VendorCatalogSyncStatus Status(
        string providerName,
        CatalogCredentialState credentialState,
        DateTimeOffset? lastSync = null,
        int imported = 0,
        int linked = 0,
        int warnings = 0,
        bool isEnabled = true) =>
        new(
            ProviderName: providerName,
            IsEnabled: isEnabled,
            CredentialState: credentialState,
            LastSync: lastSync,
            ImportedCount: imported,
            LinkedCount: linked,
            WarningCount: warnings);
}
