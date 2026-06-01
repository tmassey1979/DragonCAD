using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DragonCAD.App.BoardEditor;
using DragonCAD.App.BuiltInLibraries;
using DragonCAD.App.ComponentManager;
using DragonCAD.App.Datasheets;
using DragonCAD.App.Diagnostics;
using DragonCAD.App.Fabrication;
using DragonCAD.App.Fabrication.Export;
using DragonCAD.App.Fabrication.Handoff;
using DragonCAD.App.Fabrication.Ordering;
using DragonCAD.App.Marketplace;
using DragonCAD.App.Marketplace.Audit;
using DragonCAD.App.Marketplace.Bom;
using DragonCAD.App.Marketplace.Cart;
using DragonCAD.App.Marketplace.Cart.Commands;
using DragonCAD.App.Marketplace.Cart.Export;
using DragonCAD.App.Marketplace.Cart.Ordering;
using DragonCAD.App.Marketplace.Deduplication;
using DragonCAD.App.Marketplace.Filters;
using DragonCAD.App.Marketplace.Quality;
using DragonCAD.App.Marketplace.Smoke;
using DragonCAD.App.Marketplace.Status;
using DragonCAD.App.Marketplace.Sync;
using DragonCAD.App.Marketplace.Sync.InUse;
using DragonCAD.App.Marketplace.Sync.Results;
using DragonCAD.App.Marketplace.TrustedLibrary;
using DragonCAD.App.Placement;
using DragonCAD.App.SchematicEditor;
using DragonCAD.Core.Components.Catalog;
using DragonCAD.Core.Components.Identity;
using DragonCAD.Core.Components.Marketplace;
using DragonCAD.Core.Components.Marketplace.Provenance;
using DragonCAD.Core.Geometry;
using DragonCAD.Core.Libraries;
using DragonCAD.Sourcing;
using DragonCAD.Sourcing.Bom;
using DragonCAD.Sourcing.Catalog;
using DragonCAD.Sourcing.Catalog.DigiKey;
using DragonCAD.Sourcing.Catalog.Mouser;
using DragonCAD.Sourcing.Catalog.Sync;
using DragonCAD.Sourcing.Deduplication;
using DragonCAD.Sourcing.TrustedLibrary;

namespace DragonCAD.App;

public sealed class MainWindowViewModel : INotifyPropertyChanged, ISchematicPlacementTarget
{
    public const int DefaultInitialBuiltInDeviceLimit = int.MaxValue;
    private const int DefaultSearchResultLimit = 40;
    private readonly BuiltInHawkCadLibraryService builtInLibraryService;
    private readonly string datasheetPromotionArtifactDirectory;
    private string librarySearchText = "";
    private bool isLibrarySearchInProgress;
    private ComponentPlacementIntent? activePlacement;
    private string placementStatus = "Select a component, then choose Place to arm schematic placement.";
    private string activeSchematicTool = "Select";
    private string activeWorkspaceTab = "ComponentManager";
    private CadVector schematicDragOffset;
    private bool isDraggingSchematicComponent;
    private bool isDraggingSchematicWireSegment;
    private string selectedMarketplaceFilterPresetName = "All";
    private IReadOnlyList<MarketplaceComponentRow> filteredMarketplacePresetRows = [];
    private string marketplaceBomCsvExportText = "";
    private int marketplaceOrderDraftSequence;
    private MarketplaceInAppOrderDraftViewModel? activeMarketplaceOrderDraft;
    private MarketplaceCheckoutReadinessViewModel? marketplaceCheckoutReadiness;
    private MarketplacePlacedOrderViewModel? activeMarketplacePlacedOrder;
    private int marketplacePlacedOrderSequence;
    private bool hasCheckoutShippingProfile;
    private bool hasCheckoutPaymentMethod;
    private readonly HashSet<string> checkoutCredentialedProviders = new(StringComparer.Ordinal);
    private int datasheetLinkPromotionRecordSequence;
    private DatasheetLinkPromotionRecordViewModel? activeDatasheetLinkPromotionRecord;
    private string savedDatasheetPromotionArtifactPath = "";
    private string savedDatasheetPromotionManifestPath = "";
    private string savedDatasheetPromotionAuditPath = "";
    private string savedDatasheetPromotionLedgerPath = "";
    private string savedTrustedLibraryWritePlanPath = "";
    private string savedTrustedLibraryWriteSimulationPath = "";
    private string savedTrustedLibraryCandidatePath = "";
    private string datasheetPromotionPackageValidationStatus = "No local promotion package validated.";
    private readonly IVendorCatalogSyncSearchService vendorCatalogSyncSearchService;
    private string selectedVendorCatalogSyncProviderName = "Digi-Key";
    private string vendorCatalogSyncSearchText = "LM7805";
    private string vendorCatalogSyncStatusText = "Enter a vendor part number or keyword, then run API sync.";
    private bool isVendorCatalogSyncRunning;
    private VendorCatalogSyncResultViewModel vendorCatalogSyncResult = CreateSeededVendorCatalogSyncResult();
    private VendorCatalogSyncRunStatus activeVendorCatalogSyncRunStatus = VendorCatalogSyncRunStatus.Completed;
    private readonly List<InUseVendorCatalogSyncState> inUseVendorCatalogSyncStates = [];
    private readonly InUseVendorCatalogSyncStateStore inUseVendorCatalogSyncStateStore;
    private readonly InUseVendorCatalogFreshnessPolicyStore inUseVendorCatalogFreshnessPolicyStore;
    private InUseVendorCatalogFreshnessPolicy inUseVendorCatalogFreshnessPolicy = InUseVendorCatalogFreshnessPolicy.Default;
    private string inUseVendorFreshnessValidationStatus = "Freshness policy is valid.";

    private MainWindowViewModel(
        ComponentManagerViewModel componentManager,
        BuiltInLibraryViewModel builtInLibrary,
        BuiltInHawkCadLibraryService builtInLibraryService,
        string datasheetPromotionArtifactDirectory,
        IVendorCatalogSyncSearchService vendorCatalogSyncSearchService)
    {
        ComponentManager = componentManager;
        BuiltInLibrary = builtInLibrary;
        this.builtInLibraryService = builtInLibraryService;
        this.datasheetPromotionArtifactDirectory = datasheetPromotionArtifactDirectory;
        this.vendorCatalogSyncSearchService = vendorCatalogSyncSearchService;
        inUseVendorCatalogSyncStateStore = new InUseVendorCatalogSyncStateStore(DefaultInUseVendorCatalogSyncStatePath(datasheetPromotionArtifactDirectory));
        inUseVendorCatalogFreshnessPolicyStore = new InUseVendorCatalogFreshnessPolicyStore(DefaultInUseVendorCatalogFreshnessPolicyPath(datasheetPromotionArtifactDirectory));
        inUseVendorCatalogSyncStates.AddRange(inUseVendorCatalogSyncStateStore.Load());
        inUseVendorCatalogFreshnessPolicy = inUseVendorCatalogFreshnessPolicyStore.Load();
        SearchLibraryCommand = new AsyncDelegateCommand(ExecuteLibrarySearchAsync, () => !IsLibrarySearchInProgress);
        PlaceSelectedComponentCommand = new DelegateCommand(PlaceSelectedComponent);
        CancelPlacementCommand = new DelegateCommand(CancelPlacement);
        CancelActiveOperationCommand = new DelegateCommand(CancelActiveOperation);
        PlaceArmedComponentOnSchematicCommand = new DelegateCommand(PlaceArmedComponentOnSchematic);
        MoveSelectedLeftCommand = new DelegateCommand(() => MoveActiveEditorSelectionByGrid(new CadVector(-1, 0)));
        MoveSelectedRightCommand = new DelegateCommand(() => MoveActiveEditorSelectionByGrid(new CadVector(1, 0)));
        MoveSelectedUpCommand = new DelegateCommand(() => MoveActiveEditorSelectionByGrid(new CadVector(0, -1)));
        MoveSelectedDownCommand = new DelegateCommand(() => MoveActiveEditorSelectionByGrid(new CadVector(0, 1)));
        DeleteSelectedWireCommand = new DelegateCommand(DeleteSelectedWire);
        DeleteSelectedWireSegmentCommand = new DelegateCommand(DeleteSelectedWireSegment);
        InsertWireVertexCommand = new DelegateCommand(InsertWireVertex);
        DeleteSelectedPartCommand = new DelegateCommand(DeleteSelectedPart);
        DeleteActiveSelectionCommand = new DelegateCommand(DeleteActiveSelection);
        DuplicateSelectedPartCommand = new DelegateCommand(DuplicateSelectedPart);
        RotateSelectedPartCommand = new DelegateCommand(RotateSelectedPart);
        MirrorSelectedPartCommand = new DelegateCommand(MirrorSelectedPart);
        ActivateSelectToolCommand = new DelegateCommand(() => ActivateSchematicTool("Select"));
        ActivateWireToolCommand = new DelegateCommand(() => ActivateSchematicTool("Wire"));
        Load7805SampleCommand = new DelegateCommand(Load7805Sample);
        ZoomInCommand = new DelegateCommand(SchematicEditor.ZoomIn);
        ZoomOutCommand = new DelegateCommand(SchematicEditor.ZoomOut);
        ToggleGridVisibilityCommand = new DelegateCommand(ToggleGridVisibility);
        ToggleGridStyleCommand = new DelegateCommand(ToggleGridStyle);
        IncreaseGridSpacingCommand = new DelegateCommand(() => ChangeGridSpacing(1m));
        DecreaseGridSpacingCommand = new DelegateCommand(() => ChangeGridSpacing(-1m));
        ActivateBoardSelectToolCommand = new DelegateCommand(ActivateBoardSelectTool);
        ActivateBoardRouteToolCommand = new DelegateCommand(ActivateBoardRouteTool);
        FinishBoardRouteCommand = new DelegateCommand(FinishBoardRoute);
        PlaceBoardViaCommand = new DelegateCommand(PlaceBoardVia);
        InsertBoardViaIntoSelectedTraceSegmentCommand = new DelegateCommand(InsertBoardViaIntoSelectedTraceSegment);
        DeleteBoardSelectionCommand = new DelegateCommand(DeleteBoardSelection);
        MoveSelectedBoardTraceToLayerCommand = new DelegateCommand(MoveSelectedBoardTraceToLayer);
        RotateSelectedBoardComponentCommand = new DelegateCommand(RotateSelectedBoardComponent);
        MirrorSelectedBoardComponentCommand = new DelegateCommand(MirrorSelectedBoardComponent);
        ToggleSelectedBoardLayerVisibilityCommand = new DelegateCommand(ToggleSelectedBoardLayerVisibility);
        ShowComponentManagerTabCommand = new DelegateCommand(() => ActiveWorkspaceTab = "ComponentManager");
        ShowMarketplaceTabCommand = new DelegateCommand(() => ActiveWorkspaceTab = "Marketplace");
        ShowSchematicTabCommand = new DelegateCommand(() => ActiveWorkspaceTab = "Schematic");
        ShowPcbLayoutTabCommand = new DelegateCommand(() => ActiveWorkspaceTab = "PcbLayout");
        ShowDatasheetsTabCommand = new DelegateCommand(() => ActiveWorkspaceTab = "Datasheets");
        ShowFabricationTabCommand = new DelegateCommand(() => ActiveWorkspaceTab = "Fabrication");
        AddSelectedMarketplaceComponentToCartCommand = new DelegateCommand(AddSelectedMarketplaceComponentToCart);
        AddMarketplaceComponentToCartCommand = new DelegateCommand(AddMarketplaceComponentToCart);
        IncrementMarketplaceCartLineCommand = new DelegateCommand(IncrementMarketplaceCartLine);
        DecrementMarketplaceCartLineCommand = new DelegateCommand(DecrementMarketplaceCartLine);
        RemoveMarketplaceCartLineCommand = new DelegateCommand(RemoveMarketplaceCartLine);
        PrepareMarketplaceBomCsvCommand = new DelegateCommand(PrepareMarketplaceBomCsv);
        CreateMarketplaceOrderDraftCommand = new DelegateCommand(CreateMarketplaceOrderDraft);
        AddCheckoutShippingProfileCommand = new DelegateCommand(AddCheckoutShippingProfile);
        AddCheckoutPaymentMethodCommand = new DelegateCommand(AddCheckoutPaymentMethod);
        AddCheckoutProviderCredentialsCommand = new DelegateCommand(AddCheckoutProviderCredentials);
        PlaceMarketplaceOrderCommand = new DelegateCommand(PlaceMarketplaceOrder);
        ApplyMarketplaceFilterPresetCommand = new DelegateCommand(ApplyMarketplaceFilterPreset);
        SubmitDatasheetIntakeSampleCommand = new DelegateCommand(SubmitDatasheetIntakeSample);
        CreateDatasheetLinkPromotionRecordCommand = new DelegateCommand(CreateDatasheetLinkPromotionRecord);
        ApproveSafeDatasheetLinksCommand = new DelegateCommand(ApproveSafeDatasheetLinks);
        StageSafeDatasheetLinksCommand = new DelegateCommand(StageSafeDatasheetLinks);
        StageAndSaveSafeDatasheetLinksCommand = new DelegateCommand(StageAndSaveSafeDatasheetLinks);
        SaveDatasheetPromotionPreviewCommand = new DelegateCommand(SaveDatasheetPromotionPreview);
        ValidateDatasheetPromotionPackageCommand = new DelegateCommand(ValidateDatasheetPromotionPackage);
        RecordValidatedDatasheetPromotionLedgerEntryCommand = new DelegateCommand(RecordValidatedDatasheetPromotionLedgerEntry);
        SaveTrustedLibraryWritePlanCommand = new DelegateCommand(SaveTrustedLibraryWritePlan);
        SimulateTrustedLibraryWriteCommand = new DelegateCommand(SimulateTrustedLibraryWrite);
        StageTrustedLibraryCandidateCommand = new DelegateCommand(StageTrustedLibraryCandidate);
        ResetInUseVendorFreshnessPolicyCommand = new DelegateCommand(ResetInUseVendorFreshnessPolicy);
        ClearInUseVendorCatalogSyncStateCommand = new DelegateCommand(ClearInUseVendorCatalogSyncState);
        RunVendorCatalogSyncCommand = new AsyncDelegateCommand(RunVendorCatalogSyncAsync, () => !IsVendorCatalogSyncRunning);
        RunInUseVendorCatalogSyncCommand = new AsyncDelegateCommand(RunInUseVendorCatalogSyncAsync, () => !IsVendorCatalogSyncRunning);
        ForceInUseVendorCatalogSyncCommand = new AsyncDelegateCommand(ForceInUseVendorCatalogSyncAsync, () => !IsVendorCatalogSyncRunning);
        Marketplace.PropertyChanged += MarketplacePropertyChanged;
        SchematicEditor.PropertyChanged += SchematicEditorPropertyChanged;
        BoardEditor.PropertyChanged += BoardEditorPropertyChanged;
        Fabrication.PropertyChanged += FabricationPropertyChanged;
        foreach (DatasheetLinkReviewRow row in DatasheetLinkReviewPlans)
        {
            row.PropertyChanged += DatasheetLinkReviewRowPropertyChanged;
        }

        SeedMarketplaceFilterPresets();
        filteredMarketplacePresetRows = MarketplaceFilterPresetApplicator.Apply(Marketplace.Components, MarketplaceFilterPreset.All);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ComponentManagerViewModel ComponentManager { get; }

    public SchematicEditorViewModel SchematicEditor { get; } = new();

    public BoardEditorViewModel BoardEditor { get; } = new();

    public BuiltInLibraryViewModel BuiltInLibrary { get; private set; }

    public MarketplaceBrowserViewModel Marketplace { get; } = CreateSeededMarketplaceBrowser();

    public MarketplaceCartViewModel MarketplaceCart { get; } = new();

    public MarketplaceAuditTimelineViewModel MarketplaceAuditTimeline { get; } =
        MarketplaceAuditTimelineViewModel.FromRecords(SeededMarketplaceProvenance);

    public IReadOnlyList<UnifiedComponentSourceRow> UnifiedComponentSourceRows =>
        ComponentManager.Components
            .Select(component => new UnifiedComponentSourceRow(
                "Built-in Library",
                component.DisplayName,
                component.Manufacturer,
                component.ManufacturerPartNumber,
                component.Kind,
                component.ComponentId,
                component.CapabilitySummary))
            .Concat(Marketplace.Components.Select(component => new UnifiedComponentSourceRow(
                "Vendor Marketplace",
                component.DisplayName,
                component.Manufacturer,
                component.ManufacturerPartNumber,
                component.Category,
                component.CanonicalComponentId,
                component.StockPriceSummary)))
            .ToArray();

    public string UnifiedComponentSourceSummary =>
        $"{ComponentManager.Components.Count} built-in + {Marketplace.Components.Count} marketplace components";

    public MarketplaceBomExportPreviewViewModel MarketplaceBomExportPreview =>
        MarketplaceBomExportPreviewViewModel.FromCart(MarketplaceCart);

    public MarketplaceOrderPlanViewModel MarketplaceOrderPlan =>
        MarketplaceOrderPlanViewModel.FromCart(MarketplaceCart);

    public MarketplaceInAppOrderDraftViewModel? ActiveMarketplaceOrderDraft
    {
        get => activeMarketplaceOrderDraft;
        private set
        {
            if (activeMarketplaceOrderDraft == value)
            {
                return;
            }

            activeMarketplaceOrderDraft = value;
            OnPropertyChanged();
        }
    }

    public MarketplaceCheckoutReadinessViewModel? MarketplaceCheckoutReadiness
    {
        get => marketplaceCheckoutReadiness;
        private set
        {
            if (marketplaceCheckoutReadiness == value)
            {
                return;
            }

            marketplaceCheckoutReadiness = value;
            OnPropertyChanged();
        }
    }

    public MarketplacePlacedOrderViewModel? ActiveMarketplacePlacedOrder
    {
        get => activeMarketplacePlacedOrder;
        private set
        {
            if (activeMarketplacePlacedOrder == value)
            {
                return;
            }

            activeMarketplacePlacedOrder = value;
            OnPropertyChanged();
        }
    }

    public IReadOnlyList<MarketplacePlacedOrderViewModel> MarketplacePlacedOrderHistory { get; private set; } = [];

    public string MarketplacePlacedOrderHistorySummary => $"Local order records: {MarketplacePlacedOrderHistory.Count}";

    public bool HasCheckoutShippingProfile
    {
        get => hasCheckoutShippingProfile;
        private set
        {
            if (hasCheckoutShippingProfile == value)
            {
                return;
            }

            hasCheckoutShippingProfile = value;
            OnPropertyChanged();
        }
    }

    public bool HasCheckoutPaymentMethod
    {
        get => hasCheckoutPaymentMethod;
        private set
        {
            if (hasCheckoutPaymentMethod == value)
            {
                return;
            }

            hasCheckoutPaymentMethod = value;
            OnPropertyChanged();
        }
    }

    public IReadOnlyList<string> CheckoutCredentialedProviders =>
        checkoutCredentialedProviders.OrderBy(provider => provider, StringComparer.Ordinal).ToArray();

    public string MarketplaceBomCsvExportFileName => "dragoncad-bom.csv";

    public string MarketplaceBomCsvExportText
    {
        get => marketplaceBomCsvExportText;
        private set
        {
            if (marketplaceBomCsvExportText == value)
            {
                return;
            }

            marketplaceBomCsvExportText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(MarketplaceBomCsvExportLineCount));
        }
    }

    public int MarketplaceBomCsvExportLineCount =>
        string.IsNullOrEmpty(MarketplaceBomCsvExportText)
            ? 0
            : MarketplaceBomCsvExportText.Split(Environment.NewLine, StringSplitOptions.None).Length;

    public MarketplaceSavedFilterPresetStore MarketplaceFilterPresetStore { get; } = new();

    public IReadOnlyList<MarketplaceFilterPreset> MarketplaceFilterPresets => MarketplaceFilterPresetStore.Presets;

    public IReadOnlyList<MarketplaceComponentRow> FilteredMarketplacePresetRows => filteredMarketplacePresetRows;

    public string SelectedMarketplaceFilterPresetName
    {
        get => selectedMarketplaceFilterPresetName;
        private set
        {
            if (selectedMarketplaceFilterPresetName == value)
            {
                return;
            }

            selectedMarketplaceFilterPresetName = value;
            OnPropertyChanged();
        }
    }

    public IReadOnlyList<MarketplaceQualityBadge> SelectedMarketplaceQualityBadges =>
        Marketplace.SelectedComponent is null
            ? []
            : MarketplaceQualityBadgeEvaluator.Evaluate(Marketplace.SelectedComponent, SeededMarketplaceProvenance);

    public VendorCatalogSyncDashboardViewModel VendorCatalogSync { get; } = CreateSeededVendorCatalogSync();

    public MarketplaceBomCostRollupViewModel MarketplaceBomCostRollup =>
        MarketplaceBomCostRollupFactory.FromCart(MarketplaceCart, Marketplace.Components);

    public ComponentDeduplicationReviewViewModel ComponentDeduplicationReview =>
        ComponentDeduplicationReviewViewModel.FromMarketplaceRows(Marketplace.Components);

    public TrustedLibraryPromotionQueueViewModel TrustedLibraryPromotionQueue =>
        TrustedLibraryPromotionQueueViewModel.FromReviewedCandidates(CreateTrustedLibraryReviewedCandidates());

    public FabricationOrderingReadinessViewModel FabricationOrderingReadiness =>
        FabricationOrderingReadinessViewModel.FromSelectedHandoffOption(Fabrication);

    public VendorLiveSmokeViewModel VendorLiveSmoke { get; } =
        new(new VendorLiveSmokeHarnessAdapter(DragonCAD.Sourcing.Catalog.Smoke.VendorLiveSmokeHarness.CreateDefault()));

    public MarketplaceIntegrationStatusDashboardViewModel MarketplaceIntegrationStatus =>
        MarketplaceIntegrationStatusDashboardFactory.FromInputs(CreateMarketplaceIntegrationStatusInputs());

    public IReadOnlyList<string> VendorCatalogSyncProviderOptions { get; } = ["Digi-Key", "Mouser"];

    public IReadOnlyList<InUseVendorCatalogSyncRequest> InUseVendorCatalogSyncQueue =>
        InUseVendorCatalogSyncPlanner.Plan(
            SchematicEditor.Components,
            ComponentManager.Components,
            VendorCatalogSync.Providers,
            inUseVendorCatalogSyncStates,
            DateTimeOffset.UtcNow,
            inUseVendorCatalogFreshnessPolicy);

    public string InUseVendorCatalogSyncSummary
    {
        get
        {
            IReadOnlyList<InUseVendorCatalogSyncRequest> requests = InUseVendorCatalogSyncQueue;
            int componentCount = requests
                .Select(request => request.ComponentId)
                .Distinct(StringComparer.Ordinal)
                .Count();
            return requests.Count switch
            {
                0 => "In-use vendor sync queue: no sourced components in use.",
                1 => "In-use vendor sync queue: 1 request for 1 component.",
                int count => $"In-use vendor sync queue: {count} requests for {componentCount} {Pluralize(componentCount, "component")}."
            };
        }
    }

    public string InUseVendorCatalogFreshnessPolicySummary =>
        $"Freshness: Digi-Key {FormatFreshnessHours(inUseVendorCatalogFreshnessPolicy.FreshnessWindowFor("Digi-Key"))}, Mouser {FormatFreshnessHours(inUseVendorCatalogFreshnessPolicy.FreshnessWindowFor("Mouser"))}";

    public string DigiKeyInUseVendorFreshnessHours
    {
        get => FormatFreshnessHoursValue(inUseVendorCatalogFreshnessPolicy.FreshnessWindowFor("Digi-Key"));
        set => UpdateInUseVendorFreshnessHours("Digi-Key", value);
    }

    public string MouserInUseVendorFreshnessHours
    {
        get => FormatFreshnessHoursValue(inUseVendorCatalogFreshnessPolicy.FreshnessWindowFor("Mouser"));
        set => UpdateInUseVendorFreshnessHours("Mouser", value);
    }

    public string InUseVendorFreshnessValidationStatus
    {
        get => inUseVendorFreshnessValidationStatus;
        private set
        {
            if (inUseVendorFreshnessValidationStatus == value)
            {
                return;
            }

            inUseVendorFreshnessValidationStatus = value;
            OnPropertyChanged();
        }
    }

    public VendorCatalogSyncResultViewModel VendorCatalogSyncResult
    {
        get => vendorCatalogSyncResult;
        private set
        {
            if (vendorCatalogSyncResult == value)
            {
                return;
            }

            vendorCatalogSyncResult = value;
            OnPropertyChanged();
        }
    }

    public string SelectedVendorCatalogSyncProviderName
    {
        get => selectedVendorCatalogSyncProviderName;
        set
        {
            string nextValue = string.IsNullOrWhiteSpace(value) ? "Digi-Key" : value.Trim();
            if (selectedVendorCatalogSyncProviderName == nextValue)
            {
                return;
            }

            selectedVendorCatalogSyncProviderName = nextValue;
            OnPropertyChanged();
        }
    }

    public string VendorCatalogSyncSearchText
    {
        get => vendorCatalogSyncSearchText;
        set
        {
            string nextValue = value ?? string.Empty;
            if (vendorCatalogSyncSearchText == nextValue)
            {
                return;
            }

            vendorCatalogSyncSearchText = nextValue;
            OnPropertyChanged();
        }
    }

    public string VendorCatalogSyncStatusText
    {
        get => vendorCatalogSyncStatusText;
        private set
        {
            if (vendorCatalogSyncStatusText == value)
            {
                return;
            }

            vendorCatalogSyncStatusText = value;
            OnPropertyChanged();
        }
    }

    public bool IsVendorCatalogSyncRunning
    {
        get => isVendorCatalogSyncRunning;
        private set
        {
            if (isVendorCatalogSyncRunning == value)
            {
                return;
            }

            isVendorCatalogSyncRunning = value;
            OnPropertyChanged();
            RunVendorCatalogSyncCommand.RaiseCanExecuteChanged();
            RunInUseVendorCatalogSyncCommand.RaiseCanExecuteChanged();
            ForceInUseVendorCatalogSyncCommand.RaiseCanExecuteChanged();
        }
    }

    public VendorCatalogSyncRunStatus ActiveVendorCatalogSyncRunStatus
    {
        get => activeVendorCatalogSyncRunStatus;
        private set
        {
            if (activeVendorCatalogSyncRunStatus == value)
            {
                return;
            }

            activeVendorCatalogSyncRunStatus = value;
            OnPropertyChanged();
        }
    }

    public DatasheetIntakeQueueViewModel DatasheetIntakeQueue { get; } = new();

    public DatasheetCandidateLinkingViewModel DatasheetCandidateLinking { get; } =
        DatasheetCandidateLinkingViewModel.CreateSample();

    public DatasheetReviewQueueViewModel DatasheetReviewQueue { get; } = CreateSeededDatasheetReviewQueue();

    public IReadOnlyList<DatasheetLinkReviewRow> DatasheetLinkReviewPlans { get; } =
        DatasheetLinkReviewSeedData.CreateRows();

    public IReadOnlyList<DatasheetLinkPromotionQueueRow> DatasheetLinkPromotionQueue =>
        DatasheetLinkReviewPlans
            .Where(row => row.IsApprovedForPromotion)
            .Select(DatasheetLinkPromotionQueueRow.FromReviewRow)
            .ToArray();

    public string DatasheetLinkPromotionQueueSummary =>
        DatasheetLinkPromotionQueue.Count switch
        {
            0 => "No approved links pending promotion",
            1 => "1 approved link pending promotion",
            int count => $"{count} approved links pending promotion"
        };

    public DatasheetLinkPromotionRecordViewModel? ActiveDatasheetLinkPromotionRecord
    {
        get => activeDatasheetLinkPromotionRecord;
        private set
        {
            if (activeDatasheetLinkPromotionRecord == value)
            {
                return;
            }

            activeDatasheetLinkPromotionRecord = value;
            OnPropertyChanged();
        }
    }

    public IList<DatasheetLinkPromotionRecordViewModel> DatasheetLinkPromotionRecordHistory { get; } =
        new List<DatasheetLinkPromotionRecordViewModel>();

    public string SavedDatasheetPromotionArtifactPath
    {
        get => savedDatasheetPromotionArtifactPath;
        private set
        {
            if (savedDatasheetPromotionArtifactPath == value)
            {
                return;
            }

            savedDatasheetPromotionArtifactPath = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DatasheetPromotionTrustedLibraryGateStatus));
        }
    }

    public string SavedDatasheetPromotionManifestPath
    {
        get => savedDatasheetPromotionManifestPath;
        private set
        {
            if (savedDatasheetPromotionManifestPath == value)
            {
                return;
            }

            savedDatasheetPromotionManifestPath = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DatasheetPromotionTrustedLibraryGateStatus));
        }
    }

    public string SavedDatasheetPromotionAuditPath
    {
        get => savedDatasheetPromotionAuditPath;
        private set
        {
            if (savedDatasheetPromotionAuditPath == value)
            {
                return;
            }

            savedDatasheetPromotionAuditPath = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DatasheetPromotionTrustedLibraryGateStatus));
        }
    }

    public string DatasheetPromotionPackageValidationStatus
    {
        get => datasheetPromotionPackageValidationStatus;
        private set
        {
            if (datasheetPromotionPackageValidationStatus == value)
            {
                return;
            }

            datasheetPromotionPackageValidationStatus = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DatasheetPromotionTrustedLibraryGateStatus));
        }
    }

    public string SavedDatasheetPromotionLedgerPath
    {
        get => savedDatasheetPromotionLedgerPath;
        private set
        {
            if (savedDatasheetPromotionLedgerPath == value)
            {
                return;
            }

            savedDatasheetPromotionLedgerPath = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DatasheetPromotionTrustedLibraryGateStatus));
        }
    }

    public string SavedTrustedLibraryWritePlanPath
    {
        get => savedTrustedLibraryWritePlanPath;
        private set
        {
            if (savedTrustedLibraryWritePlanPath == value)
            {
                return;
            }

            savedTrustedLibraryWritePlanPath = value;
            OnPropertyChanged();
        }
    }

    public string SavedTrustedLibraryWriteSimulationPath
    {
        get => savedTrustedLibraryWriteSimulationPath;
        private set
        {
            if (savedTrustedLibraryWriteSimulationPath == value)
            {
                return;
            }

            savedTrustedLibraryWriteSimulationPath = value;
            OnPropertyChanged();
        }
    }

    public string SavedTrustedLibraryCandidatePath
    {
        get => savedTrustedLibraryCandidatePath;
        private set
        {
            if (savedTrustedLibraryCandidatePath == value)
            {
                return;
            }

            savedTrustedLibraryCandidatePath = value;
            OnPropertyChanged();
        }
    }

    public string DatasheetPromotionTrustedLibraryGateStatus =>
        !string.IsNullOrWhiteSpace(SavedDatasheetPromotionLedgerPath) &&
        DatasheetPromotionPackageValidationStatus == "Valid package: promotion JSON, manifest, and audit hashes match."
            ? "Ready: local package validated and ledgered; trusted-library write still requires explicit implementation."
            : "Blocked: save, validate, and ledger a promotion package first.";

    public FabricationHandoffViewModel Fabrication { get; } = FabricationHandoffViewModel.CreateSample();

    public AsyncDelegateCommand SearchLibraryCommand { get; }

    public DelegateCommand PlaceSelectedComponentCommand { get; }

    public DelegateCommand CancelPlacementCommand { get; }

    public DelegateCommand CancelActiveOperationCommand { get; }

    public DelegateCommand PlaceArmedComponentOnSchematicCommand { get; }

    public DelegateCommand MoveSelectedLeftCommand { get; }

    public DelegateCommand MoveSelectedRightCommand { get; }

    public DelegateCommand MoveSelectedUpCommand { get; }

    public DelegateCommand MoveSelectedDownCommand { get; }

    public DelegateCommand DeleteSelectedWireCommand { get; }

    public DelegateCommand DeleteSelectedWireSegmentCommand { get; }

    public DelegateCommand InsertWireVertexCommand { get; }

    public DelegateCommand DeleteSelectedPartCommand { get; }

    public DelegateCommand DeleteActiveSelectionCommand { get; }

    public DelegateCommand DuplicateSelectedPartCommand { get; }

    public DelegateCommand RotateSelectedPartCommand { get; }

    public DelegateCommand MirrorSelectedPartCommand { get; }

    public DelegateCommand ActivateSelectToolCommand { get; }

    public DelegateCommand ActivateWireToolCommand { get; }

    public DelegateCommand Load7805SampleCommand { get; }

    public DelegateCommand ZoomInCommand { get; }

    public DelegateCommand ZoomOutCommand { get; }

    public DelegateCommand ToggleGridVisibilityCommand { get; }

    public DelegateCommand ToggleGridStyleCommand { get; }

    public DelegateCommand IncreaseGridSpacingCommand { get; }

    public DelegateCommand DecreaseGridSpacingCommand { get; }

    public DelegateCommand ActivateBoardSelectToolCommand { get; }

    public DelegateCommand ActivateBoardRouteToolCommand { get; }

    public DelegateCommand FinishBoardRouteCommand { get; }

    public DelegateCommand PlaceBoardViaCommand { get; }

    public DelegateCommand InsertBoardViaIntoSelectedTraceSegmentCommand { get; }

    public DelegateCommand DeleteBoardSelectionCommand { get; }

    public DelegateCommand MoveSelectedBoardTraceToLayerCommand { get; }

    public DelegateCommand RotateSelectedBoardComponentCommand { get; }

    public DelegateCommand MirrorSelectedBoardComponentCommand { get; }

    public DelegateCommand ToggleSelectedBoardLayerVisibilityCommand { get; }

    public DelegateCommand ShowComponentManagerTabCommand { get; }

    public DelegateCommand ShowMarketplaceTabCommand { get; }

    public DelegateCommand ShowSchematicTabCommand { get; }

    public DelegateCommand ShowPcbLayoutTabCommand { get; }

    public DelegateCommand ShowDatasheetsTabCommand { get; }

    public DelegateCommand ShowFabricationTabCommand { get; }

    public DelegateCommand AddSelectedMarketplaceComponentToCartCommand { get; }

    public DelegateCommand AddMarketplaceComponentToCartCommand { get; }

    public DelegateCommand IncrementMarketplaceCartLineCommand { get; }

    public DelegateCommand DecrementMarketplaceCartLineCommand { get; }

    public DelegateCommand RemoveMarketplaceCartLineCommand { get; }

    public DelegateCommand PrepareMarketplaceBomCsvCommand { get; }

    public DelegateCommand CreateMarketplaceOrderDraftCommand { get; }

    public DelegateCommand AddCheckoutShippingProfileCommand { get; }

    public DelegateCommand AddCheckoutPaymentMethodCommand { get; }

    public DelegateCommand AddCheckoutProviderCredentialsCommand { get; }

    public DelegateCommand PlaceMarketplaceOrderCommand { get; }

    public DelegateCommand ApplyMarketplaceFilterPresetCommand { get; }

    public DelegateCommand SubmitDatasheetIntakeSampleCommand { get; }

    public DelegateCommand CreateDatasheetLinkPromotionRecordCommand { get; }

    public DelegateCommand ApproveSafeDatasheetLinksCommand { get; }

    public DelegateCommand StageSafeDatasheetLinksCommand { get; }

    public DelegateCommand StageAndSaveSafeDatasheetLinksCommand { get; }

    public DelegateCommand SaveDatasheetPromotionPreviewCommand { get; }

    public DelegateCommand ValidateDatasheetPromotionPackageCommand { get; }

    public DelegateCommand RecordValidatedDatasheetPromotionLedgerEntryCommand { get; }

    public DelegateCommand SaveTrustedLibraryWritePlanCommand { get; }

    public DelegateCommand SimulateTrustedLibraryWriteCommand { get; }

    public DelegateCommand StageTrustedLibraryCandidateCommand { get; }

    public DelegateCommand ResetInUseVendorFreshnessPolicyCommand { get; }

    public DelegateCommand ClearInUseVendorCatalogSyncStateCommand { get; }

    public AsyncDelegateCommand RunVendorCatalogSyncCommand { get; }

    public AsyncDelegateCommand RunInUseVendorCatalogSyncCommand { get; }

    public AsyncDelegateCommand ForceInUseVendorCatalogSyncCommand { get; }

    public FabricationHandoffActionPlan SelectedFabricationHandoffPlan =>
        CreateFabricationHandoffPlan(Fabrication.SelectedOption);

    public FabricationChecklistPreview FabricationChecklistPreview =>
        FabricationChecklistExportPreview.FromOption(
            Fabrication.SelectedOption ?? Fabrication.Options.First(),
            SelectedFabricationHandoffPlan);

    public string ActiveWorkspaceTab
    {
        get => activeWorkspaceTab;
        private set
        {
            if (activeWorkspaceTab == value)
            {
                return;
            }

            activeWorkspaceTab = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsComponentManagerTabActive));
            OnPropertyChanged(nameof(IsMarketplaceTabActive));
            OnPropertyChanged(nameof(IsSchematicTabActive));
            OnPropertyChanged(nameof(IsPcbLayoutTabActive));
            OnPropertyChanged(nameof(IsDatasheetsTabActive));
            OnPropertyChanged(nameof(IsFabricationTabActive));
        }
    }

    public bool IsComponentManagerTabActive => ActiveWorkspaceTab == "ComponentManager";

    public bool IsMarketplaceTabActive => ActiveWorkspaceTab == "Marketplace";

    public bool IsSchematicTabActive => ActiveWorkspaceTab == "Schematic";

    public bool IsPcbLayoutTabActive => ActiveWorkspaceTab == "PcbLayout";

    public bool IsDatasheetsTabActive => ActiveWorkspaceTab == "Datasheets";

    public bool IsFabricationTabActive => ActiveWorkspaceTab == "Fabrication";

    public void ApplyStartupTab(string? tabName)
    {
        ActiveWorkspaceTab = tabName?.Trim() switch
        {
            "Marketplace" => "Marketplace",
            "Schematic" => "Schematic",
            "PcbLayout" => "PcbLayout",
            "Datasheets" => "Datasheets",
            "Fabrication" => "Fabrication",
            _ => ActiveWorkspaceTab
        };
    }

    public bool IsSelectToolActive => ActiveSchematicTool == "Select";

    public bool IsWireToolActive => ActiveSchematicTool == "Wire";

    public bool IsDraggingSchematicComponent
    {
        get => isDraggingSchematicComponent;
        private set
        {
            if (isDraggingSchematicComponent == value)
            {
                return;
            }

            isDraggingSchematicComponent = value;
            OnPropertyChanged();
        }
    }

    public bool IsDraggingSchematicWireSegment
    {
        get => isDraggingSchematicWireSegment;
        private set
        {
            if (isDraggingSchematicWireSegment == value)
            {
                return;
            }

            isDraggingSchematicWireSegment = value;
            OnPropertyChanged();
        }
    }

    public string ActiveSchematicTool
    {
        get => activeSchematicTool;
        private set
        {
            if (activeSchematicTool == value)
            {
                return;
            }

            activeSchematicTool = value;
            PlacementStatus = $"Schematic tool: {value}";
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsSelectToolActive));
            OnPropertyChanged(nameof(IsWireToolActive));
        }
    }

    public ComponentPlacementIntent? ActivePlacement
    {
        get => activePlacement;
        private set
        {
            if (activePlacement == value)
            {
                return;
            }

            activePlacement = value;
            OnPropertyChanged();
        }
    }

    public string PlacementStatus
    {
        get => placementStatus;
        private set
        {
            if (placementStatus == value)
            {
                return;
            }

            placementStatus = value;
            OnPropertyChanged();
        }
    }

    public string SelectedSchematicReferenceDesignator
    {
        get => SchematicEditor.SelectedComponent?.ReferenceDesignator ?? "";
        set => UpdateSelectedSchematicComponentProperties(referenceDesignator: value);
    }

    public string SelectedSchematicComponentName
    {
        get => SchematicEditor.SelectedComponent?.DisplayName ?? "";
        set => UpdateSelectedSchematicComponentProperties(displayName: value);
    }

    public string SelectedSchematicComponentValue
    {
        get => SchematicEditor.SelectedComponent?.Value ?? "";
        set => UpdateSelectedSchematicComponentProperties(value: value);
    }

    public string SelectedSchematicRotationDegrees =>
        SchematicEditor.SelectedComponent?.RotationDegrees.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "";

    public string SelectedSchematicWireNetName
    {
        get => SchematicEditor.SelectedWire?.NetName ?? "";
        set => UpdateSelectedSchematicWireNetName(value);
    }

    public string BoardSelectionSummary
    {
        get
        {
            if (BoardEditor.SelectedTrace is not null)
            {
                string segmentText = BoardEditor.SelectedTraceSegmentIndex is { } segmentIndex
                    ? $", segment {segmentIndex}"
                    : "";
                return $"Trace: {BoardEditor.SelectedTrace.LayerName}, {BoardEditor.SelectedTrace.RoutePoints.Count} points{segmentText}";
            }

            if (BoardEditor.SelectedVia is not null)
            {
                BoardVia via = BoardEditor.SelectedVia;
                return $"Via: {via.FromLayerName} -> {via.ToLayerName} at {FormatMillimeters(via.Position.X)} mm, {FormatMillimeters(via.Position.Y)} mm";
            }

            if (BoardEditor.SelectedComponent is not null)
            {
                BoardComponentInstance component = BoardEditor.SelectedComponent;
                string mirrorText = component.IsMirrored ? ", mirrored" : "";
                return $"Component: {component.ReferenceDesignator} {component.DisplayName}, rot {component.RotationDegrees}{mirrorText}";
            }

            return "No board selection";
        }
    }

    public string SelectedBoardTraceWidthMillimeters
    {
        get => BoardEditor.SelectedTrace is null
            ? ""
            : FormatMillimeters(BoardEditor.SelectedTrace.WidthInternal);
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            if (!decimal.TryParse(value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out decimal millimeters))
            {
                PlacementStatus = "Trace width must be a number in millimeters.";
                return;
            }

            try
            {
                long widthInternal = (long)Math.Round(millimeters * CadUnit.InternalUnitsPerMillimeter, MidpointRounding.AwayFromZero);
                BoardEditor.SetSelectedTraceWidthInternal(widthInternal);
                PlacementStatus = BoardEditor.StatusText;
                OnPropertyChanged();
            }
            catch (InvalidOperationException error)
            {
                PlacementStatus = error.Message;
            }
            catch (ArgumentOutOfRangeException error)
            {
                PlacementStatus = error.Message;
            }
        }
    }

    public string GridStatusText =>
        $"Grid {FormatMillimeters(SchematicEditor.GridSpacingInternal)} mm {SchematicEditor.GridStyle} {(SchematicEditor.IsGridVisible ? "Visible" : "Hidden")}";

    public string ActiveBoardTool => BoardEditor.ActiveTool;

    public IReadOnlyList<string> BoardLayerNames => BoardEditor.Layers.Select(layer => layer.Name).ToArray();

    public string SelectedBoardLayerName
    {
        get => BoardEditor.ActiveLayerName;
        set
        {
            try
            {
                BoardEditor.SetActiveLayer(value);
                PlacementStatus = BoardEditor.StatusText;
                OnPropertyChanged();
            }
            catch (InvalidOperationException error)
            {
                PlacementStatus = error.Message;
            }
        }
    }

    public bool IsLibrarySearchInProgress
    {
        get => isLibrarySearchInProgress;
        private set
        {
            if (isLibrarySearchInProgress == value)
            {
                return;
            }

            isLibrarySearchInProgress = value;
            OnPropertyChanged();
            SearchLibraryCommand.RaiseCanExecuteChanged();
        }
    }

    public string LibrarySearchText
    {
        get => librarySearchText;
        set
        {
            if (librarySearchText == value)
            {
                return;
            }

            librarySearchText = value;
            OnPropertyChanged();
        }
    }

    public static string DefaultHawkCadLibraryPath =>
        Path.Combine(AppContext.BaseDirectory, "BuiltInLibraries", "hawkcad-core-library.hclib.json");

    public static string SourceTreeHawkCadLibraryPath =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "BuiltInLibraries", "hawkcad-core-library.hclib.json"));

    public static string DefaultDatasheetPromotionArtifactDirectory =>
        Path.Combine(AppContext.BaseDirectory, "PromotionExports");

    public static string CuratedHawkCadStarterLibraryJsonForFallback => CuratedHawkCadStarterLibraryJson;

    public static MainWindowViewModel CreateDesignPreview(int maxBuiltInDevices = DefaultInitialBuiltInDeviceLimit) =>
        CreateFromHawkCadLibraryJson(LoadHawkCadLibraryJson(), maxBuiltInDevices);

    public static MainWindowViewModel CreateFromHawkCadLibraryJson(
        string json,
        int maxBuiltInDevices = DefaultInitialBuiltInDeviceLimit,
        string? datasheetPromotionArtifactDirectory = null,
        IVendorCatalogSyncSearchService? vendorCatalogSyncSearchService = null)
    {
        BuiltInHawkCadLibraryService service = BuiltInHawkCadLibraryService.FromJson(json, maxBuiltInDevices);
        BuiltInHawkCadLibrarySearchResult initialLoad = service.InitialLoad;
        BuiltInLibraryViewModel builtInLibrary = new(
            service.Index.LibraryName,
            initialLoad.TotalDevices,
            initialLoad.LoadedDevices,
            initialLoad.StatusText);

        return new MainWindowViewModel(
            ComponentManagerViewModel.FromCatalog(CreateStarterCatalog(initialLoad.Components)),
            builtInLibrary,
            service,
            datasheetPromotionArtifactDirectory ?? DefaultDatasheetPromotionArtifactDirectory,
            vendorCatalogSyncSearchService ?? VendorCatalogSyncSearchServiceFactory.CreateFromEnvironment());
    }

    private static ComponentCatalog CreateStarterCatalog(IReadOnlyList<DragonCAD.Core.Components.Definitions.ComponentDefinition> components) =>
        new(
            BuiltInDefinitions: components,
            UserDefinitions: [],
            ProjectDefinitions: []);

    private static MarketplaceBrowserViewModel CreateSeededMarketplaceBrowser() =>
        MarketplaceBrowserViewModel.FromRows(
        [
            new MarketplaceComponentRow(
                Provider: "Digi-Key",
                Category: "Voltage Regulator",
                DisplayName: "LM7805 5V Linear Regulator",
                Manufacturer: "Texas Instruments",
                ManufacturerPartNumber: "LM7805CT/NOPB",
                CanonicalComponentId: "dragon:lm7805",
                DuplicateOfComponentId: "",
                DatasheetUrl: "https://www.ti.com/lit/ds/symlink/lm7805.pdf",
                StockQuantity: 18420,
                MinimumUnitPriceUsd: 0.73m),
            new MarketplaceComponentRow(
                Provider: "Mouser",
                Category: "Voltage Regulator",
                DisplayName: "L7805CV 5V Linear Regulator",
                Manufacturer: "STMicroelectronics",
                ManufacturerPartNumber: "L7805CV",
                CanonicalComponentId: "dragon:lm7805",
                DuplicateOfComponentId: "dragon:lm7805",
                DatasheetUrl: "https://www.st.com/resource/en/datasheet/l78.pdf",
                StockQuantity: 32300,
                MinimumUnitPriceUsd: 0.48m),
            new MarketplaceComponentRow(
                Provider: "SparkFun",
                Category: "Module",
                DisplayName: "ESP32 Thing Plus",
                Manufacturer: "SparkFun",
                ManufacturerPartNumber: "WRL-20168",
                CanonicalComponentId: "dragon:esp32-devkit",
                DuplicateOfComponentId: "",
                DatasheetUrl: "https://docs.sparkfun.com/",
                StockQuantity: 120,
                MinimumUnitPriceUsd: 24.95m),
            new MarketplaceComponentRow(
                Provider: "Adafruit",
                Category: "Module",
                DisplayName: "Feather ESP32",
                Manufacturer: "Adafruit",
                ManufacturerPartNumber: "3405",
                CanonicalComponentId: "dragon:esp32-devkit",
                DuplicateOfComponentId: "dragon:esp32-devkit",
                DatasheetUrl: "https://learn.adafruit.com/adafruit-huzzah32-esp32-feather",
                StockQuantity: 86,
                MinimumUnitPriceUsd: 19.95m),
            new MarketplaceComponentRow(
                Provider: "Jameco",
                Category: "IC",
                DisplayName: "NE555 Timer",
                Manufacturer: "Texas Instruments",
                ManufacturerPartNumber: "NE555P",
                CanonicalComponentId: "dragon:ne555",
                DuplicateOfComponentId: "",
                DatasheetUrl: "https://www.ti.com/lit/ds/symlink/ne555.pdf",
                StockQuantity: 5400,
            MinimumUnitPriceUsd: 0.39m)
        ]);

    private static IReadOnlyList<MarketplaceComponentProvenance> SeededMarketplaceProvenance { get; } =
    [
        MarketplaceComponentProvenance.VendorImport(
            new CanonicalComponentKey("dragon:lm7805"),
            "Digi-Key",
            "https://www.digikey.com/",
            "https://www.ti.com/lit/ds/symlink/lm7805.pdf",
            "sha256:lm7805",
            new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero)),
        MarketplaceComponentProvenance.DatasheetGenerated(
            new CanonicalComponentKey("dragon:esp32-devkit"),
            "Adafruit",
            "https://www.adafruit.com/product/3405",
            "https://learn.adafruit.com/adafruit-huzzah32-esp32-feather",
            "sha256:esp32-devkit",
            "Codex",
            MarketplaceReviewState.PendingReview,
            new DateTimeOffset(2026, 5, 31, 12, 30, 0, TimeSpan.Zero))
    ];

    private static DatasheetReviewQueueViewModel CreateSeededDatasheetReviewQueue() =>
        DatasheetReviewQueueViewModel.FromRows(
        [
            new DatasheetReviewRow(
                componentName: "LM7805 5V Linear Regulator",
                datasheetSource: "https://www.ti.com/lit/ds/symlink/lm7805.pdf",
                extractedPinCount: 3,
                symbolStatus: DatasheetProposalStatus.Ready,
                footprintStatus: DatasheetProposalStatus.NeedsReview,
                threeDimensionalModelStatus: DatasheetProposalStatus.Placeholder,
                confidence: DatasheetReviewConfidence.High,
                warnings:
                [
                    new DatasheetReviewWarning(DatasheetReviewWarningSeverity.Warning, "Confirm TO-220 package height before approving 3D model.")
                ]),
            new DatasheetReviewRow(
                componentName: "ESP32 DevKit Module",
                datasheetSource: "https://www.espressif.com/sites/default/files/documentation/esp32-wroom-32_datasheet_en.pdf",
                extractedPinCount: 38,
                symbolStatus: DatasheetProposalStatus.NeedsReview,
                footprintStatus: DatasheetProposalStatus.NeedsReview,
                threeDimensionalModelStatus: DatasheetProposalStatus.Missing,
                confidence: DatasheetReviewConfidence.Medium,
                warnings:
                [
                    new DatasheetReviewWarning(DatasheetReviewWarningSeverity.Critical, "Pin count differs between module datasheet and selected dev board.")
                ])
        ]);

    private static VendorCatalogSyncDashboardViewModel CreateSeededVendorCatalogSync() =>
        VendorCatalogSyncDashboardViewModel.FromStatuses(
            new DateTimeOffset(2026, 5, 31, 12, 0, 0, TimeSpan.Zero),
            [
                new VendorCatalogSyncStatus("Digi-Key", true, ResolveDigiKeyCredentialState(), null, 0, 0, 0),
                new VendorCatalogSyncStatus("Mouser", true, ResolveMouserCredentialState(), null, 0, 0, 0),
                new VendorCatalogSyncStatus("Adafruit", true, CatalogCredentialState.NotRequired, new DateTimeOffset(2026, 5, 30, 12, 0, 0, TimeSpan.Zero), 128, 64, 2),
                new VendorCatalogSyncStatus("SparkFun", true, CatalogCredentialState.NotRequired, new DateTimeOffset(2026, 5, 29, 12, 0, 0, TimeSpan.Zero), 422, 301, 8),
                new VendorCatalogSyncStatus("Jameco", true, CatalogCredentialState.NotSupported, null, 0, 0, 0)
            ]);

    private static MarketplaceBomCostRollupViewModel CreateSeededMarketplaceBomCostRollup()
    {
        BomCostRollup rollup = BomCostRollupCalculator.RollUp(
            [
                new BomComponentQuantity("U1", "LM7805CT/NOPB", 3),
                new BomComponentQuantity("U2", "NE555P", 5),
                new BomComponentQuantity("C1-C4", "CAP-10UF-0603", 4)
            ],
            CreateSeededCatalogListings());

        return MarketplaceBomCostRollupViewModel.FromRollup(rollup);
    }

    private static ComponentDeduplicationReviewViewModel CreateSeededComponentDeduplicationReview() =>
        ComponentDeduplicationReviewViewModel.FromCandidates(
        [
            new ComponentCandidate(
                "LM7805CT/NOPB",
                "Texas Instruments",
                "TO-220",
                "5 V regulator",
                ["LM7805", "7805"],
                ["Digi-Key:296-12345-1-ND", "Mouser:595-LM7805CT"],
                [
                    new ComponentMergeWarning(
                        ComponentMergeWarningKind.PackageDisagreement,
                        "Provider catalog package fields should be reviewed before canonical merge.",
                        ["TO-220", "TO-220-3"],
                        ["Digi-Key:296-12345-1-ND", "Mouser:595-LM7805CT"])
                ]),
            new ComponentCandidate(
                "NE555P",
                "Texas Instruments",
                "DIP-8",
                "timer",
                ["555", "NE555"],
                ["Digi-Key:296-NE555P-ND", "Mouser:595-NE555P"],
                [])
        ]);

    private static TrustedLibraryPromotionQueueViewModel CreateSeededTrustedLibraryPromotionQueue()
    {
        TrustedLibraryVendorMatchPromotionPlan plan = TrustedLibraryVendorMatchPromotionPlanner.Plan(
        [
            new ReviewedVendorCatalogMatch(
                TrustedLibraryMatchReviewState.Approved,
                "Digi-Key",
                "296-12345-1-ND",
                "LM7805CT/NOPB",
                new ComponentId("dragon:lm7805"),
                [new TrustedLibraryArtifactPath("datasheet", "datasheets/lm7805.pdf", "sha256:lm7805")],
                []),
            new ReviewedVendorCatalogMatch(
                TrustedLibraryMatchReviewState.PendingReview,
                "Mouser",
                "595-NE555P",
                "NE555P",
                new ComponentId("dragon:ne555"),
                [new TrustedLibraryArtifactPath("catalog", "catalog/mouser-ne555.json", null)],
                ["Confirm package mapping before staging."])
        ]);

        return TrustedLibraryPromotionQueueViewModel.FromPlan(plan);
    }

    private static FabricationOrderingReadinessViewModel CreateSeededFabricationOrderingReadiness() =>
        FabricationOrderingReadinessViewModel.FromSources(
        [
            new FabricationOrderingReadinessSource(
                "OSH Park",
                "Prototype",
                "Prototype board order",
                [2, 4],
                3,
                999,
                []),
            new FabricationOrderingReadinessSource(
                "PCB Cart",
                "Production",
                "Production quote",
                [2, 4, 6],
                5,
                10000,
                [
                    FabricationOrderingDiagnostic.Error("DragonCAD.Fabrication.MissingPickAndPlace", "Pick-and-place CSV is required for assembly quoting.", "Pick and Place"),
                    FabricationOrderingDiagnostic.Warning("DragonCAD.Fabrication.BomReview", "BOM should be reviewed before requesting production quote.")
                ])
        ]);

    private static MarketplaceIntegrationStatusDashboardViewModel CreateSeededMarketplaceIntegrationStatus() =>
        MarketplaceIntegrationStatusDashboardViewModel.FromSections(
        [
            new MarketplaceIntegrationSectionStatus(MarketplaceIntegrationSection.ApiSync, 2, 0, 0),
            new MarketplaceIntegrationSectionStatus(MarketplaceIntegrationSection.InUseSync, 0, 1, 0),
            new MarketplaceIntegrationSectionStatus(MarketplaceIntegrationSection.BomRollup, 2, 0, 1),
            new MarketplaceIntegrationSectionStatus(MarketplaceIntegrationSection.DedupReview, 1, 1, 0),
            new MarketplaceIntegrationSectionStatus(MarketplaceIntegrationSection.TrustedLibraryPromotion, 1, 1, 0),
            new MarketplaceIntegrationSectionStatus(MarketplaceIntegrationSection.FabricationOrdering, 1, 1, 1),
            new MarketplaceIntegrationSectionStatus(MarketplaceIntegrationSection.LiveSmoke, 0, 0, 2)
        ]);

    private IReadOnlyList<TrustedLibraryReviewedCandidate> CreateTrustedLibraryReviewedCandidates() =>
        Marketplace.Components
            .Select(row => new TrustedLibraryReviewedCandidate(
                componentId: string.IsNullOrWhiteSpace(row.CanonicalComponentId) ? $"marketplace:{row.Provider}:{row.ManufacturerPartNumber}" : row.CanonicalComponentId,
                provider: row.Provider,
                sku: row.ManufacturerPartNumber,
                manufacturerPartNumber: row.ManufacturerPartNumber,
                reviewState: DetermineTrustedLibraryReviewState(row),
                artifactPaths: CreateTrustedLibraryArtifacts(row),
                warnings: CreateTrustedLibraryWarnings(row)))
            .ToArray();

    private static TrustedLibraryMatchReviewState DetermineTrustedLibraryReviewState(MarketplaceComponentRow row)
    {
        if (!row.IsCanonical)
        {
            return TrustedLibraryMatchReviewState.PendingReview;
        }

        return row.HasDatasheet
            ? TrustedLibraryMatchReviewState.Approved
            : TrustedLibraryMatchReviewState.PendingReview;
    }

    private static IReadOnlyList<TrustedLibraryReviewedArtifactCandidate> CreateTrustedLibraryArtifacts(MarketplaceComponentRow row)
    {
        if (!row.HasDatasheet)
        {
            return [];
        }

        return
        [
            new TrustedLibraryReviewedArtifactCandidate("datasheet", row.DatasheetUrl, null)
        ];
    }

    private static IReadOnlyList<string> CreateTrustedLibraryWarnings(MarketplaceComponentRow row)
    {
        List<string> warnings = [];
        if (!row.HasDatasheet)
        {
            warnings.Add("Datasheet link is missing.");
        }

        if (!row.IsCanonical)
        {
            warnings.Add($"Review duplicate mapping to {row.DuplicateOfComponentId} before promotion.");
        }

        return warnings;
    }

    private MarketplaceIntegrationStatusInputs CreateMarketplaceIntegrationStatusInputs()
    {
        MarketplaceBomCostRollupViewModel bomRollup = MarketplaceBomCostRollup;
        ComponentDeduplicationReviewViewModel dedupReview = ComponentDeduplicationReview;
        TrustedLibraryPromotionQueueViewModel trustedPromotion = TrustedLibraryPromotionQueue;
        FabricationOrderingReadinessViewModel fabricationOrdering = FabricationOrderingReadiness;
        IReadOnlyList<InUseVendorCatalogSyncRequest> inUseQueue = InUseVendorCatalogSyncQueue;

        return new MarketplaceIntegrationStatusInputs(
            ApiSync: new MarketplaceApiSyncStatusInput(
                SyncedVendorCount: VendorCatalogSync.Providers.Count(provider => provider.CanSync),
                WarningCount: VendorCatalogSync.Providers.Count(provider => !string.IsNullOrWhiteSpace(provider.Warning)),
                BlockedCount: VendorCatalogSync.Providers.Count(provider => provider.IsEnabled && !provider.CanSync)),
            InUseSync: new MarketplaceInUseSyncStatusInput(
                SyncedQueueCount: inUseQueue.Count(request => !request.IsDue && request.SyncStateLabel != "Never synced"),
                PendingQueueCount: inUseQueue.Count(request => request.SyncStateLabel == "Never synced"),
                DueQueueCount: inUseQueue.Count(request => request.IsDue)),
            BomRollup: new MarketplaceBomRollupStatusInput(
                CompleteLineCount: bomRollup.Rows.Count(row => row.Diagnostics.Count == 0),
                DiagnosticCount: bomRollup.Diagnostics.Count,
                IncompleteLineCount: bomRollup.Rows.Count(row => row.Diagnostics.Count > 0)),
            DedupReview: new MarketplaceDedupReviewStatusInput(
                ClearComponentCount: dedupReview.Rows.Count(row => row.Warnings.Count == 0),
                PendingComponentCount: dedupReview.Rows.Count(row => row.ReviewState == ComponentDeduplicationReviewState.Pending),
                WarningCount: dedupReview.Rows.Sum(row => row.Warnings.Count)),
            TrustedLibraryPromotion: new MarketplaceTrustedPromotionStatusInput(
                ReadyComponentCount: trustedPromotion.Rows.Count(row => row.CanStage),
                WarningCount: trustedPromotion.Rows.Sum(row => row.Warnings.Count),
                BlockedCount: trustedPromotion.Rows.Count(row => !row.CanStage)),
            FabricationOrdering: new MarketplaceFabricationOrderingStatusInput(
                ReadyOrderCount: fabricationOrdering.Rows.Count(row => row.PackageReadiness == "Ready"),
                WarningCount: fabricationOrdering.Rows.Sum(row => row.Warnings.Count),
                BlockedCount: fabricationOrdering.Rows.Count(row => row.PackageReadiness != "Ready")),
            LiveSmoke: new MarketplaceLiveSmokeStatusInput(
                PassingCheckCount: VendorLiveSmoke.Providers.Count(row => row.Status == "Succeeded"),
                WarningCount: VendorLiveSmoke.Diagnostics.Count(row => row.Severity.Equals("Warning", StringComparison.OrdinalIgnoreCase)),
                BlockedCheckCount: VendorLiveSmoke.Providers.Count(row => !row.CanRun)));
    }

    private static IReadOnlyList<NormalizedCatalogListing> CreateSeededCatalogListings() =>
    [
        new NormalizedCatalogListing(
            "Digi-Key",
            "296-12345-1-ND",
            "LM7805CT/NOPB",
            "Texas Instruments",
            "5V linear regulator",
            PriceLadder.Normalize([new QuantityPriceBreak(1, Money.Usd(0.72m)), new QuantityPriceBreak(10, Money.Usd(0.51m))]),
            1234,
            new Uri("https://example.test/lm7805.pdf"),
            new Uri("https://www.digikey.com/en/products/detail/texas-instruments/LM7805CT-NOPB/12345"),
            new Dictionary<string, string> { ["PackageType"] = "TO-220" },
            CatalogProviderCapabilities.Api),
        new NormalizedCatalogListing(
            "Mouser",
            "595-LM7805CT",
            "LM7805CT/NOPB",
            "Texas Instruments",
            "Linear Voltage Regulators 5V",
            PriceLadder.Normalize([new QuantityPriceBreak(1, Money.Usd(0.81m)), new QuantityPriceBreak(10, Money.Usd(0.58m))]),
            810,
            new Uri("https://example.test/lm7805-mouser.pdf"),
            new Uri("https://www.mouser.com/ProductDetail/595-LM7805CT"),
            new Dictionary<string, string> { ["Packaging"] = "Tube" },
            CatalogProviderCapabilities.Api),
        new NormalizedCatalogListing(
            "Digi-Key",
            "296-NE555P-ND",
            "NE555P",
            "Texas Instruments",
            "Precision timer",
            PriceLadder.Normalize([new QuantityPriceBreak(1, Money.Usd(0.39m)), new QuantityPriceBreak(10, Money.Usd(0.29m))]),
            5400,
            new Uri("https://example.test/ne555.pdf"),
            new Uri("https://www.digikey.com/en/products/detail/texas-instruments/NE555P/277057"),
            new Dictionary<string, string> { ["PackageType"] = "PDIP" },
            CatalogProviderCapabilities.Api)
    ];

    private static CatalogCredentialState ResolveDigiKeyCredentialState()
    {
        DigiKeyOAuthClientOptions options = DigiKeyOAuthClientOptions.FromEnvironment();
        return string.IsNullOrWhiteSpace(options.ClientId) || string.IsNullOrWhiteSpace(options.ClientSecret)
            ? CatalogCredentialState.Missing
            : CatalogCredentialState.Configured;
    }

    private static CatalogCredentialState ResolveMouserCredentialState()
    {
        MouserSearchClientOptions options = MouserSearchClientOptions.FromEnvironment();
        return string.IsNullOrWhiteSpace(options.ApiKey)
            ? CatalogCredentialState.Missing
            : CatalogCredentialState.Configured;
    }

    private static VendorCatalogSyncResultViewModel CreateSeededVendorCatalogSyncResult() =>
        VendorCatalogSyncResultViewModel.FromRunResult(
            new VendorCatalogSyncRunResult(
                "Digi-Key",
                "LM7805",
                VendorCatalogSyncRunStatus.Completed,
                [
                    new NormalizedCatalogListing(
                        "Digi-Key",
                        "296-12345-1-ND",
                        "LM7805CT/NOPB",
                        "Texas Instruments",
                        "5V linear regulator",
                        PriceLadder.Normalize([new QuantityPriceBreak(1, Money.Usd(0.72m)), new QuantityPriceBreak(10, Money.Usd(0.51m))]),
                        1234,
                        new Uri("https://example.test/lm7805.pdf"),
                        new Uri("https://www.digikey.com/en/products/detail/texas-instruments/LM7805CT-NOPB/12345"),
                        new Dictionary<string, string> { ["PackageType"] = "TO-220" },
                        CatalogProviderCapabilities.Api),
                    new NormalizedCatalogListing(
                        "Mouser",
                        "595-LM7805CT",
                        "LM7805CT/NOPB",
                        "Texas Instruments",
                        "Linear Voltage Regulators 5V",
                        PriceLadder.Normalize([new QuantityPriceBreak(1, Money.Usd(0.74m)), new QuantityPriceBreak(10, Money.Usd(0.53m))]),
                        876,
                        new Uri("https://example.test/lm7805-mouser.pdf"),
                        new Uri("https://www.mouser.com/ProductDetail/595-LM7805CT"),
                        new Dictionary<string, string> { ["Packaging"] = "Tube" },
                        CatalogProviderCapabilities.Api)
                ],
                [
                    new CatalogImportDiagnostic(
                        CatalogDiagnosticSeverity.Warning,
                        "vendor-sync.sample",
                        "Sample API sync results are shown until a live provider run is started.",
                        "Digi-Key",
                        null)
                ]));

    public async Task RunVendorCatalogSyncAsync()
    {
        string query = VendorCatalogSyncSearchText.Trim();
        if (query.Length == 0)
        {
            ApplyVendorCatalogSyncResult(new VendorCatalogSyncRunResult(
                SelectedVendorCatalogSyncProviderName,
                query,
                VendorCatalogSyncRunStatus.Blocked,
                [],
                [new CatalogImportDiagnostic(
                    CatalogDiagnosticSeverity.Error,
                    VendorCatalogSyncDiagnosticCodes.MissingQuery,
                    "Enter a part number or keyword before running vendor sync.",
                    SelectedVendorCatalogSyncProviderName,
                    null)]));
            return;
        }

        try
        {
            IsVendorCatalogSyncRunning = true;
            VendorCatalogSyncStatusText = $"Running {SelectedVendorCatalogSyncProviderName} API sync for '{query}'...";
            VendorCatalogSyncRunResult result = await vendorCatalogSyncSearchService
                .SearchAsync(SelectedVendorCatalogSyncProviderName, query, 25, CancellationToken.None)
                .ConfigureAwait(false);
            ApplyVendorCatalogSyncResult(result);
        }
        finally
        {
            IsVendorCatalogSyncRunning = false;
        }
    }

    public async Task RunInUseVendorCatalogSyncAsync()
    {
        await RunInUseVendorCatalogSyncAsync(forceRefresh: false).ConfigureAwait(false);
    }

    public async Task ForceInUseVendorCatalogSyncAsync()
    {
        await RunInUseVendorCatalogSyncAsync(forceRefresh: true).ConfigureAwait(false);
    }

    private async Task RunInUseVendorCatalogSyncAsync(bool forceRefresh)
    {
        IReadOnlyList<InUseVendorCatalogSyncRequest> queue = InUseVendorCatalogSyncQueue;
        InUseVendorCatalogSyncRequest[] runnableRequests = queue
            .Where(request => forceRefresh ? request.CanRun : request.IsDue)
            .ToArray();
        if (queue.Count == 0)
        {
            ApplyVendorCatalogSyncResult(new VendorCatalogSyncRunResult(
                "In-use vendor sync",
                "",
                VendorCatalogSyncRunStatus.Blocked,
                [],
                [new CatalogImportDiagnostic(
                    CatalogDiagnosticSeverity.Warning,
                    VendorCatalogSyncDiagnosticCodes.MissingQuery,
                    "Place sourced components on the schematic before running in-use vendor sync.",
                    "In-use vendor sync",
                    null)]));
            return;
        }

        if (runnableRequests.Length == 0)
        {
            VendorCatalogSyncStatusText = $"In-use vendor sync skipped: all {queue.Count} {Pluralize(queue.Count, "request")} are fresh.";
            return;
        }

        try
        {
            IsVendorCatalogSyncRunning = true;
            VendorCatalogSyncStatusText = forceRefresh
                ? $"Forcing in-use vendor sync for {runnableRequests.Length} requests..."
                : $"Running in-use vendor sync for {runnableRequests.Length} due requests...";
            List<NormalizedCatalogListing> listings = [];
            List<CatalogImportDiagnostic> diagnostics = [];
            DateTimeOffset syncedAt = DateTimeOffset.UtcNow;
            foreach (InUseVendorCatalogSyncRequest request in runnableRequests)
            {
                VendorCatalogSyncRunResult result = await vendorCatalogSyncSearchService
                    .SearchAsync(request.ProviderName, request.Query, 10, CancellationToken.None)
                    .ConfigureAwait(false);
                listings.AddRange(result.Listings);
                diagnostics.AddRange(result.Diagnostics);
                UpsertInUseVendorCatalogSyncState(
                    request,
                    syncedAt,
                    result.ImportedCount,
                    result.WarningCount);
            }

            VendorCatalogSyncRunStatus status = diagnostics.Any(diagnostic => diagnostic.Severity == CatalogDiagnosticSeverity.Error)
                ? VendorCatalogSyncRunStatus.Blocked
                : VendorCatalogSyncRunStatus.Completed;
            string resultQuery = forceRefresh
                ? $"{runnableRequests.Length} forced requests"
                : $"{runnableRequests.Length} due requests";
            ApplyVendorCatalogSyncResult(new VendorCatalogSyncRunResult(
                "In-use vendor sync",
                resultQuery,
                status,
                listings,
                diagnostics));
            string completionPrefix = forceRefresh ? "Forced in-use vendor sync" : "In-use vendor sync";
            VendorCatalogSyncStatusText = $"{completionPrefix} completed: {runnableRequests.Length} {Pluralize(runnableRequests.Length, "request")}, {listings.Count} {Pluralize(listings.Count, "catalog candidate")}.";
            inUseVendorCatalogSyncStateStore.Save(inUseVendorCatalogSyncStates);
            OnPropertyChanged(nameof(InUseVendorCatalogSyncQueue));
            OnPropertyChanged(nameof(InUseVendorCatalogSyncSummary));
            OnPropertyChanged(nameof(MarketplaceIntegrationStatus));
        }
        finally
        {
            IsVendorCatalogSyncRunning = false;
        }
    }

    private void UpsertInUseVendorCatalogSyncState(
        InUseVendorCatalogSyncRequest request,
        DateTimeOffset syncedAt,
        int importedCount,
        int warningCount)
    {
        inUseVendorCatalogSyncStates.RemoveAll(state =>
            string.Equals(state.ComponentId, request.ComponentId, StringComparison.Ordinal) &&
            string.Equals(state.ProviderName, request.ProviderName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(state.Query, request.Query, StringComparison.OrdinalIgnoreCase));
        inUseVendorCatalogSyncStates.Add(new InUseVendorCatalogSyncState(
            request.ComponentId,
            request.ProviderName,
            request.Query,
            syncedAt,
            importedCount,
            warningCount));
    }

    private void UpdateInUseVendorFreshnessHours(string providerName, string value)
    {
        if (!double.TryParse(value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out double hours) ||
            hours <= 0)
        {
            InUseVendorFreshnessValidationStatus = "Freshness hours must be a positive number.";
            return;
        }

        InUseVendorFreshnessValidationStatus = "Freshness policy is valid.";
        Dictionary<string, TimeSpan> windows = inUseVendorCatalogFreshnessPolicy.ProviderFreshnessWindows
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        windows[providerName] = TimeSpan.FromHours(hours);
        inUseVendorCatalogFreshnessPolicy = new InUseVendorCatalogFreshnessPolicy(
            inUseVendorCatalogFreshnessPolicy.DefaultFreshnessWindow,
            windows);
        inUseVendorCatalogFreshnessPolicyStore.Save(inUseVendorCatalogFreshnessPolicy);
        OnPropertyChanged(providerName.Equals("Digi-Key", StringComparison.OrdinalIgnoreCase)
            ? nameof(DigiKeyInUseVendorFreshnessHours)
            : nameof(MouserInUseVendorFreshnessHours));
        OnPropertyChanged(nameof(InUseVendorCatalogFreshnessPolicySummary));
        OnPropertyChanged(nameof(InUseVendorCatalogSyncQueue));
        OnPropertyChanged(nameof(InUseVendorCatalogSyncSummary));
        OnPropertyChanged(nameof(MarketplaceIntegrationStatus));
    }

    private void ResetInUseVendorFreshnessPolicy()
    {
        inUseVendorCatalogFreshnessPolicy = InUseVendorCatalogFreshnessPolicy.Default;
        inUseVendorCatalogFreshnessPolicyStore.Save(inUseVendorCatalogFreshnessPolicy);
        OnPropertyChanged(nameof(DigiKeyInUseVendorFreshnessHours));
        OnPropertyChanged(nameof(MouserInUseVendorFreshnessHours));
        OnPropertyChanged(nameof(InUseVendorCatalogFreshnessPolicySummary));
        OnPropertyChanged(nameof(InUseVendorCatalogSyncQueue));
        OnPropertyChanged(nameof(InUseVendorCatalogSyncSummary));
        OnPropertyChanged(nameof(MarketplaceIntegrationStatus));
        VendorCatalogSyncStatusText = "In-use vendor freshness policy reset to defaults.";
        InUseVendorFreshnessValidationStatus = "Freshness policy is valid.";
    }

    private void ClearInUseVendorCatalogSyncState()
    {
        inUseVendorCatalogSyncStates.Clear();
        inUseVendorCatalogSyncStateStore.Save(inUseVendorCatalogSyncStates);
        OnPropertyChanged(nameof(InUseVendorCatalogSyncQueue));
        OnPropertyChanged(nameof(InUseVendorCatalogSyncSummary));
        OnPropertyChanged(nameof(MarketplaceIntegrationStatus));
        VendorCatalogSyncStatusText = "In-use vendor sync state cleared.";
    }

    private void ApplyVendorCatalogSyncResult(VendorCatalogSyncRunResult result)
    {
        ActiveVendorCatalogSyncRunStatus = result.Status;
        VendorCatalogSyncResult = VendorCatalogSyncResultViewModel.FromRunResult(result);
        VendorCatalogSyncStatusText = result.Status == VendorCatalogSyncRunStatus.Completed
            ? $"{result.ProviderName} API sync completed: {result.Summary}"
            : result.Summary;
        OnPropertyChanged(nameof(MarketplaceIntegrationStatus));
    }

    private void AddSelectedMarketplaceComponentToCart()
    {
        AddMarketplaceRowToCart(Marketplace.SelectedComponent);
    }

    private void AddMarketplaceComponentToCart(object? parameter)
    {
        AddMarketplaceRowToCart(parameter as MarketplaceComponentRow);
    }

    private void AddMarketplaceRowToCart(MarketplaceComponentRow? row)
    {
        if (row is null)
        {
            PlacementStatus = "Select a marketplace component before adding it to the BOM.";
            return;
        }

        int previousLineCount = MarketplaceCart.Lines.Count;
        MarketplaceCart.AddItem(row);
        if (MarketplaceCart.Lines.Count > previousLineCount || MarketplaceCart.Lines.Any(line => line.ManufacturerPartNumber == row.ManufacturerPartNumber))
        {
            PlacementStatus = $"Added {row.DisplayName} to BOM cart.";
        }
        else
        {
            PlacementStatus = $"Could not add {row.DisplayName} to BOM cart.";
        }

        OnPropertyChanged(nameof(MarketplaceCart));
        OnPropertyChanged(nameof(MarketplaceBomExportPreview));
        OnPropertyChanged(nameof(MarketplaceOrderPlan));
        OnMarketplaceDerivedPanelsChanged();
        OnPropertyChanged(nameof(UnifiedComponentSourceRows));
        OnPropertyChanged(nameof(UnifiedComponentSourceSummary));
    }

    private void IncrementMarketplaceCartLine(object? parameter)
    {
        string? lineId = parameter as string;
        if (string.IsNullOrWhiteSpace(lineId))
        {
            PlacementStatus = "Select a BOM cart line before changing quantity.";
            return;
        }

        ApplyMarketplaceCartCommand(new MarketplaceCartCommandService(MarketplaceCart).Increment(lineId));
    }

    private void DecrementMarketplaceCartLine(object? parameter)
    {
        string? lineId = parameter as string;
        if (string.IsNullOrWhiteSpace(lineId))
        {
            PlacementStatus = "Select a BOM cart line before changing quantity.";
            return;
        }

        ApplyMarketplaceCartCommand(new MarketplaceCartCommandService(MarketplaceCart).Decrement(lineId));
    }

    private void RemoveMarketplaceCartLine(object? parameter)
    {
        string? lineId = parameter as string;
        if (string.IsNullOrWhiteSpace(lineId))
        {
            PlacementStatus = "Select a BOM cart line before removing it.";
            return;
        }

        ApplyMarketplaceCartCommand(new MarketplaceCartCommandService(MarketplaceCart).Remove(lineId));
    }

    private void ApplyMarketplaceCartCommand(MarketplaceCartCommandResult result)
    {
        PlacementStatus = result.StatusMessage;
        OnPropertyChanged(nameof(MarketplaceCart));
        OnPropertyChanged(nameof(MarketplaceBomExportPreview));
        OnPropertyChanged(nameof(MarketplaceOrderPlan));
        OnMarketplaceDerivedPanelsChanged();
    }

    private void PrepareMarketplaceBomCsv()
    {
        MarketplaceBomExportPreviewViewModel preview = MarketplaceBomExportPreview;
        MarketplaceBomCsvExportText = string.Join(Environment.NewLine, preview.CsvLines);

        int lineItemCount = preview.Rows.Count;
        PlacementStatus = lineItemCount == 1
            ? "Prepared BOM CSV export with 1 line item."
            : $"Prepared BOM CSV export with {lineItemCount} line items.";
    }

    private void CreateMarketplaceOrderDraft()
    {
        MarketplaceOrderPlanViewModel orderPlan = MarketplaceOrderPlan;
        if (orderPlan.Providers.Count == 0)
        {
            PlacementStatus = "Add BOM cart items before creating an in-app order draft.";
            return;
        }

        marketplaceOrderDraftSequence++;
        string draftId = $"DRAFT-{marketplaceOrderDraftSequence:0000}";
        ActiveMarketplaceOrderDraft = MarketplaceInAppOrderDraftViewModel.Create(draftId, orderPlan);
        RefreshMarketplaceCheckoutReadiness();
        PlacementStatus = $"Created in-app order draft {draftId} for {ActiveMarketplaceOrderDraft.ProviderOrders.Count} provider order.";
    }

    private void AddCheckoutShippingProfile()
    {
        HasCheckoutShippingProfile = true;
        RefreshMarketplaceCheckoutReadiness();
        PlacementStatus = "Checkout setup updated: shipping profile is available.";
    }

    private void AddCheckoutPaymentMethod()
    {
        HasCheckoutPaymentMethod = true;
        RefreshMarketplaceCheckoutReadiness();
        PlacementStatus = "Checkout setup updated: payment method is available.";
    }

    private void AddCheckoutProviderCredentials(object? parameter)
    {
        string provider = parameter as string ?? MarketplaceOrderPlan.Providers.FirstOrDefault()?.Provider ?? "";
        if (string.IsNullOrWhiteSpace(provider))
        {
            PlacementStatus = "Create an order draft before adding provider credentials.";
            return;
        }

        checkoutCredentialedProviders.Add(provider);
        OnPropertyChanged(nameof(CheckoutCredentialedProviders));
        RefreshMarketplaceCheckoutReadiness();
        PlacementStatus = $"Checkout setup updated: {provider} credentials are available.";
    }

    private void RefreshMarketplaceCheckoutReadiness()
    {
        if (ActiveMarketplaceOrderDraft is null)
        {
            MarketplaceCheckoutReadiness = null;
            return;
        }

        MarketplaceCheckoutReadiness = MarketplaceCheckoutReadinessViewModel.FromDraft(
            ActiveMarketplaceOrderDraft,
            HasCheckoutShippingProfile,
            HasCheckoutPaymentMethod,
            checkoutCredentialedProviders);
    }

    private void PlaceMarketplaceOrder()
    {
        if (ActiveMarketplaceOrderDraft is null || MarketplaceCheckoutReadiness is null)
        {
            PlacementStatus = "Create an in-app order draft before placing an order.";
            return;
        }

        if (!MarketplaceCheckoutReadiness.CanPlaceOrder)
        {
            PlacementStatus = MarketplaceCheckoutReadiness.PrimaryActionLabel;
            return;
        }

        marketplacePlacedOrderSequence++;
        string orderId = $"ORDER-{marketplacePlacedOrderSequence:0000}";
        ActiveMarketplacePlacedOrder = MarketplacePlacedOrderViewModel.CreateLocalRecord(
            orderId,
            ActiveMarketplaceOrderDraft,
            MarketplaceCheckoutReadiness);
        MarketplacePlacedOrderHistory = [ActiveMarketplacePlacedOrder, .. MarketplacePlacedOrderHistory];
        OnPropertyChanged(nameof(MarketplacePlacedOrderHistory));
        OnPropertyChanged(nameof(MarketplacePlacedOrderHistorySummary));
        PlacementStatus = $"Created local order record {orderId}; no live vendor order was placed.";
    }

    private void SeedMarketplaceFilterPresets()
    {
        MarketplaceFilterPresetStore.Save(
            "Stocked datasheets",
            "All",
            "All",
            "",
            requiresDatasheet: true,
            inStockOnly: true);
        MarketplaceFilterPresetStore.Save(
            "Regulators",
            "All",
            "Voltage Regulator",
            "",
            requiresDatasheet: false,
            inStockOnly: true);
        OnPropertyChanged(nameof(MarketplaceFilterPresets));
    }

    private void ApplyMarketplaceFilterPreset(object? parameter)
    {
        string presetName = parameter as string ?? "All";
        MarketplaceFilterPreset preset = string.Equals(presetName, MarketplaceFilterPreset.All.Name, StringComparison.OrdinalIgnoreCase)
            ? MarketplaceFilterPreset.All
            : MarketplaceFilterPresets.FirstOrDefault(candidate => string.Equals(candidate.Name, presetName, StringComparison.OrdinalIgnoreCase)) ?? MarketplaceFilterPreset.All;

        filteredMarketplacePresetRows = MarketplaceFilterPresetApplicator.Apply(Marketplace.Components, preset);
        SelectedMarketplaceFilterPresetName = preset.Name;
        PlacementStatus = $"Applied marketplace filter preset {preset.Name}.";
        OnPropertyChanged(nameof(FilteredMarketplacePresetRows));
    }

    private void SubmitDatasheetIntakeSample()
    {
        Directory.CreateDirectory(datasheetPromotionArtifactDirectory);
        string samplePath = Path.Combine(datasheetPromotionArtifactDirectory, "sample-lm7805-datasheet.pdf");
        if (!File.Exists(samplePath))
        {
            File.WriteAllText(samplePath, "%PDF-1.7 DragonCAD sample datasheet intake");
        }

        DatasheetIntakeSubmissionResult result = DatasheetIntakeQueue.Submit(new DatasheetIntakeRequest(
            SourceIdentifier: samplePath,
            SubmittedActor: "DragonCAD local user",
            ManufacturerPartNumber: "LM7805CT",
            VendorProductId: "DigiKey-296-1389-5-ND",
            PackageName: "TO-220-3",
            SourceNotes: "Sample controlled intake item; no AI generation or trusted-library mutation performed."));

        PlacementStatus = result.Accepted
            ? "Added sample datasheet intake item for LM7805CT; trusted library was not modified."
            : $"Datasheet intake blocked: {string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Message))}";
    }

    private void CreateDatasheetLinkPromotionRecord()
    {
        DatasheetLinkReviewRow[] sourceRows = DatasheetLinkReviewPlans
            .Where(row => row.IsApprovedForPromotion)
            .ToArray();
        DatasheetLinkPromotionQueueRow[] rows = sourceRows
            .Select(DatasheetLinkPromotionQueueRow.FromReviewRow)
            .ToArray();
        if (rows.Length == 0)
        {
            PlacementStatus = "Approve at least one datasheet link before creating a promotion record.";
            return;
        }

        datasheetLinkPromotionRecordSequence++;
        string recordId = $"PROMO-{datasheetLinkPromotionRecordSequence:0000}";
        ActiveDatasheetLinkPromotionRecord = new DatasheetLinkPromotionRecordViewModel(
            recordId,
            "Local promotion record created",
            rows);
        DatasheetLinkPromotionRecordHistory.Add(ActiveDatasheetLinkPromotionRecord);
        foreach (DatasheetLinkReviewRow row in sourceRows)
        {
            row.MarkStaged();
        }

        OnPropertyChanged(nameof(DatasheetLinkPromotionRecordHistory));
        PlacementStatus = $"Created local datasheet promotion record {recordId}; trusted library write is still pending.";
    }

    private void ApproveSafeDatasheetLinks()
    {
        DatasheetLinkReviewRow[] rows = ApprovePendingSafeDatasheetLinks();

        PlacementStatus = rows.Length == 1
            ? "Approved 1 safe datasheet link for promotion review."
            : $"Approved {rows.Length} safe datasheet links for promotion review.";
    }

    private void StageSafeDatasheetLinks()
    {
        DatasheetLinkReviewRow[] approvedRows = ApprovePendingSafeDatasheetLinks();
        if (DatasheetLinkPromotionQueue.Count == 0)
        {
            PlacementStatus = "No safe datasheet links are ready to stage.";
            return;
        }

        CreateDatasheetLinkPromotionRecord();
        if (ActiveDatasheetLinkPromotionRecord is not null)
        {
            PlacementStatus = approvedRows.Length == 1
                ? $"Staged 1 safe datasheet link in promotion record {ActiveDatasheetLinkPromotionRecord.RecordId}."
                : $"Staged {approvedRows.Length} safe datasheet links in promotion record {ActiveDatasheetLinkPromotionRecord.RecordId}.";
        }
    }

    private void StageAndSaveSafeDatasheetLinks()
    {
        StageSafeDatasheetLinks();
        if (ActiveDatasheetLinkPromotionRecord is null)
        {
            return;
        }

        string recordId = ActiveDatasheetLinkPromotionRecord.RecordId;
        SaveDatasheetPromotionPreview();
        PlacementStatus = $"Saved local datasheet promotion package {recordId}; trusted library write is still pending.";
    }

    private void SaveDatasheetPromotionPreview()
    {
        if (ActiveDatasheetLinkPromotionRecord is null)
        {
            PlacementStatus = "Create a datasheet promotion record before saving an artifact.";
            return;
        }

        Directory.CreateDirectory(datasheetPromotionArtifactDirectory);
        string artifactPath = Path.Combine(
            datasheetPromotionArtifactDirectory,
            ActiveDatasheetLinkPromotionRecord.ExportFileName);
        File.WriteAllText(artifactPath, ActiveDatasheetLinkPromotionRecord.ExportJsonPreview);
        string manifestPath = Path.Combine(
            datasheetPromotionArtifactDirectory,
            ActiveDatasheetLinkPromotionRecord.ExportManifestFileName);
        File.WriteAllText(manifestPath, ActiveDatasheetLinkPromotionRecord.ExportManifestJsonPreview);
        string auditPath = Path.Combine(
            datasheetPromotionArtifactDirectory,
            ActiveDatasheetLinkPromotionRecord.ExportAuditFileName);
        File.WriteAllText(auditPath, ActiveDatasheetLinkPromotionRecord.ExportAuditJsonPreview);
        SavedDatasheetPromotionArtifactPath = artifactPath;
        SavedDatasheetPromotionManifestPath = manifestPath;
        SavedDatasheetPromotionAuditPath = auditPath;
        PlacementStatus = $"Saved datasheet promotion artifacts for {ActiveDatasheetLinkPromotionRecord.RecordId}.";
    }

    private void ValidateDatasheetPromotionPackage()
    {
        if (string.IsNullOrWhiteSpace(SavedDatasheetPromotionArtifactPath) ||
            string.IsNullOrWhiteSpace(SavedDatasheetPromotionManifestPath) ||
            string.IsNullOrWhiteSpace(SavedDatasheetPromotionAuditPath))
        {
            SetDatasheetPromotionPackageValidationFailure("promotion package has not been saved");
            return;
        }

        if (!File.Exists(SavedDatasheetPromotionArtifactPath))
        {
            SetDatasheetPromotionPackageValidationFailure("promotion artifact is missing");
            return;
        }

        if (!File.Exists(SavedDatasheetPromotionManifestPath))
        {
            SetDatasheetPromotionPackageValidationFailure("manifest artifact is missing");
            return;
        }

        if (!File.Exists(SavedDatasheetPromotionAuditPath))
        {
            SetDatasheetPromotionPackageValidationFailure("audit artifact is missing");
            return;
        }

        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(SavedDatasheetPromotionManifestPath));
        string expectedPromotionHash = manifest.RootElement.GetProperty("promotionArtifactSha256").GetString() ?? "";
        string expectedAuditHash = manifest.RootElement.GetProperty("auditArtifactSha256").GetString() ?? "";

        if (!string.Equals(expectedPromotionHash, ComputeSha256(File.ReadAllText(SavedDatasheetPromotionArtifactPath)), StringComparison.Ordinal))
        {
            SetDatasheetPromotionPackageValidationFailure("promotion artifact hash mismatch");
            return;
        }

        if (!string.Equals(expectedAuditHash, ComputeSha256(File.ReadAllText(SavedDatasheetPromotionAuditPath)), StringComparison.Ordinal))
        {
            SetDatasheetPromotionPackageValidationFailure("audit artifact hash mismatch");
            return;
        }

        DatasheetPromotionPackageValidationStatus = "Valid package: promotion JSON, manifest, and audit hashes match.";
        PlacementStatus = "Datasheet promotion package validated.";
    }

    private void SetDatasheetPromotionPackageValidationFailure(string reason)
    {
        DatasheetPromotionPackageValidationStatus = $"Invalid package: {reason}.";
        PlacementStatus = $"Datasheet promotion package validation failed: {reason}.";
    }

    private void RecordValidatedDatasheetPromotionLedgerEntry()
    {
        if (ActiveDatasheetLinkPromotionRecord is null)
        {
            PlacementStatus = "Create a datasheet promotion record before recording a ledger entry.";
            return;
        }

        if (DatasheetPromotionPackageValidationStatus != "Valid package: promotion JSON, manifest, and audit hashes match.")
        {
            PlacementStatus = "Validate a saved datasheet promotion package before recording a ledger entry.";
            return;
        }

        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(SavedDatasheetPromotionManifestPath));
        string promotionHash = manifest.RootElement.GetProperty("promotionArtifactSha256").GetString() ?? "";
        string auditHash = manifest.RootElement.GetProperty("auditArtifactSha256").GetString() ?? "";
        string ledgerPath = Path.Combine(datasheetPromotionArtifactDirectory, "datasheet-promotion-ledger.jsonl");
        if (LedgerAlreadyContainsRecord(ledgerPath, ActiveDatasheetLinkPromotionRecord.RecordId))
        {
            SavedDatasheetPromotionLedgerPath = ledgerPath;
            PlacementStatus = $"Datasheet promotion record {ActiveDatasheetLinkPromotionRecord.RecordId} already exists in the datasheet promotion ledger.";
            return;
        }

        string ledgerLine =
            "{" +
            $"\"recordId\":\"{ActiveDatasheetLinkPromotionRecord.RecordId}\"," +
            "\"status\":\"validated-local-package\"," +
            $"\"promotionArtifact\":\"{ActiveDatasheetLinkPromotionRecord.ExportFileName}\"," +
            $"\"promotionArtifactSha256\":\"{promotionHash}\"," +
            $"\"manifestArtifact\":\"{ActiveDatasheetLinkPromotionRecord.ExportManifestFileName}\"," +
            $"\"auditArtifact\":\"{ActiveDatasheetLinkPromotionRecord.ExportAuditFileName}\"," +
            $"\"auditArtifactSha256\":\"{auditHash}\"," +
            "\"trustedLibraryMutation\":\"not-performed\"" +
            "}";

        Directory.CreateDirectory(datasheetPromotionArtifactDirectory);
        File.AppendAllText(ledgerPath, ledgerLine + Environment.NewLine);
        SavedDatasheetPromotionLedgerPath = ledgerPath;
        PlacementStatus = $"Recorded validated datasheet promotion ledger entry {ActiveDatasheetLinkPromotionRecord.RecordId}; trusted library write is still pending.";
    }

    private void SaveTrustedLibraryWritePlan()
    {
        if (ActiveDatasheetLinkPromotionRecord is null)
        {
            PlacementStatus = "Create a datasheet promotion record before creating a trusted-library write plan.";
            return;
        }

        if (DatasheetPromotionTrustedLibraryGateStatus != "Ready: local package validated and ledgered; trusted-library write still requires explicit implementation.")
        {
            PlacementStatus = "Save, validate, and ledger a promotion package before creating a trusted-library write plan.";
            return;
        }

        Directory.CreateDirectory(datasheetPromotionArtifactDirectory);
        string planPath = Path.Combine(
            datasheetPromotionArtifactDirectory,
            $"trusted-library-write-plan-{ActiveDatasheetLinkPromotionRecord.RecordId}.json");

        File.WriteAllText(planPath, BuildTrustedLibraryWritePlanJson(ActiveDatasheetLinkPromotionRecord));
        SavedTrustedLibraryWritePlanPath = planPath;
        PlacementStatus = $"Saved trusted-library write plan {ActiveDatasheetLinkPromotionRecord.RecordId}; no trusted library mutation was performed.";
    }

    private void SimulateTrustedLibraryWrite()
    {
        if (ActiveDatasheetLinkPromotionRecord is null)
        {
            PlacementStatus = "Create a datasheet promotion record before simulating the trusted-library write.";
            return;
        }

        if (string.IsNullOrWhiteSpace(SavedTrustedLibraryWritePlanPath) || !File.Exists(SavedTrustedLibraryWritePlanPath))
        {
            PlacementStatus = "Save a trusted-library write plan before simulating the trusted-library write.";
            return;
        }

        Directory.CreateDirectory(datasheetPromotionArtifactDirectory);
        string simulationPath = Path.Combine(
            datasheetPromotionArtifactDirectory,
            $"trusted-library-write-simulation-{ActiveDatasheetLinkPromotionRecord.RecordId}.json");

        File.WriteAllText(
            simulationPath,
            BuildTrustedLibraryWriteSimulationJson(
                ActiveDatasheetLinkPromotionRecord,
                Path.GetFileName(SavedTrustedLibraryWritePlanPath)));
        SavedTrustedLibraryWriteSimulationPath = simulationPath;
        PlacementStatus = $"Simulated trusted-library write {ActiveDatasheetLinkPromotionRecord.RecordId}; no trusted library mutation was performed.";
    }

    private void StageTrustedLibraryCandidate()
    {
        if (ActiveDatasheetLinkPromotionRecord is null)
        {
            PlacementStatus = "Create a datasheet promotion record before staging a trusted-library candidate.";
            return;
        }

        if (string.IsNullOrWhiteSpace(SavedTrustedLibraryWriteSimulationPath) || !File.Exists(SavedTrustedLibraryWriteSimulationPath))
        {
            PlacementStatus = "Simulate the trusted-library write before staging a trusted-library candidate.";
            return;
        }

        Directory.CreateDirectory(datasheetPromotionArtifactDirectory);
        string candidatePath = Path.Combine(
            datasheetPromotionArtifactDirectory,
            $"trusted-library-candidate-{ActiveDatasheetLinkPromotionRecord.RecordId}.json");

        File.WriteAllText(
            candidatePath,
            BuildTrustedLibraryCandidateJson(
                ActiveDatasheetLinkPromotionRecord,
                Path.GetFileName(SavedTrustedLibraryWriteSimulationPath)));
        SavedTrustedLibraryCandidatePath = candidatePath;
        PlacementStatus = $"Staged trusted-library candidate {ActiveDatasheetLinkPromotionRecord.RecordId}; shipped core library was not modified.";
    }

    private static string BuildTrustedLibraryWritePlanJson(DatasheetLinkPromotionRecordViewModel record) =>
        string.Join(
            Environment.NewLine,
            [
                "{",
                $"  \"recordId\": \"{EscapeJson(record.RecordId)}\",",
                "  \"trustedLibraryMutation\": \"not-performed\",",
                "  \"promotionLedger\": \"datasheet-promotion-ledger.jsonl\",",
                "  \"nextStep\": \"manual-trusted-library-writer-not-implemented\",",
                "  \"operations\": [",
                .. record.Rows.SelectMany((row, index) => FormatTrustedLibraryWritePlanOperation(row, index == record.Rows.Count - 1)),
                "  ]",
                "}"
            ]);

    private static string BuildTrustedLibraryCandidateJson(DatasheetLinkPromotionRecordViewModel record, string sourceSimulationFileName) =>
        string.Join(
            Environment.NewLine,
            [
                "{",
                $"  \"recordId\": \"{EscapeJson(record.RecordId)}\",",
                $"  \"sourceSimulation\": \"{EscapeJson(sourceSimulationFileName)}\",",
                "  \"candidateStatus\": \"staged-review-only\",",
                "  \"trustedLibraryMutation\": \"not-performed\",",
                "  \"promotedLinks\": [",
                .. record.Rows.SelectMany((row, index) => FormatTrustedLibraryCandidateLink(row, index == record.Rows.Count - 1)),
                "  ]",
                "}"
            ]);

    private static string BuildTrustedLibraryWriteSimulationJson(DatasheetLinkPromotionRecordViewModel record, string sourcePlanFileName) =>
        string.Join(
            Environment.NewLine,
            [
                "{",
                $"  \"recordId\": \"{EscapeJson(record.RecordId)}\",",
                $"  \"sourcePlan\": \"{EscapeJson(sourcePlanFileName)}\",",
                "  \"simulationStatus\": \"dry-run-only\",",
                "  \"mutationApplied\": false,",
                "  \"operations\": [",
                .. record.Rows.SelectMany((row, index) => FormatTrustedLibrarySimulationOperation(row, index == record.Rows.Count - 1)),
                "  ]",
                "}"
            ]);

    private static IEnumerable<string> FormatTrustedLibraryWritePlanOperation(DatasheetLinkPromotionQueueRow row, bool isLast)
    {
        yield return "    {";
        yield return "      \"operation\": \"link-existing-component\",";
        yield return $"      \"componentName\": \"{EscapeJson(row.ComponentName)}\",";
        yield return $"      \"targetComponentId\": \"{EscapeJson(row.TargetComponentId)}\",";
        yield return $"      \"decision\": \"{EscapeJson(row.DecisionDisplay)}\"";
        yield return isLast ? "    }" : "    },";
    }

    private static IEnumerable<string> FormatTrustedLibrarySimulationOperation(DatasheetLinkPromotionQueueRow row, bool isLast)
    {
        yield return "    {";
        yield return "      \"operation\": \"link-existing-component\",";
        yield return $"      \"targetComponentId\": \"{EscapeJson(row.TargetComponentId)}\",";
        yield return $"      \"componentName\": \"{EscapeJson(row.ComponentName)}\",";
        yield return "      \"wouldUpdateTrustedLibrary\": true,";
        yield return "      \"mutationApplied\": false";
        yield return isLast ? "    }" : "    },";
    }

    private static IEnumerable<string> FormatTrustedLibraryCandidateLink(DatasheetLinkPromotionQueueRow row, bool isLast)
    {
        yield return "    {";
        yield return $"      \"targetComponentId\": \"{EscapeJson(row.TargetComponentId)}\",";
        yield return $"      \"componentName\": \"{EscapeJson(row.ComponentName)}\",";
        yield return $"      \"decision\": \"{EscapeJson(row.DecisionDisplay)}\",";
        yield return "      \"reviewRequired\": true";
        yield return isLast ? "    }" : "    },";
    }

    private static string EscapeJson(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);

    private static bool LedgerAlreadyContainsRecord(string ledgerPath, string recordId)
    {
        if (!File.Exists(ledgerPath))
        {
            return false;
        }

        string recordNeedle = $"\"recordId\":\"{recordId}\"";
        return File.ReadLines(ledgerPath).Any(line => line.Contains(recordNeedle, StringComparison.Ordinal));
    }

    private DatasheetLinkReviewRow[] ApprovePendingSafeDatasheetLinks()
    {
        DatasheetLinkReviewRow[] rows = DatasheetLinkReviewPlans
            .Where(row => row.CanApprove && row.IsSafeExistingComponentLink)
            .ToArray();

        foreach (DatasheetLinkReviewRow row in rows)
        {
            row.ApproveCommand.Execute(null);
        }

        return rows;
    }

    private static FabricationHandoffActionPlan CreateFabricationHandoffPlan(FabricationHandoffOptionViewModel? option)
    {
        if (option is null)
        {
            return FabricationHandoffActionPlanner.Plan(
                FabricationHandoffPackageOption.CreateExportPackage(
                    "none",
                    "No provider",
                    "No package selected",
                    "manufacturing/package.zip",
                    [FabricationHandoffPackageFile.Missing("Provider selection")]));
        }

        FabricationHandoffActionKind actionKind = option.ProviderName == "OSH Park"
            ? FabricationHandoffActionKind.OpenUploadPage
            : FabricationHandoffActionKind.OpenQuotePage;
        string target = option.ProviderName == "OSH Park"
            ? "https://oshpark.com"
            : "https://www.pcbcart.com/quote";

        FabricationHandoffPackageFile[] files = option.RequiredFiles
            .Select(file => file.IsReady
                ? FabricationHandoffPackageFile.Present(file.DisplayName, file.RelativePath)
                : FabricationHandoffPackageFile.Missing(file.DisplayName))
            .ToArray();

        FabricationHandoffPackageOption packageOption = actionKind == FabricationHandoffActionKind.OpenUploadPage
            ? FabricationHandoffPackageOption.CreateUploadPage(option.ProviderId, option.ProviderName, option.OrderKindLabel, target, files)
            : FabricationHandoffPackageOption.CreateQuotePage(option.ProviderId, option.ProviderName, option.OrderKindLabel, target, files);

        return FabricationHandoffActionPlanner.Plan(packageOption);
    }

    public async Task ExecuteLibrarySearchAsync()
    {
        if (IsLibrarySearchInProgress)
        {
            return;
        }

        IsLibrarySearchInProgress = true;
        try
        {
            BuiltInHawkCadLibrarySearchResult result = await Task.Run(() =>
                builtInLibraryService.Search(librarySearchText, DefaultSearchResultLimit));
            ApplyLibrarySearchResult(result);
        }
        finally
        {
            IsLibrarySearchInProgress = false;
        }
    }

    private void PlaceSelectedComponent()
    {
        ComponentManagerRow? selected = ComponentManager.SelectedComponent;
        if (selected is null)
        {
            ActivePlacement = null;
            PlacementStatus = "Select a component before placing it.";
            return;
        }

        ActivePlacement = new ComponentPlacementIntent(
            selected.ComponentId,
            selected.DisplayName,
            selected.SymbolCount,
            selected.FootprintCount,
            selected.Source,
            selected.SymbolPreview,
            selected.FootprintPreview);
        PlacementStatus = $"Placement armed: {selected.DisplayName}";
        DragonCadLog.Info($"placement armed componentId={selected.ComponentId} displayName={selected.DisplayName}");
    }

    private void PlaceArmedComponentOnSchematic()
    {
        PlaceArmedComponentOnSchematicAt(new CadPoint(0, 0));
    }

    public void PlaceArmedComponentOnSchematicAt(CadPoint point)
    {
        if (ActivePlacement is null)
        {
            PlacementStatus = "Choose Place on a component before dropping it on the schematic.";
            return;
        }

        SchematicEditor.PlaceComponent(ActivePlacement, point);
        SynchronizeBoardFromSchematic();
        PlacementStatus = $"{SchematicEditor.StatusText} Board sync: {BoardEditor.StatusText}";
        DragonCadLog.Info($"schematic placement point=({point.X},{point.Y}) status=\"{PlacementStatus}\" schematicCount={SchematicEditor.Components.Count} boardCount={BoardEditor.Components.Count}");
    }

    public void HandleSchematicCanvasClick(CadPoint point)
    {
        if (ActivePlacement is not null)
        {
            PlaceArmedComponentOnSchematicAt(point);
            return;
        }

        if (ActiveSchematicTool == "Wire")
        {
            SchematicEditor.TraceClickAt(point);
            SynchronizeBoardFromSchematic();
            PlacementStatus = $"{SchematicEditor.StatusText} Board sync: {BoardEditor.StatusText}";
            DragonCadLog.Info($"schematic wire click point=({point.X},{point.Y}) status=\"{PlacementStatus}\" wires={SchematicEditor.Wires.Count}");
            return;
        }

        SchematicEditor.SelectComponentAt(point);
        PlacementStatus = SchematicEditor.StatusText;
        DragonCadLog.Info($"schematic select point=({point.X},{point.Y}) status=\"{PlacementStatus}\"");
    }

    public void HandleSchematicPointerPressed(CadPoint point)
    {
        if (ActivePlacement is not null || ActiveSchematicTool != "Select")
        {
            HandleSchematicCanvasClick(point);
            return;
        }

        SchematicEditor.SelectComponentAt(point);
        PlacementStatus = SchematicEditor.StatusText;
        if (SchematicEditor.SelectedComponent is null)
        {
            IsDraggingSchematicComponent = false;
            IsDraggingSchematicWireSegment = SchematicEditor.SelectedWire is not null &&
                SchematicEditor.SelectedWireSegmentIndex is not null;
            if (IsDraggingSchematicWireSegment)
            {
                DragonCadLog.Info($"schematic wire drag start point=({point.X},{point.Y}) segment={SchematicEditor.SelectedWireSegmentIndex}");
            }

            return;
        }

        IsDraggingSchematicWireSegment = false;
        schematicDragOffset = SchematicEditor.SelectedComponent.Position - point;
        IsDraggingSchematicComponent = true;
        DragonCadLog.Info($"schematic drag start point=({point.X},{point.Y}) component={SchematicEditor.SelectedComponent.ReferenceDesignator}");
    }

    public void HandleSchematicPointerMoved(CadPoint point)
    {
        if (ActiveSchematicTool == "Wire")
        {
            SchematicEditor.UpdateTracePreviewAt(point);
            PlacementStatus = SchematicEditor.StatusText;
            return;
        }

        if (IsDraggingSchematicWireSegment)
        {
            SchematicEditor.MoveSelectedWireSegmentTo(point);
            SynchronizeBoardFromSchematic();
            PlacementStatus = SchematicEditor.StatusText;
            return;
        }

        if (!IsDraggingSchematicComponent || SchematicEditor.SelectedComponent is null)
        {
            return;
        }

        SchematicEditor.MoveSelectedComponentTo(point + schematicDragOffset);
        SynchronizeBoardFromSchematic();
        PlacementStatus = SchematicEditor.StatusText;
    }

    public void HandleSchematicPointerReleased(CadPoint point)
    {
        if (!IsDraggingSchematicComponent && !IsDraggingSchematicWireSegment)
        {
            return;
        }

        HandleSchematicPointerMoved(point);
        bool endedWireDrag = IsDraggingSchematicWireSegment;
        IsDraggingSchematicComponent = false;
        IsDraggingSchematicWireSegment = false;
        DragonCadLog.Info(endedWireDrag
            ? $"schematic wire drag end point=({point.X},{point.Y}) status=\"{PlacementStatus}\""
            : $"schematic drag end point=({point.X},{point.Y}) status=\"{PlacementStatus}\"");
    }

    private void CancelPlacement()
    {
        ActivePlacement = null;
        PlacementStatus = "Placement cancelled. Click a schematic object to select it.";
        DragonCadLog.Info("placement cancelled");
    }

    private void CancelActiveOperation()
    {
        if (ActivePlacement is not null)
        {
            CancelPlacement();
            return;
        }

        if (SchematicEditor.CancelPendingWire())
        {
            PlacementStatus = SchematicEditor.StatusText;
            return;
        }

        IsDraggingSchematicComponent = false;
        IsDraggingSchematicWireSegment = false;
        PlacementStatus = "No active operation to cancel.";
    }

    private void ActivateSchematicTool(string toolName)
    {
        ActivePlacement = null;
        ActiveSchematicTool = toolName;
        DragonCadLog.Info($"schematic tool activated {toolName}");
    }

    private void Load7805Sample()
    {
        ActivePlacement = null;
        ActiveWorkspaceTab = "Schematic";
        SchematicEditor.Clear();
        BoardEditor.Clear();

        SchematicComponentInstance regulator = SchematicEditor.PlaceComponent(
            Sample7805Regulator(),
            new CadPoint(0, 0));
        SchematicComponentInstance inputCap = SchematicEditor.PlaceComponent(
            SampleCapacitor("0.33uF input capacitor", "CIN"),
            new CadPoint(-9_000_000, 0));
        SchematicComponentInstance outputCap = SchematicEditor.PlaceComponent(
            SampleCapacitor("0.1uF output capacitor", "COUT"),
            new CadPoint(9_000_000, 0));

        Connect(regulator, "IN", inputCap, "P", [new CadPoint(-6_000_000, -2_000_000)]);
        Connect(regulator, "OUT", outputCap, "P", [new CadPoint(6_000_000, -2_000_000)]);
        Connect(regulator, "GND", inputCap, "N", [new CadPoint(-4_000_000, 5_000_000)]);
        Connect(regulator, "GND", outputCap, "N", [new CadPoint(4_000_000, 5_000_000)]);
        Connect(inputCap, "N", outputCap, "N", [new CadPoint(0, 7_000_000)]);

        SynchronizeBoardFromSchematic();
        ActiveSchematicTool = "Wire";
        PlacementStatus = $"Loaded 7805 TO-220 5V regulator sample: {SchematicEditor.Components.Count} parts, {SchematicEditor.Wires.Count} wires, {SchematicEditor.Nets.Count} nets. Board sync: {BoardEditor.StatusText}";
        DragonCadLog.Info($"sample 7805 loaded components={SchematicEditor.Components.Count} wires={SchematicEditor.Wires.Count} nets={SchematicEditor.Nets.Count}");
    }

    private void ToggleGridVisibility()
    {
        SchematicEditor.ToggleGridVisibility();
        BoardEditor.ToggleGridVisibility();
        PlacementStatus = GridStatusText;
        OnPropertyChanged(nameof(GridStatusText));
    }

    private void ToggleGridStyle()
    {
        SchematicEditor.ToggleGridStyle();
        BoardEditor.ToggleGridStyle();
        PlacementStatus = GridStatusText;
        OnPropertyChanged(nameof(GridStatusText));
    }

    private void ChangeGridSpacing(decimal millimetersDelta)
    {
        decimal currentMillimeters = (decimal)SchematicEditor.GridSpacingInternal / CadUnit.InternalUnitsPerMillimeter;
        decimal nextMillimeters = Math.Clamp(currentMillimeters + millimetersDelta, 0.1m, 25.4m);
        SchematicEditor.SetGridSpacingMillimeters(nextMillimeters);
        BoardEditor.SetGridSpacingMillimeters(nextMillimeters);
        PlacementStatus = GridStatusText;
        OnPropertyChanged(nameof(GridStatusText));
    }

    private void Connect(
        SchematicComponentInstance startComponent,
        string startPinName,
        SchematicComponentInstance endComponent,
        string endPinName,
        IReadOnlyList<CadPoint> viaPoints)
    {
        CadPoint start = PinWorldPoint(startComponent, startPinName);
        CadPoint end = PinWorldPoint(endComponent, endPinName);
        SchematicEditor.TraceClickAt(start);
        foreach (CadPoint viaPoint in viaPoints)
        {
            SchematicEditor.TraceClickAt(viaPoint);
        }

        SchematicEditor.TraceClickAt(end);
    }

    private static CadPoint PinWorldPoint(SchematicComponentInstance component, string pinName)
    {
        ComponentSymbolPinPreview pin = component.SymbolPreview.Pins.Single(pin => pin.Name == pinName);
        return component.Position + (pin.ConnectPoint - new CadPoint(0, 0));
    }

    private static ComponentPlacementIntent Sample7805Regulator() =>
        new(
            "dragoncad:sample/lm7805-to220",
            "LM7805 5V regulator TO-220",
            SymbolCount: 1,
            FootprintCount: 1,
            Source: "Sample",
            SymbolPreview: new ComponentSymbolPreview(
                new CadRectangle(-4_000_000, -4_000_000, 4_000_000, 4_000_000),
                [
                    new ComponentPreviewLine(new CadPoint(-2_500_000, -3_000_000), new CadPoint(2_500_000, -3_000_000)),
                    new ComponentPreviewLine(new CadPoint(2_500_000, -3_000_000), new CadPoint(2_500_000, 3_000_000)),
                    new ComponentPreviewLine(new CadPoint(2_500_000, 3_000_000), new CadPoint(-2_500_000, 3_000_000)),
                    new ComponentPreviewLine(new CadPoint(-2_500_000, 3_000_000), new CadPoint(-2_500_000, -3_000_000))
                ],
                [
                    new ComponentSymbolPinPreview("IN", new CadPoint(-4_000_000, -1_500_000), new CadPoint(-2_500_000, -1_500_000), "Right"),
                    new ComponentSymbolPinPreview("GND", new CadPoint(0, 4_000_000), new CadPoint(0, 3_000_000), "Up"),
                    new ComponentSymbolPinPreview("OUT", new CadPoint(4_000_000, -1_500_000), new CadPoint(2_500_000, -1_500_000), "Left")
                ]),
            FootprintPreview: new ComponentFootprintPreview(
                new CadRectangle(-3_500_000, -2_000_000, 3_500_000, 4_500_000),
                [
                    new ComponentPreviewLine(new CadPoint(-3_500_000, -2_000_000), new CadPoint(3_500_000, -2_000_000)),
                    new ComponentPreviewLine(new CadPoint(3_500_000, -2_000_000), new CadPoint(3_500_000, 4_500_000)),
                    new ComponentPreviewLine(new CadPoint(3_500_000, 4_500_000), new CadPoint(-3_500_000, 4_500_000)),
                    new ComponentPreviewLine(new CadPoint(-3_500_000, 4_500_000), new CadPoint(-3_500_000, -2_000_000))
                ],
                [
                    new ComponentFootprintPadPreview("IN", new CadPoint(-2_540_000, 0), new CadVector(1_300_000, 1_300_000), "Round", "ThroughHole"),
                    new ComponentFootprintPadPreview("GND", new CadPoint(0, 0), new CadVector(1_300_000, 1_300_000), "Round", "ThroughHole"),
                    new ComponentFootprintPadPreview("OUT", new CadPoint(2_540_000, 0), new CadVector(1_300_000, 1_300_000), "Round", "ThroughHole")
                ]));

    private static ComponentPlacementIntent SampleCapacitor(string displayName, string idSuffix) =>
        new(
            $"dragoncad:sample/{idSuffix.ToLowerInvariant()}",
            displayName,
            SymbolCount: 1,
            FootprintCount: 1,
            Source: "Sample",
            SymbolPreview: new ComponentSymbolPreview(
                new CadRectangle(-2_000_000, -3_000_000, 2_000_000, 3_000_000),
                [
                    new ComponentPreviewLine(new CadPoint(-1_000_000, -500_000), new CadPoint(1_000_000, -500_000)),
                    new ComponentPreviewLine(new CadPoint(-1_000_000, 500_000), new CadPoint(1_000_000, 500_000))
                ],
                [
                    new ComponentSymbolPinPreview("P", new CadPoint(0, -3_000_000), new CadPoint(0, -500_000), "Down"),
                    new ComponentSymbolPinPreview("N", new CadPoint(0, 3_000_000), new CadPoint(0, 500_000), "Up")
                ]),
            FootprintPreview: new ComponentFootprintPreview(
                new CadRectangle(-1_600_000, -1_000_000, 1_600_000, 1_000_000),
                [
                    new ComponentPreviewLine(new CadPoint(-1_600_000, -1_000_000), new CadPoint(1_600_000, -1_000_000)),
                    new ComponentPreviewLine(new CadPoint(1_600_000, -1_000_000), new CadPoint(1_600_000, 1_000_000)),
                    new ComponentPreviewLine(new CadPoint(1_600_000, 1_000_000), new CadPoint(-1_600_000, 1_000_000)),
                    new ComponentPreviewLine(new CadPoint(-1_600_000, 1_000_000), new CadPoint(-1_600_000, -1_000_000))
                ],
                [
                    new ComponentFootprintPadPreview("P", new CadPoint(-700_000, 0), new CadVector(800_000, 800_000), "Round", "ThroughHole"),
                    new ComponentFootprintPadPreview("N", new CadPoint(700_000, 0), new CadVector(800_000, 800_000), "Round", "ThroughHole")
                ]));

    public void MoveSelectedSchematicComponentByGrid(CadVector gridSteps)
    {
        if (SchematicEditor.SelectedComponent is null)
        {
            PlacementStatus = "Select a schematic component before moving it.";
            return;
        }

        CadPoint current = SchematicEditor.SelectedComponent.Position;
        CadPoint requested = new(
            current.X + (gridSteps.X * CadUnit.InternalUnitsPerMillimeter),
            current.Y + (gridSteps.Y * CadUnit.InternalUnitsPerMillimeter));
        SchematicEditor.MoveSelectedComponentTo(requested);
        PlacementStatus = SchematicEditor.StatusText;
    }

    public void MoveSelectedBoardComponentByGrid(CadVector gridSteps)
    {
        CadVector gridDelta = new(
            gridSteps.X * BoardEditor.GridSpacingInternal,
            gridSteps.Y * BoardEditor.GridSpacingInternal);

        if (BoardEditor.SelectedComponent is not null)
        {
            CadPoint current = BoardEditor.SelectedComponent.Position;
            BoardEditor.MoveSelectedComponentTo(current + gridDelta);
            PlacementStatus = BoardEditor.StatusText;
            return;
        }

        if (BoardEditor.SelectedVia is not null)
        {
            CadPoint current = BoardEditor.SelectedVia.Position;
            BoardEditor.MoveSelectedViaTo(current + gridDelta);
            PlacementStatus = BoardEditor.StatusText;
            return;
        }

        if (BoardEditor.SelectedTrace is not null && BoardEditor.SelectedTraceSegmentIndex is { } segmentIndex)
        {
            IReadOnlyList<CadPoint> routePoints = BoardEditor.SelectedTrace.RoutePoints;
            if (segmentIndex > 0 && segmentIndex < routePoints.Count)
            {
                CadPoint start = routePoints[segmentIndex - 1];
                CadPoint end = routePoints[segmentIndex];
                CadPoint requested = new(
                    ((start.X + end.X) / 2) + gridDelta.X,
                    ((start.Y + end.Y) / 2) + gridDelta.Y);
                BoardEditor.MoveSelectedTraceSegmentTo(requested);
                PlacementStatus = BoardEditor.StatusText;
                return;
            }
        }

        PlacementStatus = "Select a board object before moving it.";
    }

    public void HandleBoardCanvasClick(CadPoint point)
    {
        if (BoardEditor.ActiveTool == "Route")
        {
            BoardEditor.TraceClickAt(point);
            PlacementStatus = BoardEditor.StatusText;
            return;
        }

        BoardEditor.SelectAt(point);
        PlacementStatus = BoardEditor.StatusText;
    }

    private void MoveActiveEditorSelectionByGrid(CadVector gridSteps)
    {
        if (IsPcbLayoutTabActive)
        {
            MoveSelectedBoardComponentByGrid(gridSteps);
            return;
        }

        MoveSelectedSchematicComponentByGrid(gridSteps);
    }

    private void ActivateBoardSelectTool()
    {
        BoardEditor.ActivateSelectTool();
        PlacementStatus = BoardEditor.StatusText;
        OnPropertyChanged(nameof(ActiveBoardTool));
    }

    private void ActivateBoardRouteTool()
    {
        BoardEditor.ActivateRouteTool();
        ActiveWorkspaceTab = "PcbLayout";
        PlacementStatus = BoardEditor.StatusText;
        OnPropertyChanged(nameof(ActiveBoardTool));
    }

    private void FinishBoardRoute(object? parameter)
    {
        CadPoint point = parameter is CadPoint cadPoint
            ? cadPoint
            : BoardEditor.PendingTraceRoutePoints.LastOrDefault();
        BoardEditor.CompleteTraceAt(point);
        PlacementStatus = BoardEditor.StatusText;
    }

    private void PlaceBoardVia(object? parameter)
    {
        CadPoint point = parameter is CadPoint cadPoint
            ? cadPoint
            : BoardEditor.PendingTraceRoutePoints.LastOrDefault();
        BoardEditor.PlaceViaAt(point);
        PlacementStatus = BoardEditor.StatusText;
        OnPropertyChanged(nameof(SelectedBoardLayerName));
    }

    private void InsertBoardViaIntoSelectedTraceSegment(object? parameter)
    {
        CadPoint point = parameter is CadPoint cadPoint
            ? cadPoint
            : BoardEditor.SelectedTrace?.RoutePoints.ElementAtOrDefault(BoardEditor.SelectedTraceSegmentIndex ?? 0) ?? default;

        try
        {
            BoardEditor.InsertViaIntoSelectedTraceSegment(point);
            PlacementStatus = BoardEditor.StatusText;
            OnPropertyChanged(nameof(SelectedBoardLayerName));
        }
        catch (InvalidOperationException error)
        {
            PlacementStatus = error.Message;
        }
    }

    private void ToggleSelectedBoardLayerVisibility()
    {
        BoardLayer layer = BoardEditor.Layers.First(layer => layer.Name == BoardEditor.ActiveLayerName);
        BoardEditor.SetLayerVisibility(layer.Name, !layer.IsVisible);
        PlacementStatus = BoardEditor.StatusText;
    }

    private void DeleteBoardSelection()
    {
        BoardEditor.DeleteSelectedBoardObject();
        PlacementStatus = BoardEditor.StatusText;
    }

    private void MoveSelectedBoardTraceToLayer()
    {
        try
        {
            BoardEditor.MoveSelectedTraceToActiveLayer();
            PlacementStatus = BoardEditor.StatusText;
        }
        catch (InvalidOperationException error)
        {
            PlacementStatus = error.Message;
        }
    }

    private void RotateSelectedBoardComponent()
    {
        try
        {
            BoardEditor.RotateSelectedComponentClockwise();
            PlacementStatus = BoardEditor.StatusText;
        }
        catch (InvalidOperationException error)
        {
            PlacementStatus = error.Message;
        }
    }

    private void MirrorSelectedBoardComponent()
    {
        try
        {
            BoardEditor.MirrorSelectedComponent();
            PlacementStatus = BoardEditor.StatusText;
        }
        catch (InvalidOperationException error)
        {
            PlacementStatus = error.Message;
        }
    }

    private void DeleteSelectedWire()
    {
        if (!SchematicEditor.DeleteSelectedWire())
        {
            PlacementStatus = SchematicEditor.StatusText;
            return;
        }

        SynchronizeBoardFromSchematic();
        PlacementStatus = $"{SchematicEditor.StatusText} Board sync: {BoardEditor.StatusText}";
    }

    private void DeleteSelectedWireSegment()
    {
        try
        {
            SchematicEditor.DeleteSelectedWireSegment();
            SynchronizeBoardFromSchematic();
            PlacementStatus = $"{SchematicEditor.StatusText} Board sync: {BoardEditor.StatusText}";
        }
        catch (InvalidOperationException error)
        {
            PlacementStatus = error.Message;
        }
    }

    private void InsertWireVertex(object? parameter)
    {
        CadPoint point = parameter is CadPoint cadPoint
            ? cadPoint
            : SchematicEditor.SelectedWire?.RoutePoints.ElementAtOrDefault(SchematicEditor.SelectedWireSegmentIndex ?? 0) ?? default;

        try
        {
            SchematicEditor.InsertVertexIntoSelectedWireSegment(point);
            SynchronizeBoardFromSchematic();
            PlacementStatus = $"{SchematicEditor.StatusText} Board sync: {BoardEditor.StatusText}";
        }
        catch (InvalidOperationException error)
        {
            PlacementStatus = error.Message;
        }
    }

    private void DeleteSelectedPart()
    {
        if (!SchematicEditor.DeleteSelectedComponent())
        {
            PlacementStatus = SchematicEditor.StatusText;
            return;
        }

        SynchronizeBoardFromSchematic();
        PlacementStatus = $"{SchematicEditor.StatusText} Board sync: {BoardEditor.StatusText}";
        OnPropertyChanged(nameof(SelectedSchematicReferenceDesignator));
        OnPropertyChanged(nameof(SelectedSchematicComponentName));
        OnPropertyChanged(nameof(SelectedSchematicComponentValue));
        OnPropertyChanged(nameof(SelectedSchematicRotationDegrees));
    }

    private void DeleteActiveSelection()
    {
        if (SchematicEditor.SelectedWire is not null)
        {
            DeleteSelectedWire();
            return;
        }

        DeleteSelectedPart();
    }

    private void DuplicateSelectedPart()
    {
        if (SchematicEditor.SelectedComponent is null)
        {
            PlacementStatus = "Select a schematic component before duplicating it.";
            return;
        }

        SchematicEditor.DuplicateSelectedComponent();
        SynchronizeBoardFromSchematic();
        PlacementStatus = $"{SchematicEditor.StatusText} Board sync: {BoardEditor.StatusText}";
        OnPropertyChanged(nameof(SelectedSchematicReferenceDesignator));
        OnPropertyChanged(nameof(SelectedSchematicComponentName));
        OnPropertyChanged(nameof(SelectedSchematicComponentValue));
        OnPropertyChanged(nameof(SelectedSchematicRotationDegrees));
    }

    private void RotateSelectedPart()
    {
        if (SchematicEditor.SelectedComponent is null)
        {
            PlacementStatus = "Select a schematic component before rotating it.";
            return;
        }

        SchematicEditor.RotateSelectedComponentClockwise();
        SynchronizeBoardFromSchematic();
        PlacementStatus = $"{SchematicEditor.StatusText} Board sync: {BoardEditor.StatusText}";
        OnPropertyChanged(nameof(SelectedSchematicRotationDegrees));
    }

    private void MirrorSelectedPart()
    {
        if (SchematicEditor.SelectedComponent is null)
        {
            PlacementStatus = "Select a schematic component before mirroring it.";
            return;
        }

        SchematicEditor.MirrorSelectedComponent();
        SynchronizeBoardFromSchematic();
        PlacementStatus = $"{SchematicEditor.StatusText} Board sync: {BoardEditor.StatusText}";
    }

    private void SynchronizeBoardFromSchematic()
    {
        BoardEditor.SynchronizeFromSchematic(SchematicEditor.Components, SchematicEditor.Wires);
        OnPropertyChanged(nameof(InUseVendorCatalogSyncQueue));
        OnPropertyChanged(nameof(InUseVendorCatalogSyncSummary));
    }

    private void UpdateSelectedSchematicComponentProperties(
        string? referenceDesignator = null,
        string? displayName = null,
        string? value = null)
    {
        if (SchematicEditor.SelectedComponent is null)
        {
            PlacementStatus = "Select a schematic component before editing properties.";
            return;
        }

        SchematicComponentInstance selected = SchematicEditor.SelectedComponent;
        try
        {
            SchematicEditor.UpdateSelectedComponentProperties(
                referenceDesignator ?? selected.ReferenceDesignator,
                displayName ?? selected.DisplayName,
                value ?? selected.Value);
            SynchronizeBoardFromSchematic();
            PlacementStatus = $"{SchematicEditor.StatusText} Board sync: {BoardEditor.StatusText}";
        }
        catch (InvalidOperationException error)
        {
            PlacementStatus = error.Message;
        }
    }

    private void UpdateSelectedSchematicWireNetName(string netName)
    {
        if (SchematicEditor.SelectedWire is null)
        {
            PlacementStatus = "Select a schematic wire before editing its net name.";
            return;
        }

        try
        {
            SchematicEditor.RenameSelectedWireNet(netName);
            SynchronizeBoardFromSchematic();
            PlacementStatus = $"{SchematicEditor.StatusText} Board sync: {BoardEditor.StatusText}";
            OnPropertyChanged(nameof(SelectedSchematicWireNetName));
        }
        catch (InvalidOperationException error)
        {
            PlacementStatus = error.Message;
        }
    }

    private void SchematicEditorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SchematicEditorViewModel.SelectedComponent))
        {
            OnPropertyChanged(nameof(SelectedSchematicReferenceDesignator));
            OnPropertyChanged(nameof(SelectedSchematicComponentName));
            OnPropertyChanged(nameof(SelectedSchematicComponentValue));
            OnPropertyChanged(nameof(SelectedSchematicRotationDegrees));
        }

        if (e.PropertyName == nameof(SchematicEditorViewModel.SelectedWire))
        {
            OnPropertyChanged(nameof(SelectedSchematicWireNetName));
        }
    }

    private void BoardEditorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(BoardEditorViewModel.SelectedComponent) or
            nameof(BoardEditorViewModel.SelectedTrace) or
            nameof(BoardEditorViewModel.SelectedTraceSegmentIndex) or
            nameof(BoardEditorViewModel.SelectedVia))
        {
            OnPropertyChanged(nameof(BoardSelectionSummary));
            OnPropertyChanged(nameof(SelectedBoardTraceWidthMillimeters));
        }
    }

    private void FabricationPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FabricationHandoffViewModel.SelectedOption))
        {
            OnPropertyChanged(nameof(SelectedFabricationHandoffPlan));
            OnPropertyChanged(nameof(FabricationChecklistPreview));
            OnPropertyChanged(nameof(FabricationOrderingReadiness));
            OnPropertyChanged(nameof(MarketplaceIntegrationStatus));
        }
    }

    private void DatasheetLinkReviewRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DatasheetLinkReviewRow.IsApprovedForPromotion))
        {
            OnPropertyChanged(nameof(DatasheetLinkPromotionQueue));
            OnPropertyChanged(nameof(DatasheetLinkPromotionQueueSummary));
            OnPropertyChanged(nameof(TrustedLibraryPromotionQueue));
            OnPropertyChanged(nameof(MarketplaceIntegrationStatus));
        }
    }

    private void MarketplacePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MarketplaceBrowserViewModel.SelectedComponent))
        {
            OnPropertyChanged(nameof(SelectedMarketplaceQualityBadges));
        }
    }

    private void OnMarketplaceDerivedPanelsChanged()
    {
        OnPropertyChanged(nameof(MarketplaceBomCostRollup));
        OnPropertyChanged(nameof(ComponentDeduplicationReview));
        OnPropertyChanged(nameof(TrustedLibraryPromotionQueue));
        OnPropertyChanged(nameof(FabricationOrderingReadiness));
        OnPropertyChanged(nameof(MarketplaceIntegrationStatus));
    }

    private void ApplyLibrarySearchResult(BuiltInHawkCadLibrarySearchResult result)
    {
        ComponentManager.ReplaceFromCatalog(CreateStarterCatalog(result.Components));
        BuiltInLibrary = BuiltInLibrary with
        {
            LoadedDevices = result.LoadedDevices,
            StatusText = result.StatusText
        };
        OnPropertyChanged(nameof(BuiltInLibrary));
        OnPropertyChanged(nameof(UnifiedComponentSourceRows));
        OnPropertyChanged(nameof(UnifiedComponentSourceSummary));
        OnPropertyChanged(nameof(InUseVendorCatalogSyncQueue));
        OnPropertyChanged(nameof(InUseVendorCatalogSyncSummary));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private static string DefaultInUseVendorCatalogSyncStatePath(string artifactDirectory) =>
        Path.Combine(artifactDirectory, "vendor-sync", "in-use-vendor-sync-state.json");

    private static string DefaultInUseVendorCatalogFreshnessPolicyPath(string artifactDirectory) =>
        Path.Combine(artifactDirectory, "vendor-sync", "in-use-vendor-freshness-policy.json");

    private static string FormatFreshnessHours(TimeSpan window) =>
        $"{FormatFreshnessHoursValue(window)}h";

    private static string FormatFreshnessHoursValue(TimeSpan window) =>
        window.TotalHours.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

    private static string FormatMillimeters(long internalUnits) =>
        ((decimal)internalUnits / CadUnit.InternalUnitsPerMillimeter).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);

    private static string Pluralize(int count, string singular) => count == 1 ? singular : $"{singular}s";

    private static string ComputeSha256(string content) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();

    private static string LoadHawkCadLibraryJson()
    {
        if (File.Exists(DefaultHawkCadLibraryPath))
        {
            return File.ReadAllText(DefaultHawkCadLibraryPath);
        }

        if (File.Exists(SourceTreeHawkCadLibraryPath))
        {
            return File.ReadAllText(SourceTreeHawkCadLibraryPath);
        }

        return CuratedHawkCadStarterLibraryJson;
    }

    private const string CuratedHawkCadStarterLibraryJson = """
        {
          "attributes": [
            {
              "name": "Description",
              "value": "Curated DragonCAD starter subset derived from the archived HawkCAD core Eagle library."
            },
            {
              "name": "SeedSources",
              "value": "adafruit-eagle-library, sparkfun-eagle-libraries"
            }
          ],
          "devices": [
            {
              "attributes": [
                {
                  "name": "Description",
                  "value": "555 timer from the archived HawkCAD/Adafruit Eagle library seed."
                },
                {
                  "name": "Prefix",
                  "value": "IC"
                }
              ],
              "gates": [
                {
                  "name": "A",
                  "symbolName": "adafruit-eagle-library/adafruit/555",
                  "variantName": "adafruit-eagle-library/adafruit/DIP8"
                }
              ],
              "mappings": [
                {
                  "gateName": "A",
                  "pinName": "GND",
                  "padName": "1"
                },
                {
                  "gateName": "A",
                  "pinName": "TR",
                  "padName": "2"
                },
                {
                  "gateName": "A",
                  "pinName": "Q",
                  "padName": "3"
                },
                {
                  "gateName": "A",
                  "pinName": "R",
                  "padName": "4"
                },
                {
                  "gateName": "A",
                  "pinName": "CV",
                  "padName": "5"
                },
                {
                  "gateName": "A",
                  "pinName": "THR",
                  "padName": "6"
                },
                {
                  "gateName": "A",
                  "pinName": "DIS",
                  "padName": "7"
                },
                {
                  "gateName": "A",
                  "pinName": "V+",
                  "padName": "8"
                }
              ],
              "name": "adafruit-eagle-library/adafruit/*555",
              "variants": [
                {
                  "name": "adafruit-eagle-library/adafruit/DIP8",
                  "packageName": "adafruit-eagle-library/adafruit/DIP8"
                }
              ]
            },
            {
              "attributes": [
                {
                  "name": "Description",
                  "value": "0603 resistor from the archived HawkCAD/SparkFun Eagle library seed."
                },
                {
                  "name": "Manufacturer",
                  "value": "Yageo"
                },
                {
                  "name": "PartNumber",
                  "value": "RC0603FR-0710KL"
                }
              ],
              "gates": [
                {
                  "name": "G$1",
                  "symbolName": "sparkfun-eagle-libraries/SparkFun-Resistors/R-US",
                  "variantName": "sparkfun-eagle-libraries/SparkFun-Resistors/0603"
                }
              ],
              "mappings": [
                {
                  "gateName": "G$1",
                  "pinName": "1",
                  "padName": "1"
                },
                {
                  "gateName": "G$1",
                  "pinName": "2",
                  "padName": "2"
                }
              ],
              "name": "sparkfun-eagle-libraries/SparkFun-Resistors/RESISTOR-0603",
              "variants": [
                {
                  "name": "sparkfun-eagle-libraries/SparkFun-Resistors/0603",
                  "packageName": "sparkfun-eagle-libraries/SparkFun-Resistors/0603"
                }
              ]
            }
          ],
          "name": "DragonCAD Curated HawkCAD Starter Components",
          "packages": [
            {
              "name": "adafruit-eagle-library/adafruit/DIP8",
              "pads": [
                {
                  "name": "1",
                  "position": { "x": -3810000, "y": -3810000 },
                  "size": { "x": 1524000, "y": 1524000 },
                  "shape": "Round",
                  "drillSize": { "x": 900000, "y": 900000 }
                },
                {
                  "name": "2",
                  "position": { "x": -1270000, "y": -3810000 },
                  "size": { "x": 1524000, "y": 1524000 },
                  "shape": "Round",
                  "drillSize": { "x": 900000, "y": 900000 }
                },
                {
                  "name": "3",
                  "position": { "x": 1270000, "y": -3810000 },
                  "size": { "x": 1524000, "y": 1524000 },
                  "shape": "Round",
                  "drillSize": { "x": 900000, "y": 900000 }
                },
                {
                  "name": "4",
                  "position": { "x": 3810000, "y": -3810000 },
                  "size": { "x": 1524000, "y": 1524000 },
                  "shape": "Round",
                  "drillSize": { "x": 900000, "y": 900000 }
                },
                {
                  "name": "5",
                  "position": { "x": 3810000, "y": 3810000 },
                  "size": { "x": 1524000, "y": 1524000 },
                  "shape": "Round",
                  "drillSize": { "x": 900000, "y": 900000 }
                },
                {
                  "name": "6",
                  "position": { "x": 1270000, "y": 3810000 },
                  "size": { "x": 1524000, "y": 1524000 },
                  "shape": "Round",
                  "drillSize": { "x": 900000, "y": 900000 }
                },
                {
                  "name": "7",
                  "position": { "x": -1270000, "y": 3810000 },
                  "size": { "x": 1524000, "y": 1524000 },
                  "shape": "Round",
                  "drillSize": { "x": 900000, "y": 900000 }
                },
                {
                  "name": "8",
                  "position": { "x": -3810000, "y": 3810000 },
                  "size": { "x": 1524000, "y": 1524000 },
                  "shape": "Round",
                  "drillSize": { "x": 900000, "y": 900000 }
                }
              ],
              "silkscreen": [
                {
                  "start": { "x": -5080000, "y": -5080000 },
                  "end": { "x": 5080000, "y": -5080000 }
                },
                {
                  "start": { "x": 5080000, "y": -5080000 },
                  "end": { "x": 5080000, "y": 5080000 }
                },
                {
                  "start": { "x": 5080000, "y": 5080000 },
                  "end": { "x": -5080000, "y": 5080000 }
                },
                {
                  "start": { "x": -5080000, "y": 5080000 },
                  "end": { "x": -5080000, "y": -5080000 }
                }
              ]
            },
            {
              "name": "sparkfun-eagle-libraries/SparkFun-Resistors/0603",
              "pads": [
                {
                  "name": "1",
                  "position": { "x": -750000, "y": 0 },
                  "size": { "x": 900000, "y": 700000 },
                  "technology": "SurfaceMount"
                },
                {
                  "name": "2",
                  "position": { "x": 750000, "y": 0 },
                  "size": { "x": 900000, "y": 700000 },
                  "technology": "SurfaceMount"
                }
              ],
              "silkscreen": [
                {
                  "start": { "x": -1300000, "y": -500000 },
                  "end": { "x": 1300000, "y": -500000 }
                },
                {
                  "start": { "x": -1300000, "y": 500000 },
                  "end": { "x": 1300000, "y": 500000 }
                }
              ]
            }
          ],
          "symbols": [
            {
              "name": "adafruit-eagle-library/adafruit/555",
              "pins": [
                {
                  "name": "GND",
                  "position": { "x": -3810000, "y": -3810000 },
                  "electricalType": "Power"
                },
                {
                  "name": "TR",
                  "position": { "x": -3810000, "y": -2540000 },
                  "electricalType": "Input"
                },
                {
                  "name": "Q",
                  "position": { "x": 3810000, "y": -1270000 },
                  "electricalType": "Output"
                },
                {
                  "name": "R",
                  "position": { "x": -3810000, "y": 0 },
                  "electricalType": "Input"
                },
                {
                  "name": "CV",
                  "position": { "x": -3810000, "y": 1270000 },
                  "electricalType": "Input"
                },
                {
                  "name": "THR",
                  "position": { "x": -3810000, "y": 2540000 },
                  "electricalType": "Input"
                },
                {
                  "name": "DIS",
                  "position": { "x": 3810000, "y": 2540000 },
                  "electricalType": "Output"
                },
                {
                  "name": "V+",
                  "position": { "x": 3810000, "y": 3810000 },
                  "electricalType": "Power"
                }
              ],
              "outlines": [
                {
                  "start": { "x": -2540000, "y": -5080000 },
                  "end": { "x": 2540000, "y": -5080000 }
                },
                {
                  "start": { "x": 2540000, "y": -5080000 },
                  "end": { "x": 2540000, "y": 5080000 }
                },
                {
                  "start": { "x": 2540000, "y": 5080000 },
                  "end": { "x": -2540000, "y": 5080000 }
                },
                {
                  "start": { "x": -2540000, "y": 5080000 },
                  "end": { "x": -2540000, "y": -5080000 }
                }
              ],
              "texts": [
                {
                  "kind": "Name",
                  "position": { "x": 0, "y": 6350000 },
                  "value": ">NAME"
                },
                {
                  "kind": "Value",
                  "position": { "x": 0, "y": -6350000 },
                  "value": ">VALUE"
                }
              ]
            },
            {
              "name": "sparkfun-eagle-libraries/SparkFun-Resistors/R-US",
              "pins": [
                {
                  "name": "1",
                  "position": { "x": -2540000, "y": 0 }
                },
                {
                  "name": "2",
                  "position": { "x": 2540000, "y": 0 }
                }
              ],
              "outlines": [
                {
                  "start": { "x": -1270000, "y": 0 },
                  "end": { "x": -846000, "y": 423000 }
                },
                {
                  "start": { "x": -846000, "y": 423000 },
                  "end": { "x": -423000, "y": -423000 }
                },
                {
                  "start": { "x": -423000, "y": -423000 },
                  "end": { "x": 0, "y": 423000 }
                },
                {
                  "start": { "x": 0, "y": 423000 },
                  "end": { "x": 423000, "y": -423000 }
                },
                {
                  "start": { "x": 423000, "y": -423000 },
                  "end": { "x": 846000, "y": 423000 }
                },
                {
                  "start": { "x": 846000, "y": 423000 },
                  "end": { "x": 1270000, "y": 0 }
                }
              ],
              "texts": [
                {
                  "kind": "Name",
                  "position": { "x": 0, "y": 1270000 },
                  "value": ">NAME"
                },
                {
                  "kind": "Value",
                  "position": { "x": 0, "y": -1270000 },
                  "value": ">VALUE"
                }
              ]
            }
          ],
          "version": 1
        }
        """;

}

public sealed record BuiltInLibraryViewModel(
    string Name,
    int TotalDevices,
    int LoadedDevices,
    string StatusText);

public sealed record UnifiedComponentSourceRow(
    string SourceKind,
    string DisplayName,
    string Manufacturer,
    string ManufacturerPartNumber,
    string Category,
    string ComponentId,
    string Detail);
