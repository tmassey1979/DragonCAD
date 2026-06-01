using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DragonCAD.App;
using DragonCAD.App.BoardEditor;
using DragonCAD.App.SchematicEditor;
using DragonCAD.Core.Geometry;

namespace DragonCAD.App.Tests.Shell;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public void DesignPreviewContainsHawkCadComponentsForTheComponentManager()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateDesignPreview(maxBuiltInDevices: 2);

        Assert.True(viewModel.BuiltInLibrary.TotalDevices > viewModel.ComponentManager.Components.Count);
        Assert.Equal(2, viewModel.BuiltInLibrary.LoadedDevices);
        Assert.True(viewModel.ComponentManager.Components.Count >= 2);
        Assert.All(viewModel.ComponentManager.Components, row => Assert.False(string.IsNullOrWhiteSpace(row.CapabilitySummary)));
        Assert.All(viewModel.ComponentManager.Components, row => Assert.StartsWith("hawkcad:", row.ComponentId, StringComparison.Ordinal));
    }

    [Fact]
    public void DefaultDesignPreviewPreloadsFullBundledHawkCadLibrary()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateDesignPreview();

        Assert.True(viewModel.BuiltInLibrary.LoadedDevices > 250);
        Assert.True(viewModel.BuiltInLibrary.TotalDevices >= viewModel.BuiltInLibrary.LoadedDevices);
        Assert.Equal(viewModel.BuiltInLibrary.LoadedDevices, viewModel.ComponentManager.Components.Count);
        Assert.StartsWith("Loaded ", viewModel.BuiltInLibrary.StatusText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LibrarySearchCommandSearchesTheFullHawkCadIndexAndReplacesVisibleRows()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);

        Assert.DoesNotContain(viewModel.ComponentManager.Components, row => row.DisplayName.Contains("RESISTOR", StringComparison.Ordinal));

        viewModel.LibrarySearchText = "resistor";

        Assert.Equal("resistor", viewModel.LibrarySearchText);
        Assert.DoesNotContain(viewModel.ComponentManager.Components, row => row.DisplayName.Contains("RESISTOR", StringComparison.Ordinal));

        await viewModel.ExecuteLibrarySearchAsync();

        var row = Assert.Single(viewModel.ComponentManager.Components);
        Assert.Equal("sparkfun-eagle-libraries/SparkFun-Resistors/RESISTOR-0603", row.DisplayName);
        Assert.Contains("for \"resistor\"", viewModel.BuiltInLibrary.StatusText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LibrarySearchCommandClearsRowsWhenThereAreNoMatches()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);

        viewModel.LibrarySearchText = "not-a-real-part";
        await viewModel.ExecuteLibrarySearchAsync();

        Assert.Empty(viewModel.ComponentManager.Components);
        Assert.Equal("No HawkCAD library devices match \"not-a-real-part\".", viewModel.BuiltInLibrary.StatusText);
    }

    [Fact]
    public async Task LibrarySearchCommandReportsProgressWhileSearchRuns()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);
        viewModel.LibrarySearchText = "resistor";

        Task search = viewModel.ExecuteLibrarySearchAsync();

        Assert.True(viewModel.IsLibrarySearchInProgress);

        await search;

        Assert.False(viewModel.IsLibrarySearchInProgress);
    }

    [Fact]
    public void DesignPreviewUsesCuratedHawkCadLibraryGeometryInsteadOfPlaceholderBoxes()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback, maxBuiltInDevices: 20);

        var timer = Assert.Single(viewModel.ComponentManager.Components, row => row.DisplayName == "adafruit-eagle-library/adafruit/*555");

        Assert.Equal("BuiltIn", timer.Source);
        Assert.True(timer.SymbolPreview.Pins.Count >= 8);
        Assert.True(timer.SymbolPreview.Lines.Count >= 4);
        Assert.Contains(timer.SymbolPreview.Pins, pin => pin.Name == "GND");
        Assert.Contains(timer.SymbolPreview.Pins, pin => pin.Name == "V+");
        Assert.True(timer.FootprintPreview.Pads.Count >= 8);
    }

    [Fact]
    public void PlaceSelectedComponentCommandArmsTheSelectedPartForEditorPlacement()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 20);
        var resistor = Assert.Single(viewModel.ComponentManager.Components, row => row.DisplayName.Contains("RESISTOR", StringComparison.Ordinal));
        viewModel.ComponentManager.SelectedComponent = resistor;

        viewModel.PlaceSelectedComponentCommand.Execute(null);

        Assert.NotNull(viewModel.ActivePlacement);
        Assert.Equal(resistor.ComponentId, viewModel.ActivePlacement.ComponentId);
        Assert.Equal(resistor.DisplayName, viewModel.ActivePlacement.DisplayName);
        Assert.Equal(resistor.SymbolCount, viewModel.ActivePlacement.SymbolCount);
        Assert.Equal(resistor.FootprintCount, viewModel.ActivePlacement.FootprintCount);
        Assert.Equal(resistor.FootprintPreview.Pads.Count, viewModel.ActivePlacement.FootprintPreview?.Pads.Count);
        Assert.Contains(resistor.DisplayName, viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void PlaceSelectedComponentCommandReportsMissingSelectionWithoutArmingPlacement()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 20);
        viewModel.ComponentManager.SelectedComponent = null;

        viewModel.PlaceSelectedComponentCommand.Execute(null);

        Assert.Null(viewModel.ActivePlacement);
        Assert.Equal("Select a component before placing it.", viewModel.PlacementStatus);
    }

    [Fact]
    public void PlaceArmedComponentOnSchematicCommandCreatesAPlacedSchematicInstance()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 20);
        var resistor = Assert.Single(viewModel.ComponentManager.Components, row => row.DisplayName.Contains("RESISTOR", StringComparison.Ordinal));
        viewModel.ComponentManager.SelectedComponent = resistor;
        viewModel.PlaceSelectedComponentCommand.Execute(null);

        viewModel.PlaceArmedComponentOnSchematicCommand.Execute(null);

        var instance = Assert.Single(viewModel.SchematicEditor.Components);
        Assert.Equal(resistor.ComponentId, instance.ComponentId);
        Assert.Equal(resistor.DisplayName, instance.DisplayName);
        Assert.Equal("U1", instance.ReferenceDesignator);
        Assert.Equal(resistor.SymbolPreview.Lines.Count, instance.SymbolPreview.Lines.Count);
        Assert.Equal(resistor.SymbolPreview.Pins.Count, instance.SymbolPreview.Pins.Count);
        Assert.Equal(resistor.FootprintPreview.Pads.Count, instance.FootprintPreview.Pads.Count);
        Assert.Contains("Placed U1", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void PlaceArmedComponentOnSchematicAtUsesTheCanvasCadPoint()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 20);
        viewModel.ComponentManager.SelectedComponent = Assert.Single(
            viewModel.ComponentManager.Components,
            row => row.DisplayName.Contains("RESISTOR", StringComparison.Ordinal));
        viewModel.PlaceSelectedComponentCommand.Execute(null);

        viewModel.PlaceArmedComponentOnSchematicAt(new CadPoint(2_300_000, -1_600_000));

        var instance = Assert.Single(viewModel.SchematicEditor.Components);
        Assert.Equal(new CadPoint(2_000_000, -2_000_000), instance.Position);
    }

    [Fact]
    public void PlacingComponentOnSchematicSynchronizesBoardComponentShell()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 20);
        var resistor = Assert.Single(viewModel.ComponentManager.Components, row => row.DisplayName.Contains("RESISTOR", StringComparison.Ordinal));
        viewModel.ComponentManager.SelectedComponent = resistor;
        viewModel.PlaceSelectedComponentCommand.Execute(null);

        viewModel.PlaceArmedComponentOnSchematicAt(new CadPoint(0, 0));

        var schematicComponent = Assert.Single(viewModel.SchematicEditor.Components);
        var boardComponent = Assert.Single(viewModel.BoardEditor.Components);
        Assert.Equal(schematicComponent.InstanceId, boardComponent.SyncId);
        Assert.Equal(schematicComponent.ReferenceDesignator, boardComponent.ReferenceDesignator);
        Assert.Equal(resistor.ComponentId, boardComponent.ComponentId);
        Assert.Equal(resistor.FootprintPreview.Pads.Count, boardComponent.FootprintPreview.Pads.Count);
        Assert.Equal(resistor.FootprintPreview.Lines.Count, boardComponent.FootprintPreview.Lines.Count);
        Assert.Contains("Board sync: Synchronized 1 board component from schematic.", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void PlacingSourcedComponentOnSchematicQueuesInUseVendorSync()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 20);
        var sourcedComponent = Assert.Single(
            viewModel.ComponentManager.Components,
            row => !string.IsNullOrWhiteSpace(row.ManufacturerPartNumber));
        viewModel.ComponentManager.SelectedComponent = sourcedComponent;
        viewModel.PlaceSelectedComponentCommand.Execute(null);

        viewModel.PlaceArmedComponentOnSchematicAt(new CadPoint(0, 0));

        Assert.Equal(2, viewModel.InUseVendorCatalogSyncQueue.Count);
        Assert.Equal(["Digi-Key", "Mouser"], viewModel.InUseVendorCatalogSyncQueue.Select(request => request.ProviderName).ToArray());
        Assert.All(viewModel.InUseVendorCatalogSyncQueue, request =>
        {
            Assert.Equal(sourcedComponent.ComponentId, request.ComponentId);
            Assert.Equal(sourcedComponent.ManufacturerPartNumber, request.Query);
            Assert.Equal("In-use vendor sync queue: 2 requests for 1 component.", viewModel.InUseVendorCatalogSyncSummary);
        });
    }

    [Fact]
    public void HandleSchematicCanvasClickSelectsExistingComponentWhenNoPlacementIsArmed()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 20);
        viewModel.ComponentManager.SelectedComponent = Assert.Single(
            viewModel.ComponentManager.Components,
            row => row.DisplayName.Contains("RESISTOR", StringComparison.Ordinal));
        viewModel.PlaceSelectedComponentCommand.Execute(null);
        viewModel.HandleSchematicCanvasClick(new CadPoint(0, 0));
        viewModel.CancelPlacementCommand.Execute(null);

        viewModel.HandleSchematicCanvasClick(new CadPoint(0, 0));

        Assert.NotNull(viewModel.SchematicEditor.SelectedComponent);
        Assert.Equal("U1", viewModel.SchematicEditor.SelectedComponent.ReferenceDesignator);
        Assert.Contains("Selected U1", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void MoveSelectedSchematicComponentByGridMovesSelectionOneGridStep()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 20);
        viewModel.ComponentManager.SelectedComponent = Assert.Single(
            viewModel.ComponentManager.Components,
            row => row.DisplayName.Contains("RESISTOR", StringComparison.Ordinal));
        viewModel.PlaceSelectedComponentCommand.Execute(null);
        viewModel.HandleSchematicCanvasClick(new CadPoint(0, 0));

        viewModel.MoveSelectedSchematicComponentByGrid(new CadVector(1, -1));

        Assert.Equal(new CadPoint(1_000_000, -1_000_000), viewModel.SchematicEditor.SelectedComponent?.Position);
        Assert.Contains("Moved U1", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void DraggingSelectedSchematicComponentMovesItToPointerWithOriginalGrabOffset()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 20);
        viewModel.ComponentManager.SelectedComponent = Assert.Single(
            viewModel.ComponentManager.Components,
            row => row.DisplayName.Contains("RESISTOR", StringComparison.Ordinal));
        viewModel.PlaceSelectedComponentCommand.Execute(null);
        viewModel.HandleSchematicCanvasClick(new CadPoint(0, 0));
        viewModel.ActivateSelectToolCommand.Execute(null);
        viewModel.HandleSchematicPointerPressed(new CadPoint(0, 0));

        Assert.True(viewModel.IsDraggingSchematicComponent);

        viewModel.HandleSchematicPointerMoved(new CadPoint(2_600_000, -1_600_000));
        viewModel.HandleSchematicPointerReleased(new CadPoint(2_600_000, -1_600_000));

        Assert.False(viewModel.IsDraggingSchematicComponent);
        Assert.Equal(new CadPoint(3_000_000, -2_000_000), viewModel.SchematicEditor.SelectedComponent?.Position);
        Assert.Contains("Moved U1", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void WireToolCanvasClicksConnectPins()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 20);
        viewModel.ComponentManager.SelectedComponent = Assert.Single(
            viewModel.ComponentManager.Components,
            row => row.DisplayName.Contains("RESISTOR", StringComparison.Ordinal));
        viewModel.PlaceSelectedComponentCommand.Execute(null);
        viewModel.HandleSchematicCanvasClick(new CadPoint(0, 0));
        viewModel.HandleSchematicCanvasClick(new CadPoint(5_000_000, 0));
        viewModel.ActivateWireToolCommand.Execute(null);

        viewModel.HandleSchematicCanvasClick(new CadPoint(-2_540_000, 0));
        viewModel.HandleSchematicCanvasClick(new CadPoint(2_460_000, 0));

        Assert.Single(viewModel.SchematicEditor.Wires);
        Assert.Equal("Wire", viewModel.ActiveSchematicTool);
        Assert.Contains("Connected", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void ActiveSchematicToolExposesToolPaletteState()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);

        Assert.True(viewModel.IsSelectToolActive);
        Assert.False(viewModel.IsWireToolActive);

        viewModel.ActivateWireToolCommand.Execute(null);

        Assert.False(viewModel.IsSelectToolActive);
        Assert.True(viewModel.IsWireToolActive);

        viewModel.ActivateSelectToolCommand.Execute(null);

        Assert.True(viewModel.IsSelectToolActive);
        Assert.False(viewModel.IsWireToolActive);
    }

    [Fact]
    public void WireToolCanvasClicksAddRoutedSegmentsBetweenPins()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 20);
        viewModel.ComponentManager.SelectedComponent = Assert.Single(
            viewModel.ComponentManager.Components,
            row => row.DisplayName.Contains("RESISTOR", StringComparison.Ordinal));
        viewModel.PlaceSelectedComponentCommand.Execute(null);
        viewModel.HandleSchematicCanvasClick(new CadPoint(0, 0));
        viewModel.HandleSchematicCanvasClick(new CadPoint(5_000_000, 0));
        viewModel.ActivateWireToolCommand.Execute(null);

        viewModel.HandleSchematicCanvasClick(new CadPoint(-2_540_000, 0));
        viewModel.HandleSchematicCanvasClick(new CadPoint(0, 2_100_000));
        viewModel.HandleSchematicCanvasClick(new CadPoint(2_460_000, 0));

        SchematicWire wire = Assert.Single(viewModel.SchematicEditor.Wires);
        Assert.Equal(
            [
                new CadPoint(-2_540_000, 0),
                new CadPoint(0, 0),
                new CadPoint(0, 2_000_000),
                new CadPoint(2_460_000, 2_000_000),
                new CadPoint(2_460_000, 0)
            ],
            wire.RoutePoints);
    }

    [Fact]
    public void SelectToolPointerDragMovesSelectedWireSegment()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 20);
        viewModel.ComponentManager.SelectedComponent = Assert.Single(
            viewModel.ComponentManager.Components,
            row => row.DisplayName.Contains("RESISTOR", StringComparison.Ordinal));
        viewModel.PlaceSelectedComponentCommand.Execute(null);
        viewModel.HandleSchematicCanvasClick(new CadPoint(0, 0));
        viewModel.HandleSchematicCanvasClick(new CadPoint(5_000_000, 0));
        viewModel.ActivateWireToolCommand.Execute(null);
        viewModel.HandleSchematicCanvasClick(new CadPoint(-2_540_000, 0));
        viewModel.HandleSchematicCanvasClick(new CadPoint(0, 2_100_000));
        viewModel.HandleSchematicCanvasClick(new CadPoint(2_460_000, 0));
        viewModel.ActivateSelectToolCommand.Execute(null);

        viewModel.HandleSchematicPointerPressed(new CadPoint(1_000_000, 2_000_000));
        Assert.True(viewModel.IsDraggingSchematicWireSegment);
        Assert.False(viewModel.IsDraggingSchematicComponent);

        viewModel.HandleSchematicPointerMoved(new CadPoint(1_000_000, 3_400_000));
        viewModel.HandleSchematicPointerReleased(new CadPoint(1_000_000, 3_400_000));

        Assert.False(viewModel.IsDraggingSchematicWireSegment);
        SchematicWire wire = Assert.Single(viewModel.SchematicEditor.Wires);
        Assert.Equal(
            [
                new CadPoint(-2_540_000, 0),
                new CadPoint(0, 0),
                new CadPoint(0, 3_000_000),
                new CadPoint(2_460_000, 3_000_000),
                new CadPoint(2_460_000, 0)
            ],
            wire.RoutePoints);
        Assert.Contains("Moved wire segment", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void SchematicPropertyFieldsEditSelectedPartAndSynchronizeBoard()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 20);
        viewModel.ComponentManager.SelectedComponent = Assert.Single(
            viewModel.ComponentManager.Components,
            row => row.DisplayName.Contains("RESISTOR", StringComparison.Ordinal));
        viewModel.PlaceSelectedComponentCommand.Execute(null);
        viewModel.HandleSchematicCanvasClick(new CadPoint(0, 0));
        viewModel.ActivateSelectToolCommand.Execute(null);
        viewModel.HandleSchematicPointerPressed(new CadPoint(0, 0));

        viewModel.SelectedSchematicReferenceDesignator = "R12";
        viewModel.SelectedSchematicComponentName = "RESISTOR-0603";
        viewModel.SelectedSchematicComponentValue = "10k";

        SchematicComponentInstance schematic = Assert.Single(viewModel.SchematicEditor.Components);
        Assert.Equal("R12", schematic.ReferenceDesignator);
        Assert.Equal("RESISTOR-0603", schematic.DisplayName);
        Assert.Equal("10k", schematic.Value);

        BoardComponentInstance board = Assert.Single(viewModel.BoardEditor.Components);
        Assert.Equal("R12", board.ReferenceDesignator);
        Assert.Equal("RESISTOR-0603", board.DisplayName);
        Assert.Equal("10k", board.Value);
        Assert.Contains("Updated R12 properties", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void RotateSelectedPartCommandRotatesSchematicAndBoardInstances()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 20);
        viewModel.ComponentManager.SelectedComponent = Assert.Single(
            viewModel.ComponentManager.Components,
            row => row.DisplayName.Contains("RESISTOR", StringComparison.Ordinal));
        viewModel.PlaceSelectedComponentCommand.Execute(null);
        viewModel.HandleSchematicCanvasClick(new CadPoint(0, 0));

        viewModel.RotateSelectedPartCommand.Execute(null);

        SchematicComponentInstance schematic = Assert.Single(viewModel.SchematicEditor.Components);
        BoardComponentInstance board = Assert.Single(viewModel.BoardEditor.Components);
        Assert.Equal(90, schematic.RotationDegrees);
        Assert.Equal(90, board.RotationDegrees);
        Assert.Equal("90", viewModel.SelectedSchematicRotationDegrees);
        Assert.Contains("Rotated U1 to 90 degrees", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void MirrorSelectedPartCommandMirrorsSchematicAndBoardInstances()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 20);
        viewModel.ComponentManager.SelectedComponent = Assert.Single(
            viewModel.ComponentManager.Components,
            row => row.DisplayName.Contains("RESISTOR", StringComparison.Ordinal));
        viewModel.PlaceSelectedComponentCommand.Execute(null);
        viewModel.HandleSchematicCanvasClick(new CadPoint(0, 0));

        viewModel.MirrorSelectedPartCommand.Execute(null);

        SchematicComponentInstance schematic = Assert.Single(viewModel.SchematicEditor.Components);
        BoardComponentInstance board = Assert.Single(viewModel.BoardEditor.Components);
        Assert.True(schematic.IsMirrored);
        Assert.True(board.IsMirrored);
        Assert.Contains("Mirrored U1", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void DeleteSelectedPartCommandRemovesPartWiresAndBoardShell()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 20);
        viewModel.ComponentManager.SelectedComponent = Assert.Single(
            viewModel.ComponentManager.Components,
            row => row.DisplayName.Contains("RESISTOR", StringComparison.Ordinal));
        viewModel.PlaceSelectedComponentCommand.Execute(null);
        viewModel.HandleSchematicCanvasClick(new CadPoint(0, 0));
        viewModel.HandleSchematicCanvasClick(new CadPoint(5_000_000, 0));
        viewModel.ActivateWireToolCommand.Execute(null);
        viewModel.HandleSchematicCanvasClick(new CadPoint(-2_540_000, 0));
        viewModel.HandleSchematicCanvasClick(new CadPoint(2_460_000, 0));
        viewModel.ActivateSelectToolCommand.Execute(null);
        viewModel.HandleSchematicPointerPressed(new CadPoint(0, 0));

        viewModel.DeleteSelectedPartCommand.Execute(null);

        Assert.Single(viewModel.SchematicEditor.Components);
        Assert.Empty(viewModel.SchematicEditor.Wires);
        Assert.Empty(viewModel.SchematicEditor.Nets);
        Assert.Single(viewModel.BoardEditor.Components);
        Assert.Empty(viewModel.BoardEditor.Airwires);
        Assert.Contains("Deleted U1", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void DuplicateSelectedPartCommandCreatesSecondSchematicAndBoardPart()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 20);
        viewModel.ComponentManager.SelectedComponent = Assert.Single(
            viewModel.ComponentManager.Components,
            row => row.DisplayName.Contains("RESISTOR", StringComparison.Ordinal));
        viewModel.PlaceSelectedComponentCommand.Execute(null);
        viewModel.HandleSchematicCanvasClick(new CadPoint(0, 0));
        viewModel.SelectedSchematicComponentValue = "10k";

        viewModel.DuplicateSelectedPartCommand.Execute(null);

        Assert.Equal(2, viewModel.SchematicEditor.Components.Count);
        Assert.Equal(2, viewModel.BoardEditor.Components.Count);
        SchematicComponentInstance duplicate = viewModel.SchematicEditor.SelectedComponent!;
        Assert.Equal("U2", duplicate.ReferenceDesignator);
        Assert.Equal("10k", duplicate.Value);
        Assert.Contains("Duplicated U1 as U2", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void DeleteActiveSelectionCommandDeletesSelectedWireBeforePart()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 20);
        viewModel.ComponentManager.SelectedComponent = Assert.Single(
            viewModel.ComponentManager.Components,
            row => row.DisplayName.Contains("RESISTOR", StringComparison.Ordinal));
        viewModel.PlaceSelectedComponentCommand.Execute(null);
        viewModel.HandleSchematicCanvasClick(new CadPoint(0, 0));
        viewModel.HandleSchematicCanvasClick(new CadPoint(5_000_000, 0));
        viewModel.ActivateWireToolCommand.Execute(null);
        viewModel.HandleSchematicCanvasClick(new CadPoint(-2_540_000, 0));
        viewModel.HandleSchematicCanvasClick(new CadPoint(2_460_000, 0));
        viewModel.ActivateSelectToolCommand.Execute(null);
        viewModel.SchematicEditor.SelectWireAt(new CadPoint(0, 0));

        viewModel.DeleteActiveSelectionCommand.Execute(null);

        Assert.Equal(2, viewModel.SchematicEditor.Components.Count);
        Assert.Empty(viewModel.SchematicEditor.Wires);
        Assert.Equal(2, viewModel.BoardEditor.Components.Count);
        Assert.Empty(viewModel.BoardEditor.Airwires);
        Assert.Contains("Deleted selected wire", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void DeleteSelectedWireSegmentCommandRemovesOnlySelectedSegmentAndSynchronizesBoard()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 20);
        viewModel.ComponentManager.SelectedComponent = Assert.Single(
            viewModel.ComponentManager.Components,
            row => row.DisplayName.Contains("RESISTOR", StringComparison.Ordinal));
        viewModel.PlaceSelectedComponentCommand.Execute(null);
        viewModel.HandleSchematicCanvasClick(new CadPoint(0, 0));
        viewModel.HandleSchematicCanvasClick(new CadPoint(5_000_000, 0));
        viewModel.ActivateWireToolCommand.Execute(null);
        viewModel.HandleSchematicCanvasClick(new CadPoint(-2_540_000, 0));
        viewModel.HandleSchematicCanvasClick(new CadPoint(0, 2_000_000));
        viewModel.HandleSchematicCanvasClick(new CadPoint(2_460_000, 0));
        viewModel.ActivateSelectToolCommand.Execute(null);
        viewModel.SchematicEditor.SelectWireAt(new CadPoint(1_000_000, 2_000_000));

        viewModel.DeleteSelectedWireSegmentCommand.Execute(null);

        SchematicWire wire = Assert.Single(viewModel.SchematicEditor.Wires);
        Assert.Equal([new CadPoint(-2_540_000, 0), new CadPoint(2_460_000, 0)], wire.RoutePoints);
        Assert.Single(viewModel.BoardEditor.Airwires);
        Assert.Contains("Deleted wire segment", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void InsertWireVertexCommandAddsVertexToSelectedWireSegmentAndSynchronizesBoard()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 20);
        viewModel.ComponentManager.SelectedComponent = Assert.Single(
            viewModel.ComponentManager.Components,
            row => row.DisplayName.Contains("RESISTOR", StringComparison.Ordinal));
        viewModel.PlaceSelectedComponentCommand.Execute(null);
        viewModel.HandleSchematicCanvasClick(new CadPoint(0, 0));
        viewModel.HandleSchematicCanvasClick(new CadPoint(5_000_000, 0));
        viewModel.ActivateWireToolCommand.Execute(null);
        viewModel.HandleSchematicCanvasClick(new CadPoint(-2_540_000, 0));
        viewModel.HandleSchematicCanvasClick(new CadPoint(0, 2_000_000));
        viewModel.HandleSchematicCanvasClick(new CadPoint(2_460_000, 0));
        viewModel.ActivateSelectToolCommand.Execute(null);
        viewModel.SchematicEditor.SelectWireAt(new CadPoint(1_000_000, 2_000_000));

        viewModel.InsertWireVertexCommand.Execute(new CadPoint(1_200_000, 3_400_000));

        SchematicWire wire = Assert.Single(viewModel.SchematicEditor.Wires);
        Assert.Contains(new CadPoint(1_000_000, 3_000_000), wire.RoutePoints);
        Assert.Single(viewModel.BoardEditor.Airwires);
        Assert.Contains("Inserted wire vertex", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void SchematicWirePropertyFieldRenamesSelectedNetAndSynchronizesBoardAirwire()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 20);
        viewModel.ComponentManager.SelectedComponent = Assert.Single(
            viewModel.ComponentManager.Components,
            row => row.DisplayName.Contains("RESISTOR", StringComparison.Ordinal));
        viewModel.PlaceSelectedComponentCommand.Execute(null);
        viewModel.HandleSchematicCanvasClick(new CadPoint(0, 0));
        viewModel.HandleSchematicCanvasClick(new CadPoint(5_000_000, 0));
        viewModel.ActivateWireToolCommand.Execute(null);
        viewModel.HandleSchematicCanvasClick(new CadPoint(-2_540_000, 0));
        viewModel.HandleSchematicCanvasClick(new CadPoint(2_460_000, 0));
        viewModel.ActivateSelectToolCommand.Execute(null);
        viewModel.SchematicEditor.SelectWireAt(new CadPoint(0, 0));

        viewModel.SelectedSchematicWireNetName = "+5V";

        Assert.Equal("+5V", Assert.Single(viewModel.SchematicEditor.Wires).NetName);
        Assert.Equal("+5V", Assert.Single(viewModel.SchematicEditor.Nets).Name);
        Assert.Equal("+5V", Assert.Single(viewModel.BoardEditor.Airwires).NetName);
        Assert.Contains("Renamed selected net to +5V", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void DeleteActiveSelectionCommandDeletesSelectedPartWhenNoWireIsSelected()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 20);
        viewModel.ComponentManager.SelectedComponent = Assert.Single(
            viewModel.ComponentManager.Components,
            row => row.DisplayName.Contains("RESISTOR", StringComparison.Ordinal));
        viewModel.PlaceSelectedComponentCommand.Execute(null);
        viewModel.HandleSchematicCanvasClick(new CadPoint(0, 0));

        viewModel.DeleteActiveSelectionCommand.Execute(null);

        Assert.Empty(viewModel.SchematicEditor.Components);
        Assert.Empty(viewModel.BoardEditor.Components);
        Assert.Contains("Deleted U1", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void CancelActiveOperationCommandCancelsArmedPlacement()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 20);
        viewModel.ComponentManager.SelectedComponent = Assert.Single(
            viewModel.ComponentManager.Components,
            row => row.DisplayName.Contains("RESISTOR", StringComparison.Ordinal));
        viewModel.PlaceSelectedComponentCommand.Execute(null);

        viewModel.CancelActiveOperationCommand.Execute(null);

        Assert.Null(viewModel.ActivePlacement);
        Assert.Contains("Placement cancelled", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void CancelActiveOperationCommandCancelsPendingWire()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 20);
        viewModel.ComponentManager.SelectedComponent = Assert.Single(
            viewModel.ComponentManager.Components,
            row => row.DisplayName.Contains("RESISTOR", StringComparison.Ordinal));
        viewModel.PlaceSelectedComponentCommand.Execute(null);
        viewModel.HandleSchematicCanvasClick(new CadPoint(0, 0));
        viewModel.ActivateWireToolCommand.Execute(null);
        viewModel.HandleSchematicCanvasClick(new CadPoint(-2_540_000, 0));

        viewModel.CancelActiveOperationCommand.Execute(null);

        Assert.Null(viewModel.SchematicEditor.PendingWireStart);
        Assert.Empty(viewModel.SchematicEditor.PendingWireRoutePoints);
        Assert.Contains("Cancelled pending wire", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void WireToolPointerMoveShowsLivePreviewAndSyncsBoardAirwireWhenCompleted()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 20);
        viewModel.ComponentManager.SelectedComponent = Assert.Single(
            viewModel.ComponentManager.Components,
            row => row.DisplayName.Contains("RESISTOR", StringComparison.Ordinal));
        viewModel.PlaceSelectedComponentCommand.Execute(null);
        viewModel.HandleSchematicCanvasClick(new CadPoint(0, 0));
        viewModel.HandleSchematicCanvasClick(new CadPoint(5_000_000, 0));
        viewModel.ActivateWireToolCommand.Execute(null);

        viewModel.HandleSchematicPointerPressed(new CadPoint(-2_540_000, 0));
        viewModel.HandleSchematicPointerMoved(new CadPoint(-200_000, 1_800_000));

        Assert.Equal(new CadPoint(0, 2_000_000), viewModel.SchematicEditor.PendingWirePreviewPoint);

        viewModel.HandleSchematicPointerPressed(new CadPoint(2_460_000, 0));

        Assert.Single(viewModel.SchematicEditor.Wires);
        Assert.Single(viewModel.SchematicEditor.Nets);
        BoardAirwire airwire = Assert.Single(viewModel.BoardEditor.Airwires);
        Assert.Equal("N$1", airwire.NetName);
        Assert.Contains("Board sync", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void ZoomCommandsChangeSchematicZoomLevel()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);

        viewModel.ZoomInCommand.Execute(null);
        Assert.Equal(1.25, viewModel.SchematicEditor.ZoomLevel);

        viewModel.ZoomOutCommand.Execute(null);
        Assert.Equal(1.0, viewModel.SchematicEditor.ZoomLevel);
    }

    [Fact]
    public void GridCommandsUpdateSchematicAndBoardEditorSettings()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);

        viewModel.ToggleGridVisibilityCommand.Execute(null);
        viewModel.ToggleGridStyleCommand.Execute(null);
        viewModel.IncreaseGridSpacingCommand.Execute(null);

        Assert.False(viewModel.SchematicEditor.IsGridVisible);
        Assert.False(viewModel.BoardEditor.IsGridVisible);
        Assert.Equal("Lines", viewModel.SchematicEditor.GridStyle);
        Assert.Equal("Lines", viewModel.BoardEditor.GridStyle);
        Assert.Equal(2 * CadUnit.InternalUnitsPerMillimeter, viewModel.SchematicEditor.GridSpacingInternal);
        Assert.Equal(2 * CadUnit.InternalUnitsPerMillimeter, viewModel.BoardEditor.GridSpacingInternal);
        Assert.Equal("Grid 2.000 mm Lines Hidden", viewModel.GridStatusText);
    }

    [Fact]
    public void WorkspaceTabCommandsSwitchBetweenComponentManagerAndSchematic()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);

        Assert.Equal("ComponentManager", viewModel.ActiveWorkspaceTab);
        Assert.True(viewModel.IsComponentManagerTabActive);
        Assert.False(viewModel.IsSchematicTabActive);

        viewModel.ShowSchematicTabCommand.Execute(null);

        Assert.Equal("Schematic", viewModel.ActiveWorkspaceTab);
        Assert.False(viewModel.IsComponentManagerTabActive);
        Assert.True(viewModel.IsSchematicTabActive);

        viewModel.ShowComponentManagerTabCommand.Execute(null);

        Assert.Equal("ComponentManager", viewModel.ActiveWorkspaceTab);
        Assert.True(viewModel.IsComponentManagerTabActive);
        Assert.False(viewModel.IsSchematicTabActive);
    }

    [Fact]
    public void WorkspaceTabCommandsSwitchToPcbLayout()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);

        viewModel.ShowPcbLayoutTabCommand.Execute(null);

        Assert.Equal("PcbLayout", viewModel.ActiveWorkspaceTab);
        Assert.True(viewModel.IsPcbLayoutTabActive);
        Assert.False(viewModel.IsComponentManagerTabActive);
        Assert.False(viewModel.IsSchematicTabActive);
    }

    [Fact]
    public void WorkspaceTabCommandsSwitchToMarketplaceWithSeededVendorRows()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);

        viewModel.ShowMarketplaceTabCommand.Execute(null);

        Assert.Equal("Marketplace", viewModel.ActiveWorkspaceTab);
        Assert.True(viewModel.IsMarketplaceTabActive);
        Assert.False(viewModel.IsComponentManagerTabActive);
        Assert.False(viewModel.IsSchematicTabActive);
        Assert.False(viewModel.IsPcbLayoutTabActive);
        Assert.Contains("Digi-Key", viewModel.Marketplace.ProviderFilterOptions);
        Assert.Contains("Mouser", viewModel.Marketplace.ProviderFilterOptions);
        Assert.Contains("Adafruit", viewModel.Marketplace.ProviderFilterOptions);
        Assert.Contains("SparkFun", viewModel.Marketplace.ProviderFilterOptions);
        Assert.Contains("Jameco", viewModel.Marketplace.ProviderFilterOptions);
        Assert.Contains(viewModel.Marketplace.Components, row => row.DisplayName.Contains("7805", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(viewModel.Marketplace.Components, row => row.CanonicalBadge.Contains("Duplicate", StringComparison.Ordinal));
    }

    [Fact]
    public void MarketplaceAddSelectedComponentCommandAddsSelectedRowToBomCart()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);
        viewModel.Marketplace.SelectedComponent = viewModel.Marketplace.Components.Single(row => row.Provider == "Digi-Key");

        viewModel.AddSelectedMarketplaceComponentToCartCommand.Execute(null);

        Assert.Single(viewModel.MarketplaceCart.Lines);
        Assert.Equal("LM7805 5V Linear Regulator", viewModel.MarketplaceCart.Lines[0].DisplayName);
        Assert.Equal("$0.73", viewModel.MarketplaceCart.TotalSummary);
        Assert.Contains("Added LM7805", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void MarketplaceCartQuantityCommandsUpdateBomCartFromShell()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);
        viewModel.Marketplace.SelectedComponent = viewModel.Marketplace.Components.Single(row => row.Provider == "Digi-Key");
        viewModel.AddSelectedMarketplaceComponentToCartCommand.Execute(null);
        string lineId = viewModel.MarketplaceCart.Lines[0].LineId;

        viewModel.IncrementMarketplaceCartLineCommand.Execute(lineId);
        viewModel.DecrementMarketplaceCartLineCommand.Execute(lineId);

        Assert.Single(viewModel.MarketplaceCart.Lines);
        Assert.Equal(1, viewModel.MarketplaceCart.Lines[0].Quantity);
        Assert.Equal("$0.73", viewModel.MarketplaceBomExportPreview.TotalSummary);
        Assert.Contains("quantity set to 1", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void MarketplaceCartRemoveCommandRemovesBomCartLineFromShell()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);
        viewModel.Marketplace.SelectedComponent = viewModel.Marketplace.Components.Single(row => row.Provider == "Digi-Key");
        viewModel.AddSelectedMarketplaceComponentToCartCommand.Execute(null);
        string lineId = viewModel.MarketplaceCart.Lines[0].LineId;

        viewModel.RemoveMarketplaceCartLineCommand.Execute(lineId);

        Assert.Empty(viewModel.MarketplaceCart.Lines);
        Assert.Equal("$0.00", viewModel.MarketplaceBomExportPreview.TotalSummary);
        Assert.Contains("removed from BOM cart", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void ShellExposesMarketplaceExportQualityAndSavedFilters()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);
        viewModel.Marketplace.SelectedComponent = viewModel.Marketplace.Components.Single(row => row.Provider == "Mouser");
        viewModel.AddSelectedMarketplaceComponentToCartCommand.Execute(null);

        Assert.Equal("$0.48", viewModel.MarketplaceBomExportPreview.TotalSummary);
        Assert.Contains(viewModel.MarketplaceBomExportPreview.Rows, row => row.Provider == "Mouser" && row.ManufacturerPartNumber == "L7805CV");
        Assert.Contains(viewModel.MarketplaceBomExportPreview.CsvLines, line => line.Contains("Mouser,L7805CV", StringComparison.Ordinal));
        Assert.Equal("$0.48", viewModel.MarketplaceOrderPlan.TotalSummary);
        Assert.Contains(viewModel.MarketplaceOrderPlan.Providers, provider => provider.Provider == "Mouser" && provider.ActionLabel == "Open Mouser cart");
        Assert.Contains(viewModel.SelectedMarketplaceQualityBadges, badge => badge.Label.Contains("Duplicate", StringComparison.Ordinal));
        Assert.Contains(viewModel.MarketplaceFilterPresets, preset => preset.Name == "Stocked datasheets");

        viewModel.ApplyMarketplaceFilterPresetCommand.Execute("Stocked datasheets");

        Assert.Equal("Stocked datasheets", viewModel.SelectedMarketplaceFilterPresetName);
        Assert.NotEmpty(viewModel.FilteredMarketplacePresetRows);
        Assert.All(viewModel.FilteredMarketplacePresetRows, row => Assert.True(row.HasDatasheet && row.StockQuantity > 0));
    }

    [Fact]
    public void ShellExposesReadOnlyMarketplaceAuditTimeline()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);

        Assert.NotEmpty(viewModel.MarketplaceAuditTimeline.Rows);
        Assert.Contains(viewModel.MarketplaceAuditTimeline.Rows, row => row.SourceType == "Vendor Import" && row.Vendor == "Digi-Key");
        Assert.Contains(viewModel.MarketplaceAuditTimeline.Rows, row => row.SourceType == "Datasheet Generated" && row.ReviewState == "Pending Review");
        Assert.Contains("Datasheet Generated", viewModel.MarketplaceAuditTimeline.SourceFilterOptions);
        Assert.Contains("Pending Review", viewModel.MarketplaceAuditTimeline.ReviewStateFilterOptions);
    }

    [Fact]
    public void UnifiedComponentSourceRowsCombineBuiltInLibraryAndMarketplaceRows()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 2);

        Assert.Contains(viewModel.UnifiedComponentSourceRows, row => row.SourceKind == "Built-in Library");
        Assert.Contains(viewModel.UnifiedComponentSourceRows, row => row.SourceKind == "Vendor Marketplace");
        Assert.Contains(viewModel.UnifiedComponentSourceRows, row => row.DisplayName.Contains("LM7805", StringComparison.Ordinal));
        Assert.Equal("2 built-in + 5 marketplace components", viewModel.UnifiedComponentSourceSummary);
    }

    [Fact]
    public void PrepareMarketplaceBomCsvCommandMaterializesExportTextAndStatus()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);
        viewModel.Marketplace.SelectedComponent = viewModel.Marketplace.Components.Single(row => row.Provider == "Digi-Key");
        viewModel.AddSelectedMarketplaceComponentToCartCommand.Execute(null);

        viewModel.PrepareMarketplaceBomCsvCommand.Execute(null);

        Assert.Equal("dragoncad-bom.csv", viewModel.MarketplaceBomCsvExportFileName);
        Assert.Equal(2, viewModel.MarketplaceBomCsvExportLineCount);
        Assert.Contains("Vendor,MPN,Manufacturer,Component,Quantity,Unit Price,Subtotal,Canonical Id", viewModel.MarketplaceBomCsvExportText, StringComparison.Ordinal);
        Assert.Contains("Digi-Key,LM7805CT/NOPB", viewModel.MarketplaceBomCsvExportText, StringComparison.Ordinal);
        Assert.Contains("Prepared BOM CSV export with 1 line item.", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateMarketplaceOrderDraftCommandCreatesInAppCheckoutDraftFromCart()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);
        viewModel.Marketplace.SelectedComponent = viewModel.Marketplace.Components.Single(row => row.Provider == "Digi-Key");
        viewModel.AddSelectedMarketplaceComponentToCartCommand.Execute(null);

        viewModel.CreateMarketplaceOrderDraftCommand.Execute(null);

        Assert.NotNull(viewModel.ActiveMarketplaceOrderDraft);
        Assert.Equal("DRAFT-0001", viewModel.ActiveMarketplaceOrderDraft.DraftId);
        Assert.Equal("$0.73", viewModel.ActiveMarketplaceOrderDraft.TotalSummary);
        Assert.Contains(viewModel.ActiveMarketplaceOrderDraft.ProviderOrders, provider => provider.Provider == "Digi-Key");
        Assert.Contains("Created in-app order draft DRAFT-0001", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void ShellExposesCheckoutReadinessBlockersForInAppOrderDraft()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);
        viewModel.Marketplace.SelectedComponent = viewModel.Marketplace.Components.Single(row => row.Provider == "Digi-Key");
        viewModel.AddSelectedMarketplaceComponentToCartCommand.Execute(null);
        viewModel.CreateMarketplaceOrderDraftCommand.Execute(null);

        Assert.NotNull(viewModel.MarketplaceCheckoutReadiness);
        Assert.False(viewModel.MarketplaceCheckoutReadiness.CanPlaceOrder);
        Assert.Equal("Blocked: checkout setup required", viewModel.MarketplaceCheckoutReadiness.Status);
        Assert.Contains(viewModel.MarketplaceCheckoutReadiness.Blockers, blocker => blocker.Code == "ShippingProfileMissing");
        Assert.Contains(viewModel.MarketplaceCheckoutReadiness.Blockers, blocker => blocker.Code == "PaymentMethodMissing");
        Assert.Contains(viewModel.MarketplaceCheckoutReadiness.Blockers, blocker => blocker.Code == "ProviderCredentialsMissing");
    }

    [Fact]
    public void CheckoutSetupCommandsCanMakeLocalOrderDraftReadyForInAppPlacement()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);
        viewModel.Marketplace.SelectedComponent = viewModel.Marketplace.Components.Single(row => row.Provider == "Digi-Key");
        viewModel.AddSelectedMarketplaceComponentToCartCommand.Execute(null);
        viewModel.CreateMarketplaceOrderDraftCommand.Execute(null);

        viewModel.AddCheckoutShippingProfileCommand.Execute(null);
        viewModel.AddCheckoutPaymentMethodCommand.Execute(null);
        viewModel.AddCheckoutProviderCredentialsCommand.Execute("Digi-Key");

        Assert.True(viewModel.HasCheckoutShippingProfile);
        Assert.True(viewModel.HasCheckoutPaymentMethod);
        Assert.Contains("Digi-Key", viewModel.CheckoutCredentialedProviders);
        Assert.NotNull(viewModel.MarketplaceCheckoutReadiness);
        Assert.True(viewModel.MarketplaceCheckoutReadiness.CanPlaceOrder);
        Assert.Equal("Ready for in-app order placement", viewModel.MarketplaceCheckoutReadiness.Status);
        Assert.Contains("Checkout setup updated", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void PlaceMarketplaceOrderCommandCreatesLocalOrderRecordWhenCheckoutIsReady()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);
        viewModel.Marketplace.SelectedComponent = viewModel.Marketplace.Components.Single(row => row.Provider == "Digi-Key");
        viewModel.AddSelectedMarketplaceComponentToCartCommand.Execute(null);
        viewModel.CreateMarketplaceOrderDraftCommand.Execute(null);
        viewModel.AddCheckoutShippingProfileCommand.Execute(null);
        viewModel.AddCheckoutPaymentMethodCommand.Execute(null);
        viewModel.AddCheckoutProviderCredentialsCommand.Execute("Digi-Key");

        viewModel.PlaceMarketplaceOrderCommand.Execute(null);

        Assert.NotNull(viewModel.ActiveMarketplacePlacedOrder);
        Assert.Equal("ORDER-0001", viewModel.ActiveMarketplacePlacedOrder.OrderId);
        Assert.Equal("DRAFT-0001", viewModel.ActiveMarketplacePlacedOrder.DraftId);
        Assert.Equal("$0.73", viewModel.ActiveMarketplacePlacedOrder.TotalSummary);
        Assert.Contains("Created local order record ORDER-0001", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void PlaceMarketplaceOrderCommandAddsLocalRecordToOrderHistory()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);
        viewModel.Marketplace.SelectedComponent = viewModel.Marketplace.Components.Single(row => row.Provider == "Digi-Key");
        viewModel.AddSelectedMarketplaceComponentToCartCommand.Execute(null);
        viewModel.CreateMarketplaceOrderDraftCommand.Execute(null);
        viewModel.AddCheckoutShippingProfileCommand.Execute(null);
        viewModel.AddCheckoutPaymentMethodCommand.Execute(null);
        viewModel.AddCheckoutProviderCredentialsCommand.Execute("Digi-Key");

        viewModel.PlaceMarketplaceOrderCommand.Execute(null);

        var order = Assert.Single(viewModel.MarketplacePlacedOrderHistory);
        Assert.Equal("ORDER-0001", order.OrderId);
        Assert.Equal("Local order record created", order.Status);
        Assert.Contains("No live vendor order was placed.", order.ProviderOrders[0].ProviderSubmissionStatus, StringComparison.Ordinal);
        Assert.Equal("Local order records: 1", viewModel.MarketplacePlacedOrderHistorySummary);
    }

    [Fact]
    public void PlaceMarketplaceOrderCommandReportsReadinessBlockersWhenCheckoutIsBlocked()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);
        viewModel.Marketplace.SelectedComponent = viewModel.Marketplace.Components.Single(row => row.Provider == "Digi-Key");
        viewModel.AddSelectedMarketplaceComponentToCartCommand.Execute(null);
        viewModel.CreateMarketplaceOrderDraftCommand.Execute(null);

        viewModel.PlaceMarketplaceOrderCommand.Execute(null);

        Assert.Null(viewModel.ActiveMarketplacePlacedOrder);
        Assert.Contains("Resolve 3 checkout blockers", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void ShellExposesVendorSyncDashboardForMarketplace()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);

        Assert.Equal(["Digi-Key", "Mouser", "Adafruit", "SparkFun", "Jameco"], viewModel.VendorCatalogSync.Providers.Select(row => row.ProviderName).ToArray());
        Assert.Contains(viewModel.VendorCatalogSync.Providers, row =>
            row.ProviderName == "Digi-Key" &&
            (row.NextActionLabel == "Add API credentials" || row.NextActionLabel == "Sync now"));
        Assert.Contains(viewModel.VendorCatalogSync.Providers, row =>
            row.ProviderName == "Mouser" &&
            (row.NextActionLabel == "Add API credentials" || row.NextActionLabel == "Sync now"));
        Assert.Contains(viewModel.VendorCatalogSync.Providers, row => row.ProviderName == "SparkFun" && row.NextActionLabel == "Refresh source libraries");
    }

    [Fact]
    public void StartupTabSelectionCanOpenMarketplaceForReviewBuilds()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);

        viewModel.ApplyStartupTab("Marketplace");

        Assert.Equal("Marketplace", viewModel.ActiveWorkspaceTab);
        Assert.True(viewModel.IsMarketplaceTabActive);
    }

    [Fact]
    public void WorkspaceTabCommandsSwitchToFabricationWithSeededManufacturingHandoffs()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);

        viewModel.ShowFabricationTabCommand.Execute(null);

        Assert.Equal("Fabrication", viewModel.ActiveWorkspaceTab);
        Assert.True(viewModel.IsFabricationTabActive);
        Assert.False(viewModel.IsMarketplaceTabActive);
        Assert.Contains("OSH Park", viewModel.Fabrication.ProviderFilterOptions);
        Assert.Contains("PCBCart", viewModel.Fabrication.ProviderFilterOptions);
        Assert.Contains(viewModel.Fabrication.Options, option => option.ProviderName == "OSH Park" && option.CanStartHandoff);
        Assert.Contains(viewModel.Fabrication.Options, option => option.ProviderName == "PCBCart" && !option.CanStartHandoff);
    }

    [Fact]
    public void ShellExposesFabricationChecklistExportPreview()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);

        Assert.Equal("OSH Park", viewModel.FabricationChecklistPreview.ProviderName);
        Assert.Equal("Ready", viewModel.FabricationChecklistPreview.Status);
        Assert.Contains(viewModel.FabricationChecklistPreview.Rows, row => row.FileName == "Gerbers" && row.Status == "Ready");

        viewModel.Fabrication.SelectedOption = viewModel.Fabrication.Options.Single(option => option.ProviderName == "PCBCart");

        Assert.Equal("PCBCart", viewModel.FabricationChecklistPreview.ProviderName);
        Assert.Equal("Blocked", viewModel.FabricationChecklistPreview.Status);
        Assert.Contains(viewModel.FabricationChecklistPreview.Diagnostics, diagnostic => diagnostic.Contains("Missing BOM", StringComparison.Ordinal));
    }

    [Fact]
    public void ShellExposesFabricationHandoffActionPlanForSelectedOption()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);

        Assert.True(viewModel.SelectedFabricationHandoffPlan.IsReady);
        Assert.Equal("Open OSH Park upload page", viewModel.SelectedFabricationHandoffPlan.Action?.Label);

        viewModel.Fabrication.SelectedOption = viewModel.Fabrication.Options.Single(option => option.ProviderName == "PCBCart");

        Assert.False(viewModel.SelectedFabricationHandoffPlan.IsReady);
        Assert.Contains(viewModel.SelectedFabricationHandoffPlan.Diagnostics, diagnostic => diagnostic.Contains("Missing BOM", StringComparison.Ordinal));
    }

    [Fact]
    public void WorkspaceTabCommandsSwitchToDatasheetsWithSeededReviewQueue()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);

        viewModel.ShowDatasheetsTabCommand.Execute(null);

        Assert.Equal("Datasheets", viewModel.ActiveWorkspaceTab);
        Assert.True(viewModel.IsDatasheetsTabActive);
        Assert.False(viewModel.IsMarketplaceTabActive);
        Assert.Contains("Pending", viewModel.DatasheetReviewQueue.ReviewStateFilterOptions);
        Assert.Contains(viewModel.DatasheetReviewQueue.Rows, row => row.ComponentName.Contains("7805", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(viewModel.DatasheetReviewQueue.Rows, row => row.WarningDisplay.Contains("package", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ShellExposesDatasheetLinkReviewPlans()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);

        Assert.NotEmpty(viewModel.DatasheetLinkReviewPlans);
        Assert.Contains(
            viewModel.DatasheetLinkReviewPlans,
            row => row.ComponentName.Contains("LM7805", StringComparison.OrdinalIgnoreCase)
                   && row.DecisionDisplay == "Link Existing Component"
                   && row.TargetComponentId.Contains("dragon:lm7805", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            viewModel.DatasheetLinkReviewPlans,
            row => row.ComponentName.Contains("ESP32", StringComparison.OrdinalIgnoreCase)
                   && row.DecisionDisplay == "Needs New Component"
                   && row.WarningDisplay.Contains("review", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DatasheetLinkReviewRowsCanBeApprovedOrRejectedLocally()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);
        var linkRow = viewModel.DatasheetLinkReviewPlans.Single(row => row.TargetComponentId == "dragon:lm7805");
        var generatedRow = viewModel.DatasheetLinkReviewPlans.Single(row => row.TargetComponentId == "New component required");

        linkRow.ApproveCommand.Execute(null);
        generatedRow.RejectCommand.Execute(null);

        Assert.Equal("Approved for Promotion", linkRow.ReviewStateDisplay);
        Assert.False(linkRow.CanApprove);
        Assert.Equal("Rejected", generatedRow.ReviewStateDisplay);
        Assert.Equal("Rejected before trusted library promotion.", generatedRow.ReviewNote);
    }

    [Fact]
    public void ApprovedDatasheetLinkReviewsAppearInPromotionQueue()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);
        var linkRow = viewModel.DatasheetLinkReviewPlans.Single(row => row.TargetComponentId == "dragon:lm7805");
        var generatedRow = viewModel.DatasheetLinkReviewPlans.Single(row => row.TargetComponentId == "New component required");

        linkRow.ApproveCommand.Execute(null);
        generatedRow.RejectCommand.Execute(null);

        var promotion = Assert.Single(viewModel.DatasheetLinkPromotionQueue);
        Assert.Equal("LM7805 5V Linear Regulator", promotion.ComponentName);
        Assert.Equal("dragon:lm7805", promotion.TargetComponentId);
        Assert.Equal("Link Existing Component", promotion.DecisionDisplay);
        Assert.Equal("Ready for trusted-library promotion", promotion.PromotionStatus);
        Assert.Equal("1 approved link pending promotion", viewModel.DatasheetLinkPromotionQueueSummary);
    }

    [Fact]
    public void CreateDatasheetLinkPromotionRecordCommandSnapshotsApprovedQueue()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);
        var linkRow = viewModel.DatasheetLinkReviewPlans.Single(row => row.TargetComponentId == "dragon:lm7805");
        linkRow.ApproveCommand.Execute(null);

        viewModel.CreateDatasheetLinkPromotionRecordCommand.Execute(null);

        Assert.NotNull(viewModel.ActiveDatasheetLinkPromotionRecord);
        Assert.Equal("PROMO-0001", viewModel.ActiveDatasheetLinkPromotionRecord.RecordId);
        Assert.Equal("Local promotion record created", viewModel.ActiveDatasheetLinkPromotionRecord.Status);
        Assert.Contains(viewModel.ActiveDatasheetLinkPromotionRecord.Rows, row => row.TargetComponentId == "dragon:lm7805");
        Assert.Single(viewModel.DatasheetLinkPromotionRecordHistory);
        Assert.Contains("Created local datasheet promotion record PROMO-0001", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void DatasheetLinkPromotionRecordExposesDeterministicJsonPreview()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);
        var linkRow = viewModel.DatasheetLinkReviewPlans.Single(row => row.TargetComponentId == "dragon:lm7805");
        linkRow.ApproveCommand.Execute(null);
        viewModel.CreateDatasheetLinkPromotionRecordCommand.Execute(null);

        Assert.NotNull(viewModel.ActiveDatasheetLinkPromotionRecord);
        Assert.Equal("datasheet-promotion-PROMO-0001.json", viewModel.ActiveDatasheetLinkPromotionRecord.ExportFileName);
        Assert.Equal(13, viewModel.ActiveDatasheetLinkPromotionRecord.ExportLineCount);
        Assert.Contains("\"recordId\": \"PROMO-0001\"", viewModel.ActiveDatasheetLinkPromotionRecord.ExportJsonPreview, StringComparison.Ordinal);
        Assert.Contains("\"targetComponentId\": \"dragon:lm7805\"", viewModel.ActiveDatasheetLinkPromotionRecord.ExportJsonPreview, StringComparison.Ordinal);
        Assert.Contains("\"trustedLibraryWrite\": \"pending\"", viewModel.ActiveDatasheetLinkPromotionRecord.ExportJsonPreview, StringComparison.Ordinal);
    }

    [Fact]
    public void ApproveSafeDatasheetLinksCommandOnlyApprovesCleanExistingLinks()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);

        viewModel.ApproveSafeDatasheetLinksCommand.Execute(null);

        var existingLink = viewModel.DatasheetLinkReviewPlans.Single(row => row.TargetComponentId == "dragon:lm7805");
        var generatedLink = viewModel.DatasheetLinkReviewPlans.Single(row => row.TargetComponentId == "New component required");
        Assert.Equal("Approved for Promotion", existingLink.ReviewStateDisplay);
        Assert.Equal("Pending Review", generatedLink.ReviewStateDisplay);
        Assert.Single(viewModel.DatasheetLinkPromotionQueue);
        Assert.Contains("Approved 1 safe datasheet link", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void StageSafeDatasheetLinksCommandApprovesAndCreatesPromotionRecord()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);

        viewModel.StageSafeDatasheetLinksCommand.Execute(null);

        var existingLink = viewModel.DatasheetLinkReviewPlans.Single(row => row.TargetComponentId == "dragon:lm7805");
        var generatedLink = viewModel.DatasheetLinkReviewPlans.Single(row => row.TargetComponentId == "New component required");
        Assert.Equal("Staged for Promotion", existingLink.ReviewStateDisplay);
        Assert.Equal("Pending Review", generatedLink.ReviewStateDisplay);
        Assert.NotNull(viewModel.ActiveDatasheetLinkPromotionRecord);
        Assert.Equal("PROMO-0001", viewModel.ActiveDatasheetLinkPromotionRecord.RecordId);
        Assert.Contains("\"targetComponentId\": \"dragon:lm7805\"", viewModel.ActiveDatasheetLinkPromotionRecord.ExportJsonPreview, StringComparison.Ordinal);
        Assert.Contains("Staged 1 safe datasheet link in promotion record PROMO-0001", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void StageSafeDatasheetLinksCommandDoesNotCreateDuplicatePromotionRecords()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);

        viewModel.StageSafeDatasheetLinksCommand.Execute(null);
        viewModel.StageSafeDatasheetLinksCommand.Execute(null);

        var existingLink = viewModel.DatasheetLinkReviewPlans.Single(row => row.TargetComponentId == "dragon:lm7805");
        Assert.Equal("Staged for Promotion", existingLink.ReviewStateDisplay);
        Assert.Empty(viewModel.DatasheetLinkPromotionQueue);
        Assert.Single(viewModel.DatasheetLinkPromotionRecordHistory);
        Assert.Equal("PROMO-0001", viewModel.ActiveDatasheetLinkPromotionRecord?.RecordId);
        Assert.Contains("No safe datasheet links are ready to stage", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void DatasheetPromotionRecordExposesTrustedLibraryWriteChecklist()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);

        viewModel.StageSafeDatasheetLinksCommand.Execute(null);

        Assert.NotNull(viewModel.ActiveDatasheetLinkPromotionRecord);
        Assert.Equal("Blocked: trusted-library write pending", viewModel.ActiveDatasheetLinkPromotionRecord.ReadinessStatus);
        Assert.Contains(viewModel.ActiveDatasheetLinkPromotionRecord.Checklist, row => row.Label == "Promotion JSON artifact" && row.Status == "Preview only");
        Assert.Contains(viewModel.ActiveDatasheetLinkPromotionRecord.Checklist, row => row.Label == "Trusted library write" && row.Status == "Pending implementation");
        Assert.Contains(viewModel.ActiveDatasheetLinkPromotionRecord.Checklist, row => row.Label == "Audit entry" && row.Status == "Pending implementation");
    }

    [Fact]
    public void SaveDatasheetPromotionPreviewCommandWritesDeterministicJsonArtifact()
    {
        string artifactDirectory = Path.Combine(Path.GetTempPath(), $"dragoncad-promotion-{Guid.NewGuid():N}");
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1,
            datasheetPromotionArtifactDirectory: artifactDirectory);
        viewModel.StageSafeDatasheetLinksCommand.Execute(null);

        viewModel.SaveDatasheetPromotionPreviewCommand.Execute(null);

        Assert.NotNull(viewModel.ActiveDatasheetLinkPromotionRecord);
        string expectedPath = Path.Combine(artifactDirectory, viewModel.ActiveDatasheetLinkPromotionRecord.ExportFileName);
        Assert.Equal(expectedPath, viewModel.SavedDatasheetPromotionArtifactPath);
        Assert.True(File.Exists(expectedPath));
        Assert.Equal(viewModel.ActiveDatasheetLinkPromotionRecord.ExportJsonPreview, File.ReadAllText(expectedPath));
        Assert.Contains("Saved datasheet promotion artifact", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void SaveDatasheetPromotionPreviewCommandWritesDeterministicManifestArtifact()
    {
        string artifactDirectory = Path.Combine(Path.GetTempPath(), $"dragoncad-promotion-{Guid.NewGuid():N}");
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1,
            datasheetPromotionArtifactDirectory: artifactDirectory);
        viewModel.StageSafeDatasheetLinksCommand.Execute(null);

        viewModel.SaveDatasheetPromotionPreviewCommand.Execute(null);

        Assert.NotNull(viewModel.ActiveDatasheetLinkPromotionRecord);
        string expectedManifestPath = Path.Combine(artifactDirectory, viewModel.ActiveDatasheetLinkPromotionRecord.ExportManifestFileName);
        Assert.Equal(expectedManifestPath, viewModel.SavedDatasheetPromotionManifestPath);
        Assert.True(File.Exists(expectedManifestPath));
        Assert.Equal(viewModel.ActiveDatasheetLinkPromotionRecord.ExportManifestJsonPreview, File.ReadAllText(expectedManifestPath));
        Assert.Contains("\"promotionArtifact\": \"datasheet-promotion-PROMO-0001.json\"", viewModel.ActiveDatasheetLinkPromotionRecord.ExportManifestJsonPreview, StringComparison.Ordinal);
        Assert.Contains("\"rowCount\": 1", viewModel.ActiveDatasheetLinkPromotionRecord.ExportManifestJsonPreview, StringComparison.Ordinal);
        Assert.Contains("\"auditEntry\": \"pending\"", viewModel.ActiveDatasheetLinkPromotionRecord.ExportManifestJsonPreview, StringComparison.Ordinal);
    }

    [Fact]
    public void SaveDatasheetPromotionPreviewCommandWritesDeterministicAuditArtifact()
    {
        string artifactDirectory = Path.Combine(Path.GetTempPath(), $"dragoncad-promotion-{Guid.NewGuid():N}");
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1,
            datasheetPromotionArtifactDirectory: artifactDirectory);
        viewModel.StageSafeDatasheetLinksCommand.Execute(null);

        viewModel.SaveDatasheetPromotionPreviewCommand.Execute(null);

        Assert.NotNull(viewModel.ActiveDatasheetLinkPromotionRecord);
        string expectedAuditPath = Path.Combine(artifactDirectory, viewModel.ActiveDatasheetLinkPromotionRecord.ExportAuditFileName);
        Assert.Equal(expectedAuditPath, viewModel.SavedDatasheetPromotionAuditPath);
        Assert.True(File.Exists(expectedAuditPath));
        Assert.Equal(viewModel.ActiveDatasheetLinkPromotionRecord.ExportAuditJsonPreview, File.ReadAllText(expectedAuditPath));
        Assert.Contains("\"event\": \"datasheetPromotionPreviewSaved\"", viewModel.ActiveDatasheetLinkPromotionRecord.ExportAuditJsonPreview, StringComparison.Ordinal);
        Assert.Contains("\"trustedLibraryMutation\": \"not-performed\"", viewModel.ActiveDatasheetLinkPromotionRecord.ExportAuditJsonPreview, StringComparison.Ordinal);
        Assert.Contains("\"reviewedRows\": 1", viewModel.ActiveDatasheetLinkPromotionRecord.ExportAuditJsonPreview, StringComparison.Ordinal);
    }

    [Fact]
    public void StageAndSaveSafeDatasheetLinksCommandCreatesLocalPromotionPackage()
    {
        string artifactDirectory = Path.Combine(Path.GetTempPath(), $"dragoncad-promotion-{Guid.NewGuid():N}");
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1,
            datasheetPromotionArtifactDirectory: artifactDirectory);

        viewModel.StageAndSaveSafeDatasheetLinksCommand.Execute(null);

        var existingLink = viewModel.DatasheetLinkReviewPlans.Single(row => row.TargetComponentId == "dragon:lm7805");
        var generatedLink = viewModel.DatasheetLinkReviewPlans.Single(row => row.TargetComponentId == "New component required");
        Assert.Equal("Staged for Promotion", existingLink.ReviewStateDisplay);
        Assert.Equal("Pending Review", generatedLink.ReviewStateDisplay);
        Assert.NotNull(viewModel.ActiveDatasheetLinkPromotionRecord);
        Assert.Equal("PROMO-0001", viewModel.ActiveDatasheetLinkPromotionRecord.RecordId);
        Assert.True(File.Exists(viewModel.SavedDatasheetPromotionArtifactPath));
        Assert.True(File.Exists(viewModel.SavedDatasheetPromotionManifestPath));
        Assert.True(File.Exists(viewModel.SavedDatasheetPromotionAuditPath));
        Assert.Empty(viewModel.DatasheetLinkPromotionQueue);
        Assert.Contains("Saved local datasheet promotion package PROMO-0001", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void DatasheetPromotionManifestIncludesPromotionPackageHashes()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);
        viewModel.StageSafeDatasheetLinksCommand.Execute(null);

        Assert.NotNull(viewModel.ActiveDatasheetLinkPromotionRecord);
        string promotionHash = ComputeSha256(viewModel.ActiveDatasheetLinkPromotionRecord.ExportJsonPreview);
        string auditHash = ComputeSha256(viewModel.ActiveDatasheetLinkPromotionRecord.ExportAuditJsonPreview);

        Assert.Contains($"\"promotionArtifactSha256\": \"{promotionHash}\"", viewModel.ActiveDatasheetLinkPromotionRecord.ExportManifestJsonPreview, StringComparison.Ordinal);
        Assert.Contains($"\"auditArtifactSha256\": \"{auditHash}\"", viewModel.ActiveDatasheetLinkPromotionRecord.ExportManifestJsonPreview, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateDatasheetPromotionPackageCommandReportsValidSavedPackage()
    {
        string artifactDirectory = Path.Combine(Path.GetTempPath(), $"dragoncad-promotion-{Guid.NewGuid():N}");
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1,
            datasheetPromotionArtifactDirectory: artifactDirectory);
        viewModel.StageAndSaveSafeDatasheetLinksCommand.Execute(null);

        viewModel.ValidateDatasheetPromotionPackageCommand.Execute(null);

        Assert.Equal("Valid package: promotion JSON, manifest, and audit hashes match.", viewModel.DatasheetPromotionPackageValidationStatus);
        Assert.Contains("validated", viewModel.PlacementStatus, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateDatasheetPromotionPackageCommandReportsTamperedPackage()
    {
        string artifactDirectory = Path.Combine(Path.GetTempPath(), $"dragoncad-promotion-{Guid.NewGuid():N}");
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1,
            datasheetPromotionArtifactDirectory: artifactDirectory);
        viewModel.StageAndSaveSafeDatasheetLinksCommand.Execute(null);
        File.AppendAllText(viewModel.SavedDatasheetPromotionAuditPath, Environment.NewLine + "tampered");

        viewModel.ValidateDatasheetPromotionPackageCommand.Execute(null);

        Assert.Equal("Invalid package: audit artifact hash mismatch.", viewModel.DatasheetPromotionPackageValidationStatus);
        Assert.Contains("hash mismatch", viewModel.PlacementStatus, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RecordValidatedDatasheetPromotionLedgerEntryCommandWritesLedgerAfterValidation()
    {
        string artifactDirectory = Path.Combine(Path.GetTempPath(), $"dragoncad-promotion-{Guid.NewGuid():N}");
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1,
            datasheetPromotionArtifactDirectory: artifactDirectory);
        viewModel.StageAndSaveSafeDatasheetLinksCommand.Execute(null);
        viewModel.ValidateDatasheetPromotionPackageCommand.Execute(null);

        viewModel.RecordValidatedDatasheetPromotionLedgerEntryCommand.Execute(null);

        Assert.Equal(Path.Combine(artifactDirectory, "datasheet-promotion-ledger.jsonl"), viewModel.SavedDatasheetPromotionLedgerPath);
        Assert.True(File.Exists(viewModel.SavedDatasheetPromotionLedgerPath));
        string ledgerLine = Assert.Single(File.ReadAllLines(viewModel.SavedDatasheetPromotionLedgerPath));
        Assert.Contains("\"recordId\":\"PROMO-0001\"", ledgerLine, StringComparison.Ordinal);
        Assert.Contains("\"status\":\"validated-local-package\"", ledgerLine, StringComparison.Ordinal);
        Assert.Contains("\"trustedLibraryMutation\":\"not-performed\"", ledgerLine, StringComparison.Ordinal);
        Assert.Contains("Recorded validated datasheet promotion ledger entry PROMO-0001", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void RecordValidatedDatasheetPromotionLedgerEntryCommandRequiresValidPackage()
    {
        string artifactDirectory = Path.Combine(Path.GetTempPath(), $"dragoncad-promotion-{Guid.NewGuid():N}");
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1,
            datasheetPromotionArtifactDirectory: artifactDirectory);
        viewModel.StageAndSaveSafeDatasheetLinksCommand.Execute(null);

        viewModel.RecordValidatedDatasheetPromotionLedgerEntryCommand.Execute(null);

        Assert.False(File.Exists(Path.Combine(artifactDirectory, "datasheet-promotion-ledger.jsonl")));
        Assert.Contains("Validate a saved datasheet promotion package before recording a ledger entry", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void RecordValidatedDatasheetPromotionLedgerEntryCommandDoesNotDuplicateRecord()
    {
        string artifactDirectory = Path.Combine(Path.GetTempPath(), $"dragoncad-promotion-{Guid.NewGuid():N}");
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1,
            datasheetPromotionArtifactDirectory: artifactDirectory);
        viewModel.StageAndSaveSafeDatasheetLinksCommand.Execute(null);
        viewModel.ValidateDatasheetPromotionPackageCommand.Execute(null);

        viewModel.RecordValidatedDatasheetPromotionLedgerEntryCommand.Execute(null);
        viewModel.RecordValidatedDatasheetPromotionLedgerEntryCommand.Execute(null);

        Assert.Single(File.ReadAllLines(viewModel.SavedDatasheetPromotionLedgerPath));
        Assert.Contains("already exists in the datasheet promotion ledger", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void DatasheetPromotionTrustedLibraryGateStatusTracksLocalPrerequisites()
    {
        string artifactDirectory = Path.Combine(Path.GetTempPath(), $"dragoncad-promotion-{Guid.NewGuid():N}");
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1,
            datasheetPromotionArtifactDirectory: artifactDirectory);

        Assert.Equal("Blocked: save, validate, and ledger a promotion package first.", viewModel.DatasheetPromotionTrustedLibraryGateStatus);

        viewModel.StageAndSaveSafeDatasheetLinksCommand.Execute(null);
        viewModel.ValidateDatasheetPromotionPackageCommand.Execute(null);
        viewModel.RecordValidatedDatasheetPromotionLedgerEntryCommand.Execute(null);

        Assert.Equal("Ready: local package validated and ledgered; trusted-library write still requires explicit implementation.", viewModel.DatasheetPromotionTrustedLibraryGateStatus);
    }

    [Fact]
    public void SaveTrustedLibraryWritePlanCommandWritesPlanAfterGateReady()
    {
        string artifactDirectory = Path.Combine(Path.GetTempPath(), $"dragoncad-promotion-{Guid.NewGuid():N}");
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1,
            datasheetPromotionArtifactDirectory: artifactDirectory);
        viewModel.StageAndSaveSafeDatasheetLinksCommand.Execute(null);
        viewModel.ValidateDatasheetPromotionPackageCommand.Execute(null);
        viewModel.RecordValidatedDatasheetPromotionLedgerEntryCommand.Execute(null);

        viewModel.SaveTrustedLibraryWritePlanCommand.Execute(null);

        string expectedPath = Path.Combine(artifactDirectory, "trusted-library-write-plan-PROMO-0001.json");
        Assert.Equal(expectedPath, viewModel.SavedTrustedLibraryWritePlanPath);
        Assert.True(File.Exists(expectedPath));
        string plan = File.ReadAllText(expectedPath);
        Assert.Contains("\"recordId\": \"PROMO-0001\"", plan, StringComparison.Ordinal);
        Assert.Contains("\"operation\": \"link-existing-component\"", plan, StringComparison.Ordinal);
        Assert.Contains("\"targetComponentId\": \"dragon:lm7805\"", plan, StringComparison.Ordinal);
        Assert.Contains("\"promotionLedger\": \"datasheet-promotion-ledger.jsonl\"", plan, StringComparison.Ordinal);
        Assert.Contains("\"trustedLibraryMutation\": \"not-performed\"", plan, StringComparison.Ordinal);
        Assert.Contains("\"nextStep\": \"manual-trusted-library-writer-not-implemented\"", plan, StringComparison.Ordinal);
        Assert.Contains("Saved trusted-library write plan PROMO-0001", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void SaveTrustedLibraryWritePlanCommandRequiresReadyGate()
    {
        string artifactDirectory = Path.Combine(Path.GetTempPath(), $"dragoncad-promotion-{Guid.NewGuid():N}");
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1,
            datasheetPromotionArtifactDirectory: artifactDirectory);
        viewModel.StageAndSaveSafeDatasheetLinksCommand.Execute(null);

        viewModel.SaveTrustedLibraryWritePlanCommand.Execute(null);

        Assert.Equal("", viewModel.SavedTrustedLibraryWritePlanPath);
        Assert.False(File.Exists(Path.Combine(artifactDirectory, "trusted-library-write-plan-PROMO-0001.json")));
        Assert.Contains("Save, validate, and ledger a promotion package before creating a trusted-library write plan", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void SimulateTrustedLibraryWriteCommandWritesDryRunDiffAfterPlanExists()
    {
        string artifactDirectory = Path.Combine(Path.GetTempPath(), $"dragoncad-promotion-{Guid.NewGuid():N}");
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1,
            datasheetPromotionArtifactDirectory: artifactDirectory);
        viewModel.StageAndSaveSafeDatasheetLinksCommand.Execute(null);
        viewModel.ValidateDatasheetPromotionPackageCommand.Execute(null);
        viewModel.RecordValidatedDatasheetPromotionLedgerEntryCommand.Execute(null);
        viewModel.SaveTrustedLibraryWritePlanCommand.Execute(null);

        viewModel.SimulateTrustedLibraryWriteCommand.Execute(null);

        string expectedPath = Path.Combine(artifactDirectory, "trusted-library-write-simulation-PROMO-0001.json");
        Assert.Equal(expectedPath, viewModel.SavedTrustedLibraryWriteSimulationPath);
        Assert.True(File.Exists(expectedPath));
        string simulation = File.ReadAllText(expectedPath);
        Assert.Contains("\"recordId\": \"PROMO-0001\"", simulation, StringComparison.Ordinal);
        Assert.Contains("\"sourcePlan\": \"trusted-library-write-plan-PROMO-0001.json\"", simulation, StringComparison.Ordinal);
        Assert.Contains("\"simulationStatus\": \"dry-run-only\"", simulation, StringComparison.Ordinal);
        Assert.Contains("\"targetComponentId\": \"dragon:lm7805\"", simulation, StringComparison.Ordinal);
        Assert.Contains("\"mutationApplied\": false", simulation, StringComparison.Ordinal);
        Assert.Contains("Simulated trusted-library write PROMO-0001", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void SimulateTrustedLibraryWriteCommandRequiresSavedPlan()
    {
        string artifactDirectory = Path.Combine(Path.GetTempPath(), $"dragoncad-promotion-{Guid.NewGuid():N}");
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1,
            datasheetPromotionArtifactDirectory: artifactDirectory);
        viewModel.StageAndSaveSafeDatasheetLinksCommand.Execute(null);
        viewModel.ValidateDatasheetPromotionPackageCommand.Execute(null);
        viewModel.RecordValidatedDatasheetPromotionLedgerEntryCommand.Execute(null);

        viewModel.SimulateTrustedLibraryWriteCommand.Execute(null);

        Assert.Equal("", viewModel.SavedTrustedLibraryWriteSimulationPath);
        Assert.False(File.Exists(Path.Combine(artifactDirectory, "trusted-library-write-simulation-PROMO-0001.json")));
        Assert.Contains("Save a trusted-library write plan before simulating the trusted-library write", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void StageTrustedLibraryCandidateCommandWritesCandidateAfterSimulationExists()
    {
        string artifactDirectory = Path.Combine(Path.GetTempPath(), $"dragoncad-promotion-{Guid.NewGuid():N}");
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1,
            datasheetPromotionArtifactDirectory: artifactDirectory);
        viewModel.StageAndSaveSafeDatasheetLinksCommand.Execute(null);
        viewModel.ValidateDatasheetPromotionPackageCommand.Execute(null);
        viewModel.RecordValidatedDatasheetPromotionLedgerEntryCommand.Execute(null);
        viewModel.SaveTrustedLibraryWritePlanCommand.Execute(null);
        viewModel.SimulateTrustedLibraryWriteCommand.Execute(null);

        viewModel.StageTrustedLibraryCandidateCommand.Execute(null);

        string expectedPath = Path.Combine(artifactDirectory, "trusted-library-candidate-PROMO-0001.json");
        Assert.Equal(expectedPath, viewModel.SavedTrustedLibraryCandidatePath);
        Assert.True(File.Exists(expectedPath));
        string candidate = File.ReadAllText(expectedPath);
        Assert.Contains("\"recordId\": \"PROMO-0001\"", candidate, StringComparison.Ordinal);
        Assert.Contains("\"sourceSimulation\": \"trusted-library-write-simulation-PROMO-0001.json\"", candidate, StringComparison.Ordinal);
        Assert.Contains("\"candidateStatus\": \"staged-review-only\"", candidate, StringComparison.Ordinal);
        Assert.Contains("\"targetComponentId\": \"dragon:lm7805\"", candidate, StringComparison.Ordinal);
        Assert.Contains("\"trustedLibraryMutation\": \"not-performed\"", candidate, StringComparison.Ordinal);
        Assert.Contains("Staged trusted-library candidate PROMO-0001", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void StageTrustedLibraryCandidateCommandRequiresSimulation()
    {
        string artifactDirectory = Path.Combine(Path.GetTempPath(), $"dragoncad-promotion-{Guid.NewGuid():N}");
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1,
            datasheetPromotionArtifactDirectory: artifactDirectory);
        viewModel.StageAndSaveSafeDatasheetLinksCommand.Execute(null);
        viewModel.ValidateDatasheetPromotionPackageCommand.Execute(null);
        viewModel.RecordValidatedDatasheetPromotionLedgerEntryCommand.Execute(null);
        viewModel.SaveTrustedLibraryWritePlanCommand.Execute(null);

        viewModel.StageTrustedLibraryCandidateCommand.Execute(null);

        Assert.Equal("", viewModel.SavedTrustedLibraryCandidatePath);
        Assert.False(File.Exists(Path.Combine(artifactDirectory, "trusted-library-candidate-PROMO-0001.json")));
        Assert.Contains("Simulate the trusted-library write before staging a trusted-library candidate", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void MoveSelectedBoardComponentByGridMovesBoardSelectionOneGridStep()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 20);
        viewModel.ComponentManager.SelectedComponent = Assert.Single(
            viewModel.ComponentManager.Components,
            row => row.DisplayName.Contains("RESISTOR", StringComparison.Ordinal));
        viewModel.PlaceSelectedComponentCommand.Execute(null);
        viewModel.HandleSchematicCanvasClick(new CadPoint(0, 0));
        viewModel.BoardEditor.SelectComponentAt(new CadPoint(0, 0));

        viewModel.MoveSelectedBoardComponentByGrid(new CadVector(1, -1));

        Assert.Equal(new CadPoint(1_000_000, -1_000_000), viewModel.BoardEditor.SelectedComponent?.Position);
        Assert.Contains("Moved U1", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void BoardRouteToolCommandRoutesTraceFromBoardCanvasClicks()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);

        viewModel.ShowPcbLayoutTabCommand.Execute(null);
        viewModel.SelectedBoardLayerName = "Bottom";
        viewModel.ActivateBoardRouteToolCommand.Execute(null);
        viewModel.HandleBoardCanvasClick(new CadPoint(0, 0));
        viewModel.FinishBoardRouteCommand.Execute(new CadPoint(3_100_000, 2_200_000));

        BoardTrace trace = Assert.Single(viewModel.BoardEditor.Traces);
        Assert.Equal("Bottom", trace.LayerName);
        Assert.Equal("Route", viewModel.ActiveBoardTool);
        Assert.Equal("Bottom", viewModel.SelectedBoardLayerName);
        Assert.Equal([new CadPoint(0, 0), new CadPoint(3_000_000, 0), new CadPoint(3_000_000, 2_000_000)], trace.RoutePoints);
        Assert.Contains("Routed board trace on Bottom", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void BoardLayerVisibilityCommandHidesLayerTraces()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);

        viewModel.SelectedBoardLayerName = "Bottom";
        viewModel.ActivateBoardRouteToolCommand.Execute(null);
        viewModel.HandleBoardCanvasClick(new CadPoint(0, 0));
        viewModel.FinishBoardRouteCommand.Execute(new CadPoint(2_000_000, 0));

        viewModel.ToggleSelectedBoardLayerVisibilityCommand.Execute(null);

        Assert.Empty(viewModel.BoardEditor.VisibleTraces);
        Assert.Contains("Layer Bottom hidden", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void PlaceBoardViaCommandPlacesViaAndSwitchesLayer()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);

        viewModel.ActivateBoardRouteToolCommand.Execute(null);
        viewModel.HandleBoardCanvasClick(new CadPoint(0, 0));
        viewModel.PlaceBoardViaCommand.Execute(new CadPoint(2_200_000, 1_700_000));

        BoardVia via = Assert.Single(viewModel.BoardEditor.Vias);
        Assert.Equal(new CadPoint(2_000_000, 2_000_000), via.Position);
        Assert.Equal("Bottom", viewModel.SelectedBoardLayerName);
        Assert.Contains("Placed via", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void DeleteBoardSelectionCommandDeletesSelectedTrace()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);
        viewModel.ActivateBoardRouteToolCommand.Execute(null);
        viewModel.HandleBoardCanvasClick(new CadPoint(0, 0));
        viewModel.FinishBoardRouteCommand.Execute(new CadPoint(4_000_000, 0));
        viewModel.ActivateBoardSelectToolCommand.Execute(null);

        viewModel.HandleBoardCanvasClick(new CadPoint(2_000_000, 0));
        viewModel.DeleteBoardSelectionCommand.Execute(null);

        Assert.Empty(viewModel.BoardEditor.Traces);
        Assert.Contains("Deleted selected board trace", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void MoveSelectedBoardTraceToLayerCommandMovesTraceToSelectedLayer()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);
        viewModel.ActivateBoardRouteToolCommand.Execute(null);
        viewModel.HandleBoardCanvasClick(new CadPoint(0, 0));
        viewModel.FinishBoardRouteCommand.Execute(new CadPoint(4_000_000, 0));
        viewModel.ActivateBoardSelectToolCommand.Execute(null);
        viewModel.HandleBoardCanvasClick(new CadPoint(2_000_000, 0));

        viewModel.SelectedBoardLayerName = "Bottom";
        viewModel.MoveSelectedBoardTraceToLayerCommand.Execute(null);

        Assert.Equal("Bottom", Assert.Single(viewModel.BoardEditor.Traces).LayerName);
        Assert.Contains("Moved selected board trace to Bottom", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void MoveSelectedBoardComponentByGridMovesSelectedViaOneGridStep()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);
        viewModel.BoardEditor.PlaceViaAt(new CadPoint(2_000_000, 2_000_000));
        viewModel.BoardEditor.ActivateSelectTool();
        viewModel.BoardEditor.SelectAt(new CadPoint(2_000_000, 2_000_000));

        viewModel.MoveSelectedBoardComponentByGrid(new CadVector(1, -1));

        Assert.Equal(new CadPoint(3_000_000, 1_000_000), viewModel.BoardEditor.SelectedVia?.Position);
        Assert.Contains("Moved via", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void MoveSelectedBoardComponentByGridMovesSelectedTraceSegmentOneGridStep()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);
        viewModel.BoardEditor.ActivateRouteTool();
        viewModel.BoardEditor.TraceClickAt(new CadPoint(0, 0));
        viewModel.BoardEditor.TraceClickAt(new CadPoint(2_000_000, 2_000_000));
        viewModel.BoardEditor.CompleteTraceAt(new CadPoint(6_000_000, 2_000_000));
        viewModel.BoardEditor.ActivateSelectTool();
        viewModel.BoardEditor.SelectAt(new CadPoint(2_000_000, 1_000_000));

        viewModel.MoveSelectedBoardComponentByGrid(new CadVector(1, 0));

        Assert.Equal(
            [
                new CadPoint(0, 0),
                new CadPoint(3_000_000, 0),
                new CadPoint(3_000_000, 2_000_000),
                new CadPoint(6_000_000, 2_000_000)
            ],
            Assert.Single(viewModel.BoardEditor.Traces).RoutePoints);
        Assert.Contains("Moved selected board trace segment", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void BoardSelectionSummaryDescribesSelectedTraceAndVia()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);
        viewModel.ActivateBoardRouteToolCommand.Execute(null);
        viewModel.HandleBoardCanvasClick(new CadPoint(0, 0));
        viewModel.FinishBoardRouteCommand.Execute(new CadPoint(4_000_000, 2_000_000));
        viewModel.PlaceBoardViaCommand.Execute(new CadPoint(6_100_000, 2_200_000));
        viewModel.ActivateBoardSelectToolCommand.Execute(null);

        viewModel.HandleBoardCanvasClick(new CadPoint(4_000_000, 1_000_000));
        Assert.Equal("Trace: Top, 3 points, segment 2", viewModel.BoardSelectionSummary);

        viewModel.HandleBoardCanvasClick(new CadPoint(6_000_000, 2_000_000));
        Assert.Equal("Via: Top -> Bottom at 6.000 mm, 2.000 mm", viewModel.BoardSelectionSummary);
    }

    [Fact]
    public void BoardComponentTransformCommandsRotateAndMirrorSelection()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 20);
        viewModel.ComponentManager.SelectedComponent = Assert.Single(
            viewModel.ComponentManager.Components,
            row => row.DisplayName.Contains("RESISTOR", StringComparison.Ordinal));
        viewModel.PlaceSelectedComponentCommand.Execute(null);
        viewModel.HandleSchematicCanvasClick(new CadPoint(0, 0));
        viewModel.ShowPcbLayoutTabCommand.Execute(null);
        viewModel.BoardEditor.SelectComponentAt(new CadPoint(0, 0));

        viewModel.RotateSelectedBoardComponentCommand.Execute(null);
        viewModel.MirrorSelectedBoardComponentCommand.Execute(null);

        BoardComponentInstance component = Assert.Single(viewModel.BoardEditor.Components);
        Assert.Equal(90, component.RotationDegrees);
        Assert.True(component.IsMirrored);
        Assert.Contains("Mirrored board component", viewModel.PlacementStatus, StringComparison.Ordinal);
        Assert.Contains("rot 90", viewModel.BoardSelectionSummary, StringComparison.Ordinal);
        Assert.Contains("mirrored", viewModel.BoardSelectionSummary, StringComparison.Ordinal);
    }

    [Fact]
    public void InsertBoardViaIntoSelectedTraceSegmentCommandSplitsTraceAndSwitchesLayer()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);
        viewModel.ActivateBoardRouteToolCommand.Execute(null);
        viewModel.HandleBoardCanvasClick(new CadPoint(0, 0));
        viewModel.FinishBoardRouteCommand.Execute(new CadPoint(4_000_000, 0));
        viewModel.ActivateBoardSelectToolCommand.Execute(null);
        viewModel.HandleBoardCanvasClick(new CadPoint(2_000_000, 0));

        viewModel.InsertBoardViaIntoSelectedTraceSegmentCommand.Execute(new CadPoint(2_200_000, 1_700_000));

        BoardVia via = Assert.Single(viewModel.BoardEditor.Vias);
        BoardTrace trace = Assert.Single(viewModel.BoardEditor.Traces);
        Assert.Equal(new CadPoint(2_000_000, 2_000_000), via.Position);
        Assert.Contains(via.Position, trace.RoutePoints);
        Assert.Equal("Bottom", viewModel.SelectedBoardLayerName);
        Assert.Contains("Inserted via", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void SelectedBoardTraceWidthPropertyEditsSelectedTraceWidth()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);
        viewModel.ActivateBoardRouteToolCommand.Execute(null);
        viewModel.HandleBoardCanvasClick(new CadPoint(0, 0));
        viewModel.FinishBoardRouteCommand.Execute(new CadPoint(4_000_000, 0));
        viewModel.ActivateBoardSelectToolCommand.Execute(null);
        viewModel.HandleBoardCanvasClick(new CadPoint(2_000_000, 0));

        viewModel.SelectedBoardTraceWidthMillimeters = "0.5";

        Assert.Equal(500_000, Assert.Single(viewModel.BoardEditor.Traces).WidthInternal);
        Assert.Equal("0.500", viewModel.SelectedBoardTraceWidthMillimeters);
        Assert.Contains("trace width", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void Load7805SampleCreatesWiredSchematicAndBoardFootprints()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateFromHawkCadLibraryJson(
            MainWindowViewModel.CuratedHawkCadStarterLibraryJsonForFallback,
            maxBuiltInDevices: 1);

        viewModel.Load7805SampleCommand.Execute(null);

        Assert.Equal("Schematic", viewModel.ActiveWorkspaceTab);
        Assert.Equal(3, viewModel.SchematicEditor.Components.Count);
        Assert.Contains(viewModel.SchematicEditor.Components, component => component.DisplayName.Contains("LM7805", StringComparison.Ordinal));
        Assert.Equal(5, viewModel.SchematicEditor.Wires.Count);
        Assert.Equal(3, viewModel.SchematicEditor.Nets.Count);
        Assert.Equal(3, viewModel.BoardEditor.Components.Count);
        Assert.Equal(5, viewModel.BoardEditor.Airwires.Count);
        Assert.All(viewModel.BoardEditor.Components, component => Assert.NotEmpty(component.FootprintPreview.Pads));
        Assert.Contains("Loaded 7805", viewModel.PlacementStatus, StringComparison.Ordinal);
    }

    [Fact]
    public void FullHawkCadCoreLibraryAssetIsAvailableForShipping()
    {
        FileInfo asset = new(MainWindowViewModel.DefaultHawkCadLibraryPath);

        Assert.True(asset.Exists, $"Expected built-in library asset at {asset.FullName}");
        Assert.True(asset.Length > 100_000_000);
    }

    [Fact]
    public void DesignPreviewExposesMarketplaceIntegrationPanels()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateDesignPreview(maxBuiltInDevices: 1);

        Assert.Empty(viewModel.MarketplaceBomCostRollup.Rows);
        Assert.Equal("Total: $0.00 across 0 components", viewModel.MarketplaceBomCostRollup.TotalSummary);
        Assert.NotEmpty(viewModel.ComponentDeduplicationReview.Rows);
        Assert.NotEmpty(viewModel.TrustedLibraryPromotionQueue.Rows);
        Assert.NotEmpty(viewModel.FabricationOrderingReadiness.Rows);
        Assert.Equal("Live vendor smoke is disabled", viewModel.VendorLiveSmoke.GateStatus);
        Assert.Equal(7, viewModel.MarketplaceIntegrationStatus.Rows.Count);
        Assert.Contains(viewModel.MarketplaceIntegrationStatus.Rows, row => row.SectionLabel == "BOM rollup");
        Assert.Contains(viewModel.MarketplaceIntegrationStatus.Rows, row => row.SectionLabel == "Trusted-library promotion");
    }

    [Fact]
    public void MarketplaceIntegrationPanelsDeriveFromLiveCartState()
    {
        MainWindowViewModel viewModel = MainWindowViewModel.CreateDesignPreview(maxBuiltInDevices: 1);
        List<string?> changedProperties = [];
        viewModel.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

        viewModel.Marketplace.SelectedComponent = viewModel.Marketplace.Components.Single(row => row.Provider == "Digi-Key");
        viewModel.AddSelectedMarketplaceComponentToCartCommand.Execute(null);

        Assert.Single(viewModel.MarketplaceBomCostRollup.Rows);
        Assert.Contains("$0.73", viewModel.MarketplaceBomCostRollup.TotalSummary, StringComparison.Ordinal);
        Assert.Contains(nameof(MainWindowViewModel.MarketplaceBomCostRollup), changedProperties);
        Assert.Contains(nameof(MainWindowViewModel.MarketplaceIntegrationStatus), changedProperties);

        var bomStatus = Assert.Single(viewModel.MarketplaceIntegrationStatus.Rows, row => row.SectionLabel == "BOM rollup");
        Assert.Equal(1, bomStatus.ReadyCount);
        Assert.Equal(0, bomStatus.BlockedCount);
    }

    private static string ComputeSha256(string content) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();
}
