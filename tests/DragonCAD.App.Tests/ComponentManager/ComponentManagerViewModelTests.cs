using DragonCAD.App.ComponentManager;
using DragonCAD.App.Placement;
using DragonCAD.Core.Components.Catalog;
using DragonCAD.Core.Components.Definitions;
using DragonCAD.Core.Components.Identity;
using DragonCAD.Core.Geometry;

namespace DragonCAD.App.Tests.ComponentManager;

public sealed class ComponentManagerViewModelTests
{
    [Fact]
    public void FromCatalogBuildsDeterministicRowsWithComponentCapabilities()
    {
        ComponentCatalog catalog = new(
            BuiltInDefinitions:
            [
                Component(
                    "dragon:esp32-s3-wroom-1",
                    "ESP32-S3-WROOM-1",
                    ComponentKind.Module,
                    "Espressif",
                    "ESP32-S3-WROOM-1",
                    symbolCount: 1,
                    footprintCount: 2,
                    hasDatasheet: true,
                    hasSourcing: true,
                    hasModel3d: true),
                Component(
                    "dragon:usb-c-receptacle",
                    "USB-C Receptacle",
                    ComponentKind.Connector,
                    "GCT",
                    "USB4105",
                    symbolCount: 1,
                    footprintCount: 1,
                    hasDatasheet: true,
                    hasSourcing: false,
                    hasModel3d: false)
            ],
            UserDefinitions: [],
            ProjectDefinitions: []);

        ComponentManagerViewModel viewModel = ComponentManagerViewModel.FromCatalog(catalog);

        Assert.Equal(["ESP32-S3-WROOM-1", "USB-C Receptacle"], viewModel.Components.Select(row => row.DisplayName));
        ComponentManagerRow esp32 = viewModel.Components[0];
        Assert.Equal("dragon:esp32-s3-wroom-1", esp32.ComponentId);
        Assert.Equal("Module", esp32.Kind);
        Assert.Equal("Espressif", esp32.Manufacturer);
        Assert.Equal("ESP32-S3-WROOM-1", esp32.ManufacturerPartNumber);
        Assert.Equal("BuiltIn", esp32.Source);
        Assert.Equal(1, esp32.SymbolCount);
        Assert.Equal(2, esp32.FootprintCount);
        Assert.True(esp32.HasDatasheet);
        Assert.True(esp32.HasSourcing);
        Assert.True(esp32.HasModel3D);
        Assert.Equal("1 symbol / 2 footprints / datasheet / sourcing / 3D", esp32.CapabilitySummary);
    }

    [Fact]
    public void SearchTextFiltersByNameIdentityManufacturerAndPartNumber()
    {
        ComponentCatalog catalog = new(
            BuiltInDefinitions:
            [
                Component("dragon:ap2112k-3v3", "AP2112K-3.3", ComponentKind.IntegratedCircuit, "Diodes Inc.", "AP2112K-3.3TRG1"),
                Component("dragon:resistor-0603", "Resistor 0603", ComponentKind.Passive, "Yageo", "RC0603FR-0710KL")
            ],
            UserDefinitions: [],
            ProjectDefinitions: []);
        ComponentManagerViewModel viewModel = ComponentManagerViewModel.FromCatalog(catalog);

        viewModel.SearchText = "yageo";

        ComponentManagerRow row = Assert.Single(viewModel.Components);
        Assert.Equal("Resistor 0603", row.DisplayName);

        viewModel.SearchText = "ap2112";

        row = Assert.Single(viewModel.Components);
        Assert.Equal("AP2112K-3.3", row.DisplayName);
    }

    [Fact]
    public void SearchTextFiltersByPackageAndFootprintText()
    {
        ComponentFootprintId soic8FootprintId = new("dragon:opamp:footprint:soic-8");
        ComponentFootprintId qfn16FootprintId = new("dragon:opamp:footprint:qfn-16");
        ComponentDefinition opAmp = new(
            new ComponentId("dragon:opamp"),
            "Dual Op Amp",
            ComponentKind.IntegratedCircuit,
            "Dragon",
            "DOP-1",
            Description: "",
            Attributes: [],
            Pins: [],
            Gates: [],
            Symbols: [],
            Footprints:
            [
                Footprint(soic8FootprintId, "SOIC-8 Narrow", padCount: 8),
                Footprint(qfn16FootprintId, "QFN-16 Exposed Pad", padCount: 16)
            ],
            Variants:
            [
                new ComponentVariant(new ComponentVariantId("dragon:opamp:variant:soic-8"), "SOIC package", soic8FootprintId, []),
                new ComponentVariant(new ComponentVariantId("dragon:opamp:variant:qfn-16"), "QFN package", qfn16FootprintId, [])
            ],
            PinPadMappings: [],
            Datasheets: [],
            Sourcing: [],
            PackageModels3D: [],
            Provenance: []);
        ComponentCatalog catalog = new(
            BuiltInDefinitions:
            [
                opAmp,
                Component("dragon:resistor-0603", "Resistor 0603", ComponentKind.Passive, "Yageo", "RC0603FR-0710KL")
            ],
            UserDefinitions: [],
            ProjectDefinitions: []);
        ComponentManagerViewModel viewModel = ComponentManagerViewModel.FromCatalog(catalog);

        viewModel.SearchText = "exposed pad";

        ComponentManagerRow row = Assert.Single(viewModel.Components);
        Assert.Equal("Dual Op Amp", row.DisplayName);

        viewModel.SearchText = "qfn package";

        row = Assert.Single(viewModel.Components);
        Assert.Equal("Dual Op Amp", row.DisplayName);
    }

    [Fact]
    public void TypeFilterOptionsContainAllPlusAvailableComponentKinds()
    {
        ComponentCatalog catalog = new(
            BuiltInDefinitions:
            [
                Component("dragon:module", "Module", ComponentKind.Module, "Dragon", "MOD"),
                Component("dragon:connector", "Connector", ComponentKind.Connector, "Dragon", "CON"),
                Component("dragon:passive", "Passive", ComponentKind.Passive, "Dragon", "PAS")
            ],
            UserDefinitions: [],
            ProjectDefinitions: []);

        ComponentManagerViewModel viewModel = ComponentManagerViewModel.FromCatalog(catalog);

        Assert.Equal(["All components (3)", "Connectors (1)", "Modules (1)", "Passives (1)"], viewModel.TypeFilterOptions);
        Assert.Equal("All components (3)", viewModel.SelectedTypeFilter);
    }

    [Fact]
    public void TypeFilterOptionsShowUserFacingLabelsWithCounts()
    {
        ComponentCatalog catalog = new(
            BuiltInDefinitions:
            [
                Component("dragon:module", "Module", ComponentKind.Module, "Dragon", "MOD"),
                Component("dragon:connector", "Connector", ComponentKind.Connector, "Dragon", "CON"),
                Component("dragon:passive", "Passive", ComponentKind.Passive, "Dragon", "PAS"),
                Component("dragon:capacitor", "Capacitor", ComponentKind.Passive, "Dragon", "CAP")
            ],
            UserDefinitions: [],
            ProjectDefinitions: []);

        ComponentManagerViewModel viewModel = ComponentManagerViewModel.FromCatalog(catalog);

        Assert.Equal(["All components (4)", "Connectors (1)", "Modules (1)", "Passives (2)"], viewModel.TypeFilterOptions);
        Assert.Equal("All components (4)", viewModel.SelectedTypeFilter);
    }

    [Fact]
    public void SelectedTypeFilterNarrowsVisibleComponentsByKind()
    {
        ComponentCatalog catalog = new(
            BuiltInDefinitions:
            [
                Component("dragon:module", "Module", ComponentKind.Module, "Dragon", "MOD"),
                Component("dragon:connector", "Connector", ComponentKind.Connector, "Dragon", "CON"),
                Component("dragon:passive", "Passive", ComponentKind.Passive, "Dragon", "PAS")
            ],
            UserDefinitions: [],
            ProjectDefinitions: []);
        ComponentManagerViewModel viewModel = ComponentManagerViewModel.FromCatalog(catalog);

        viewModel.SelectedTypeFilter = "Connector";

        ComponentManagerRow row = Assert.Single(viewModel.Components);
        Assert.Equal("Connector", row.Kind);
        Assert.Equal("Connector", row.DisplayName);
    }

    [Fact]
    public void SearchTextAndTypeFilterCompose()
    {
        ComponentCatalog catalog = new(
            BuiltInDefinitions:
            [
                Component("dragon:ap2112k-3v3", "AP2112K-3.3", ComponentKind.IntegratedCircuit, "Diodes Inc.", "AP2112K-3.3TRG1"),
                Component("dragon:resistor-0603", "Resistor 0603", ComponentKind.Passive, "Yageo", "RC0603FR-0710KL"),
                Component("dragon:resistor-array", "Resistor Array IC", ComponentKind.IntegratedCircuit, "Dragon", "RA-1")
            ],
            UserDefinitions: [],
            ProjectDefinitions: []);
        ComponentManagerViewModel viewModel = ComponentManagerViewModel.FromCatalog(catalog);

        viewModel.SearchText = "resistor";
        viewModel.SelectedTypeFilter = "IntegratedCircuit";

        ComponentManagerRow row = Assert.Single(viewModel.Components);
        Assert.Equal("Resistor Array IC", row.DisplayName);
    }

    [Fact]
    public void FiltersComposeAcrossValuePackageVendorLifecycleVerifiedStatusAndSource()
    {
        ComponentDefinition productionResistor = Component(
            "dragon:resistor-0603",
            "Resistor 0603",
            ComponentKind.Passive,
            "Yageo",
            "RC0603FR-0710KL",
            symbolCount: 1,
            footprintCount: 1,
            hasSourcing: true,
            attributes:
            [
                new ComponentAttribute("Value", "10 kOhm"),
                new ComponentAttribute("Lifecycle", "Production"),
                new ComponentAttribute("Package", "0603")
            ],
            provenance: [new ComponentProvenanceRecord(ComponentProvenanceKind.Native, "DragonCAD", "Curated native part")]);
        ComponentDefinition obsoleteResistor = Component(
            "dragon:resistor-0805",
            "Resistor 0805",
            ComponentKind.Passive,
            "Yageo",
            "RC0805JR-070RL",
            symbolCount: 1,
            footprintCount: 1,
            hasSourcing: false,
            attributes:
            [
                new ComponentAttribute("Value", "0 Ohm"),
                new ComponentAttribute("Lifecycle", "Obsolete"),
                new ComponentAttribute("Package", "0805")
            ],
            provenance: [new ComponentProvenanceRecord(ComponentProvenanceKind.EagleImport, "Eagle", "Imported library part")]);
        ComponentCatalog catalog = new(
            BuiltInDefinitions: [productionResistor],
            UserDefinitions: [obsoleteResistor],
            ProjectDefinitions: []);
        ComponentManagerViewModel viewModel = ComponentManagerViewModel.FromCatalog(catalog);

        viewModel.SelectedTypeFilter = "Passive";
        viewModel.ValueFilter = "10 k";
        viewModel.PackageFilter = "0603";
        viewModel.SelectedVendorAvailabilityFilter = "Vendor offers";
        viewModel.SelectedLifecycleFilter = "Production";
        viewModel.SelectedVerifiedStatusFilter = "Verified";
        viewModel.SelectedSourceFilter = "BuiltIn";

        ComponentManagerRow row = Assert.Single(viewModel.Components);
        Assert.Equal("Resistor 0603", row.DisplayName);
        Assert.Equal("10 kOhm", row.Value);
        Assert.Equal("Production", row.Lifecycle);
        Assert.True(row.HasVendorOffers);
        Assert.True(row.IsVerified);
    }

    [Fact]
    public void ResultCollectionsSeparateVerifiedPlaceableCatalogOnlyAndDraftComponents()
    {
        ComponentCatalog catalog = new(
            BuiltInDefinitions:
            [
                Component(
                    "dragon:verified-opamp",
                    "Verified Op Amp",
                    ComponentKind.IntegratedCircuit,
                    "Dragon",
                    "DOP-1",
                    symbolCount: 1,
                    footprintCount: 1,
                    provenance: [new ComponentProvenanceRecord(ComponentProvenanceKind.Native, "DragonCAD", "Curated native part")]),
                Component(
                    "dragon:catalog-only-regulator",
                    "Catalog Only Regulator",
                    ComponentKind.IntegratedCircuit,
                    "Texas Instruments",
                    "LM7805CT",
                    symbolCount: 0,
                    footprintCount: 0,
                    hasDatasheet: true,
                    hasSourcing: true)
            ],
            UserDefinitions: [],
            ProjectDefinitions:
            [
                Component(
                    "dragon:generated-draft",
                    "Generated Draft",
                    ComponentKind.IntegratedCircuit,
                    "AI",
                    "DRAFT-1",
                    symbolCount: 1,
                    footprintCount: 1,
                    provenance: [new ComponentProvenanceRecord(ComponentProvenanceKind.DatasheetGenerated, "AI", "Pending review")])
            ]);

        ComponentManagerViewModel viewModel = ComponentManagerViewModel.FromCatalog(catalog);

        Assert.Equal(["Verified Op Amp"], viewModel.VerifiedPlaceableComponents.Select(row => row.DisplayName));
        Assert.Equal(["Catalog Only Regulator"], viewModel.CatalogOnlyComponents.Select(row => row.DisplayName));
        Assert.Equal(["Generated Draft"], viewModel.DraftComponents.Select(row => row.DisplayName));
    }

    [Fact]
    public void RowsExposeDatasheetVendorOffersWarningsAndPlacementReviewGate()
    {
        ComponentCatalog catalog = new(
            BuiltInDefinitions:
            [
                Component(
                    "dragon:verified-regulator",
                    "Verified Regulator",
                    ComponentKind.IntegratedCircuit,
                    "Texas Instruments",
                    "LM7805CT",
                    symbolCount: 1,
                    footprintCount: 1,
                    hasDatasheet: true,
                    hasSourcing: true,
                    provenance: [new ComponentProvenanceRecord(ComponentProvenanceKind.Native, "DragonCAD", "Curated native part")]),
                Component(
                    "dragon:generated-draft",
                    "Generated Draft",
                    ComponentKind.IntegratedCircuit,
                    "AI",
                    "DRAFT-1",
                    symbolCount: 1,
                    footprintCount: 1,
                    provenance: [new ComponentProvenanceRecord(ComponentProvenanceKind.DatasheetGenerated, "AI", "Pending review")])
            ],
            UserDefinitions: [],
            ProjectDefinitions: []);
        ComponentManagerViewModel viewModel = ComponentManagerViewModel.FromCatalog(catalog);

        ComponentManagerRow verified = viewModel.Components.Single(row => row.DisplayName == "Verified Regulator");
        Assert.Equal("https://example.test/datasheet.pdf", verified.DatasheetLink);
        ComponentVendorOffer offer = Assert.Single(verified.VendorOffers);
        Assert.Equal("Digi-Key", offer.Vendor);
        Assert.Equal("123", offer.DistributorPartNumber);
        Assert.True(verified.CanPlaceWithoutReview);
        Assert.False(verified.RequiresReviewBeforePlacement);
        Assert.Empty(verified.Warnings);

        ComponentManagerRow draft = viewModel.Components.Single(row => row.DisplayName == "Generated Draft");
        Assert.False(draft.CanPlaceWithoutReview);
        Assert.True(draft.RequiresReviewBeforePlacement);
        Assert.Contains(draft.Warnings, warning => warning.Contains("review", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SelectedComponentStaysVisibleWhenFilterStillContainsIt()
    {
        ComponentCatalog catalog = new(
            BuiltInDefinitions:
            [
                Component("dragon:module", "Module", ComponentKind.Module, "Dragon", "MOD"),
                Component("dragon:connector", "Connector", ComponentKind.Connector, "Dragon", "CON"),
                Component("dragon:resistor", "Resistor", ComponentKind.Passive, "Dragon", "RES"),
                Component("dragon:capacitor", "Capacitor", ComponentKind.Passive, "Dragon", "CAP")
            ],
            UserDefinitions: [],
            ProjectDefinitions: []);
        ComponentManagerViewModel viewModel = ComponentManagerViewModel.FromCatalog(catalog);
        ComponentManagerRow resistor = viewModel.Components.Single(row => row.DisplayName == "Resistor");
        viewModel.SelectedComponent = resistor;

        viewModel.SelectedTypeFilter = "Passives (2)";

        Assert.Equal(resistor, viewModel.SelectedComponent);
        Assert.Equal(["Capacitor", "Resistor"], viewModel.Components.Select(row => row.DisplayName));
    }

    [Fact]
    public void SelectedComponentFallsBackWhenFilterExcludesIt()
    {
        ComponentCatalog catalog = new(
            BuiltInDefinitions:
            [
                Component("dragon:module", "Module", ComponentKind.Module, "Dragon", "MOD"),
                Component("dragon:connector", "Connector", ComponentKind.Connector, "Dragon", "CON"),
                Component("dragon:resistor", "Resistor", ComponentKind.Passive, "Dragon", "RES")
            ],
            UserDefinitions: [],
            ProjectDefinitions: []);
        ComponentManagerViewModel viewModel = ComponentManagerViewModel.FromCatalog(catalog);
        viewModel.SelectedComponent = viewModel.Components.Single(row => row.DisplayName == "Resistor");

        viewModel.SelectedTypeFilter = "Connectors (1)";

        ComponentManagerRow row = Assert.Single(viewModel.Components);
        Assert.Equal("Connector", row.DisplayName);
        Assert.Equal(row, viewModel.SelectedComponent);
    }

    [Fact]
    public void RowsExposeSymbolAndFootprintPreviewGeometryForRendering()
    {
        ComponentCatalog catalog = new(
            BuiltInDefinitions:
            [
                Component(
                    "dragon:test-ic",
                    "Test IC",
                    ComponentKind.IntegratedCircuit,
                    "Dragon",
                    "TIC-1",
                    symbolCount: 1,
                    footprintCount: 1,
                    includePreviewGeometry: true)
            ],
            UserDefinitions: [],
            ProjectDefinitions: []);

        ComponentManagerRow row = Assert.Single(ComponentManagerViewModel.FromCatalog(catalog).Components);

        ComponentSymbolPreview symbol = row.SymbolPreview;
        Assert.Equal(new CadRectangle(-150000, -100000, 100000, 100000), symbol.Bounds);
        Assert.Contains(symbol.Lines, line => line.Start == new CadPoint(-100000, -100000) && line.End == new CadPoint(100000, -100000));
        ComponentSymbolPinPreview pin = Assert.Single(symbol.Pins);
        Assert.Equal("P1", pin.Name);
        Assert.Equal(new CadPoint(-150000, 0), pin.ConnectPoint);
        Assert.Equal(new CadPoint(-100000, 0), pin.BodyPoint);

        ComponentFootprintPreview footprint = row.FootprintPreview;
        ComponentFootprintPadPreview pad = Assert.Single(footprint.Pads);
        Assert.Equal("1", pad.Name);
        Assert.Equal(new CadPoint(0, 0), pad.Position);
        Assert.Equal(new CadVector(60000, 80000), pad.Size);
        Assert.Equal(new CadRectangle(-30000, -40000, 100000, 100000), footprint.Bounds);
    }

    [Fact]
    public void RowsExposeStablePackageOptionsForSelector()
    {
        ComponentFootprintId soic8FootprintId = new("dragon:opamp:footprint:soic-8");
        ComponentFootprintId tssop8FootprintId = new("dragon:opamp:footprint:tssop-8");
        ComponentVariantId soic8VariantId = new("dragon:opamp:variant:soic-8");
        ComponentVariantId tssop8VariantId = new("dragon:opamp:variant:tssop-8");
        ComponentDefinition definition = new(
            new ComponentId("dragon:opamp"),
            "Dual Op Amp",
            ComponentKind.IntegratedCircuit,
            "Dragon",
            "DOP-1",
            Description: "",
            Attributes: [],
            Pins: [],
            Gates: [],
            Symbols: [],
            Footprints:
            [
                Footprint(soic8FootprintId, "SOIC-8", padCount: 8),
                Footprint(tssop8FootprintId, "TSSOP-8", padCount: 8)
            ],
            Variants:
            [
                new ComponentVariant(soic8VariantId, "SOIC package", soic8FootprintId, [new ComponentAttribute("Package", "SOIC-8")]),
                new ComponentVariant(tssop8VariantId, "TSSOP package", tssop8FootprintId, [new ComponentAttribute("Package", "TSSOP-8")])
            ],
            PinPadMappings: [],
            Datasheets: [],
            Sourcing: [],
            PackageModels3D: [new ComponentPackageModel3D("soic-step", ComponentPackageModel3DFormat.Step, "models/soic.step", soic8VariantId)],
            Provenance: []);
        ComponentCatalog catalog = new(BuiltInDefinitions: [definition], UserDefinitions: [], ProjectDefinitions: []);

        ComponentManagerRow row = Assert.Single(ComponentManagerViewModel.FromCatalog(catalog).Components);

        Assert.Equal(2, row.PackageOptionCount);
        Assert.Equal("SOIC package (SOIC-8)", row.ActivePackageLabel);
        Assert.Equal(
            ["SOIC package (SOIC-8) - 8 pads - 3D model", "TSSOP package (TSSOP-8) - 8 pads"],
            row.PackageOptions.Select(option => option.DisplayText));
        Assert.Equal(
            ["dragon:opamp:variant:soic-8", "dragon:opamp:variant:tssop-8"],
            row.PackageOptions.Select(option => option.VariantId));
        Assert.Equal(
            ["dragon:opamp:footprint:soic-8", "dragon:opamp:footprint:tssop-8"],
            row.PackageOptions.Select(option => option.FootprintId));
        Assert.True(row.PackageOptions[0].IsActive);
        Assert.False(row.PackageOptions[1].IsActive);
    }

    [Fact]
    public void RowCanSelectActivePackageOptionWithoutMutatingCatalogDefinition()
    {
        ComponentFootprintId soic8FootprintId = new("dragon:opamp:footprint:soic-8");
        ComponentFootprintId tssop8FootprintId = new("dragon:opamp:footprint:tssop-8");
        ComponentVariantId soic8VariantId = new("dragon:opamp:variant:soic-8");
        ComponentVariantId tssop8VariantId = new("dragon:opamp:variant:tssop-8");
        ComponentDefinition definition = new(
            new ComponentId("dragon:opamp"),
            "Dual Op Amp",
            ComponentKind.IntegratedCircuit,
            "Dragon",
            "DOP-1",
            Description: "",
            Attributes: [],
            Pins: [],
            Gates: [],
            Symbols: [],
            Footprints:
            [
                Footprint(soic8FootprintId, "SOIC-8", padCount: 8),
                Footprint(tssop8FootprintId, "TSSOP-8", padCount: 8)
            ],
            Variants:
            [
                new ComponentVariant(soic8VariantId, "SOIC package", soic8FootprintId, [new ComponentAttribute("Package", "SOIC-8")]),
                new ComponentVariant(tssop8VariantId, "TSSOP package", tssop8FootprintId, [new ComponentAttribute("Package", "TSSOP-8")])
            ],
            PinPadMappings: [],
            Datasheets: [],
            Sourcing: [],
            PackageModels3D: [],
            Provenance: []);
        ComponentCatalog catalog = new(BuiltInDefinitions: [definition], UserDefinitions: [], ProjectDefinitions: []);
        ComponentManagerViewModel viewModel = ComponentManagerViewModel.FromCatalog(catalog);
        ComponentManagerRow row = Assert.Single(viewModel.Components);

        viewModel.SelectPackageOption(row, row.PackageOptions[1]);

        ComponentManagerRow updatedRow = Assert.Single(viewModel.Components);
        Assert.Equal(updatedRow, viewModel.SelectedComponent);
        Assert.Equal("TSSOP package (TSSOP-8)", updatedRow.ActivePackageLabel);
        Assert.Equal(updatedRow.PackageOptions[1], updatedRow.SelectedPackageOption);
        Assert.False(updatedRow.PackageOptions[0].IsActive);
        Assert.True(updatedRow.PackageOptions[1].IsActive);
        Assert.Equal(["SOIC package", "TSSOP package"], definition.Variants.Select(variant => variant.Name));
    }

    [Fact]
    public void RowsExposeSelectedPackageSummaryForUiBinding()
    {
        ComponentFootprintId soic8FootprintId = new("dragon:opamp:footprint:soic-8");
        ComponentFootprintId tssop8FootprintId = new("dragon:opamp:footprint:tssop-8");
        ComponentVariantId soic8VariantId = new("dragon:opamp:variant:soic-8");
        ComponentVariantId tssop8VariantId = new("dragon:opamp:variant:tssop-8");
        ComponentDefinition definition = new(
            new ComponentId("dragon:opamp"),
            "Dual Op Amp",
            ComponentKind.IntegratedCircuit,
            "Dragon",
            "DOP-1",
            Description: "",
            Attributes: [],
            Pins: [],
            Gates: [],
            Symbols: [],
            Footprints:
            [
                Footprint(soic8FootprintId, "SOIC-8", padCount: 8),
                Footprint(tssop8FootprintId, "TSSOP-8", padCount: 8)
            ],
            Variants:
            [
                new ComponentVariant(soic8VariantId, "SOIC package", soic8FootprintId, []),
                new ComponentVariant(tssop8VariantId, "TSSOP package", tssop8FootprintId, [])
            ],
            PinPadMappings: [],
            Datasheets: [],
            Sourcing: [],
            PackageModels3D: [new ComponentPackageModel3D("soic-step", ComponentPackageModel3DFormat.Step, "models/soic.step", soic8VariantId)],
            Provenance: []);
        ComponentCatalog catalog = new(BuiltInDefinitions: [definition], UserDefinitions: [], ProjectDefinitions: []);
        ComponentManagerViewModel viewModel = ComponentManagerViewModel.FromCatalog(catalog);
        ComponentManagerRow row = Assert.Single(viewModel.Components);

        Assert.Equal("dragon:opamp:footprint:soic-8", row.SelectedPackageSummary.FootprintId);
        Assert.Equal(8, row.SelectedPackageSummary.PadCount);
        Assert.True(row.SelectedPackageSummary.HasModel3D);
        Assert.Equal("SOIC package (SOIC-8) - dragon:opamp:footprint:soic-8 - 8 pads - 3D model", row.SelectedPackageSummary.DisplayText);

        viewModel.SelectPackageOption(row, row.PackageOptions[1]);

        ComponentManagerRow updatedRow = Assert.Single(viewModel.Components);
        Assert.Equal("dragon:opamp:footprint:tssop-8", updatedRow.SelectedPackageSummary.FootprintId);
        Assert.Equal(8, updatedRow.SelectedPackageSummary.PadCount);
        Assert.False(updatedRow.SelectedPackageSummary.HasModel3D);
        Assert.Equal("TSSOP package (TSSOP-8) - dragon:opamp:footprint:tssop-8 - 8 pads", updatedRow.SelectedPackageSummary.DisplayText);
    }

    [Fact]
    public void SelectedPackageSummaryPropertiesExposeActivePackageDetailsAndReadiness()
    {
        ComponentFootprintId soic8FootprintId = new("dragon:opamp:footprint:soic-8");
        ComponentFootprintId qfn16FootprintId = new("dragon:opamp:footprint:qfn-16");
        ComponentVariantId soic8VariantId = new("dragon:opamp:variant:soic-8");
        ComponentVariantId qfn16VariantId = new("dragon:opamp:variant:qfn-16");
        ComponentDefinition definition = new(
            new ComponentId("dragon:opamp"),
            "Dual Op Amp",
            ComponentKind.IntegratedCircuit,
            "Dragon",
            "DOP-1",
            Description: "",
            Attributes: [],
            Pins: [],
            Gates: [],
            Symbols: [new ComponentSymbol(new ComponentSymbolId("dragon:opamp:symbol"), "Primary", [], [], [])],
            Footprints:
            [
                Footprint(soic8FootprintId, "SOIC-8", padCount: 8),
                Footprint(qfn16FootprintId, "QFN-16", padCount: 16)
            ],
            Variants:
            [
                new ComponentVariant(soic8VariantId, "SOIC package", soic8FootprintId, [new ComponentAttribute("Temperature", "-40C to 85C")]),
                new ComponentVariant(qfn16VariantId, "QFN package", qfn16FootprintId, [new ComponentAttribute("Temperature", "-40C to 125C")])
            ],
            PinPadMappings: [],
            Datasheets: [],
            Sourcing: [],
            PackageModels3D: [new ComponentPackageModel3D("qfn-step", ComponentPackageModel3DFormat.Step, "models/qfn.step", qfn16VariantId)],
            Provenance: [new ComponentProvenanceRecord(ComponentProvenanceKind.Native, "DragonCAD", "Curated native part")]);
        ComponentCatalog catalog = new(BuiltInDefinitions: [definition], UserDefinitions: [], ProjectDefinitions: []);
        ComponentManagerViewModel viewModel = ComponentManagerViewModel.FromCatalog(catalog);
        ComponentManagerRow row = Assert.Single(viewModel.Components);

        viewModel.SelectPackageOption(row, row.PackageOptions.Single(option => option.Label == "QFN package (QFN-16)"));

        Assert.Equal("QFN package (QFN-16)", viewModel.SelectedPackageName);
        Assert.Equal("2 packages", viewModel.SelectedPackageCountText);
        Assert.Equal("Variant dragon:opamp:variant:qfn-16 / Footprint dragon:opamp:footprint:qfn-16", viewModel.SelectedPackageVariantMetadata);
        Assert.Equal("QFN package (QFN-16) - dragon:opamp:footprint:qfn-16 - 16 pads - 3D model", viewModel.SelectedPackagePreviewSummary);
        Assert.Equal("Ready for placement", viewModel.SelectedPackagePlacementReadiness);
        Assert.Equal("dragon:opamp:footprint:qfn-16", viewModel.SelectedPackageSummary.FootprintId);
        Assert.Equal(16, viewModel.SelectedPackageSummary.PadCount);
        Assert.True(viewModel.SelectedPackageSummary.HasModel3D);
    }

    [Fact]
    public void ReplaceFromCatalogPreservesActivePackageWhenStillAvailable()
    {
        ComponentFootprintId soic8FootprintId = new("dragon:opamp:footprint:soic-8");
        ComponentFootprintId tssop8FootprintId = new("dragon:opamp:footprint:tssop-8");
        ComponentVariantId soic8VariantId = new("dragon:opamp:variant:soic-8");
        ComponentVariantId tssop8VariantId = new("dragon:opamp:variant:tssop-8");
        ComponentDefinition original = MultiPackageComponent(soic8FootprintId, tssop8FootprintId, soic8VariantId, tssop8VariantId);
        ComponentDefinition refreshed = MultiPackageComponent(soic8FootprintId, tssop8FootprintId, soic8VariantId, tssop8VariantId);
        ComponentManagerViewModel viewModel = ComponentManagerViewModel.FromCatalog(new ComponentCatalog(BuiltInDefinitions: [original], UserDefinitions: [], ProjectDefinitions: []));
        ComponentManagerRow row = Assert.Single(viewModel.Components);
        viewModel.SelectPackageOption(row, row.PackageOptions[1]);

        viewModel.ReplaceFromCatalog(new ComponentCatalog(BuiltInDefinitions: [refreshed], UserDefinitions: [], ProjectDefinitions: []));

        ComponentManagerRow updatedRow = Assert.Single(viewModel.Components);
        Assert.Equal("TSSOP package (TSSOP-8)", updatedRow.ActivePackageLabel);
        Assert.True(updatedRow.PackageOptions[1].IsActive);
        Assert.Equal("TSSOP package (TSSOP-8)", viewModel.SelectedPackageName);
        Assert.Equal("Package selection preserved.", viewModel.SelectedPackageAvailabilityMessage);
    }

    [Fact]
    public void PackageFilterPreservesActivePackageWhenSelectedComponentStillMatches()
    {
        ComponentFootprintId soic8FootprintId = new("dragon:opamp:footprint:soic-8");
        ComponentFootprintId tssop8FootprintId = new("dragon:opamp:footprint:tssop-8");
        ComponentVariantId soic8VariantId = new("dragon:opamp:variant:soic-8");
        ComponentVariantId tssop8VariantId = new("dragon:opamp:variant:tssop-8");
        ComponentDefinition opAmp = MultiPackageComponent(soic8FootprintId, tssop8FootprintId, soic8VariantId, tssop8VariantId);
        ComponentCatalog catalog = new(
            BuiltInDefinitions:
            [
                opAmp,
                Component("dragon:resistor-0603", "Resistor 0603", ComponentKind.Passive, "Yageo", "RC0603FR-0710KL")
            ],
            UserDefinitions: [],
            ProjectDefinitions: []);
        ComponentManagerViewModel viewModel = ComponentManagerViewModel.FromCatalog(catalog);
        ComponentManagerRow row = viewModel.Components.Single(component => component.DisplayName == "Dual Op Amp");
        viewModel.SelectPackageOption(row, row.PackageOptions[1]);

        viewModel.PackageFilter = "TSSOP-8";

        ComponentManagerRow filteredRow = Assert.Single(viewModel.Components);
        Assert.Equal("Dual Op Amp", filteredRow.DisplayName);
        Assert.Equal("TSSOP package (TSSOP-8)", filteredRow.ActivePackageLabel);
        Assert.Equal("TSSOP package (TSSOP-8)", viewModel.SelectedPackageName);
    }

    [Fact]
    public void PackageFilterSelectsMatchingPackageWhenActivePackageIsFilteredOut()
    {
        ComponentFootprintId soic8FootprintId = new("dragon:opamp:footprint:soic-8");
        ComponentFootprintId tssop8FootprintId = new("dragon:opamp:footprint:tssop-8");
        ComponentVariantId soic8VariantId = new("dragon:opamp:variant:soic-8");
        ComponentVariantId tssop8VariantId = new("dragon:opamp:variant:tssop-8");
        ComponentDefinition opAmp = MultiPackageComponent(soic8FootprintId, tssop8FootprintId, soic8VariantId, tssop8VariantId);
        ComponentManagerViewModel viewModel = ComponentManagerViewModel.FromCatalog(new ComponentCatalog(BuiltInDefinitions: [opAmp], UserDefinitions: [], ProjectDefinitions: []));
        ComponentManagerRow row = Assert.Single(viewModel.Components);
        viewModel.SelectPackageOption(row, row.PackageOptions[1]);

        viewModel.PackageFilter = "SOIC-8";

        ComponentManagerRow filteredRow = Assert.Single(viewModel.Components);
        Assert.Equal("Dual Op Amp", filteredRow.DisplayName);
        Assert.Equal("SOIC package (SOIC-8)", filteredRow.ActivePackageLabel);
        Assert.Equal("SOIC package (SOIC-8)", viewModel.SelectedPackageName);
        Assert.Equal("Active package 'TSSOP package (TSSOP-8)' is filtered out by package filter 'SOIC-8'; using 'SOIC package (SOIC-8)'.", viewModel.SelectedPackageAvailabilityMessage);
    }

    [Fact]
    public void PackageFilterMatchesFootprintTypeValueAndPackageMetadata()
    {
        ComponentFootprintId axialFootprintId = new("dragon:resistor:footprint:axial");
        ComponentFootprintId chipFootprintId = new("dragon:resistor:footprint:0603");
        ComponentDefinition resistor = new(
            new ComponentId("dragon:resistor"),
            "Resistor",
            ComponentKind.Passive,
            "Dragon",
            "R-1",
            Description: "",
            Attributes: [new ComponentAttribute("Value", "10 kOhm")],
            Pins: [],
            Gates: [],
            Symbols: [],
            Footprints:
            [
                new ComponentFootprint(
                    axialFootprintId,
                    "Axial resistor",
                    [new ComponentFootprintPad(new ComponentPadId("dragon:resistor:axial:pad:1"), "1", new CadPoint(0, 0), new CadVector(60_000, 80_000), ComponentPadTechnology.ThroughHole, ComponentPadShape.Round)],
                    [],
                    []),
                new ComponentFootprint(
                    chipFootprintId,
                    "0603 chip resistor",
                    [new ComponentFootprintPad(new ComponentPadId("dragon:resistor:0603:pad:1"), "1", new CadPoint(0, 0), new CadVector(60_000, 80_000), ComponentPadTechnology.SurfaceMount, ComponentPadShape.Rectangle)],
                    [],
                    [])
            ],
            Variants:
            [
                new ComponentVariant(new ComponentVariantId("dragon:resistor:variant:axial"), "Default axial", axialFootprintId, [new ComponentAttribute("Tolerance", "5%")]),
                new ComponentVariant(new ComponentVariantId("dragon:resistor:variant:0603"), "Precision chip", chipFootprintId, [new ComponentAttribute("Tolerance", "1%"), new ComponentAttribute("Power", "0.1 W")])
            ],
            PinPadMappings: [],
            Datasheets: [],
            Sourcing: [],
            PackageModels3D: [],
            Provenance: []);
        ComponentManagerViewModel viewModel = ComponentManagerViewModel.FromCatalog(new ComponentCatalog(BuiltInDefinitions: [resistor], UserDefinitions: [], ProjectDefinitions: []));

        viewModel.PackageFilter = "SurfaceMount";
        ComponentManagerRow filteredRow = Assert.Single(viewModel.Components);
        Assert.Equal("Precision chip (0603 chip resistor)", filteredRow.ActivePackageLabel);

        viewModel.PackageFilter = "10 kOhm";
        filteredRow = Assert.Single(viewModel.Components);
        Assert.Equal("Precision chip (0603 chip resistor)", filteredRow.ActivePackageLabel);

        viewModel.PackageFilter = "1%";
        filteredRow = Assert.Single(viewModel.Components);
        Assert.Equal("Precision chip (0603 chip resistor)", filteredRow.ActivePackageLabel);
    }

    [Fact]
    public void ReplaceFromCatalogExplainsWhenActivePackageIsMissing()
    {
        ComponentFootprintId soic8FootprintId = new("dragon:opamp:footprint:soic-8");
        ComponentFootprintId tssop8FootprintId = new("dragon:opamp:footprint:tssop-8");
        ComponentVariantId soic8VariantId = new("dragon:opamp:variant:soic-8");
        ComponentVariantId tssop8VariantId = new("dragon:opamp:variant:tssop-8");
        ComponentDefinition original = MultiPackageComponent(soic8FootprintId, tssop8FootprintId, soic8VariantId, tssop8VariantId);
        ComponentDefinition refreshed = SinglePackageComponent(soic8FootprintId, soic8VariantId);
        ComponentManagerViewModel viewModel = ComponentManagerViewModel.FromCatalog(new ComponentCatalog(BuiltInDefinitions: [original], UserDefinitions: [], ProjectDefinitions: []));
        ComponentManagerRow row = Assert.Single(viewModel.Components);
        viewModel.SelectPackageOption(row, row.PackageOptions[1]);

        viewModel.ReplaceFromCatalog(new ComponentCatalog(BuiltInDefinitions: [refreshed], UserDefinitions: [], ProjectDefinitions: []));

        ComponentManagerRow updatedRow = Assert.Single(viewModel.Components);
        Assert.Equal("SOIC package (SOIC-8)", updatedRow.ActivePackageLabel);
        Assert.Equal("Previously selected package 'TSSOP package (TSSOP-8)' is no longer available; using 'SOIC package (SOIC-8)'.", viewModel.SelectedPackageAvailabilityMessage);
    }

    [Fact]
    public void SelectedPackageOptionDrivesFootprintPreviewGeometry()
    {
        ComponentFootprintId soic8FootprintId = new("dragon:opamp:footprint:soic-8");
        ComponentFootprintId tssop8FootprintId = new("dragon:opamp:footprint:tssop-8");
        ComponentDefinition definition = new(
            new ComponentId("dragon:opamp"),
            "Dual Op Amp",
            ComponentKind.IntegratedCircuit,
            "Dragon",
            "DOP-1",
            Description: "",
            Attributes: [],
            Pins: [],
            Gates: [],
            Symbols: [],
            Footprints:
            [
                new ComponentFootprint(
                    soic8FootprintId,
                    "SOIC-8",
                    [new ComponentFootprintPad(new ComponentPadId("dragon:opamp:soic:pad:1"), "1", new CadPoint(100_000, 0), new CadVector(60_000, 80_000), ComponentPadTechnology.SurfaceMount, ComponentPadShape.Rectangle)],
                    [new ComponentLine(new CadPoint(0, 0), new CadPoint(100_000, 0))],
                    []),
                new ComponentFootprint(
                    tssop8FootprintId,
                    "TSSOP-8",
                    [new ComponentFootprintPad(new ComponentPadId("dragon:opamp:tssop:pad:1"), "1", new CadPoint(800_000, 0), new CadVector(40_000, 60_000), ComponentPadTechnology.SurfaceMount, ComponentPadShape.Round)],
                    [new ComponentLine(new CadPoint(700_000, 0), new CadPoint(900_000, 0))],
                    [])
            ],
            Variants:
            [
                new ComponentVariant(new ComponentVariantId("dragon:opamp:variant:soic-8"), "SOIC package", soic8FootprintId, []),
                new ComponentVariant(new ComponentVariantId("dragon:opamp:variant:tssop-8"), "TSSOP package", tssop8FootprintId, [])
            ],
            PinPadMappings: [],
            Datasheets: [],
            Sourcing: [],
            PackageModels3D: [],
            Provenance: []);
        ComponentCatalog catalog = new(BuiltInDefinitions: [definition], UserDefinitions: [], ProjectDefinitions: []);
        ComponentManagerViewModel viewModel = ComponentManagerViewModel.FromCatalog(catalog);
        ComponentManagerRow row = Assert.Single(viewModel.Components);

        viewModel.SelectPackageOption(row, row.PackageOptions[1]);

        ComponentManagerRow updatedRow = Assert.Single(viewModel.Components);
        ComponentFootprintPadPreview pad = Assert.Single(updatedRow.FootprintPreview.Pads);
        Assert.Equal(new CadPoint(800_000, 0), pad.Position);
        Assert.Equal(new CadVector(40_000, 60_000), pad.Size);
        Assert.Equal("Round", pad.Shape);
        ComponentPreviewLine line = Assert.Single(updatedRow.FootprintPreview.Lines);
        Assert.Equal(new CadPoint(700_000, 0), line.Start);
        Assert.Equal(new CadPoint(900_000, 0), line.End);
    }

    [Fact]
    public void RowsExposeFootprintsAsPackageOptionsWhenNoVariantsExist()
    {
        ComponentDefinition definition = new(
            new ComponentId("dragon:test-header"),
            "Test Header",
            ComponentKind.Connector,
            "Dragon",
            "HDR",
            Description: "",
            Attributes: [],
            Pins: [],
            Gates: [],
            Symbols: [],
            Footprints:
            [
                Footprint(new ComponentFootprintId("dragon:test-header:footprint:1x03"), "1x03 Header", padCount: 3),
                Footprint(new ComponentFootprintId("dragon:test-header:footprint:1x04"), "1x04 Header", padCount: 4)
            ],
            Variants: [],
            PinPadMappings: [],
            Datasheets: [],
            Sourcing: [],
            PackageModels3D: [],
            Provenance: []);
        ComponentCatalog catalog = new(BuiltInDefinitions: [definition], UserDefinitions: [], ProjectDefinitions: []);

        ComponentManagerRow row = Assert.Single(ComponentManagerViewModel.FromCatalog(catalog).Components);

        Assert.Equal(2, row.PackageOptionCount);
        Assert.Equal("1x03 Header", row.ActivePackageLabel);
        Assert.Equal(
            ["1x03 Header - 3 pads", "1x04 Header - 4 pads"],
            row.PackageOptions.Select(option => option.DisplayText));
        Assert.All(row.PackageOptions, option => Assert.Equal("", option.VariantId));
    }

    [Fact]
    public void TrustedSelectedComponentCanArmPlacementFromFilteredChooser()
    {
        ComponentCatalog catalog = new(
            BuiltInDefinitions:
            [
                Component(
                    "dragon:trusted-opamp",
                    "Trusted Op Amp",
                    ComponentKind.IntegratedCircuit,
                    "Dragon",
                    "DOP-1",
                    symbolCount: 1,
                    footprintCount: 1,
                    provenance: [new ComponentProvenanceRecord(ComponentProvenanceKind.Native, "DragonCAD", "Curated native part")]),
                Component("dragon:resistor", "Resistor", ComponentKind.Passive, "Dragon", "RES")
            ],
            UserDefinitions: [],
            ProjectDefinitions: []);
        ComponentManagerViewModel viewModel = ComponentManagerViewModel.FromCatalog(catalog);
        viewModel.SearchText = "trusted";

        Assert.True(viewModel.TryArmSelectedComponentPlacement());

        ComponentPlacementIntent armed = Assert.IsType<ComponentPlacementIntent>(viewModel.ArmedPlacement);
        Assert.Equal("dragon:trusted-opamp", armed.ComponentId);
        Assert.Equal("Trusted Op Amp", armed.DisplayName);
        Assert.Equal("Placement armed: Trusted Op Amp", viewModel.PlacementDiagnostic);
    }

    [Fact]
    public void TrustedSelectedComponentPlacementIncludesGateUnitOptions()
    {
        ComponentCatalog catalog = new(
            BuiltInDefinitions:
            [
                MultiGateComponent()
            ],
            UserDefinitions: [],
            ProjectDefinitions: []);
        ComponentManagerViewModel viewModel = ComponentManagerViewModel.FromCatalog(catalog);

        Assert.True(viewModel.TryArmSelectedComponentPlacement());

        ComponentPlacementIntent armed = Assert.IsType<ComponentPlacementIntent>(viewModel.ArmedPlacement);
        Assert.Equal(["A", "B", "PWR"], armed.Units.Select(unit => unit.Name));
        Assert.Equal(["A", "B", "PWR"], armed.Units.Select(unit => unit.UnitId));
        Assert.Equal([true, true, false], armed.Units.Select(unit => unit.IsRequired));
        Assert.Equal([false, false, true], armed.Units.Select(unit => unit.CanPlaceMultiple));
    }

    [Fact]
    public void ChoosingDifferentTrustedPartReplacesActivePlacementCandidate()
    {
        ComponentCatalog catalog = new(
            BuiltInDefinitions:
            [
                Component(
                    "dragon:first",
                    "First",
                    ComponentKind.IntegratedCircuit,
                    "Dragon",
                    "FIRST",
                    symbolCount: 1,
                    footprintCount: 1,
                    provenance: [new ComponentProvenanceRecord(ComponentProvenanceKind.Native, "DragonCAD", "Curated native part")]),
                Component(
                    "dragon:second",
                    "Second",
                    ComponentKind.IntegratedCircuit,
                    "Dragon",
                    "SECOND",
                    symbolCount: 1,
                    footprintCount: 1,
                    provenance: [new ComponentProvenanceRecord(ComponentProvenanceKind.Native, "DragonCAD", "Curated native part")])
            ],
            UserDefinitions: [],
            ProjectDefinitions: []);
        ComponentManagerViewModel viewModel = ComponentManagerViewModel.FromCatalog(catalog);
        ComponentManagerRow first = viewModel.Components.Single(row => row.DisplayName == "First");
        ComponentManagerRow second = viewModel.Components.Single(row => row.DisplayName == "Second");

        Assert.True(viewModel.TryArmComponentPlacement(first));
        Assert.True(viewModel.TryArmComponentPlacement(second));

        Assert.Equal("dragon:second", viewModel.ArmedPlacement?.ComponentId);
        Assert.Equal("Placement armed: Second", viewModel.PlacementDiagnostic);
    }

    [Fact]
    public void CatalogOnlyDraftAndUntrustedRowsCannotArmPlacementAndShowDiagnostics()
    {
        ComponentCatalog catalog = new(
            BuiltInDefinitions:
            [
                Component(
                    "dragon:catalog-only",
                    "Catalog Only",
                    ComponentKind.IntegratedCircuit,
                    "Dragon",
                    "CAT",
                    symbolCount: 0,
                    footprintCount: 0)
            ],
            UserDefinitions:
            [
                Component(
                    "dragon:untrusted",
                    "Untrusted",
                    ComponentKind.IntegratedCircuit,
                    "Dragon",
                    "UNTRUSTED",
                    symbolCount: 1,
                    footprintCount: 1,
                    provenance: [new ComponentProvenanceRecord(ComponentProvenanceKind.EagleImport, "Eagle", "Imported library part")])
            ],
            ProjectDefinitions:
            [
                Component(
                    "dragon:draft",
                    "Draft",
                    ComponentKind.IntegratedCircuit,
                    "Dragon",
                    "DRAFT",
                    symbolCount: 1,
                    footprintCount: 1,
                    provenance: [new ComponentProvenanceRecord(ComponentProvenanceKind.DatasheetGenerated, "AI", "Pending review")])
            ]);
        ComponentManagerViewModel viewModel = ComponentManagerViewModel.FromCatalog(catalog);
        ComponentManagerRow catalogOnly = viewModel.Components.Single(row => row.DisplayName == "Catalog Only");
        ComponentManagerRow draft = viewModel.Components.Single(row => row.DisplayName == "Draft");
        ComponentManagerRow untrusted = viewModel.Components.Single(row => row.DisplayName == "Untrusted");

        Assert.False(viewModel.TryArmComponentPlacement(catalogOnly));
        Assert.Null(viewModel.ArmedPlacement);
        Assert.Equal("Catalog Only cannot be placed because it is catalog-only and is missing verified placement geometry.", viewModel.PlacementDiagnostic);

        Assert.False(viewModel.TryArmComponentPlacement(draft));
        Assert.Null(viewModel.ArmedPlacement);
        Assert.Equal("Draft cannot be placed until the draft component is reviewed.", viewModel.PlacementDiagnostic);

        Assert.False(viewModel.TryArmComponentPlacement(untrusted));
        Assert.Null(viewModel.ArmedPlacement);
        Assert.Equal("Untrusted cannot be placed because its source is not trusted for schematic placement.", viewModel.PlacementDiagnostic);
    }

    private static ComponentDefinition Component(
        string id,
        string displayName,
        ComponentKind kind,
        string manufacturer,
        string manufacturerPartNumber,
        int symbolCount = 0,
        int footprintCount = 0,
        bool hasDatasheet = false,
        bool hasSourcing = false,
        bool hasModel3d = false,
        bool includePreviewGeometry = false,
        IReadOnlyList<ComponentAttribute>? attributes = null,
        IReadOnlyList<ComponentProvenanceRecord>? provenance = null)
    {
        ComponentFootprintId footprintId = new($"{id}:footprint");
        ComponentVariantId variantId = new($"{id}:default");
        ComponentPinId previewPinId = new($"{id}:pin:p1");

        return new ComponentDefinition(
            new ComponentId(id),
            displayName,
            kind,
            manufacturer,
            manufacturerPartNumber,
            Description: "",
            Attributes: attributes ?? [],
            Pins: includePreviewGeometry ? [new ComponentPin(previewPinId, "P1", "1", ComponentPinElectricalType.Input)] : [],
            Gates: [],
            Symbols: Enumerable.Range(0, symbolCount)
                .Select(index => new ComponentSymbol(
                    new ComponentSymbolId($"{id}:symbol:{index}"),
                    $"Symbol {index}",
                    includePreviewGeometry ? [new ComponentSymbolPin(previewPinId, new CadPoint(-150000, 0), ComponentPinOrientation.Right)] : [],
                    includePreviewGeometry
                        ?
                        [
                            new ComponentLine(new CadPoint(-100000, -100000), new CadPoint(100000, -100000)),
                            new ComponentLine(new CadPoint(100000, -100000), new CadPoint(100000, 100000)),
                            new ComponentLine(new CadPoint(100000, 100000), new CadPoint(-100000, 100000)),
                            new ComponentLine(new CadPoint(-100000, 100000), new CadPoint(-100000, -100000))
                        ]
                        : [],
                    []))
                .ToArray(),
            Footprints: Enumerable.Range(0, footprintCount)
                .Select(index => new ComponentFootprint(
                    new ComponentFootprintId($"{id}:footprint:{index}"),
                    $"Footprint {index}",
                    includePreviewGeometry
                        ? [new ComponentFootprintPad(new ComponentPadId($"{id}:pad:1"), "1", new CadPoint(0, 0), new CadVector(60000, 80000), ComponentPadTechnology.SurfaceMount, ComponentPadShape.Rectangle)]
                        : [],
                    includePreviewGeometry ? [new ComponentLine(new CadPoint(100000, 0), new CadPoint(100000, 100000))] : [],
                    []))
                .ToArray(),
            Variants: hasModel3d ? [new ComponentVariant(variantId, "Default", footprintId, [])] : [],
            PinPadMappings: [],
            Datasheets: hasDatasheet
                ? [new ComponentDatasheetReference(displayName, ComponentDatasheetLocationKind.Url, "https://example.test/datasheet.pdf", manufacturer, manufacturerPartNumber)]
                : [],
            Sourcing: hasSourcing
                ? [new ComponentSourcingReference("Digi-Key", "123", manufacturer, manufacturerPartNumber)]
                : [],
            PackageModels3D: hasModel3d
                ? [new ComponentPackageModel3D("model", ComponentPackageModel3DFormat.Step, "models/part.step", variantId)]
                : [],
            Provenance: provenance ?? []);
    }

    private static ComponentFootprint Footprint(ComponentFootprintId footprintId, string name, int padCount) =>
        new(
            footprintId,
            name,
            Enumerable.Range(1, padCount)
                .Select(index => new ComponentFootprintPad(
                    new ComponentPadId($"{footprintId.Value}:pad:{index}"),
                    index.ToString(),
                    new CadPoint(index * 100_000, 0),
                    new CadVector(60_000, 80_000),
                    ComponentPadTechnology.SurfaceMount,
                    ComponentPadShape.Rectangle))
                .ToArray(),
            [],
            []);

    private static ComponentDefinition MultiPackageComponent(
        ComponentFootprintId firstFootprintId,
        ComponentFootprintId secondFootprintId,
        ComponentVariantId firstVariantId,
        ComponentVariantId secondVariantId) =>
        new(
            new ComponentId("dragon:opamp"),
            "Dual Op Amp",
            ComponentKind.IntegratedCircuit,
            "Dragon",
            "DOP-1",
            Description: "",
            Attributes: [],
            Pins: [],
            Gates: [],
            Symbols: [],
            Footprints:
            [
                Footprint(firstFootprintId, "SOIC-8", padCount: 8),
                Footprint(secondFootprintId, "TSSOP-8", padCount: 8)
            ],
            Variants:
            [
                new ComponentVariant(firstVariantId, "SOIC package", firstFootprintId, [new ComponentAttribute("Package", "SOIC-8")]),
                new ComponentVariant(secondVariantId, "TSSOP package", secondFootprintId, [new ComponentAttribute("Package", "TSSOP-8")])
            ],
            PinPadMappings: [],
            Datasheets: [],
            Sourcing: [],
            PackageModels3D: [],
            Provenance: []);

    private static ComponentDefinition SinglePackageComponent(ComponentFootprintId footprintId, ComponentVariantId variantId) =>
        new(
            new ComponentId("dragon:opamp"),
            "Dual Op Amp",
            ComponentKind.IntegratedCircuit,
            "Dragon",
            "DOP-1",
            Description: "",
            Attributes: [],
            Pins: [],
            Gates: [],
            Symbols: [],
            Footprints: [Footprint(footprintId, "SOIC-8", padCount: 8)],
            Variants: [new ComponentVariant(variantId, "SOIC package", footprintId, [new ComponentAttribute("Package", "SOIC-8")])],
            PinPadMappings: [],
            Datasheets: [],
            Sourcing: [],
            PackageModels3D: [],
            Provenance: []);

    private static ComponentDefinition MultiGateComponent()
    {
        ComponentPinId outA = new("dragon:dual-opamp:pin:out-a");
        ComponentPinId outB = new("dragon:dual-opamp:pin:out-b");
        ComponentPinId vcc = new("dragon:dual-opamp:pin:vcc");
        ComponentSymbolId symbolA = new("dragon:dual-opamp:symbol:a");
        ComponentSymbolId symbolB = new("dragon:dual-opamp:symbol:b");
        ComponentSymbolId symbolPower = new("dragon:dual-opamp:symbol:power");
        ComponentFootprintId footprintId = new("dragon:dual-opamp:footprint:soic8");
        ComponentVariantId variantId = new("dragon:dual-opamp:variant:soic8");

        return new ComponentDefinition(
            new ComponentId("dragon:dual-opamp"),
            "Dual Op Amp",
            ComponentKind.IntegratedCircuit,
            "Dragon",
            "DOP-2",
            Description: "",
            Attributes: [],
            Pins:
            [
                new ComponentPin(outA, "OUTA", "1", ComponentPinElectricalType.Output),
                new ComponentPin(outB, "OUTB", "7", ComponentPinElectricalType.Output),
                new ComponentPin(vcc, "VCC", "8", ComponentPinElectricalType.Power)
            ],
            Gates:
            [
                new ComponentGate(new ComponentGateId("dragon:dual-opamp:gate:a"), "A", symbolA, [outA]),
                new ComponentGate(new ComponentGateId("dragon:dual-opamp:gate:b"), "B", symbolB, [outB]),
                new ComponentGate(new ComponentGateId("dragon:dual-opamp:gate:pwr"), "PWR", symbolPower, [vcc])
            ],
            Symbols:
            [
                new ComponentSymbol(
                    symbolA,
                    "A",
                    [new ComponentSymbolPin(outA, new CadPoint(1_000_000, 0), ComponentPinOrientation.Right)],
                    [new ComponentLine(new CadPoint(-1_000_000, -1_000_000), new CadPoint(1_000_000, 1_000_000))],
                    []),
                new ComponentSymbol(
                    symbolB,
                    "B",
                    [new ComponentSymbolPin(outB, new CadPoint(1_000_000, 0), ComponentPinOrientation.Right)],
                    [new ComponentLine(new CadPoint(-1_000_000, 1_000_000), new CadPoint(1_000_000, -1_000_000))],
                    []),
                new ComponentSymbol(
                    symbolPower,
                    "PWR",
                    [new ComponentSymbolPin(vcc, new CadPoint(0, -500_000), ComponentPinOrientation.Down)],
                    [new ComponentLine(new CadPoint(-500_000, 0), new CadPoint(500_000, 0))],
                    [])
            ],
            Footprints: [Footprint(footprintId, "SOIC-8", padCount: 8)],
            Variants: [new ComponentVariant(variantId, "SOIC package", footprintId, [new ComponentAttribute("Package", "SOIC-8")])],
            PinPadMappings: [],
            Datasheets: [],
            Sourcing: [],
            PackageModels3D: [],
            Provenance: [new ComponentProvenanceRecord(ComponentProvenanceKind.Native, "DragonCAD", "Curated native part")]);
    }
}
