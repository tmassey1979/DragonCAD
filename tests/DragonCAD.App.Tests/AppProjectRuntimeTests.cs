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
        Assert.Contains("x:Name=\"MarketplaceDashboardStrip\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Classes.active=\"{Binding IsMarketplaceTabActive}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Classes.active=\"{Binding IsComponentManagerTabActive}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Button.workbench-tab.active", mainWindow, StringComparison.Ordinal);
        Assert.Contains("MarketplaceBomCostRollup.TotalSummary", mainWindow, StringComparison.Ordinal);
        Assert.Contains("MarketplaceIntegrationStatus.NextActionText", mainWindow, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"MarketplaceBomCartStrip\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("MarketplaceCart.CartSummary", mainWindow, StringComparison.Ordinal);
        Assert.Contains("MarketplaceCart.EmptyStateMessage", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Create Order Draft", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Prepare CSV", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Marketplace.SelectedComponentTitle", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Marketplace.SelectedComponentVendorSummary", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Marketplace.SelectedComponentAvailabilitySummary", mainWindow, StringComparison.Ordinal);
        Assert.Contains("MarketplaceCart.Lines", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ProviderSourceSummary", mainWindow, StringComparison.Ordinal);
        Assert.Contains("NextActionLabel", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ActiveMarketplaceOrderDraft.ProviderOrderCountSummary", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ActiveMarketplaceOrderDraft.CheckoutReadinessStatus", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ComponentDeduplicationReview.BulkReviewReadinessSummary", mainWindow, StringComparison.Ordinal);
        Assert.Contains("TrustedLibraryPromotionQueue.PromotionReadinessSummary", mainWindow, StringComparison.Ordinal);
        Assert.Contains("TrustedLibraryPromotionQueue.PromotionActionSummary", mainWindow, StringComparison.Ordinal);
        Assert.Contains("PackageActionSummary", mainWindow, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowUsesRealMenusForWorkbenchCommands()
    {
        string mainWindow = ReadSourceFile("src", "DragonCAD.App", "MainWindow.axaml");

        Assert.Contains("<Menu x:Name=\"MainWorkbenchMenu\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("<MenuItem Header=\"_File\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("<MenuItem Header=\"_Edit\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("<MenuItem Header=\"_View\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("<MenuItem Header=\"_Place\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("<MenuItem Header=\"_Route\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("<MenuItem Header=\"_Marketplace\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("<MenuItem Header=\"Fa_brication\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("<MenuItem Header=\"_Help\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ShowHelpTabCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ShowComponentManagerTabCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ShowMarketplaceTabCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("PlaceSelectedComponentCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("PrepareMarketplaceBomCsvCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("RunVendorCatalogSyncCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("RunInUseVendorCatalogSyncCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Load7805SampleCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"WorkbenchCommandBar\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"WorkspaceTabStrip\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Library", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Design", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Manufacturing", mainWindow, StringComparison.Ordinal);
        Assert.Contains("MarketplaceIntegrationStatus.ActionStripSummary.NextActionText", mainWindow, StringComparison.Ordinal);
        Assert.Contains("VendorCatalogSync.RunReadinessSummary", mainWindow, StringComparison.Ordinal);
        Assert.Contains("VendorCatalogSync.NextActionSummary", mainWindow, StringComparison.Ordinal);
        Assert.Contains("MarketplaceBomCostRollup.ProcurementReadinessSummary", mainWindow, StringComparison.Ordinal);
        Assert.Contains("MarketplaceBomCostRollup.ProcurementActionSummary", mainWindow, StringComparison.Ordinal);
        Assert.Contains("InUseVendorCatalogSyncActionSummary.FreshnessLabel", mainWindow, StringComparison.Ordinal);
        Assert.Contains("InUseVendorCatalogSyncActionSummary.PrimaryActionLabel", mainWindow, StringComparison.Ordinal);
        Assert.Contains("FabricationChecklistPreview.ActionSummary.SummaryText", mainWindow, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowRendersMarkdownStyleHelpViewerAsWorkspaceTab()
    {
        string mainWindow = ReadSourceFile("src", "DragonCAD.App", "MainWindow.axaml");

        Assert.Contains("x:Name=\"HelpMarkdownViewer\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{Binding IsHelpTabActive}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ItemsSource=\"{Binding HelpSections}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ShowHelpTabCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Content=\"Help\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding Title}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding Body}\"", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"##", mainWindow, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowOrganizesRightRailIntoContextInspectorTabs()
    {
        string mainWindow = ReadSourceFile("src", "DragonCAD.App", "MainWindow.axaml");

        Assert.Contains("Text=\"Context Inspector\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ContextInspectorTabs\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("SelectedIndex=\"{Binding ContextInspectorSelectedIndex}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("<TabItem Header=\"AI\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ItemsSource=\"{Binding AiInspectorSummaryRows}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"AiInspectorActionStrip\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"AiInspectComponentsButton\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding ShowComponentManagerTabCommand}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"AiReviewSchematicButton\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding ShowSchematicTabCommand}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"AiGenerateCapsuleButton\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding ShowCapsulesTabCommand}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"AiReviewFabricationButton\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding ShowFabricationTabCommand}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"AiPromptComposer\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding AiPromptText, Mode=TwoWay}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding SubmitAiPromptCommand}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding AiPromptResponseText}\"", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("Ready for explainable engineering workflows once schematic and board graph projections are added.", mainWindow, StringComparison.Ordinal);
        Assert.Contains("<TabItem Header=\"Schematic\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("<TabItem Header=\"PCB\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("<TabItem Header=\"Grid\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"SchematicInspectorPanel\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"PcbInspectorPanel\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"GridInspectorPanel\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("TabControl#ContextInspectorTabs TabItem", mainWindow, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"FontSize\" Value=\"13\" />", mainWindow, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowOrganizesWorkbenchActionsIntoGroupedMenu()
    {
        string mainWindow = ReadSourceFile("src", "DragonCAD.App", "MainWindow.axaml");

        Assert.Contains("x:Name=\"WorkbenchActionMenu\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"WorkbenchActionHint\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("<MenuItem Header=\"_Library\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("<MenuItem Header=\"_Design\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("<MenuItem Header=\"_Marketplace\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("<MenuItem Header=\"_Manufacturing\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Quick Actions", mainWindow, StringComparison.Ordinal);
        Assert.Contains("PlaceSelectedComponentCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ActivateWireToolCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("CreateMarketplaceOrderDraftCommand", mainWindow, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowUsesProjectExplorerTree()
    {
        string mainWindow = ReadSourceFile("src", "DragonCAD.App", "MainWindow.axaml");

        Assert.Contains("x:Name=\"ProjectExplorerTree\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ColumnDefinitions=\"300,*,340\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("<TreeViewItem Header=\"DragonCAD Workspace\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("<TreeViewItem Header=\"Design\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("<TreeViewItem Header=\"Components\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Content=\"Library + Marketplace\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("<TreeViewItem Header=\"Manufacturing\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Content=\"Firmware\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Content=\"Capsules\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Content=\"Help\"", mainWindow, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowProjectExplorerLeavesNavigateWorkspaceTabs()
    {
        string mainWindow = ReadSourceFile("src", "DragonCAD.App", "MainWindow.axaml");

        Assert.Contains("Classes=\"tree-nav\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ProjectExplorerComponentsButton\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding ShowComponentManagerTabCommand}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ProjectExplorerMarketplaceButton\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding ShowMarketplaceTabCommand}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ProjectExplorerSchematicsButton\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding ShowSchematicTabCommand}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ProjectExplorerPcbButton\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding ShowPcbLayoutTabCommand}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ProjectExplorerDatasheetsButton\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding ShowDatasheetsTabCommand}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ProjectExplorerFabricationButton\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding ShowFabricationTabCommand}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ProjectExplorerBomOrdersButton\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Content=\"BOM and Orders\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ProjectExplorerFirmwareButton\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding ShowFirmwareTabCommand}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ProjectExplorerCapsulesButton\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding ShowCapsulesTabCommand}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ProjectExplorerHelpButton\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding ShowHelpTabCommand}\"", mainWindow, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowProjectExplorerLeavesTrackActiveWorkspaceTab()
    {
        string mainWindow = ReadSourceFile("src", "DragonCAD.App", "MainWindow.axaml");

        Assert.Contains("Style Selector=\"Button.tree-nav.active\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Classes.active=\"{Binding IsComponentManagerTabActive}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Classes.active=\"{Binding IsMarketplaceTabActive}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Classes.active=\"{Binding IsSchematicTabActive}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Classes.active=\"{Binding IsPcbLayoutTabActive}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Classes.active=\"{Binding IsDatasheetsTabActive}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Classes.active=\"{Binding IsFabricationTabActive}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Classes.active=\"{Binding IsFirmwareTabActive}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Classes.active=\"{Binding IsCapsulesTabActive}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Classes.active=\"{Binding IsHelpTabActive}\"", mainWindow, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowExposesFirmwareAndCapsuleWorkspaceTabs()
    {
        string mainWindow = ReadSourceFile("src", "DragonCAD.App", "MainWindow.axaml");

        Assert.Contains("Content=\"Firmware\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Content=\"Capsules\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{Binding IsFirmwareTabActive}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{Binding IsCapsulesTabActive}\"", mainWindow, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowRendersFirmwareAndCapsuleRowsInsteadOfFuturePlaceholders()
    {
        string mainWindow = ReadSourceFile("src", "DragonCAD.App", "MainWindow.axaml");

        Assert.Contains("ItemsSource=\"{Binding FirmwareWorkspaceRows}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ItemsSource=\"{Binding CapsuleReadinessRows}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Width=\"236\"", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("Future editor surface", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("Future capsule surface", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("Configure toolchain integration here before flashing hardware.", mainWindow, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowKeepsWorkspaceTabsCompactForEightTabShell()
    {
        string mainWindow = ReadSourceFile("src", "DragonCAD.App", "MainWindow.axaml");

        Assert.Contains("<Setter Property=\"Padding\" Value=\"12,8\" />", mainWindow, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"Focusable\" Value=\"False\" />", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Content=\"Components\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Content=\"Market\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Content=\"PCB\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Content=\"Fab\"", mainWindow, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowComponentInspectorHandlesLongComponentNames()
    {
        string mainWindow = ReadSourceFile("src", "DragonCAD.App", "MainWindow.axaml");

        Assert.Contains("x:Name=\"ComponentInspectorScrollViewer\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"SelectedComponentDisplayName\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"SelectedComponentIdentifier\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("TextTrimming=\"CharacterEllipsis\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Text=\"Inspector\"", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Component Inspector\"", mainWindow, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowComponentManagerRowsTrimLongImportedLibraryNames()
    {
        string mainWindow = ReadSourceFile("src", "DragonCAD.App", "MainWindow.axaml");

        Assert.Contains("x:Name=\"ComponentManagerDisplayName\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ComponentManagerManufacturerPartNumber\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("MinWidth=\"0\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("TextTrimming=\"CharacterEllipsis\"", mainWindow, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowIncludesComponentEditorWorkspace()
    {
        string mainWindow = ReadSourceFile("src", "DragonCAD.App", "MainWindow.axaml");

        Assert.Contains("ShowComponentEditorTabCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("OpenSelectedComponentEditorCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("IsComponentEditorTabActive", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ActiveComponentEditorWorkspace.ViewModel.DisplayName", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ActiveComponentEditorWorkspace.ValidationIssueDisplay", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ActiveComponentEditorWorkspace.ViewModel.PinSummaries", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ActiveComponentEditorWorkspace.ViewModel.PackageSummaries", mainWindow, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowKeepsSampleLoaderOutOfDocumentTabs()
    {
        string mainWindow = ReadSourceFile("src", "DragonCAD.App", "MainWindow.axaml");

        Assert.Contains("Load7805SampleCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("<MenuItem Header=\"Load 7805 Sample\"", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("Content=\"Load 7805 Sample\"", mainWindow, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowUsesConciseWorkbenchActionHint()
    {
        string mainWindow = ReadSourceFile("src", "DragonCAD.App", "MainWindow.axaml");

        Assert.Contains("x:Name=\"WorkbenchActionHint\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Text=\"Editor tools\"", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"{Binding PlacementStatus}\"\r\n                                   VerticalAlignment=\"Center\"\r\n                                   TextTrimming=\"CharacterEllipsis\"", mainWindow, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowFooterUsesWorkbenchStatusProjection()
    {
        string mainWindow = ReadSourceFile("src", "DragonCAD.App", "MainWindow.axaml");

        Assert.Contains("x:Name=\"WorkbenchStatusText\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding WorkbenchStatusText}\"", mainWindow, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowUsesIconMotionPadInComponentInspector()
    {
        string mainWindow = ReadSourceFile("src", "DragonCAD.App", "MainWindow.axaml");

        Assert.Contains("x:Name=\"ComponentInspectorMotionPad\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ToolTip.Tip=\"Move selected part left\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ToolTip.Tip=\"Move selected part right\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ToolTip.Tip=\"Move selected part up\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ToolTip.Tip=\"Move selected part down\"", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("Content=\"←\"", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("Content=\"→\"", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("Content=\"↑\"", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("Content=\"↓\"", mainWindow, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowKeepsSchematicHeaderFocusedOnPrimaryCommands()
    {
        string mainWindow = ReadSourceFile("src", "DragonCAD.App", "MainWindow.axaml");

        Assert.Contains("x:Name=\"SchematicCanvasHeader\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("SchematicHeaderPrimaryCommands", mainWindow, StringComparison.Ordinal);
        Assert.Contains("SchematicHeaderEditCommands", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("Content=\"L\"", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("Content=\"R\"", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("Content=\"U\"", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("Content=\"D\"", mainWindow, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowGroupsBoardHeaderCommandsAndLayerControls()
    {
        string mainWindow = ReadSourceFile("src", "DragonCAD.App", "MainWindow.axaml");

        Assert.Contains("x:Name=\"BoardCanvasHeader\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"BoardHeaderRouteCommands\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"BoardHeaderLayerControls\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"BoardHeaderStatusSummary\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Text=\"Layer\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ToolTip.Tip=\"Move selected trace to active layer\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ToolTip.Tip=\"Toggle active board layer visibility\"", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("ColumnDefinitions=\"Auto,Auto,Auto,Auto,Auto,Auto,Auto,Auto,Auto,Auto,*,Auto\"", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("Content=\"Trace Layer\"", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("Content=\"Hide/Show\"", mainWindow, StringComparison.Ordinal);
    }

    [Fact]
    public void BoardCanvasViewportUsesBoardZoomLevel()
    {
        string boardCanvas = ReadSourceFile("src", "DragonCAD.App", "BoardEditor", "BoardCanvasControl.cs");

        Assert.Contains("Editor?.ZoomLevel", boardCanvas, StringComparison.Ordinal);
        Assert.Contains("nameof(BoardEditorViewModel.ZoomLevel)", boardCanvas, StringComparison.Ordinal);
    }

    [Fact]
    public void BoardCanvasUsesLayerPaletteAndVisibilityForRenderedCopper()
    {
        string boardCanvas = ReadSourceFile("src", "DragonCAD.App", "BoardEditor", "BoardCanvasControl.cs");

        Assert.Contains("Editor.VisibleVias", boardCanvas, StringComparison.Ordinal);
        Assert.Contains("LayerBrushFor", boardCanvas, StringComparison.Ordinal);
        Assert.Contains("Color.Parse(layer.ColorHex)", boardCanvas, StringComparison.Ordinal);
        Assert.Contains("newEditor.Layers.CollectionChanged += ItemsChanged", boardCanvas, StringComparison.Ordinal);
        Assert.DoesNotContain("private static Pen TracePenForLayer(string layerName)", boardCanvas, StringComparison.Ordinal);
    }

    [Fact]
    public void BoardCanvasUsesHoverTargetsForMoveAffordance()
    {
        string boardEditor = ReadSourceFile("src", "DragonCAD.App", "BoardEditor", "BoardEditorViewModel.cs");
        string boardCanvas = ReadSourceFile("src", "DragonCAD.App", "BoardEditor", "BoardCanvasControl.cs");

        Assert.Contains("HoveredComponent", boardEditor, StringComparison.Ordinal);
        Assert.Contains("HoveredTrace", boardEditor, StringComparison.Ordinal);
        Assert.Contains("HoveredTraceSegmentIndex", boardEditor, StringComparison.Ordinal);
        Assert.Contains("HoveredVia", boardEditor, StringComparison.Ordinal);
        Assert.Contains("UpdateHoverAt", boardEditor, StringComparison.Ordinal);
        Assert.Contains("ClearHover", boardEditor, StringComparison.Ordinal);
        Assert.Contains("nameof(BoardEditorViewModel.HoveredComponent)", boardCanvas, StringComparison.Ordinal);
        Assert.Contains("nameof(BoardEditorViewModel.HoveredTrace)", boardCanvas, StringComparison.Ordinal);
        Assert.Contains("nameof(BoardEditorViewModel.HoveredVia)", boardCanvas, StringComparison.Ordinal);
        Assert.Contains("Editor.UpdateHoverAt(point)", boardCanvas, StringComparison.Ordinal);
        Assert.Contains("CursorForEditorState()", boardCanvas, StringComparison.Ordinal);
        Assert.Contains("HoverSelectionPen", boardCanvas, StringComparison.Ordinal);
        Assert.Contains("isHoveredTrace ? HoverTracePen", boardCanvas, StringComparison.Ordinal);
        Assert.Contains("isHoveredVia ? HoverSelectionPen", boardCanvas, StringComparison.Ordinal);
        Assert.Contains("isHovered ? HoverSelectionPen", boardCanvas, StringComparison.Ordinal);
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
        Assert.Contains("x:Name=\"BoardRouteCornerModeSelector\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ItemsSource=\"{Binding BoardEditor.RouteCornerModes}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("SelectedItem=\"{Binding BoardEditor.RouteCornerMode, Mode=TwoWay}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ToggleSelectedBoardLayerVisibilityCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"BoardLayerPalette\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ItemsSource=\"{Binding BoardLayerRows}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Background=\"{Binding ColorHex}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding $parent[Window].DataContext.SelectBoardLayerCommand}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding $parent[Window].DataContext.ToggleBoardLayerVisibilityCommand}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("IsChecked=\"{Binding IsVisible}\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("PlaceBoardViaCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ToolTip.Tip=\"Board Via\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("DeleteBoardSelectionCommand", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ToolTip.Tip=\"Delete Board Selection\"", mainWindow, StringComparison.Ordinal);
    }

    [Fact]
    public void SchematicCanvasUsesHoverTargetsForMoveAffordance()
    {
        string schematicCanvas = ReadSourceFile("src", "DragonCAD.App", "SchematicEditor", "SchematicCanvasControl.cs");

        Assert.Contains("nameof(SchematicEditorViewModel.HoveredComponent)", schematicCanvas, StringComparison.Ordinal);
        Assert.Contains("nameof(SchematicEditorViewModel.HoveredWire)", schematicCanvas, StringComparison.Ordinal);
        Assert.Contains("Editor?.HoveredComponent is not null", schematicCanvas, StringComparison.Ordinal);
        Assert.Contains("Editor?.HoveredWire is not null", schematicCanvas, StringComparison.Ordinal);
        Assert.Contains("HoverSelectionPen", schematicCanvas, StringComparison.Ordinal);
        Assert.Contains("ReferenceEquals(instance, Editor.HoveredComponent)", schematicCanvas, StringComparison.Ordinal);
        Assert.Contains("isHoveredWire ? HoverWirePen : WirePen", schematicCanvas, StringComparison.Ordinal);
        Assert.Contains("isHoveredWire ? Editor.HoveredWireSegmentIndex : null", schematicCanvas, StringComparison.Ordinal);
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
