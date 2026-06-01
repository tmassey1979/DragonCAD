using DragonCAD.App.Marketplace.Sync;
using DragonCAD.App.Marketplace.Sync.Planning;

namespace DragonCAD.App.Tests.Marketplace.Sync.Planning;

public sealed class VendorCatalogSyncRunPlannerTests
{
    [Fact]
    public void DigiKeyMissingCredentialIsBlocked()
    {
        VendorCatalogSyncRunPlan plan = VendorCatalogSyncRunPlanner.Plan(Row(
            providerName: "Digi-Key",
            credentialStatus: "Credential missing",
            nextActionLabel: "Add API credentials",
            canSync: false));

        Assert.Equal("Digi-Key", plan.ProviderName);
        Assert.Equal(VendorCatalogSyncRunPlanStatus.Blocked, plan.Status);
        Assert.Equal(VendorCatalogSyncRunActionKind.None, plan.ActionKind);
        Assert.Equal("Add API credentials", plan.ActionLabel);
        Assert.Equal("Digi-Key catalog sync requires API credentials.", plan.Diagnostic);
        Assert.True(plan.RequiresCredential);
        Assert.False(plan.RequiresUserFile);
    }

    [Fact]
    public void MouserConfiguredCreatesCatalogSyncPlan()
    {
        VendorCatalogSyncRunPlan plan = VendorCatalogSyncRunPlanner.Plan(Row(
            providerName: "Mouser",
            credentialStatus: "Credential configured",
            nextActionLabel: "Sync now"));

        Assert.Equal(VendorCatalogSyncRunPlanStatus.Ready, plan.Status);
        Assert.Equal(VendorCatalogSyncRunActionKind.ApiCatalogSync, plan.ActionKind);
        Assert.Equal("Sync Mouser catalog", plan.ActionLabel);
        Assert.Equal("Prepare Mouser API catalog import request; no network call is executed by the planner.", plan.Summary);
        Assert.True(plan.RequiresCredential);
        Assert.False(plan.RequiresUserFile);
        Assert.Equal("", plan.Diagnostic);
    }

    [Fact]
    public void AdafruitPublicCatalogCreatesPublicCatalogPlan()
    {
        VendorCatalogSyncRunPlan plan = VendorCatalogSyncRunPlanner.Plan(Row(
            providerName: "Adafruit",
            credentialStatus: "Public catalog",
            nextActionLabel: "Sync public catalog"));

        Assert.Equal(VendorCatalogSyncRunPlanStatus.Ready, plan.Status);
        Assert.Equal(VendorCatalogSyncRunActionKind.PublicCatalogSync, plan.ActionKind);
        Assert.Equal("Sync Adafruit public catalog", plan.ActionLabel);
        Assert.Equal("Prepare Adafruit public catalog import; no network call is executed by the planner.", plan.Summary);
        Assert.False(plan.RequiresCredential);
        Assert.False(plan.RequiresUserFile);
    }

    [Fact]
    public void SparkFunPublicSourceCreatesSourceLibraryPlan()
    {
        VendorCatalogSyncRunPlan plan = VendorCatalogSyncRunPlanner.Plan(Row(
            providerName: "SparkFun",
            credentialStatus: "Public source",
            nextActionLabel: "Refresh source libraries"));

        Assert.Equal(VendorCatalogSyncRunPlanStatus.Ready, plan.Status);
        Assert.Equal(VendorCatalogSyncRunActionKind.SourceLibrarySync, plan.ActionKind);
        Assert.Equal("Refresh SparkFun source libraries", plan.ActionLabel);
        Assert.Equal("Prepare SparkFun source-library import from configured local/source package cache.", plan.Summary);
        Assert.False(plan.RequiresCredential);
        Assert.False(plan.RequiresUserFile);
    }

    [Fact]
    public void JamecoCreatesManualFeedImportPlan()
    {
        VendorCatalogSyncRunPlan plan = VendorCatalogSyncRunPlanner.Plan(Row(
            providerName: "Jameco",
            credentialStatus: "Manual feed",
            nextActionLabel: "Import vendor feed",
            canSync: false));

        Assert.Equal(VendorCatalogSyncRunPlanStatus.Ready, plan.Status);
        Assert.Equal(VendorCatalogSyncRunActionKind.ManualFeedImport, plan.ActionKind);
        Assert.Equal("Import Jameco vendor feed", plan.ActionLabel);
        Assert.Equal("Prepare Jameco manual/feed import; user-selected catalog file is required before execution.", plan.Summary);
        Assert.False(plan.RequiresCredential);
        Assert.True(plan.RequiresUserFile);
    }

    [Fact]
    public void DisabledProviderIsBlocked()
    {
        VendorCatalogSyncRunPlan plan = VendorCatalogSyncRunPlanner.Plan(Row(
            providerName: "Adafruit",
            isEnabled: false,
            credentialStatus: "Public catalog",
            nextActionLabel: "Disabled",
            canSync: false));

        Assert.Equal(VendorCatalogSyncRunPlanStatus.Blocked, plan.Status);
        Assert.Equal(VendorCatalogSyncRunActionKind.None, plan.ActionKind);
        Assert.Equal("Disabled", plan.ActionLabel);
        Assert.Equal("Adafruit catalog sync is disabled.", plan.Diagnostic);
        Assert.False(plan.RequiresCredential);
        Assert.False(plan.RequiresUserFile);
    }

    private static VendorCatalogSyncProviderRow Row(
        string providerName,
        string credentialStatus,
        string nextActionLabel,
        bool isEnabled = true,
        bool canSync = true) =>
        new(
            ProviderName: providerName,
            IsEnabled: isEnabled,
            CredentialStatus: credentialStatus,
            LastSyncStatus: "Never synced",
            NextActionLabel: nextActionLabel,
            Warning: "",
            ResultSummary: "0 imported, 0 linked, 0 warnings",
            CanSync: canSync);
}
