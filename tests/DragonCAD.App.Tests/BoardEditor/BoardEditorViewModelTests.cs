using DragonCAD.App.BoardEditor;
using DragonCAD.App.ComponentManager;
using DragonCAD.App.SchematicEditor;
using DragonCAD.Core.Geometry;

namespace DragonCAD.App.Tests.BoardEditor;

public sealed class BoardEditorViewModelTests
{
    [Fact]
    public void SynchronizeFromSchematicCreatesBoardComponentShellsWithMatchingSyncIds()
    {
        BoardEditorViewModel board = new();
        SchematicComponentInstance schematicComponent = new(
            "sync-1",
            "U1",
            "hawkcad:part",
            "Part",
            new CadPoint(1_000_000, 2_000_000),
            ComponentSymbolPreview.Empty,
            FootprintWithTwoPads());

        board.SynchronizeFromSchematic([schematicComponent]);

        BoardComponentInstance component = Assert.Single(board.Components);
        Assert.Equal("sync-1", component.SyncId);
        Assert.Equal("U1", component.ReferenceDesignator);
        Assert.Equal("hawkcad:part", component.ComponentId);
        Assert.Equal("Part", component.DisplayName);
        Assert.Equal(new CadPoint(0, 0), component.Position);
        Assert.Equal(2, component.FootprintPreview.Pads.Count);
        Assert.Equal("Synchronized 1 board component from schematic.", board.StatusText);
    }

    [Fact]
    public void SynchronizeFromSchematicUpdatesExistingShellInsteadOfDuplicatingIt()
    {
        BoardEditorViewModel board = new();
        SchematicComponentInstance original = new(
            "sync-1",
            "U1",
            "hawkcad:old",
            "Old",
            new CadPoint(0, 0),
            ComponentSymbolPreview.Empty);
        SchematicComponentInstance updated = original with
        {
            ComponentId = "hawkcad:new",
            DisplayName = "New"
        };

        board.SynchronizeFromSchematic([original]);
        board.SynchronizeFromSchematic([updated]);

        BoardComponentInstance component = Assert.Single(board.Components);
        Assert.Equal("sync-1", component.SyncId);
        Assert.Equal("hawkcad:new", component.ComponentId);
        Assert.Equal("New", component.DisplayName);
    }

    [Fact]
    public void SynchronizeFromSchematicAssignsStableBoardPositionsForNewComponents()
    {
        BoardEditorViewModel board = new();
        SchematicComponentInstance first = new(
            "sync-1",
            "U1",
            "hawkcad:first",
            "First",
            new CadPoint(0, 0),
            ComponentSymbolPreview.Empty);
        SchematicComponentInstance second = new(
            "sync-2",
            "U2",
            "hawkcad:second",
            "Second",
            new CadPoint(5_000_000, 0),
            ComponentSymbolPreview.Empty);

        board.SynchronizeFromSchematic([first, second]);

        Assert.Equal(new CadPoint(0, 0), board.Components[0].Position);
        Assert.Equal(new CadPoint(8_000_000, 0), board.Components[1].Position);

        board.SynchronizeFromSchematic([second, first]);

        Assert.Equal(new CadPoint(0, 0), board.Components.Single(component => component.SyncId == "sync-1").Position);
        Assert.Equal(new CadPoint(8_000_000, 0), board.Components.Single(component => component.SyncId == "sync-2").Position);
    }

    [Fact]
    public void SynchronizeFromSchematicCreatesAirwiresForSchematicWires()
    {
        BoardEditorViewModel board = new();
        SchematicComponentInstance first = new(
            "sync-1",
            "U1",
            "hawkcad:first",
            "First",
            new CadPoint(0, 0),
            ComponentSymbolPreview.Empty);
        SchematicComponentInstance second = new(
            "sync-2",
            "U2",
            "hawkcad:second",
            "Second",
            new CadPoint(5_000_000, 0),
            ComponentSymbolPreview.Empty);
        SchematicWire wire = new(
            "wire-1",
            new SchematicPinEndpoint("sync-1", "U1", "OUT", new CadPoint(1_000_000, 0)),
            new SchematicPinEndpoint("sync-2", "U2", "IN", new CadPoint(4_000_000, 0)),
            [new CadPoint(1_000_000, 0), new CadPoint(4_000_000, 0)],
            "N$1");

        board.SynchronizeFromSchematic([first, second], [wire]);

        BoardAirwire airwire = Assert.Single(board.Airwires);
        Assert.Equal("N$1", airwire.NetName);
        Assert.Equal("sync-1", airwire.StartSyncId);
        Assert.Equal("OUT", airwire.StartPinName);
        Assert.Equal(new CadPoint(0, 0), airwire.StartPosition);
        Assert.Equal("sync-2", airwire.EndSyncId);
        Assert.Equal("IN", airwire.EndPinName);
        Assert.Equal(new CadPoint(8_000_000, 0), airwire.EndPosition);
        Assert.Contains("1 airwire", board.StatusText, StringComparison.Ordinal);
    }

    [Fact]
    public void SelectComponentAtSelectsBoardComponentByBodyBounds()
    {
        BoardEditorViewModel board = new();
        board.SynchronizeFromSchematic([
            new SchematicComponentInstance("sync-1", "U1", "hawkcad:first", "First", new CadPoint(0, 0), ComponentSymbolPreview.Empty),
            new SchematicComponentInstance("sync-2", "U2", "hawkcad:second", "Second", new CadPoint(0, 0), ComponentSymbolPreview.Empty)
        ]);

        BoardComponentInstance? selected = board.SelectComponentAt(new CadPoint(8_200_000, 100_000));

        Assert.NotNull(selected);
        Assert.Equal("U2", selected.ReferenceDesignator);
        Assert.Same(selected, board.SelectedComponent);
        Assert.Equal("Selected board component U2.", board.StatusText);
    }

    [Fact]
    public void MoveSelectedComponentToSnapsToBoardGridAndUpdatesAirwireEndpoints()
    {
        BoardEditorViewModel board = new();
        SchematicComponentInstance first = new("sync-1", "U1", "hawkcad:first", "First", new CadPoint(0, 0), ComponentSymbolPreview.Empty);
        SchematicComponentInstance second = new("sync-2", "U2", "hawkcad:second", "Second", new CadPoint(0, 0), ComponentSymbolPreview.Empty);
        SchematicWire wire = new(
            "wire-1",
            new SchematicPinEndpoint("sync-1", "U1", "OUT", new CadPoint(0, 0)),
            new SchematicPinEndpoint("sync-2", "U2", "IN", new CadPoint(0, 0)),
            [new CadPoint(0, 0), new CadPoint(1, 0)],
            "N$1");
        board.SynchronizeFromSchematic([first, second], [wire]);
        board.SelectComponentAt(new CadPoint(8_000_000, 0));

        BoardComponentInstance moved = board.MoveSelectedComponentTo(new CadPoint(12_600_000, 2_400_000));

        Assert.Equal(new CadPoint(13_000_000, 2_000_000), moved.Position);
        Assert.Same(moved, board.SelectedComponent);

        BoardAirwire airwire = Assert.Single(board.Airwires);
        Assert.Equal(new CadPoint(0, 0), airwire.StartPosition);
        Assert.Equal(new CadPoint(13_000_000, 2_000_000), airwire.EndPosition);
        Assert.Equal("Moved U2 to 13.000 mm, 2.000 mm.", board.StatusText);
    }

    [Fact]
    public void BoardRouteToolCreatesOrthogonalTraceOnGrid()
    {
        BoardEditorViewModel board = new();

        Assert.Equal("Top", board.ActiveLayerName);
        Assert.Contains(board.Layers, layer => layer.Name == "Top" && layer.IsVisible);
        Assert.Contains(board.Layers, layer => layer.Name == "Bottom" && layer.IsVisible);

        board.ActivateRouteTool();
        Assert.Equal("Route", board.ActiveTool);
        Assert.True(board.TraceClickAt(new CadPoint(1_200_000, 1_600_000)));
        Assert.NotNull(board.PendingTraceStart);
        Assert.Equal(new CadPoint(1_000_000, 2_000_000), board.PendingTraceStart);

        Assert.True(board.CompleteTraceAt(new CadPoint(5_200_000, -900_000)));

        BoardTrace trace = Assert.Single(board.Traces);
        Assert.Equal("Top", trace.LayerName);
        Assert.Equal(250_000, trace.WidthInternal);
        Assert.Equal(
            [
                new CadPoint(1_000_000, 2_000_000),
                new CadPoint(5_000_000, 2_000_000),
                new CadPoint(5_000_000, -1_000_000)
            ],
            trace.RoutePoints);
        Assert.Null(board.PendingTraceStart);
        Assert.Equal("Routed board trace on Top.", board.StatusText);
    }

    [Fact]
    public void SetSelectedTraceWidthInternalUpdatesSelectedTraceWidthAndStatus()
    {
        BoardEditorViewModel board = new();
        board.ActivateRouteTool();
        board.TraceClickAt(new CadPoint(0, 0));
        board.CompleteTraceAt(new CadPoint(4_000_000, 0));
        board.ActivateSelectTool();
        Assert.True(board.SelectAt(new CadPoint(2_000_000, 0)));

        BoardTrace updated = board.SetSelectedTraceWidthInternal(500_000);

        Assert.Equal(500_000, updated.WidthInternal);
        Assert.Same(updated, board.SelectedTrace);
        Assert.Equal(updated, Assert.Single(board.Traces));
        Assert.Equal("Set selected board trace width to 0.500 mm.", board.StatusText);
    }

    [Fact]
    public void SetSelectedTraceWidthInternalRejectsNonPositiveWidth()
    {
        BoardEditorViewModel board = new();
        board.ActivateRouteTool();
        board.TraceClickAt(new CadPoint(0, 0));
        board.CompleteTraceAt(new CadPoint(4_000_000, 0));
        board.ActivateSelectTool();
        Assert.True(board.SelectAt(new CadPoint(2_000_000, 0)));

        Assert.Throws<ArgumentOutOfRangeException>(() => board.SetSelectedTraceWidthInternal(0));

        BoardTrace trace = Assert.Single(board.Traces);
        Assert.Equal(250_000, trace.WidthInternal);
        Assert.Same(trace, board.SelectedTrace);
    }

    [Fact]
    public void BoardRoutingUsesTheActiveLayerAndLayerVisibilityFiltersRenderedTraces()
    {
        BoardEditorViewModel board = new();

        board.SetActiveLayer("Bottom");
        board.ActivateRouteTool();
        board.TraceClickAt(new CadPoint(0, 0));
        board.CompleteTraceAt(new CadPoint(2_000_000, 0));

        BoardTrace trace = Assert.Single(board.Traces);
        Assert.Equal("Bottom", trace.LayerName);
        Assert.Equal("Bottom", board.ActiveLayerName);
        Assert.Equal("Routed board trace on Bottom.", board.StatusText);

        board.SetLayerVisibility("Bottom", false);

        Assert.Empty(board.VisibleTraces);
        Assert.Contains(board.Layers, layer => layer.Name == "Bottom" && !layer.IsVisible);
    }

    [Fact]
    public void PlaceViaSnapsToGridAndSwitchesActiveRoutingLayer()
    {
        BoardEditorViewModel board = new();
        board.ActivateRouteTool();
        board.TraceClickAt(new CadPoint(0, 0));

        BoardVia via = board.PlaceViaAt(new CadPoint(2_200_000, 1_700_000));

        Assert.Equal(new CadPoint(2_000_000, 2_000_000), via.Position);
        Assert.Equal("Top", via.FromLayerName);
        Assert.Equal("Bottom", via.ToLayerName);
        Assert.Equal("Bottom", board.ActiveLayerName);
        Assert.Equal(via, Assert.Single(board.Vias));
        Assert.Equal(
            [
                new CadPoint(0, 0),
                new CadPoint(2_000_000, 0),
                new CadPoint(2_000_000, 2_000_000)
            ],
            board.PendingTraceRoutePoints);
        Assert.Equal("Placed via and switched routing layer to Bottom.", board.StatusText);
    }

    [Fact]
    public void SelectAtSelectsNearestTraceOrViaAndDeleteRemovesSelection()
    {
        BoardEditorViewModel board = new();
        board.ActivateRouteTool();
        board.TraceClickAt(new CadPoint(0, 0));
        board.CompleteTraceAt(new CadPoint(4_000_000, 0));
        board.PlaceViaAt(new CadPoint(6_000_000, 0));
        board.ActivateSelectTool();

        Assert.True(board.SelectAt(new CadPoint(2_000_000, 120_000)));
        Assert.NotNull(board.SelectedTrace);
        Assert.Null(board.SelectedVia);
        Assert.Equal("Selected board trace on Top.", board.StatusText);

        Assert.True(board.DeleteSelectedBoardObject());
        Assert.Empty(board.Traces);
        Assert.Single(board.Vias);
        Assert.Equal("Deleted selected board trace.", board.StatusText);

        Assert.True(board.SelectAt(new CadPoint(6_100_000, 100_000)));
        Assert.NotNull(board.SelectedVia);
        Assert.Null(board.SelectedTrace);

        Assert.True(board.DeleteSelectedBoardObject());
        Assert.Empty(board.Vias);
        Assert.Equal("Deleted selected via.", board.StatusText);
    }

    [Fact]
    public void DeleteSelectedBoardObjectRemovesSelectedComponentAndRelatedAirwires()
    {
        BoardEditorViewModel board = new();
        SchematicComponentInstance first = new("sync-1", "U1", "hawkcad:first", "First", new CadPoint(0, 0), ComponentSymbolPreview.Empty);
        SchematicComponentInstance second = new("sync-2", "U2", "hawkcad:second", "Second", new CadPoint(0, 0), ComponentSymbolPreview.Empty);
        SchematicWire wire = new(
            "wire-1",
            new SchematicPinEndpoint("sync-1", "U1", "OUT", new CadPoint(0, 0)),
            new SchematicPinEndpoint("sync-2", "U2", "IN", new CadPoint(0, 0)),
            [new CadPoint(0, 0), new CadPoint(1, 0)],
            "N$1");
        board.SynchronizeFromSchematic([first, second], [wire]);
        Assert.True(board.SelectAt(new CadPoint(8_000_000, 0)));

        Assert.True(board.DeleteSelectedBoardObject());

        BoardComponentInstance remaining = Assert.Single(board.Components);
        Assert.Equal("U1", remaining.ReferenceDesignator);
        Assert.Empty(board.Airwires);
        Assert.Null(board.SelectedComponent);
        Assert.Equal("Deleted board component U2 and 1 related airwire.", board.StatusText);
    }

    [Fact]
    public void BoardRouteToolSupportsIntermediateSegments()
    {
        BoardEditorViewModel board = new();

        board.ActivateRouteTool();
        board.TraceClickAt(new CadPoint(0, 0));
        board.TraceClickAt(new CadPoint(2_100_000, 2_300_000));
        board.CompleteTraceAt(new CadPoint(6_000_000, 2_000_000));

        BoardTrace trace = Assert.Single(board.Traces);
        Assert.Equal(
            [
                new CadPoint(0, 0),
                new CadPoint(2_000_000, 0),
                new CadPoint(2_000_000, 2_000_000),
                new CadPoint(6_000_000, 2_000_000)
            ],
            trace.RoutePoints);
    }

    [Fact]
    public void MoveSelectedViaToSnapsToBoardGrid()
    {
        BoardEditorViewModel board = new();
        BoardVia via = board.PlaceViaAt(new CadPoint(2_200_000, 1_700_000));
        board.ActivateSelectTool();
        Assert.True(board.SelectAt(via.Position));

        BoardVia moved = board.MoveSelectedViaTo(new CadPoint(5_200_000, -900_000));

        Assert.Equal(new CadPoint(5_000_000, -1_000_000), moved.Position);
        Assert.Same(moved, board.SelectedVia);
        Assert.Equal(moved, Assert.Single(board.Vias));
        Assert.Equal("Moved via to 5.000 mm, -1.000 mm.", board.StatusText);
    }

    [Fact]
    public void SetSelectedViaSizeInternalUpdatesDiameterDrillAndStatus()
    {
        BoardEditorViewModel board = new();
        BoardVia via = board.PlaceViaAt(new CadPoint(2_000_000, 2_000_000));
        board.ActivateSelectTool();
        Assert.True(board.SelectAt(via.Position));

        BoardVia resized = board.SetSelectedViaSizeInternal(900_000, 400_000);

        Assert.Equal(900_000, resized.DiameterInternal);
        Assert.Equal(400_000, resized.DrillInternal);
        Assert.Same(resized, board.SelectedVia);
        Assert.Equal(resized, Assert.Single(board.Vias));
        Assert.Equal("Set selected via size to 0.900 mm diameter, 0.400 mm drill.", board.StatusText);
    }

    [Fact]
    public void SetSelectedViaSizeInternalRejectsInvalidSizes()
    {
        BoardEditorViewModel board = new();
        BoardVia via = board.PlaceViaAt(new CadPoint(2_000_000, 2_000_000));
        board.ActivateSelectTool();
        Assert.True(board.SelectAt(via.Position));

        Assert.Throws<ArgumentOutOfRangeException>(() => board.SetSelectedViaSizeInternal(0, 350_000));
        Assert.Throws<ArgumentOutOfRangeException>(() => board.SetSelectedViaSizeInternal(800_000, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => board.SetSelectedViaSizeInternal(400_000, 400_000));

        BoardVia unchanged = Assert.Single(board.Vias);
        Assert.Equal(800_000, unchanged.DiameterInternal);
        Assert.Equal(350_000, unchanged.DrillInternal);
        Assert.Same(unchanged, board.SelectedVia);
    }

    [Fact]
    public void MoveSelectedTraceSegmentToMovesNearestSegmentOnGrid()
    {
        BoardEditorViewModel board = new();
        board.ActivateRouteTool();
        board.TraceClickAt(new CadPoint(0, 0));
        board.TraceClickAt(new CadPoint(2_100_000, 2_300_000));
        board.CompleteTraceAt(new CadPoint(6_000_000, 2_000_000));
        board.ActivateSelectTool();
        Assert.True(board.SelectAt(new CadPoint(2_100_000, 1_200_000)));

        BoardTrace moved = board.MoveSelectedTraceSegmentTo(new CadPoint(3_200_000, 1_200_000));

        Assert.Equal(
            [
                new CadPoint(0, 0),
                new CadPoint(3_000_000, 0),
                new CadPoint(3_000_000, 2_000_000),
                new CadPoint(6_000_000, 2_000_000)
            ],
            moved.RoutePoints);
        Assert.Same(moved, board.SelectedTrace);
        Assert.Equal(moved, Assert.Single(board.Traces));
        Assert.Equal("Moved selected board trace segment.", board.StatusText);
    }

    [Fact]
    public void MoveSelectedTraceToActiveLayerUpdatesLayerAndVisibleTraceFilter()
    {
        BoardEditorViewModel board = new();
        board.ActivateRouteTool();
        board.TraceClickAt(new CadPoint(0, 0));
        board.CompleteTraceAt(new CadPoint(4_000_000, 0));
        board.ActivateSelectTool();
        Assert.True(board.SelectAt(new CadPoint(2_000_000, 0)));

        board.SetActiveLayer("Bottom");
        BoardTrace moved = board.MoveSelectedTraceToActiveLayer();

        Assert.Equal("Bottom", moved.LayerName);
        Assert.Same(moved, board.SelectedTrace);
        Assert.Equal("Moved selected board trace to Bottom.", board.StatusText);

        board.SetLayerVisibility("Bottom", false);
        Assert.Empty(board.VisibleTraces);
    }

    [Fact]
    public void InsertViaIntoSelectedTraceSegmentSplitsRouteAndSwitchesActiveLayer()
    {
        BoardEditorViewModel board = new();
        board.ActivateRouteTool();
        board.TraceClickAt(new CadPoint(0, 0));
        board.TraceClickAt(new CadPoint(4_000_000, 0));
        board.CompleteTraceAt(new CadPoint(4_000_000, 4_000_000));
        board.ActivateSelectTool();
        Assert.True(board.SelectAt(new CadPoint(2_000_000, 120_000)));

        BoardVia via = board.InsertViaIntoSelectedTraceSegment(new CadPoint(2_200_000, 1_700_000));

        Assert.Equal(new CadPoint(2_000_000, 2_000_000), via.Position);
        Assert.Equal("Top", via.FromLayerName);
        Assert.Equal("Bottom", via.ToLayerName);
        Assert.Equal("Bottom", board.ActiveLayerName);
        Assert.Equal(via, Assert.Single(board.Vias));

        BoardTrace trace = Assert.Single(board.Traces);
        Assert.Equal("Top", trace.LayerName);
        Assert.Equal(
            [
                new CadPoint(0, 0),
                new CadPoint(2_000_000, 0),
                new CadPoint(2_000_000, 2_000_000),
                new CadPoint(4_000_000, 2_000_000),
                new CadPoint(4_000_000, 4_000_000)
            ],
            trace.RoutePoints);
        Assert.Same(trace, board.SelectedTrace);
        Assert.Equal(2, board.SelectedTraceSegmentIndex);
        Assert.Equal("Inserted via into selected board trace and switched routing layer to Bottom.", board.StatusText);
    }

    [Fact]
    public void RotateAndMirrorSelectedComponentUpdatesBoardFootprintPlacement()
    {
        BoardEditorViewModel board = new();
        SchematicComponentInstance schematicComponent = new(
            "sync-1",
            "U1",
            "hawkcad:part",
            "Part",
            new CadPoint(0, 0),
            ComponentSymbolPreview.Empty,
            FootprintWithTwoPads());
        board.SynchronizeFromSchematic([schematicComponent]);
        Assert.NotNull(board.SelectComponentAt(new CadPoint(0, 0)));

        BoardComponentInstance rotated = board.RotateSelectedComponentClockwise();
        BoardComponentInstance mirrored = board.MirrorSelectedComponent();

        Assert.Equal(90, rotated.RotationDegrees);
        Assert.Equal(90, mirrored.RotationDegrees);
        Assert.True(mirrored.IsMirrored);
        Assert.Same(mirrored, board.SelectedComponent);
        Assert.Equal(mirrored, Assert.Single(board.Components));
        Assert.Equal("Mirrored board component U1.", board.StatusText);
    }

    private static ComponentFootprintPreview FootprintWithTwoPads() =>
        new(
            new CadRectangle(-1_000_000, -500_000, 1_000_000, 500_000),
            [new ComponentPreviewLine(new CadPoint(-1_000_000, -500_000), new CadPoint(1_000_000, -500_000))],
            [
                new ComponentFootprintPadPreview("1", new CadPoint(-500_000, 0), new CadVector(400_000, 300_000), "Round", "ThroughHole"),
                new ComponentFootprintPadPreview("2", new CadPoint(500_000, 0), new CadVector(400_000, 300_000), "Round", "ThroughHole")
            ]);
}
