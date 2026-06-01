namespace DragonCAD.App.Tests;

public sealed class AppProjectRuntimeTests
{
    [Fact]
    public void AppProjectIsConfiguredAsRunnableAvaloniaDesktopApp()
    {
        string projectFile = ReadSourceFile("src", "DragonCAD.App", "DragonCAD.App.csproj");

        Assert.Contains("<OutputType>WinExe</OutputType>", projectFile, StringComparison.Ordinal);
        Assert.Contains("<PackageReference Include=\"Avalonia\"", projectFile, StringComparison.Ordinal);
        Assert.Contains("<PackageReference Include=\"Avalonia.Desktop\"", projectFile, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowShowsDragonCadFreshStartShell()
    {
        string mainWindow = ReadSourceFile("src", "DragonCAD.App", "MainWindow.axaml");

        Assert.Contains("DragonCAD", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Component-first hardware IDE", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Project Explorer", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Component Manager", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Component Library &amp; Marketplace", mainWindow, StringComparison.Ordinal);
        Assert.Contains("UnifiedComponentSourceRows", mainWindow, StringComparison.Ordinal);
        Assert.Contains("UnifiedComponentSourceSummary", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Sourcing Summary", mainWindow, StringComparison.Ordinal);
        Assert.Contains("MarketplaceOrderPlan.TotalSummary", mainWindow, StringComparison.Ordinal);
        Assert.Contains("MarketplaceBomExportPreview.CsvLines", mainWindow, StringComparison.Ordinal);
        Assert.Contains("PrepareMarketplaceBomCsvCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("CreateMarketplaceOrderDraftCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ActiveMarketplaceOrderDraft", mainWindow, StringComparison.Ordinal);
        Assert.Contains("MarketplaceCheckoutReadiness", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Checkout Readiness", mainWindow, StringComparison.Ordinal);
        Assert.Contains("AddCheckoutShippingProfileCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("AddCheckoutPaymentMethodCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("AddCheckoutProviderCredentialsCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("PlaceMarketplaceOrderCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ActiveMarketplacePlacedOrder", mainWindow, StringComparison.Ordinal);
        Assert.Contains("MarketplacePlacedOrderHistory", mainWindow, StringComparison.Ordinal);
        Assert.Contains("MarketplacePlacedOrderHistorySummary", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Order History", mainWindow, StringComparison.Ordinal);
        Assert.Contains("VerticalScrollBarVisibility=\"Auto\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("CheckoutCredentialedProviders", mainWindow, StringComparison.Ordinal);
        Assert.Contains("MarketplaceBomCsvExportFileName", mainWindow, StringComparison.Ordinal);
        Assert.Contains("MarketplaceBomCsvExportLineCount", mainWindow, StringComparison.Ordinal);
        Assert.Contains("IncrementMarketplaceCartLineCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("DecrementMarketplaceCartLineCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("RemoveMarketplaceCartLineCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("MarketplaceCart.Lines", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Marketplace Audit", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Audit Summary", mainWindow, StringComparison.Ordinal);
        Assert.Contains("MarketplaceAuditTimeline.Rows", mainWindow, StringComparison.Ordinal);
        Assert.Contains("MarketplaceAuditTimeline.SourceFilterOptions", mainWindow, StringComparison.Ordinal);
        Assert.Contains("MarketplaceAuditTimeline.ReviewStateFilterOptions", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Datasheet Link Review", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Datasheet Intake", mainWindow, StringComparison.Ordinal);
        Assert.Contains("DatasheetIntakeQueue.Items", mainWindow, StringComparison.Ordinal);
        Assert.Contains("DatasheetIntakeQueue.Summary", mainWindow, StringComparison.Ordinal);
        Assert.Contains("SubmitDatasheetIntakeSampleCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Add Sample Datasheet Intake", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Datasheet Candidate Linking", mainWindow, StringComparison.Ordinal);
        Assert.Contains("DatasheetCandidateLinking.Suggestions", mainWindow, StringComparison.Ordinal);
        Assert.Contains("DatasheetCandidateLinking.Summary", mainWindow, StringComparison.Ordinal);
        Assert.Contains("TargetTypeDisplay", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ConflictDisplay", mainWindow, StringComparison.Ordinal);
        Assert.Contains("DatasheetLinkReviewPlans", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Match Basis", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Review Warnings", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ApproveCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("RejectCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ReviewStateDisplay", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Promotion Queue", mainWindow, StringComparison.Ordinal);
        Assert.Contains("DatasheetLinkPromotionQueue", mainWindow, StringComparison.Ordinal);
        Assert.Contains("DatasheetLinkPromotionQueueSummary", mainWindow, StringComparison.Ordinal);
        Assert.Contains("CreateDatasheetLinkPromotionRecordCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ActiveDatasheetLinkPromotionRecord", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Promotion Records", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ExportJsonPreview", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ExportFileName", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ExportLineCount", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ApproveSafeDatasheetLinksCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Approve Safe Links", mainWindow, StringComparison.Ordinal);
        Assert.Contains("StageSafeDatasheetLinksCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Stage Safe Links", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ReadinessStatus", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Checklist", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Trusted Library Readiness", mainWindow, StringComparison.Ordinal);
        Assert.Contains("SaveDatasheetPromotionPreviewCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("StageAndSaveSafeDatasheetLinksCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ValidateDatasheetPromotionPackageCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("RecordValidatedDatasheetPromotionLedgerEntryCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("SaveTrustedLibraryWritePlanCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("SimulateTrustedLibraryWriteCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("StageTrustedLibraryCandidateCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Save Preview Artifact", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Stage + Save Safe Links", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Validate Package", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Record Ledger Entry", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Save Trusted-Library Plan", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Simulate Trusted-Library Write", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Stage Trusted-Library Candidate", mainWindow, StringComparison.Ordinal);
        Assert.Contains("SavedDatasheetPromotionArtifactPath", mainWindow, StringComparison.Ordinal);
        Assert.Contains("SavedDatasheetPromotionManifestPath", mainWindow, StringComparison.Ordinal);
        Assert.Contains("SavedDatasheetPromotionAuditPath", mainWindow, StringComparison.Ordinal);
        Assert.Contains("SavedDatasheetPromotionLedgerPath", mainWindow, StringComparison.Ordinal);
        Assert.Contains("SavedTrustedLibraryWritePlanPath", mainWindow, StringComparison.Ordinal);
        Assert.Contains("SavedTrustedLibraryWriteSimulationPath", mainWindow, StringComparison.Ordinal);
        Assert.Contains("SavedTrustedLibraryCandidatePath", mainWindow, StringComparison.Ordinal);
        Assert.Contains("DatasheetPromotionPackageValidationStatus", mainWindow, StringComparison.Ordinal);
        Assert.Contains("DatasheetPromotionTrustedLibraryGateStatus", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Trusted Library Gate", mainWindow, StringComparison.Ordinal);
        Assert.Contains("VendorCatalogSyncResult.ResultRows", mainWindow, StringComparison.Ordinal);
        Assert.Contains("VendorCatalogSyncResult.Diagnostics", mainWindow, StringComparison.Ordinal);
        Assert.Contains("API Sync Results", mainWindow, StringComparison.Ordinal);
        Assert.Contains("StockPriceSummary", mainWindow, StringComparison.Ordinal);
        Assert.Contains("PackageSummary", mainWindow, StringComparison.Ordinal);
        Assert.Contains("VendorCatalogSyncProviderOptions", mainWindow, StringComparison.Ordinal);
        Assert.Contains("SelectedVendorCatalogSyncProviderName", mainWindow, StringComparison.Ordinal);
        Assert.Contains("VendorCatalogSyncSearchText", mainWindow, StringComparison.Ordinal);
        Assert.Contains("RunVendorCatalogSyncCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("VendorCatalogSyncStatusText", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Run API Sync", mainWindow, StringComparison.Ordinal);
        Assert.Contains("InUseVendorCatalogSyncQueue", mainWindow, StringComparison.Ordinal);
        Assert.Contains("InUseVendorCatalogSyncSummary", mainWindow, StringComparison.Ordinal);
        Assert.Contains("InUseVendorCatalogFreshnessPolicySummary", mainWindow, StringComparison.Ordinal);
        Assert.Contains("DigiKeyInUseVendorFreshnessHours", mainWindow, StringComparison.Ordinal);
        Assert.Contains("MouserInUseVendorFreshnessHours", mainWindow, StringComparison.Ordinal);
        Assert.Contains("InUseVendorFreshnessValidationStatus", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ResetInUseVendorFreshnessPolicyCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Reset Defaults", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ClearInUseVendorCatalogSyncStateCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Clear Sync State", mainWindow, StringComparison.Ordinal);
        Assert.Contains("RunInUseVendorCatalogSyncCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ForceInUseVendorCatalogSyncCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("In-Use Vendor Refresh", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Run In-Use Sync", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Force Refresh", mainWindow, StringComparison.Ordinal);
        Assert.Contains("SyncStateLabel", mainWindow, StringComparison.Ordinal);
        Assert.Contains("MarketplaceBomCostRollup", mainWindow, StringComparison.Ordinal);
        Assert.Contains("BOM Cost Rollup", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ComponentDeduplicationReview", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Dedup Review", mainWindow, StringComparison.Ordinal);
        Assert.Contains("TrustedLibraryPromotionQueue", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Trusted-Library Promotion", mainWindow, StringComparison.Ordinal);
        Assert.Contains("VendorLiveSmoke", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Live Vendor Smoke", mainWindow, StringComparison.Ordinal);
        Assert.Contains("MarketplaceIntegrationStatus", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Integration Status", mainWindow, StringComparison.Ordinal);
        Assert.Contains("FabricationOrderingReadiness", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Ordering Readiness", mainWindow, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowIncludesSchematicIconToolRail()
    {
        string mainWindow = ReadSourceFile("src", "DragonCAD.App", "MainWindow.axaml");

        Assert.Contains("x:Name=\"SchematicToolRail\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("PathIcon", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ToolTip.Tip=\"Select\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ToolTip.Tip=\"Wire\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ToolTip.Tip=\"Mirror Part\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ToolTip.Tip=\"Duplicate Part\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ToolTip.Tip=\"Delete Part\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ToggleButton Command=\"{Binding ActivateSelectToolCommand}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("IsChecked=\"{Binding IsSelectToolActive}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("IsChecked=\"{Binding IsWireToolActive}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Selected Wire", mainWindow, StringComparison.Ordinal);
        Assert.Contains("SelectedSchematicWireNetName", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ToggleGridVisibilityCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ToggleGridStyleCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("GridStatusText", mainWindow, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"BoardToolRail\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ActivateBoardSelectToolCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ActivateBoardRouteToolCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("FinishBoardRouteCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("SelectedBoardLayerName", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ToggleSelectedBoardLayerVisibilityCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("PlaceBoardViaCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ToolTip.Tip=\"Board Via\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("DeleteBoardSelectionCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ToolTip.Tip=\"Delete Board Selection\"", mainWindow, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowIncludesSchematicKeyboardShortcuts()
    {
        string mainWindow = ReadSourceFile("src", "DragonCAD.App", "MainWindow.axaml");

        Assert.Contains("<KeyBinding Gesture=\"V\" Command=\"{Binding ActivateSelectToolCommand}\" />", mainWindow, StringComparison.Ordinal);
        Assert.Contains("<KeyBinding Gesture=\"W\" Command=\"{Binding ActivateWireToolCommand}\" />", mainWindow, StringComparison.Ordinal);
        Assert.Contains("<KeyBinding Gesture=\"R\" Command=\"{Binding RotateSelectedPartCommand}\" />", mainWindow, StringComparison.Ordinal);
        Assert.Contains("<KeyBinding Gesture=\"M\" Command=\"{Binding MirrorSelectedPartCommand}\" />", mainWindow, StringComparison.Ordinal);
        Assert.Contains("<KeyBinding Gesture=\"Ctrl+D\" Command=\"{Binding DuplicateSelectedPartCommand}\" />", mainWindow, StringComparison.Ordinal);
        Assert.Contains("<KeyBinding Gesture=\"Delete\" Command=\"{Binding DeleteActiveSelectionCommand}\" />", mainWindow, StringComparison.Ordinal);
        Assert.Contains("<KeyBinding Gesture=\"Escape\" Command=\"{Binding CancelActiveOperationCommand}\" />", mainWindow, StringComparison.Ordinal);
    }

    [Fact]
    public void AppInitializesThemeAndResourcesInCode()
    {
        string appCode = ReadSourceFile("src", "DragonCAD.App", "App.axaml.cs");

        Assert.DoesNotContain("AvaloniaXamlLoader.Load(this)", appCode, StringComparison.Ordinal);
        Assert.Contains("ConfigureThemeAndResources", appCode, StringComparison.Ordinal);
        Assert.Contains("RequestedThemeVariant = ThemeVariant.Dark", appCode, StringComparison.Ordinal);
        Assert.Contains("Styles.Add(new FluentTheme())", appCode, StringComparison.Ordinal);
    }

    private static string ReadSourceFile(params string[] pathParts)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string candidate = Path.Combine([directory.FullName, "..", "..", "..", "..", .. pathParts]);
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find source file '{Path.Combine(pathParts)}'.");
    }
}
