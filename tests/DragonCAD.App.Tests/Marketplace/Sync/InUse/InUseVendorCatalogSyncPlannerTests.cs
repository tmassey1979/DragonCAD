using DragonCAD.App.ComponentManager;
using DragonCAD.App.Marketplace.Sync;
using DragonCAD.App.Marketplace.Sync.InUse;
using DragonCAD.App.SchematicEditor;
using DragonCAD.Core.Geometry;

namespace DragonCAD.App.Tests.Marketplace.Sync.InUse;

public sealed class InUseVendorCatalogSyncPlannerTests
{
    [Fact]
    public void PlanCreatesDigiKeyAndMouserRequestsForPlacedPartsWithManufacturerPartNumbers()
    {
        SchematicComponentInstance[] placedParts =
        [
            Part("U1", "dragon:lm7805"),
            Part("U2", "dragon:lm7805"),
            Part("C1", "dragon:capacitor")
        ];

        ComponentManagerRow[] libraryRows =
        [
            Component("dragon:lm7805", "LM7805 5V Regulator", "Texas Instruments", "LM7805CT"),
            Component("dragon:capacitor", "Generic capacitor", "", "")
        ];

        VendorCatalogSyncProviderRow[] providers =
        [
            Provider("Digi-Key", canSync: true),
            Provider("Mouser", canSync: true),
            Provider("SparkFun", canSync: true)
        ];

        IReadOnlyList<InUseVendorCatalogSyncRequest> requests =
            InUseVendorCatalogSyncPlanner.Plan(placedParts, libraryRows, providers);

        Assert.Equal(["Digi-Key", "Mouser"], requests.Select(request => request.ProviderName).ToArray());
        Assert.All(requests, request =>
        {
            Assert.Equal("dragon:lm7805", request.ComponentId);
            Assert.Equal("LM7805CT", request.Query);
            Assert.Equal("U1, U2", request.ReferenceDesignators);
            Assert.True(request.CanRun);
        });
    }

    [Fact]
    public void PlanKeepsCredentialBlockedProviderVisibleForInUseParts()
    {
        SchematicComponentInstance[] placedParts = [Part("U1", "dragon:lm7805")];
        ComponentManagerRow[] libraryRows = [Component("dragon:lm7805", "LM7805 5V Regulator", "Texas Instruments", "LM7805CT")];
        VendorCatalogSyncProviderRow[] providers =
        [
            Provider("Digi-Key", canSync: false, credentialStatus: "Credential missing", nextActionLabel: "Add API credentials")
        ];

        InUseVendorCatalogSyncRequest request =
            Assert.Single(InUseVendorCatalogSyncPlanner.Plan(placedParts, libraryRows, providers));

        Assert.Equal("Digi-Key", request.ProviderName);
        Assert.False(request.CanRun);
        Assert.Equal("Add API credentials", request.ActionLabel);
        Assert.Equal("In use: U1", request.Reason);
    }

    [Fact]
    public void PlanMarksRecentlySyncedProviderRequestAsFresh()
    {
        SchematicComponentInstance[] placedParts = [Part("U1", "dragon:lm7805")];
        ComponentManagerRow[] libraryRows = [Component("dragon:lm7805", "LM7805 5V Regulator", "Texas Instruments", "LM7805CT")];
        VendorCatalogSyncProviderRow[] providers = [Provider("Digi-Key", canSync: true)];
        DateTimeOffset now = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
        InUseVendorCatalogSyncState[] syncStates =
        [
            new("dragon:lm7805", "Digi-Key", "LM7805CT", now.AddHours(-2), LastImportedCount: 3, LastWarningCount: 0)
        ];

        InUseVendorCatalogSyncRequest request = Assert.Single(
            InUseVendorCatalogSyncPlanner.Plan(placedParts, libraryRows, providers, syncStates, now));

        Assert.False(request.IsDue);
        Assert.Equal("Fresh", request.ActionLabel);
        Assert.Equal("Synced 2 hours ago: 3 candidates, 0 warnings", request.SyncStateLabel);
    }

    [Fact]
    public void PlanUsesProviderSpecificFreshnessWindows()
    {
        SchematicComponentInstance[] placedParts = [Part("U1", "dragon:lm7805")];
        ComponentManagerRow[] libraryRows = [Component("dragon:lm7805", "LM7805 5V Regulator", "Texas Instruments", "LM7805CT")];
        VendorCatalogSyncProviderRow[] providers =
        [
            Provider("Digi-Key", canSync: true),
            Provider("Mouser", canSync: true)
        ];
        DateTimeOffset now = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
        InUseVendorCatalogSyncState[] syncStates =
        [
            new("dragon:lm7805", "Digi-Key", "LM7805CT", now.AddHours(-10), LastImportedCount: 1, LastWarningCount: 0),
            new("dragon:lm7805", "Mouser", "LM7805CT", now.AddHours(-10), LastImportedCount: 1, LastWarningCount: 0)
        ];
        InUseVendorCatalogFreshnessPolicy policy = new(
            DefaultFreshnessWindow: TimeSpan.FromHours(24),
            ProviderFreshnessWindows: new Dictionary<string, TimeSpan>(StringComparer.OrdinalIgnoreCase)
            {
                ["Digi-Key"] = TimeSpan.FromHours(8),
                ["Mouser"] = TimeSpan.FromHours(24)
            });

        IReadOnlyList<InUseVendorCatalogSyncRequest> requests =
            InUseVendorCatalogSyncPlanner.Plan(placedParts, libraryRows, providers, syncStates, now, policy);

        InUseVendorCatalogSyncRequest digiKey = Assert.Single(requests, request => request.ProviderName == "Digi-Key");
        InUseVendorCatalogSyncRequest mouser = Assert.Single(requests, request => request.ProviderName == "Mouser");
        Assert.True(digiKey.IsDue);
        Assert.Equal("Sync now", digiKey.ActionLabel);
        Assert.False(mouser.IsDue);
        Assert.Equal("Fresh", mouser.ActionLabel);
    }

    private static SchematicComponentInstance Part(string referenceDesignator, string componentId) =>
        new(
            referenceDesignator.ToLowerInvariant(),
            referenceDesignator,
            componentId,
            componentId,
            new CadPoint(0, 0),
            ComponentSymbolPreview.Empty,
            ComponentFootprintPreview.Empty);

    private static ComponentManagerRow Component(
        string componentId,
        string displayName,
        string manufacturer,
        string manufacturerPartNumber) =>
        new(
            componentId,
            displayName,
            "IntegratedCircuit",
            manufacturer,
            manufacturerPartNumber,
            "BuiltIn",
            1,
            1,
            true,
            false,
            false,
            "1 symbol, 1 footprint",
            1,
            "Default",
            ComponentPackageSummary.Empty,
            null,
            [],
            ComponentSymbolPreview.Empty,
            ComponentFootprintPreview.Empty);

    private static VendorCatalogSyncProviderRow Provider(
        string providerName,
        bool canSync,
        string credentialStatus = "Credential configured",
        string nextActionLabel = "Sync now") =>
        new(
            providerName,
            IsEnabled: true,
            credentialStatus,
            LastSyncStatus: "Never synced",
            nextActionLabel,
            Warning: "",
            ResultSummary: "0 imported, 0 linked, 0 warnings",
            canSync);
}
