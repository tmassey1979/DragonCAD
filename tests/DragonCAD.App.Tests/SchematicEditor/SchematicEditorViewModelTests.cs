using DragonCAD.App.ComponentManager;
using DragonCAD.App.Placement;
using DragonCAD.App.SchematicEditor;
using DragonCAD.Core.Geometry;

namespace DragonCAD.App.Tests.SchematicEditor;

public sealed class SchematicEditorViewModelTests
{
    [Fact]
    public void PlaceComponentSnapsToGridAndAssignsStableReferenceDesignator()
    {
        SchematicEditorViewModel editor = new();
        ComponentPlacementIntent intent = new(
            "hawkcad:sparkfun-eagle-libraries/SparkFun-Resistors/RESISTOR-0603",
            "sparkfun-eagle-libraries/SparkFun-Resistors/RESISTOR-0603",
            SymbolCount: 1,
            FootprintCount: 1,
            Source: "BuiltIn");

        SchematicComponentInstance instance = editor.PlaceComponent(intent, new CadPoint(1_300_000, 2_800_000));

        Assert.Equal("U1", instance.ReferenceDesignator);
        Assert.Equal(intent.ComponentId, instance.ComponentId);
        Assert.Equal(new CadPoint(1_000_000, 3_000_000), instance.Position);
        Assert.Single(editor.Components);
        Assert.Equal("Placed U1: sparkfun-eagle-libraries/SparkFun-Resistors/RESISTOR-0603", editor.StatusText);
    }

    [Fact]
    public void PlaceComponentIncrementsReferenceDesignators()
    {
        SchematicEditorViewModel editor = new();
        ComponentPlacementIntent first = new("hawkcad:first", "First", 1, 1, "BuiltIn");
        ComponentPlacementIntent second = new("hawkcad:second", "Second", 1, 1, "BuiltIn");

        editor.PlaceComponent(first, new CadPoint(0, 0));
        SchematicComponentInstance instance = editor.PlaceComponent(second, new CadPoint(1_000_000, 0));

        Assert.Equal("U2", instance.ReferenceDesignator);
        Assert.Equal(2, editor.Components.Count);
    }

    [Fact]
    public void PlaceComponentSelectsTheNewInstance()
    {
        SchematicEditorViewModel editor = new();
        ComponentPlacementIntent intent = new("hawkcad:first", "First", 1, 1, "BuiltIn");

        SchematicComponentInstance instance = editor.PlaceComponent(intent, new CadPoint(0, 0));

        Assert.Same(instance, editor.SelectedComponent);
    }

    [Fact]
    public void SelectComponentAtSelectsTheInstanceWhoseSymbolBoundsContainThePoint()
    {
        SchematicEditorViewModel editor = new();
        SchematicComponentInstance first = editor.PlaceComponent(
            IntentWithBounds("hawkcad:first", "First", -1_000_000, -1_000_000, 1_000_000, 1_000_000),
            new CadPoint(0, 0));
        SchematicComponentInstance second = editor.PlaceComponent(
            IntentWithBounds("hawkcad:second", "Second", -1_000_000, -1_000_000, 1_000_000, 1_000_000),
            new CadPoint(5_000_000, 0));

        SchematicComponentInstance? selected = editor.SelectComponentAt(new CadPoint(5_500_000, 500_000));

        Assert.Same(second, selected);
        Assert.Same(second, editor.SelectedComponent);
        Assert.NotSame(first, editor.SelectedComponent);
        Assert.Equal("Selected U2: Second", editor.StatusText);
    }

    [Fact]
    public void MoveSelectedComponentToSnapsAndPreservesIdentity()
    {
        SchematicEditorViewModel editor = new();
        SchematicComponentInstance original = editor.PlaceComponent(
            IntentWithBounds("hawkcad:first", "First", -1_000_000, -1_000_000, 1_000_000, 1_000_000),
            new CadPoint(0, 0));

        SchematicComponentInstance moved = editor.MoveSelectedComponentTo(new CadPoint(2_400_000, -1_600_000));

        Assert.Equal(original.InstanceId, moved.InstanceId);
        Assert.Equal(original.ReferenceDesignator, moved.ReferenceDesignator);
        Assert.Equal(new CadPoint(2_000_000, -2_000_000), moved.Position);
        Assert.Same(moved, editor.SelectedComponent);
        Assert.Equal(moved, Assert.Single(editor.Components));
        Assert.Equal("Moved U1 to 2.000 mm, -2.000 mm", editor.StatusText);
    }

    [Fact]
    public void MoveSelectedComponentToMovesAttachedWireEndpointWithPin()
    {
        SchematicEditorViewModel editor = new();
        SchematicComponentInstance first = editor.PlaceComponent(
            IntentWithPin("hawkcad:first", "First", "OUT", new CadPoint(1_000_000, 0)),
            new CadPoint(0, 0));
        editor.PlaceComponent(
            IntentWithPin("hawkcad:second", "Second", "IN", new CadPoint(-1_000_000, 0)),
            new CadPoint(5_000_000, 0));
        editor.TraceClickAt(new CadPoint(1_000_000, 0));
        editor.TraceClickAt(new CadPoint(2_000_000, 2_000_000));
        editor.TraceClickAt(new CadPoint(4_000_000, 0));
        editor.SelectedComponent = first;

        editor.MoveSelectedComponentTo(new CadPoint(2_000_000, 1_000_000));

        SchematicWire wire = Assert.Single(editor.Wires);
        Assert.Equal(new CadPoint(3_000_000, 1_000_000), wire.Start.Position);
        Assert.Equal(
            [
                new CadPoint(3_000_000, 1_000_000),
                new CadPoint(3_000_000, 0),
                new CadPoint(2_000_000, 0),
                new CadPoint(2_000_000, 2_000_000),
                new CadPoint(4_000_000, 2_000_000),
                new CadPoint(4_000_000, 0)
            ],
            wire.RoutePoints);
    }

    [Fact]
    public void MoveSelectedComponentToMovesAttachedWireEndWithPin()
    {
        SchematicEditorViewModel editor = new();
        editor.PlaceComponent(
            IntentWithPin("hawkcad:first", "First", "OUT", new CadPoint(1_000_000, 0)),
            new CadPoint(0, 0));
        SchematicComponentInstance second = editor.PlaceComponent(
            IntentWithPin("hawkcad:second", "Second", "IN", new CadPoint(-1_000_000, 0)),
            new CadPoint(5_000_000, 0));
        editor.TraceClickAt(new CadPoint(1_000_000, 0));
        editor.TraceClickAt(new CadPoint(2_000_000, 2_000_000));
        editor.TraceClickAt(new CadPoint(4_000_000, 0));
        editor.SelectedComponent = second;

        editor.MoveSelectedComponentTo(new CadPoint(7_000_000, -1_000_000));

        SchematicWire wire = Assert.Single(editor.Wires);
        Assert.Equal(new CadPoint(6_000_000, -1_000_000), wire.End.Position);
        Assert.Equal(new CadPoint(6_000_000, -1_000_000), wire.RoutePoints[^1]);
        Assert.Equal(new CadPoint(1_000_000, 0), wire.RoutePoints[0]);
    }

    [Fact]
    public void UpdateSelectedComponentPropertiesRenamesReferenceNameAndValue()
    {
        SchematicEditorViewModel editor = new();
        SchematicComponentInstance placed = editor.PlaceComponent(
            IntentWithPin("hawkcad:regulator", "7805 Regulator", "IN", new CadPoint(-1_000_000, 0)),
            new CadPoint(0, 0));
        editor.SelectedComponent = placed;

        SchematicComponentInstance updated = editor.UpdateSelectedComponentProperties("U5", "LM7805", "5V regulator");

        Assert.Equal(placed.InstanceId, updated.InstanceId);
        Assert.Equal("U5", updated.ReferenceDesignator);
        Assert.Equal("LM7805", updated.DisplayName);
        Assert.Equal("5V regulator", updated.Value);
        Assert.Same(updated, editor.SelectedComponent);
        Assert.Equal(updated, Assert.Single(editor.Components));
        Assert.Equal("Updated U5 properties.", editor.StatusText);
    }

    [Fact]
    public void UpdateSelectedComponentPropertiesRejectsBlankReferenceDesignator()
    {
        SchematicEditorViewModel editor = new();
        SchematicComponentInstance placed = editor.PlaceComponent(
            IntentWithPin("hawkcad:regulator", "7805 Regulator", "IN", new CadPoint(-1_000_000, 0)),
            new CadPoint(0, 0));
        editor.SelectedComponent = placed;

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => editor.UpdateSelectedComponentProperties("   ", "LM7805", "5V regulator"));

        Assert.Equal("Reference designator is required.", error.Message);
        Assert.Same(placed, editor.SelectedComponent);
    }

    [Fact]
    public void RotateSelectedComponentClockwiseRotatesPinsAndAttachedWires()
    {
        SchematicEditorViewModel editor = new();
        SchematicComponentInstance first = editor.PlaceComponent(
            IntentWithPin("hawkcad:first", "First", "OUT", new CadPoint(1_000_000, 0)),
            new CadPoint(0, 0));
        editor.PlaceComponent(
            IntentWithPin("hawkcad:second", "Second", "IN", new CadPoint(-1_000_000, 0)),
            new CadPoint(5_000_000, 0));
        editor.TraceClickAt(new CadPoint(1_000_000, 0));
        editor.TraceClickAt(new CadPoint(4_000_000, 0));
        editor.SelectedComponent = first;

        SchematicComponentInstance rotated = editor.RotateSelectedComponentClockwise();

        Assert.Equal(90, rotated.RotationDegrees);
        SchematicWire wire = Assert.Single(editor.Wires);
        Assert.Equal(new CadPoint(0, 1_000_000), wire.Start.Position);
        Assert.Equal(new CadPoint(0, 1_000_000), wire.RoutePoints[0]);
        Assert.All(
            wire.RoutePoints.Zip(wire.RoutePoints.Skip(1)),
            pair => Assert.True(pair.First.X == pair.Second.X || pair.First.Y == pair.Second.Y));
        Assert.Equal("Rotated U1 to 90 degrees.", editor.StatusText);
    }

    [Fact]
    public void MirrorSelectedComponentFlipsPinsAcrossLocalYAxisAndRefreshesAttachedWires()
    {
        SchematicEditorViewModel editor = new();
        SchematicComponentInstance first = editor.PlaceComponent(
            IntentWithPin("hawkcad:first", "First", "OUT", new CadPoint(1_000_000, 0)),
            new CadPoint(0, 0));
        editor.PlaceComponent(
            IntentWithPin("hawkcad:second", "Second", "IN", new CadPoint(-1_000_000, 0)),
            new CadPoint(-5_000_000, 0));
        editor.TraceClickAt(new CadPoint(1_000_000, 0));
        editor.TraceClickAt(new CadPoint(-6_000_000, 0));
        editor.SelectedComponent = first;

        SchematicComponentInstance mirrored = editor.MirrorSelectedComponent();

        Assert.True(mirrored.IsMirrored);
        SchematicWire wire = Assert.Single(editor.Wires);
        Assert.Equal(new CadPoint(-1_000_000, 0), wire.Start.Position);
        Assert.Equal(new CadPoint(-1_000_000, 0), wire.RoutePoints[0]);
        Assert.All(
            wire.RoutePoints.Zip(wire.RoutePoints.Skip(1)),
            pair => Assert.True(pair.First.X == pair.Second.X || pair.First.Y == pair.Second.Y));
        Assert.Equal("Mirrored U1.", editor.StatusText);
    }

    [Fact]
    public void PlaceComponentUsesVisibleFallbackSymbolWhenLibraryGeometryIsEmpty()
    {
        SchematicEditorViewModel editor = new();
        ComponentPlacementIntent intent = new("hawkcad:empty", "Empty Geometry Part", 0, 0, "BuiltIn");

        SchematicComponentInstance instance = editor.PlaceComponent(intent, new CadPoint(0, 0));

        Assert.True(instance.SymbolPreview.Lines.Count >= 4);
        Assert.Equal(new CadRectangle(-2_000_000, -1_000_000, 2_000_000, 1_000_000), instance.SymbolPreview.Bounds);
    }

    [Fact]
    public void NewSchematicHasSheetBoundsAndZoomControls()
    {
        SchematicEditorViewModel editor = new();

        Assert.Equal(new CadRectangle(-140_000_000, -100_000_000, 140_000_000, 100_000_000), editor.SheetBounds);
        Assert.Equal(1.0, editor.ZoomLevel);
        Assert.True(editor.IsGridVisible);
        Assert.Equal("Dots", editor.GridStyle);
        Assert.Equal(CadUnit.InternalUnitsPerMillimeter, editor.GridSpacingInternal);

        editor.ZoomIn();
        Assert.Equal(1.25, editor.ZoomLevel);

        editor.ZoomOut();
        Assert.Equal(1.0, editor.ZoomLevel);
    }

    [Fact]
    public void SchematicGridSettingsControlVisibilityStyleSpacingAndSnapping()
    {
        SchematicEditorViewModel editor = new();

        editor.ToggleGridVisibility();
        editor.ToggleGridStyle();
        editor.SetGridSpacingMillimeters(2);
        Assert.Equal("Grid spacing set to 2.000 mm.", editor.StatusText);

        SchematicComponentInstance instance = editor.PlaceComponent(
            IntentWithBounds("hawkcad:first", "First", -1_000_000, -1_000_000, 1_000_000, 1_000_000),
            new CadPoint(3_100_000, 2_900_000));

        Assert.False(editor.IsGridVisible);
        Assert.Equal("Lines", editor.GridStyle);
        Assert.Equal(2 * CadUnit.InternalUnitsPerMillimeter, editor.GridSpacingInternal);
        Assert.Equal(new CadPoint(4_000_000, 2_000_000), instance.Position);
    }

    [Fact]
    public void ConnectPinAtCreatesWireBetweenTwoClickedPins()
    {
        SchematicEditorViewModel editor = new();
        editor.PlaceComponent(
            IntentWithPin("hawkcad:first", "First", "OUT", new CadPoint(1_000_000, 0)),
            new CadPoint(0, 0));
        editor.PlaceComponent(
            IntentWithPin("hawkcad:second", "Second", "IN", new CadPoint(-1_000_000, 0)),
            new CadPoint(5_000_000, 0));

        Assert.True(editor.ConnectPinAt(new CadPoint(1_000_000, 0)));
        Assert.NotNull(editor.PendingWireStart);
        Assert.Equal("Started wire at U1.OUT", editor.StatusText);

        Assert.True(editor.ConnectPinAt(new CadPoint(4_000_000, 0)));

        SchematicWire wire = Assert.Single(editor.Wires);
        Assert.Equal("U1", wire.Start.ReferenceDesignator);
        Assert.Equal("OUT", wire.Start.PinName);
        Assert.Equal("U2", wire.End.ReferenceDesignator);
        Assert.Equal("IN", wire.End.PinName);
        Assert.Null(editor.PendingWireStart);
        Assert.Equal("Connected U1.OUT to U2.IN. Net N$1.", editor.StatusText);
    }

    [Fact]
    public void TraceToolAddsSnappedSegmentsBeforeCompletingAtDestinationPin()
    {
        SchematicEditorViewModel editor = new();
        editor.PlaceComponent(
            IntentWithPin("hawkcad:first", "First", "OUT", new CadPoint(1_000_000, 0)),
            new CadPoint(0, 0));
        editor.PlaceComponent(
            IntentWithPin("hawkcad:second", "Second", "IN", new CadPoint(-1_000_000, 0)),
            new CadPoint(5_000_000, 0));

        Assert.True(editor.TraceClickAt(new CadPoint(1_000_000, 0)));
        Assert.Equal([new CadPoint(1_000_000, 0)], editor.PendingWireRoutePoints);

        Assert.True(editor.TraceClickAt(new CadPoint(1_600_000, 1_700_000)));
        Assert.Equal(
            [new CadPoint(1_000_000, 0), new CadPoint(2_000_000, 0), new CadPoint(2_000_000, 2_000_000)],
            editor.PendingWireRoutePoints);
        Assert.Equal("Added wire segment at 2.000 mm, 2.000 mm", editor.StatusText);

        Assert.True(editor.TraceClickAt(new CadPoint(4_000_000, 0)));

        SchematicWire wire = Assert.Single(editor.Wires);
        Assert.Equal(
            [
                new CadPoint(1_000_000, 0),
                new CadPoint(2_000_000, 0),
                new CadPoint(2_000_000, 2_000_000),
                new CadPoint(4_000_000, 2_000_000),
                new CadPoint(4_000_000, 0)
            ],
            wire.RoutePoints);
        Assert.Empty(editor.PendingWireRoutePoints);
        Assert.Null(editor.PendingWireStart);
        Assert.Equal("Connected U1.OUT to U2.IN. Net N$1.", editor.StatusText);
    }

    [Fact]
    public void TracePreviewSnapsToGridFromLastRoutePointWhileRouting()
    {
        SchematicEditorViewModel editor = new();
        editor.PlaceComponent(
            IntentWithPin("hawkcad:first", "First", "OUT", new CadPoint(1_000_000, 0)),
            new CadPoint(0, 0));

        editor.TraceClickAt(new CadPoint(1_000_000, 0));

        editor.UpdateTracePreviewAt(new CadPoint(1_600_000, 1_700_000));

        Assert.Equal(new CadPoint(2_000_000, 2_000_000), editor.PendingWirePreviewPoint);
        Assert.Equal(
            [new CadPoint(1_000_000, 0), new CadPoint(2_000_000, 0), new CadPoint(2_000_000, 2_000_000)],
            editor.PendingWirePreviewRoutePoints);
    }

    [Fact]
    public void CompletedWireRoutesAreAlwaysOrthogonal()
    {
        SchematicEditorViewModel editor = new();
        editor.PlaceComponent(
            IntentWithPin("hawkcad:first", "First", "OUT", new CadPoint(1_000_000, 0)),
            new CadPoint(0, 0));
        editor.PlaceComponent(
            IntentWithPin("hawkcad:second", "Second", "IN", new CadPoint(-1_000_000, 0)),
            new CadPoint(5_000_000, 0));

        editor.TraceClickAt(new CadPoint(1_000_000, 0));
        editor.TraceClickAt(new CadPoint(1_600_000, 1_700_000));
        editor.TraceClickAt(new CadPoint(4_000_000, 0));

        SchematicWire wire = Assert.Single(editor.Wires);
        Assert.All(
            wire.RoutePoints.Zip(wire.RoutePoints.Skip(1)),
            pair => Assert.True(pair.First.X == pair.Second.X || pair.First.Y == pair.Second.Y));
    }

    [Fact]
    public void HoverPinAtReportsValidWireTargets()
    {
        SchematicEditorViewModel editor = new();
        editor.PlaceComponent(
            IntentWithPin("hawkcad:first", "First", "OUT", new CadPoint(1_000_000, 0)),
            new CadPoint(0, 0));

        SchematicPinEndpoint? pin = editor.UpdateHoveredPinAt(new CadPoint(1_200_000, 200_000));

        Assert.NotNull(pin);
        Assert.Equal("U1", pin.ReferenceDesignator);
        Assert.Equal("OUT", pin.PinName);
        Assert.Equal(pin, editor.HoveredPin);
    }

    [Fact]
    public void UpdateHoverTargetReportsComponentsAndWireSegmentsBeforeSelection()
    {
        SchematicEditorViewModel editor = CreateEditorWithRoutedWire();

        string componentHover = editor.UpdateHoverTargetAt(new CadPoint(-800_000, 800_000));

        Assert.Equal("Component U1: First", componentHover);
        Assert.Equal(componentHover, editor.HoverTargetText);
        Assert.Null(editor.SelectedWire);
        Assert.NotNull(editor.HoveredComponent);

        string wireHover = editor.UpdateHoverTargetAt(new CadPoint(2_000_000, 1_600_000));

        Assert.Equal("Wire N$1 segment 2", wireHover);
        Assert.Equal(wireHover, editor.HoverTargetText);
        Assert.NotNull(editor.HoveredWire);
        Assert.Equal(2, editor.HoveredWireSegmentIndex);
        Assert.Null(editor.SelectedWire);
    }

    [Fact]
    public void SelectPinEndpointAtSelectsNearestPinWithoutSelectingComponent()
    {
        SchematicEditorViewModel editor = new();
        editor.PlaceComponent(
            IntentWithPin("hawkcad:first", "First", "OUT", new CadPoint(1_000_000, 0)),
            new CadPoint(0, 0));
        editor.PlaceComponent(
            IntentWithPin("hawkcad:second", "Second", "IN", new CadPoint(-1_000_000, 0)),
            new CadPoint(3_000_000, 0));

        SchematicPinEndpoint? pin = editor.SelectPinEndpointAt(new CadPoint(1_900_000, 200_000));

        Assert.NotNull(pin);
        Assert.Equal("U2", pin.ReferenceDesignator);
        Assert.Equal("IN", pin.PinName);
        Assert.Equal(new CadPoint(2_000_000, 0), pin.Position);
        Assert.Equal(pin, editor.SelectedPinEndpoint);
        Assert.Null(editor.SelectedComponent);
        Assert.Null(editor.SelectedWire);
        Assert.Null(editor.SelectedWireSegmentIndex);
        Assert.Equal("Selected pin U2.IN", editor.StatusText);
    }

    [Fact]
    public void SelectPinEndpointAtClearsSelectionWhenNoPinIsNear()
    {
        SchematicEditorViewModel editor = new();
        editor.PlaceComponent(
            IntentWithPin("hawkcad:first", "First", "OUT", new CadPoint(1_000_000, 0)),
            new CadPoint(0, 0));
        editor.SelectPinEndpointAt(new CadPoint(1_000_000, 0));

        SchematicPinEndpoint? pin = editor.SelectPinEndpointAt(new CadPoint(8_000_000, 8_000_000));

        Assert.Null(pin);
        Assert.Null(editor.SelectedPinEndpoint);
    }

    [Fact]
    public void PlaceNetLabelCreatesSnappedSelectedLabel()
    {
        SchematicEditorViewModel editor = new();
        editor.PlaceComponent(
            IntentWithPin("hawkcad:first", "First", "OUT", new CadPoint(1_000_000, 0)),
            new CadPoint(0, 0));

        SchematicNetLabel label = editor.PlaceNetLabel("  +5V  ", new CadPoint(1_600_000, -2_400_000));

        Assert.Equal("+5V", label.NetName);
        Assert.Equal(new CadPoint(2_000_000, -2_000_000), label.Position);
        Assert.False(string.IsNullOrWhiteSpace(label.LabelId));
        Assert.Same(label, editor.SelectedNetLabel);
        Assert.Null(editor.SelectedComponent);
        Assert.Null(editor.SelectedWire);
        Assert.Null(editor.SelectedPinEndpoint);
        Assert.Equal(label, Assert.Single(editor.NetLabels));
        Assert.Equal("Placed net label +5V at 2.000 mm, -2.000 mm.", editor.StatusText);
    }

    [Fact]
    public void SelectNetLabelAtSelectsNearestLabelWithinHitArea()
    {
        SchematicEditorViewModel editor = new();
        SchematicNetLabel first = editor.PlaceNetLabel("+5V", new CadPoint(0, 0));
        SchematicNetLabel second = editor.PlaceNetLabel("GND", new CadPoint(4_000_000, 0));

        SchematicNetLabel? selected = editor.SelectNetLabelAt(new CadPoint(4_250_000, 250_000));

        Assert.NotSame(first, selected);
        Assert.Same(second, selected);
        Assert.Same(second, editor.SelectedNetLabel);
        Assert.Equal("Selected net label GND.", editor.StatusText);
    }

    [Fact]
    public void MoveSelectedNetLabelToSnapsAndPreservesIdentity()
    {
        SchematicEditorViewModel editor = new();
        SchematicNetLabel original = editor.PlaceNetLabel("RESET", new CadPoint(0, 0));

        SchematicNetLabel moved = editor.MoveSelectedNetLabelTo(new CadPoint(3_400_000, -1_600_000));

        Assert.Equal(original.LabelId, moved.LabelId);
        Assert.Equal("RESET", moved.NetName);
        Assert.Equal(new CadPoint(3_000_000, -2_000_000), moved.Position);
        Assert.Same(moved, editor.SelectedNetLabel);
        Assert.Equal(moved, Assert.Single(editor.NetLabels));
        Assert.Equal("Moved net label RESET to 3.000 mm, -2.000 mm.", editor.StatusText);
    }

    [Fact]
    public void RenderableNetLabelsExposeSelectionStateForCanvasRendering()
    {
        SchematicEditorViewModel editor = new();
        SchematicNetLabel power = editor.PlaceNetLabel("+5V", new CadPoint(0, 0));
        SchematicNetLabel ground = editor.PlaceNetLabel("GND", new CadPoint(4_000_000, 0));
        editor.SelectNetLabelAt(ground.Position);

        SchematicNetLabelRenderItem[] labels = editor.RenderableNetLabels.ToArray();

        Assert.Collection(
            labels,
            label =>
            {
                Assert.Equal(power.LabelId, label.LabelId);
                Assert.Equal("+5V", label.NetName);
                Assert.Equal(power.Position, label.Position);
                Assert.False(label.IsSelected);
            },
            label =>
            {
                Assert.Equal(ground.LabelId, label.LabelId);
                Assert.Equal("GND", label.NetName);
                Assert.Equal(ground.Position, label.Position);
                Assert.True(label.IsSelected);
            });
    }

    [Fact]
    public void TraceClickAtUsesNearestPinInsideLargerHitArea()
    {
        SchematicEditorViewModel editor = new();
        editor.PlaceComponent(
            IntentWithPin("hawkcad:first", "First", "OUT", new CadPoint(1_000_000, 0)),
            new CadPoint(0, 0));

        Assert.True(editor.TraceClickAt(new CadPoint(2_100_000, 300_000)));

        Assert.NotNull(editor.PendingWireStart);
        Assert.Equal("OUT", editor.PendingWireStart.PinName);
        Assert.Equal("Started wire at U1.OUT", editor.StatusText);
    }

    [Fact]
    public void CancelPendingWireClearsRoutePreviewAndStartPin()
    {
        SchematicEditorViewModel editor = new();
        editor.PlaceComponent(
            IntentWithPin("hawkcad:first", "First", "OUT", new CadPoint(1_000_000, 0)),
            new CadPoint(0, 0));
        editor.TraceClickAt(new CadPoint(1_000_000, 0));
        editor.TraceClickAt(new CadPoint(2_000_000, 2_000_000));
        editor.UpdateTracePreviewAt(new CadPoint(3_000_000, 3_000_000));

        Assert.True(editor.CancelPendingWire());

        Assert.Null(editor.PendingWireStart);
        Assert.Null(editor.PendingWirePreviewPoint);
        Assert.Empty(editor.PendingWireRoutePoints);
        Assert.Empty(editor.PendingWirePreviewRoutePoints);
        Assert.Equal("Cancelled pending wire.", editor.StatusText);
    }

    [Fact]
    public void CompletedWiresCreateNamedNetsForConnectedPins()
    {
        SchematicEditorViewModel editor = new();
        editor.PlaceComponent(
            IntentWithPin("hawkcad:first", "First", "OUT", new CadPoint(1_000_000, 0)),
            new CadPoint(0, 0));
        editor.PlaceComponent(
            IntentWithPin("hawkcad:second", "Second", "IN", new CadPoint(-1_000_000, 0)),
            new CadPoint(5_000_000, 0));

        editor.TraceClickAt(new CadPoint(1_000_000, 0));
        editor.TraceClickAt(new CadPoint(4_000_000, 0));

        SchematicNet net = Assert.Single(editor.Nets);
        Assert.Equal("N$1", net.Name);
        Assert.Equal(["U1.OUT", "U2.IN"], net.PinNames);
        Assert.Contains("Net N$1", editor.StatusText, StringComparison.Ordinal);
    }

    [Fact]
    public void DirectTraceCompletionAddsVisibleDoglegWhenPinsAreVeryClose()
    {
        SchematicEditorViewModel editor = new();
        editor.PlaceComponent(
            IntentWithPin("hawkcad:first", "First", "OUT", new CadPoint(1_000_000, 0)),
            new CadPoint(0, 0));
        editor.PlaceComponent(
            IntentWithPin("hawkcad:second", "Second", "IN", new CadPoint(-1_000_000, 0)),
            new CadPoint(2_800_000, 0));

        editor.TraceClickAt(new CadPoint(1_000_000, 0));
        editor.TraceClickAt(new CadPoint(1_800_000, 0));

        SchematicWire wire = Assert.Single(editor.Wires);
        Assert.Equal(
            [
                new CadPoint(1_000_000, 0),
                new CadPoint(1_000_000, -2_000_000),
                new CadPoint(2_000_000, -2_000_000),
                new CadPoint(2_000_000, 0)
            ],
            wire.RoutePoints);
    }

    [Fact]
    public void SelectWireAtSelectsNearestRouteSegmentAndDeleteRemovesIt()
    {
        SchematicEditorViewModel editor = new();
        editor.PlaceComponent(
            IntentWithPin("hawkcad:first", "First", "OUT", new CadPoint(1_000_000, 0)),
            new CadPoint(0, 0));
        editor.PlaceComponent(
            IntentWithPin("hawkcad:second", "Second", "IN", new CadPoint(-1_000_000, 0)),
            new CadPoint(5_000_000, 0));
        editor.TraceClickAt(new CadPoint(1_000_000, 0));
        editor.TraceClickAt(new CadPoint(2_000_000, 2_000_000));
        editor.TraceClickAt(new CadPoint(4_000_000, 0));

        SchematicWire? selected = editor.SelectWireAt(new CadPoint(2_000_000, 1_600_000));

        Assert.NotNull(selected);
        Assert.Same(selected, editor.SelectedWire);
        Assert.Equal(2, editor.SelectedWireSegmentIndex);

        Assert.True(editor.DeleteSelectedWire());
        Assert.Empty(editor.Wires);
        Assert.Empty(editor.Nets);
    }

    [Fact]
    public void DeleteSelectedWireSegmentRemovesMiddleSegmentAndPreservesOrthogonalRoute()
    {
        SchematicEditorViewModel editor = CreateEditorWithRoutedWire();
        editor.SelectWireAt(new CadPoint(2_000_000, 1_000_000));

        SchematicWire updated = editor.DeleteSelectedWireSegment();

        Assert.Equal(
            [
                new CadPoint(1_000_000, 0),
                new CadPoint(4_000_000, 0)
            ],
            updated.RoutePoints);
        Assert.Equal(updated, editor.SelectedWire);
        Assert.Null(editor.SelectedWireSegmentIndex);
        Assert.Single(editor.Wires);
        Assert.Single(editor.Nets);
        Assert.Equal("Deleted wire segment 2 on N$1.", editor.StatusText);
    }

    [Fact]
    public void DeleteSelectedWireSegmentRemovesFirstSegmentWithoutMovingEndpoints()
    {
        SchematicEditorViewModel editor = CreateEditorWithRoutedWire();
        editor.SelectWireAt(new CadPoint(1_500_000, 0));

        SchematicWire updated = editor.DeleteSelectedWireSegment();

        Assert.Equal(new CadPoint(1_000_000, 0), updated.RoutePoints[0]);
        Assert.Equal(new CadPoint(4_000_000, 0), updated.RoutePoints[^1]);
        Assert.All(AdjacentSegments(updated), segment => Assert.True(IsOrthogonal(segment.Start, segment.End)));
        Assert.Single(editor.Wires);
    }

    [Fact]
    public void DeleteSelectedWireSegmentRemovesLastSegmentWithoutMovingEndpoints()
    {
        SchematicEditorViewModel editor = CreateEditorWithRoutedWire();
        editor.SelectWireAt(new CadPoint(4_000_000, 1_000_000));

        SchematicWire updated = editor.DeleteSelectedWireSegment();

        Assert.Equal(new CadPoint(1_000_000, 0), updated.RoutePoints[0]);
        Assert.Equal(new CadPoint(4_000_000, 0), updated.RoutePoints[^1]);
        Assert.All(AdjacentSegments(updated), segment => Assert.True(IsOrthogonal(segment.Start, segment.End)));
        Assert.Single(editor.Wires);
    }

    [Fact]
    public void MoveSelectedWireSegmentToMovesHorizontalSegmentOnGrid()
    {
        SchematicEditorViewModel editor = new();
        editor.PlaceComponent(
            IntentWithPin("hawkcad:first", "First", "OUT", new CadPoint(1_000_000, 0)),
            new CadPoint(0, 0));
        editor.PlaceComponent(
            IntentWithPin("hawkcad:second", "Second", "IN", new CadPoint(-1_000_000, 0)),
            new CadPoint(5_000_000, 0));
        editor.TraceClickAt(new CadPoint(1_000_000, 0));
        editor.TraceClickAt(new CadPoint(2_000_000, 2_000_000));
        editor.TraceClickAt(new CadPoint(4_000_000, 0));
        editor.SelectWireAt(new CadPoint(3_000_000, 2_000_000));

        SchematicWire moved = editor.MoveSelectedWireSegmentTo(new CadPoint(3_200_000, 3_400_000));

        Assert.Equal(3, editor.SelectedWireSegmentIndex);
        Assert.Equal(
            [
                new CadPoint(1_000_000, 0),
                new CadPoint(2_000_000, 0),
                new CadPoint(2_000_000, 3_000_000),
                new CadPoint(4_000_000, 3_000_000),
                new CadPoint(4_000_000, 0)
            ],
            moved.RoutePoints);
    }

    [Fact]
    public void MoveSelectedWireSegmentToMovesVerticalSegmentOnGrid()
    {
        SchematicEditorViewModel editor = new();
        editor.PlaceComponent(
            IntentWithPin("hawkcad:first", "First", "OUT", new CadPoint(1_000_000, 0)),
            new CadPoint(0, 0));
        editor.PlaceComponent(
            IntentWithPin("hawkcad:second", "Second", "IN", new CadPoint(-1_000_000, 0)),
            new CadPoint(5_000_000, 0));
        editor.TraceClickAt(new CadPoint(1_000_000, 0));
        editor.TraceClickAt(new CadPoint(2_000_000, 2_000_000));
        editor.TraceClickAt(new CadPoint(4_000_000, 0));
        editor.SelectWireAt(new CadPoint(2_000_000, 1_000_000));

        SchematicWire moved = editor.MoveSelectedWireSegmentTo(new CadPoint(3_400_000, 1_200_000));

        Assert.Equal(2, editor.SelectedWireSegmentIndex);
        Assert.Equal(
            [
                new CadPoint(1_000_000, 0),
                new CadPoint(3_000_000, 0),
                new CadPoint(3_000_000, 2_000_000),
                new CadPoint(4_000_000, 2_000_000),
                new CadPoint(4_000_000, 0)
            ],
            moved.RoutePoints);
    }

    [Fact]
    public void InsertVertexIntoSelectedWireSegmentAddsSnappedOrthogonalCorner()
    {
        SchematicEditorViewModel editor = CreateEditorWithRoutedWire();
        editor.SelectWireAt(new CadPoint(3_000_000, 2_000_000));

        SchematicWire updated = editor.InsertVertexIntoSelectedWireSegment(new CadPoint(3_200_000, 3_400_000));

        Assert.Equal(
            [
                new CadPoint(1_000_000, 0),
                new CadPoint(2_000_000, 0),
                new CadPoint(2_000_000, 2_000_000),
                new CadPoint(3_000_000, 2_000_000),
                new CadPoint(3_000_000, 3_000_000),
                new CadPoint(4_000_000, 3_000_000),
                new CadPoint(4_000_000, 0)
            ],
            updated.RoutePoints);
        Assert.Equal(4, editor.SelectedWireSegmentIndex);
        Assert.All(AdjacentSegments(updated), segment => Assert.True(IsOrthogonal(segment.Start, segment.End)));
        Assert.Equal("Inserted wire vertex on N$1.", editor.StatusText);
    }

    [Fact]
    public void RenameSelectedWireNetUpdatesTheWireAndNet()
    {
        SchematicEditorViewModel editor = new();
        editor.PlaceComponent(
            IntentWithPin("hawkcad:first", "First", "OUT", new CadPoint(1_000_000, 0)),
            new CadPoint(0, 0));
        editor.PlaceComponent(
            IntentWithPin("hawkcad:second", "Second", "IN", new CadPoint(-1_000_000, 0)),
            new CadPoint(5_000_000, 0));
        editor.TraceClickAt(new CadPoint(1_000_000, 0));
        editor.TraceClickAt(new CadPoint(4_000_000, 0));
        editor.SelectWireAt(new CadPoint(2_000_000, 0));

        SchematicWire renamed = editor.RenameSelectedWireNet("  +5V  ");

        Assert.Equal("+5V", renamed.NetName);
        Assert.Equal("+5V", renamed.ManualNetName);
        Assert.Equal("+5V", Assert.Single(editor.Nets).Name);
        Assert.Equal("Renamed selected net to +5V.", editor.StatusText);
    }

    [Fact]
    public void DeleteSelectedComponentRemovesAttachedWiresAndNets()
    {
        SchematicEditorViewModel editor = new();
        SchematicComponentInstance first = editor.PlaceComponent(
            IntentWithPin("hawkcad:first", "First", "OUT", new CadPoint(1_000_000, 0)),
            new CadPoint(0, 0));
        SchematicComponentInstance second = editor.PlaceComponent(
            IntentWithPin("hawkcad:second", "Second", "IN", new CadPoint(-1_000_000, 0)),
            new CadPoint(5_000_000, 0));
        editor.TraceClickAt(new CadPoint(1_000_000, 0));
        editor.TraceClickAt(new CadPoint(4_000_000, 0));
        editor.SelectedComponent = first;

        Assert.True(editor.DeleteSelectedComponent());

        Assert.Equal(second, Assert.Single(editor.Components));
        Assert.Empty(editor.Wires);
        Assert.Empty(editor.Nets);
        Assert.Null(editor.SelectedComponent);
        Assert.Equal("Deleted U1 and 1 attached wire.", editor.StatusText);
    }

    [Fact]
    public void DuplicateSelectedComponentCopiesMetadataAndOffsetsOnGrid()
    {
        SchematicEditorViewModel editor = new();
        SchematicComponentInstance placed = editor.PlaceComponent(
            IntentWithPin("hawkcad:regulator", "7805 Regulator", "IN", new CadPoint(-1_000_000, 0)),
            new CadPoint(0, 0));
        editor.SelectedComponent = placed;
        editor.UpdateSelectedComponentProperties("U5", "LM7805", "5V");
        editor.RotateSelectedComponentClockwise();

        SchematicComponentInstance duplicate = editor.DuplicateSelectedComponent();

        Assert.NotEqual(placed.InstanceId, duplicate.InstanceId);
        Assert.Equal("U2", duplicate.ReferenceDesignator);
        Assert.Equal("LM7805", duplicate.DisplayName);
        Assert.Equal("5V", duplicate.Value);
        Assert.Equal(90, duplicate.RotationDegrees);
        Assert.Equal(new CadPoint(5_000_000, 5_000_000), duplicate.Position);
        Assert.Same(duplicate, editor.SelectedComponent);
        Assert.Equal(2, editor.Components.Count);
        Assert.Empty(editor.Wires);
        Assert.Equal("Duplicated U5 as U2.", editor.StatusText);
    }

    private static ComponentPlacementIntent IntentWithBounds(
        string componentId,
        string displayName,
        long left,
        long top,
        long right,
        long bottom) =>
        new(
            componentId,
            displayName,
            SymbolCount: 1,
            FootprintCount: 1,
            Source: "BuiltIn",
            SymbolPreview: new ComponentSymbolPreview(
                new CadRectangle(left, top, right, bottom),
                [new ComponentPreviewLine(new CadPoint(left, top), new CadPoint(right, bottom))],
                []));

    private static ComponentPlacementIntent IntentWithPin(
        string componentId,
        string displayName,
        string pinName,
        CadPoint pinPosition) =>
        new(
            componentId,
            displayName,
            SymbolCount: 1,
            FootprintCount: 1,
            Source: "BuiltIn",
            SymbolPreview: new ComponentSymbolPreview(
                new CadRectangle(-1_000_000, -1_000_000, 1_000_000, 1_000_000),
                [
                    new ComponentPreviewLine(new CadPoint(-1_000_000, -1_000_000), new CadPoint(1_000_000, -1_000_000)),
                    new ComponentPreviewLine(new CadPoint(1_000_000, -1_000_000), new CadPoint(1_000_000, 1_000_000)),
                    new ComponentPreviewLine(new CadPoint(1_000_000, 1_000_000), new CadPoint(-1_000_000, 1_000_000)),
                    new ComponentPreviewLine(new CadPoint(-1_000_000, 1_000_000), new CadPoint(-1_000_000, -1_000_000))
                ],
                [new ComponentSymbolPinPreview(pinName, pinPosition, new CadPoint(0, 0), "Right")]));

    private static SchematicEditorViewModel CreateEditorWithRoutedWire()
    {
        SchematicEditorViewModel editor = new();
        editor.PlaceComponent(
            IntentWithPin("hawkcad:first", "First", "OUT", new CadPoint(1_000_000, 0)),
            new CadPoint(0, 0));
        editor.PlaceComponent(
            IntentWithPin("hawkcad:second", "Second", "IN", new CadPoint(-1_000_000, 0)),
            new CadPoint(5_000_000, 0));
        editor.TraceClickAt(new CadPoint(1_000_000, 0));
        editor.TraceClickAt(new CadPoint(2_000_000, 2_000_000));
        editor.TraceClickAt(new CadPoint(4_000_000, 0));
        return editor;
    }

    private static IEnumerable<(CadPoint Start, CadPoint End)> AdjacentSegments(SchematicWire wire)
    {
        for (int index = 1; index < wire.RoutePoints.Count; index++)
        {
            yield return (wire.RoutePoints[index - 1], wire.RoutePoints[index]);
        }
    }

    private static bool IsOrthogonal(CadPoint start, CadPoint end) =>
        start.X == end.X || start.Y == end.Y;
}
