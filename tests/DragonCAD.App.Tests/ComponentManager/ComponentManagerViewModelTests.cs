using DragonCAD.App.ComponentManager;
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
        bool includePreviewGeometry = false)
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
            Attributes: [],
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
            Provenance: []);
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
}
