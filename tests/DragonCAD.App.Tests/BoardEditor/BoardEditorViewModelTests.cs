using DragonCAD.App.BoardEditor;
using DragonCAD.App.ComponentManager;
using DragonCAD.App.SchematicEditor;
using DragonCAD.Core.Geometry;

namespace DragonCAD.App.Tests.BoardEditor;

public sealed class BoardEditorViewModelTests
{
    [Fact]
    public void ZoomCommandsUpdateBoardZoomLevelWithinBounds()
    {
        BoardEditorViewModel board = new();

        Assert.Equal(new CadPoint(4_000_000, 0), board.ViewportOrigin);

        board.ZoomIn();
        Assert.Equal(1.25, board.ZoomLevel);

        board.ZoomOut();
        Assert.Equal(1.0, board.ZoomLevel);

        for (int index = 0; index < 20; index++)
        {
            board.ZoomOut();
        }

        Assert.Equal(0.25, board.ZoomLevel);

        for (int index = 0; index < 40; index++)
        {
            board.ZoomIn();
        }

        Assert.Equal(8.0, board.ZoomLevel);
    }

    [Fact]
    public void ZoomAtKeepsCursorCadPointAnchored()
    {
        BoardEditorViewModel board = new();

        board.ZoomAt(new CadPoint(12_000_000, -8_000_000), zoomIn: true);

        Assert.Equal(1.25, board.ZoomLevel);
        Assert.Equal(new CadPoint(5_600_000, -1_600_000), board.ViewportOrigin);
        Assert.Contains("Board zoom 1.25x", board.StatusText, StringComparison.Ordinal);
    }

    [Fact]
    public void PanViewportByScreenDeltaMovesBoardUnderCursor()
    {
        BoardEditorViewModel board = new();

        board.PanViewportByScreenDelta(new Avalonia.Vector(50, -25), pixelsPerInternalUnit: 0.000025);

        Assert.Equal(new CadPoint(2_000_000, 1_000_000), board.ViewportOrigin);
        Assert.Contains("Board pan", board.StatusText, StringComparison.Ordinal);
    }

    [Fact]
    public void CenterAndFitBoardCommandsFrameBoardContents()
    {
        BoardEditorViewModel board = new();
        board.SynchronizeFromSchematic([
            new SchematicComponentInstance(
                "sync-1",
                "U1",
                "hawkcad:first",
                "First",
                new CadPoint(0, 0),
                ComponentSymbolPreview.Empty,
                FootprintWithTwoPads()),
            new SchematicComponentInstance(
                "sync-2",
                "U2",
                "hawkcad:second",
                "Second",
                new CadPoint(0, 0),
                ComponentSymbolPreview.Empty,
                FootprintWithTwoPads())
        ]);
        board.PanViewportByScreenDelta(new Avalonia.Vector(50, -25), pixelsPerInternalUnit: 0.000025);
        board.ZoomIn();

        board.CenterBoardContentsInViewport();

        Assert.Equal(new CadPoint(4_000_000, 0), board.ViewportOrigin);
        Assert.Equal(1.25, board.ZoomLevel);
        Assert.Contains("Centered board contents", board.StatusText, StringComparison.Ordinal);

        board.FitBoardContentsToViewport(viewportWidthPixels: 600, viewportHeightPixels: 400, paddingPixels: 40);

        Assert.Equal(new CadPoint(4_000_000, 0), board.ViewportOrigin);
        Assert.Equal(2.08, board.ZoomLevel);
        Assert.Contains("Fit board contents", board.StatusText, StringComparison.Ordinal);
    }

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
        board.SetFreeRouteMode(true);
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
    public void SynchronizeFromSchematicPlacesAirwireEndpointsOnMatchingPads()
    {
        BoardEditorViewModel board = new();
        SchematicComponentInstance first = new(
            "sync-1",
            "U1",
            "hawkcad:first",
            "First",
            new CadPoint(0, 0),
            ComponentSymbolPreview.Empty,
            FootprintWithNamedPads("OUT", "GND"));
        SchematicComponentInstance second = new(
            "sync-2",
            "U2",
            "hawkcad:second",
            "Second",
            new CadPoint(0, 0),
            ComponentSymbolPreview.Empty,
            FootprintWithNamedPads("IN", "GND"));
        SchematicWire wire = new(
            "wire-1",
            new SchematicPinEndpoint("sync-1", "U1", "OUT", new CadPoint(0, 0)),
            new SchematicPinEndpoint("sync-2", "U2", "IN", new CadPoint(0, 0)),
            [new CadPoint(0, 0), new CadPoint(1, 0)],
            "N$1");

        board.SynchronizeFromSchematic([first, second], [wire]);

        BoardAirwire airwire = Assert.Single(board.Airwires);
        Assert.Equal(new CadPoint(-500_000, 0), airwire.StartPosition);
        Assert.Equal(new CadPoint(7_500_000, 0), airwire.EndPosition);
    }

    [Fact]
    public void RouteClickAtInsidePadStartsTraceAtPadCenter()
    {
        BoardEditorViewModel board = BoardWithOneAirwireBetweenNamedPads();

        board.ActivateRouteTool();
        Assert.True(board.TraceClickAt(new CadPoint(-450_000, 80_000)));

        Assert.Equal(new CadPoint(-500_000, 0), board.PendingTraceStart);
        Assert.Equal([new CadPoint(-500_000, 0)], board.PendingTraceRoutePoints);
        Assert.Equal("Started board trace at pad U1.OUT.", board.StatusText);
    }

    [Fact]
    public void CompleteTraceAtMatchingPadRoutesTraceAndRetiresAirwire()
    {
        BoardEditorViewModel board = BoardWithOneAirwireBetweenNamedPads();

        board.ActivateRouteTool();
        board.TraceClickAt(new CadPoint(-450_000, 80_000));

        Assert.True(board.CompleteTraceAt(new CadPoint(7_560_000, -40_000)));

        Assert.Empty(board.Airwires);
        BoardTrace trace = Assert.Single(board.Traces);
        Assert.Equal(
            [
                new CadPoint(-500_000, 0),
                new CadPoint(7_500_000, 0)
            ],
            trace.RoutePoints);
        Assert.Equal("sync-1", trace.StartPadSyncId);
        Assert.Equal("U1", trace.StartPadReferenceDesignator);
        Assert.Equal("OUT", trace.StartPadName);
        Assert.Equal("sync-2", trace.EndPadSyncId);
        Assert.Equal("U2", trace.EndPadReferenceDesignator);
        Assert.Equal("IN", trace.EndPadName);
        Assert.Null(board.PendingTraceStart);
        Assert.Equal("Routed board trace on Top and retired airwire N$1.", board.StatusText);
    }

    [Fact]
    public void DeleteSelectedRetiredTraceRestoresMatchingAirwire()
    {
        BoardEditorViewModel board = BoardWithOneAirwireBetweenNamedPads();
        board.ActivateRouteTool();
        board.TraceClickAt(new CadPoint(-450_000, 80_000));
        board.CompleteTraceAt(new CadPoint(7_560_000, -40_000));

        board.ActivateSelectTool();
        Assert.True(board.SelectAt(new CadPoint(3_500_000, 100_000)));
        Assert.True(board.DeleteSelectedBoardObject());

        BoardAirwire restored = Assert.Single(board.Airwires);
        Assert.Equal("N$1", restored.NetName);
        Assert.Equal("sync-1", restored.StartSyncId);
        Assert.Equal("OUT", restored.StartPinName);
        Assert.Equal("sync-2", restored.EndSyncId);
        Assert.Equal("IN", restored.EndPinName);
        Assert.Empty(board.Traces);
    }

    [Fact]
    public void PartialTraceBetweenMatchingPadsDoesNotRetireAirwire()
    {
        BoardEditorViewModel board = BoardWithOneAirwireBetweenNamedPads();

        board.ActivateRouteTool();
        board.TraceClickAt(new CadPoint(-450_000, 80_000));
        Assert.True(board.TraceClickAt(new CadPoint(3_200_000, 1_100_000)));

        BoardAirwire airwire = Assert.Single(board.Airwires);
        Assert.Equal("N$1", airwire.NetName);
        Assert.Empty(board.Traces);
        Assert.Equal(
            [
                new CadPoint(-500_000, 0),
                new CadPoint(3_000_000, 0),
                new CadPoint(3_000_000, 1_000_000)
            ],
            board.PendingTraceRoutePoints);
    }

    [Fact]
    public void CompleteTraceAtMatchingPadPreservesUnrelatedAirwires()
    {
        BoardEditorViewModel board = BoardWithThreeNamedPadComponents(
            Wire("wire-1", "sync-1", "U1", "OUT", "sync-2", "U2", "IN", "N$1"),
            Wire("wire-2", "sync-1", "U1", "GND", "sync-3", "U3", "GND", "GND"));

        board.ActivateRouteTool();
        board.TraceClickAt(new CadPoint(-450_000, 80_000));
        Assert.True(board.CompleteTraceAt(new CadPoint(7_560_000, -40_000)));

        BoardAirwire remaining = Assert.Single(board.Airwires);
        Assert.Equal("GND", remaining.NetName);
        Assert.Equal("sync-1", remaining.StartSyncId);
        Assert.Equal("GND", remaining.StartPinName);
        Assert.Equal("sync-3", remaining.EndSyncId);
        Assert.Equal("GND", remaining.EndPinName);
    }

    [Fact]
    public void CompleteTraceAtMatchingPadRetiresOnlyOneDuplicateParallelAirwire()
    {
        BoardEditorViewModel board = BoardWithNamedPadAirwires(
            Wire("wire-1", "sync-1", "U1", "OUT", "sync-2", "U2", "IN", "N$1"),
            Wire("wire-2", "sync-1", "U1", "OUT", "sync-2", "U2", "IN", "N$1_DUP"));

        board.ActivateRouteTool();
        board.TraceClickAt(new CadPoint(-450_000, 80_000));
        Assert.True(board.CompleteTraceAt(new CadPoint(7_560_000, -40_000)));

        BoardAirwire remaining = Assert.Single(board.Airwires);
        Assert.Equal("N$1", remaining.NetName);
        Assert.Single(board.Traces);

        board.ActivateSelectTool();
        Assert.True(board.SelectAt(new CadPoint(3_500_000, 100_000)));
        Assert.True(board.DeleteSelectedBoardObject());

        Assert.Equal(["N$1", "N$1_DUP"], board.Airwires.Select(airwire => airwire.NetName).Order());
    }

    [Fact]
    public void CompleteTraceAtUnmatchedPadKeepsRoutePendingAndReportsDiagnostic()
    {
        BoardEditorViewModel board = BoardWithOneAirwireBetweenNamedPads();

        board.ActivateRouteTool();
        board.TraceClickAt(new CadPoint(-450_000, 80_000));

        Assert.False(board.CompleteTraceAt(new CadPoint(8_500_000, 0)));

        Assert.Single(board.Airwires);
        Assert.Empty(board.Traces);
        Assert.Equal(new CadPoint(-500_000, 0), board.PendingTraceStart);
        Assert.Equal([new CadPoint(-500_000, 0)], board.PendingTraceRoutePoints);
        Assert.Equal("Finish at a pad on the same airwire as U1.OUT.", board.StatusText);
    }

    [Fact]
    public void RouteClickAtFreeSpaceIsBlockedUnlessFreeRouteModeIsActive()
    {
        BoardEditorViewModel board = new();

        board.ActivateRouteTool();

        Assert.False(board.TraceClickAt(new CadPoint(1_200_000, 1_600_000)));
        Assert.Null(board.PendingTraceStart);
        Assert.Empty(board.PendingTraceRoutePoints);
        Assert.Equal("Start a board trace from a pad or existing trace endpoint.", board.StatusText);

        board.SetFreeRouteMode(true);

        Assert.True(board.TraceClickAt(new CadPoint(1_200_000, 1_600_000)));
        Assert.Equal(new CadPoint(1_000_000, 2_000_000), board.PendingTraceStart);
        Assert.Equal("Started board trace at 1.000 mm, 2.000 mm.", board.StatusText);
    }

    [Fact]
    public void RouteClickAtExistingTraceEndpointStartsTraceWhenFreeRouteModeIsInactive()
    {
        BoardEditorViewModel board = new();
        board.ActivateRouteTool();
        board.SetFreeRouteMode(true);
        board.TraceClickAt(new CadPoint(0, 0));
        board.CompleteTraceAt(new CadPoint(4_000_000, 0));
        board.SetFreeRouteMode(false);

        Assert.True(board.TraceClickAt(new CadPoint(4_100_000, 80_000)));

        Assert.Equal(new CadPoint(4_000_000, 0), board.PendingTraceStart);
        Assert.Equal([new CadPoint(4_000_000, 0)], board.PendingTraceRoutePoints);
        Assert.Equal("Started board trace at existing route endpoint.", board.StatusText);
    }

    [Fact]
    public void SetSelectedTraceWidthInternalUpdatesSelectedTraceWidthAndStatus()
    {
        BoardEditorViewModel board = new();
        board.ActivateRouteTool();
        board.SetFreeRouteMode(true);
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
        board.SetFreeRouteMode(true);
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
        board.SetFreeRouteMode(true);
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
    public void LayerVisibilityFiltersRenderedVias()
    {
        BoardEditorViewModel board = new();

        board.PlaceViaAt(new CadPoint(0, 0));

        Assert.Single(board.VisibleVias);

        board.SetLayerVisibility("Top", false);
        Assert.Single(board.VisibleVias);

        board.SetLayerVisibility("Bottom", false);
        Assert.Empty(board.VisibleVias);
    }

    [Fact]
    public void PlaceViaSnapsToGridAndSwitchesActiveRoutingLayer()
    {
        BoardEditorViewModel board = new();
        board.ActivateRouteTool();
        board.SetFreeRouteMode(true);
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
        board.SetFreeRouteMode(true);
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
        board.SetFreeRouteMode(true);
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
    public void BoardRouteCornerModeDefaultsToNinetyDegrees()
    {
        BoardEditorViewModel board = new();

        Assert.Equal(["90", "45"], board.RouteCornerModes);
        Assert.Equal("90", board.RouteCornerMode);

        board.ActivateRouteTool();
        board.SetFreeRouteMode(true);
        board.TraceClickAt(new CadPoint(0, 0));
        board.CompleteTraceAt(new CadPoint(2_000_000, 2_000_000));

        BoardTrace trace = Assert.Single(board.Traces);
        Assert.Equal(
            [
                new CadPoint(0, 0),
                new CadPoint(2_000_000, 0),
                new CadPoint(2_000_000, 2_000_000)
            ],
            trace.RoutePoints);
    }

    [Fact]
    public void BoardRouteCornerModeRejectsUnsupportedValues()
    {
        BoardEditorViewModel board = new();

        Assert.Throws<ArgumentOutOfRangeException>(() => board.RouteCornerMode = "free");
        Assert.Throws<ArgumentNullException>(() => board.SetRouteCornerMode(null!));
        Assert.Equal("90", board.RouteCornerMode);
    }

    [Theory]
    [MemberData(nameof(FortyFiveDegreeRouteCases))]
    public void BoardRouteCornerModeGeneratesFortyFiveDegreeRoutes(
        CadPoint target,
        IReadOnlyList<CadPoint> expectedRoutePoints)
    {
        BoardEditorViewModel board = new();

        board.SetRouteCornerMode("45");
        board.ActivateRouteTool();
        board.SetFreeRouteMode(true);
        board.TraceClickAt(new CadPoint(0, 0));
        board.CompleteTraceAt(target);

        BoardTrace trace = Assert.Single(board.Traces);
        Assert.Equal("45", board.RouteCornerMode);
        Assert.Equal(expectedRoutePoints, trace.RoutePoints);
        Assert.Equal("Routed board trace on Top.", board.StatusText);
    }

    [Fact]
    public void BoardRouteCornerModeUsesSameFortyFiveGeneratorForPreviewAndCompletedTrace()
    {
        BoardEditorViewModel board = new();

        board.SetRouteCornerMode("45");
        board.ActivateRouteTool();
        board.SetFreeRouteMode(true);
        board.TraceClickAt(new CadPoint(0, 0));
        board.TraceClickAt(new CadPoint(4_100_000, 2_300_000));

        Assert.Equal(
            [
                new CadPoint(0, 0),
                new CadPoint(2_000_000, 0),
                new CadPoint(4_000_000, 2_000_000)
            ],
            board.PendingTraceRoutePoints);

        board.CompleteTraceAt(new CadPoint(6_200_000, 5_900_000));

        BoardTrace trace = Assert.Single(board.Traces);
        Assert.Equal(
            [
                new CadPoint(0, 0),
                new CadPoint(2_000_000, 0),
                new CadPoint(4_000_000, 2_000_000),
                new CadPoint(4_000_000, 4_000_000),
                new CadPoint(6_000_000, 6_000_000)
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
    public void SelectedViaDiameterAndDrillMillimetersExposeEditableSelectedViaSize()
    {
        BoardEditorViewModel board = new();
        BoardVia via = board.PlaceViaAt(new CadPoint(2_000_000, 2_000_000));
        board.ActivateSelectTool();
        Assert.True(board.SelectAt(via.Position));

        Assert.Equal("0.800", board.SelectedViaDiameterMillimeters);
        Assert.Equal("0.350", board.SelectedViaDrillMillimeters);

        board.SelectedViaDiameterMillimeters = "0.950";
        board.SelectedViaDrillMillimeters = "0.425";

        BoardVia resized = Assert.Single(board.Vias);
        Assert.Equal(950_000, resized.DiameterInternal);
        Assert.Equal(425_000, resized.DrillInternal);
        Assert.Equal("0.950", board.SelectedViaDiameterMillimeters);
        Assert.Equal("0.425", board.SelectedViaDrillMillimeters);
        Assert.Same(resized, board.SelectedVia);
    }

    [Fact]
    public void SelectedViaDiameterAndDrillMillimetersRejectInvalidValuesWithoutChangingVia()
    {
        BoardEditorViewModel board = new();
        BoardVia via = board.PlaceViaAt(new CadPoint(2_000_000, 2_000_000));
        board.ActivateSelectTool();
        Assert.True(board.SelectAt(via.Position));

        board.SelectedViaDiameterMillimeters = "not-a-number";
        Assert.Equal("Via diameter must be a number in millimeters.", board.StatusText);

        board.SelectedViaDrillMillimeters = "0";
        Assert.Contains("Via drill must be positive.", board.StatusText, StringComparison.Ordinal);

        board.SelectedViaDiameterMillimeters = "0.300";
        Assert.Contains("Via drill must be smaller than the diameter.", board.StatusText, StringComparison.Ordinal);

        BoardVia unchanged = Assert.Single(board.Vias);
        Assert.Equal(800_000, unchanged.DiameterInternal);
        Assert.Equal(350_000, unchanged.DrillInternal);
        Assert.Equal("Top", unchanged.FromLayerName);
        Assert.Equal("Bottom", unchanged.ToLayerName);
        Assert.Same(unchanged, board.SelectedVia);
    }

    [Fact]
    public void SetSelectedViaSizeInternalPreservesRouteTransitionMetadata()
    {
        BoardEditorViewModel board = new();
        BoardVia via = board.PlaceViaAt(new CadPoint(2_000_000, 2_000_000));
        board.ActivateSelectTool();
        Assert.True(board.SelectAt(via.Position));

        BoardVia resized = board.SetSelectedViaSizeInternal(900_000, 400_000);

        Assert.Equal(via.ViaId, resized.ViaId);
        Assert.Equal(via.Position, resized.Position);
        Assert.Equal("Top", resized.FromLayerName);
        Assert.Equal("Bottom", resized.ToLayerName);
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
        board.SetFreeRouteMode(true);
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
        board.SetFreeRouteMode(true);
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
        board.SetFreeRouteMode(true);
        board.TraceClickAt(new CadPoint(0, 0));
        board.TraceClickAt(new CadPoint(4_000_000, 0));
        board.CompleteTraceAt(new CadPoint(4_000_000, 4_000_000));
        board.ActivateSelectTool();
        Assert.True(board.SelectAt(new CadPoint(2_000_000, 120_000)));

        BoardVia via = board.InsertViaIntoSelectedTraceSegment(new CadPoint(2_200_000, 1_700_000));

        Assert.Equal(new CadPoint(2_000_000, 0), via.Position);
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
                new CadPoint(4_000_000, 0),
                new CadPoint(4_000_000, 4_000_000)
            ],
            trace.RoutePoints);
        Assert.Same(trace, board.SelectedTrace);
        Assert.Equal(2, board.SelectedTraceSegmentIndex);
        Assert.Equal("Inserted via into selected board trace and switched routing layer to Bottom.", board.StatusText);
    }

    [Fact]
    public void InsertViaIntoSelectedTraceSegmentKeepsViaPointOnStraightSegment()
    {
        BoardEditorViewModel board = new();
        board.ActivateRouteTool();
        board.SetFreeRouteMode(true);
        board.TraceClickAt(new CadPoint(0, 0));
        board.CompleteTraceAt(new CadPoint(4_000_000, 0));
        board.ActivateSelectTool();
        Assert.True(board.SelectAt(new CadPoint(2_000_000, 0)));
        string selectedTraceId = Assert.Single(board.Traces).TraceId;

        BoardVia via = board.InsertViaIntoSelectedTraceSegment(new CadPoint(2_200_000, 100_000));

        Assert.Equal(new CadPoint(2_000_000, 0), via.Position);
        BoardTrace trace = Assert.Single(board.Traces);
        Assert.Equal(selectedTraceId, trace.TraceId);
        Assert.Equal(
            [
                new CadPoint(0, 0),
                new CadPoint(2_000_000, 0),
                new CadPoint(4_000_000, 0)
            ],
            trace.RoutePoints);
        Assert.Same(trace, board.SelectedTrace);
    }

    [Fact]
    public void InsertViaIntoSelectedTraceSegmentReportsDiagnosticWithoutSelection()
    {
        BoardEditorViewModel board = new();

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => board.InsertViaIntoSelectedTraceSegment(new CadPoint(2_000_000, 0)));

        Assert.Equal("No board trace segment is selected.", error.Message);
        Assert.Equal("No board trace segment is selected.", board.StatusText);
        Assert.Empty(board.Vias);
        Assert.Empty(board.Traces);
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
        Assert.NotNull(board.SelectComponentAt(new CadPoint(-500_000, 0)));

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

    public static TheoryData<CadPoint, IReadOnlyList<CadPoint>> FortyFiveDegreeRouteCases() =>
        new()
        {
            {
                new CadPoint(4_000_000, 2_000_000),
                [
                    new CadPoint(0, 0),
                    new CadPoint(2_000_000, 0),
                    new CadPoint(4_000_000, 2_000_000)
                ]
            },
            {
                new CadPoint(2_000_000, 4_000_000),
                [
                    new CadPoint(0, 0),
                    new CadPoint(0, 2_000_000),
                    new CadPoint(2_000_000, 4_000_000)
                ]
            },
            {
                new CadPoint(4_000_000, 0),
                [
                    new CadPoint(0, 0),
                    new CadPoint(4_000_000, 0)
                ]
            },
            {
                new CadPoint(0, 4_000_000),
                [
                    new CadPoint(0, 0),
                    new CadPoint(0, 4_000_000)
                ]
            },
            {
                new CadPoint(2_000_000, 2_000_000),
                [
                    new CadPoint(0, 0),
                    new CadPoint(2_000_000, 2_000_000)
                ]
            }
        };

    private static BoardEditorViewModel BoardWithOneAirwireBetweenNamedPads() =>
        BoardWithNamedPadAirwires(Wire("wire-1", "sync-1", "U1", "OUT", "sync-2", "U2", "IN", "N$1"));

    private static BoardEditorViewModel BoardWithNamedPadAirwires(params SchematicWire[] wires)
    {
        BoardEditorViewModel board = new();
        SchematicComponentInstance first = new(
            "sync-1",
            "U1",
            "hawkcad:first",
            "First",
            new CadPoint(0, 0),
            ComponentSymbolPreview.Empty,
            FootprintWithNamedPads("OUT", "GND"));
        SchematicComponentInstance second = new(
            "sync-2",
            "U2",
            "hawkcad:second",
            "Second",
            new CadPoint(0, 0),
            ComponentSymbolPreview.Empty,
            FootprintWithNamedPads("IN", "GND"));
        board.SynchronizeFromSchematic([first, second], wires);
        return board;
    }

    private static BoardEditorViewModel BoardWithThreeNamedPadComponents(params SchematicWire[] wires)
    {
        BoardEditorViewModel board = new();
        SchematicComponentInstance first = new(
            "sync-1",
            "U1",
            "hawkcad:first",
            "First",
            new CadPoint(0, 0),
            ComponentSymbolPreview.Empty,
            FootprintWithNamedPads("OUT", "GND"));
        SchematicComponentInstance second = new(
            "sync-2",
            "U2",
            "hawkcad:second",
            "Second",
            new CadPoint(0, 0),
            ComponentSymbolPreview.Empty,
            FootprintWithNamedPads("IN", "GND"));
        SchematicComponentInstance third = new(
            "sync-3",
            "U3",
            "hawkcad:third",
            "Third",
            new CadPoint(0, 0),
            ComponentSymbolPreview.Empty,
            FootprintWithNamedPads("IN", "GND"));
        board.SynchronizeFromSchematic([first, second, third], wires);
        return board;
    }

    private static SchematicWire Wire(
        string wireId,
        string startSyncId,
        string startReferenceDesignator,
        string startPinName,
        string endSyncId,
        string endReferenceDesignator,
        string endPinName,
        string netName) =>
        new(
            wireId,
            new SchematicPinEndpoint(startSyncId, startReferenceDesignator, startPinName, new CadPoint(0, 0)),
            new SchematicPinEndpoint(endSyncId, endReferenceDesignator, endPinName, new CadPoint(0, 0)),
            [new CadPoint(0, 0), new CadPoint(1, 0)],
            netName);

    private static ComponentFootprintPreview FootprintWithNamedPads(string leftPadName, string rightPadName) =>
        new(
            new CadRectangle(-1_000_000, -500_000, 1_000_000, 500_000),
            [new ComponentPreviewLine(new CadPoint(-1_000_000, -500_000), new CadPoint(1_000_000, -500_000))],
            [
                new ComponentFootprintPadPreview(leftPadName, new CadPoint(-500_000, 0), new CadVector(400_000, 300_000), "Round", "ThroughHole"),
                new ComponentFootprintPadPreview(rightPadName, new CadPoint(500_000, 0), new CadVector(400_000, 300_000), "Round", "ThroughHole")
            ]);
}
