using DragonCAD.App.BoardEditor;
using DragonCAD.App.ComponentManager;
using DragonCAD.Core.Geometry;

namespace DragonCAD.App.Tests.BoardEditor;

public sealed class BoardFootprintPrimitiveTests
{
    [Theory]
    [MemberData(nameof(FixtureFootprints))]
    public void FixtureFootprintsExposeExpectedPrimitiveFidelity(string fixtureName, BoardComponentInstance component, string[] expectedKinds)
    {
        Assert.NotEmpty(component.FootprintPrimitives);
        foreach (string expectedKind in expectedKinds)
        {
            Assert.Contains(component.FootprintPrimitives, primitive => primitive.Kind == expectedKind);
        }

        Assert.True(component.FootprintBounds.Width > 0, $"{fixtureName} should have footprint bounds.");
        Assert.True(component.FootprintBounds.Height > 0, $"{fixtureName} should have footprint bounds.");
    }

    [Fact]
    public void LayerVisibilityFiltersFootprintPrimitivesByLayerName()
    {
        BoardEditorViewModel board = new();
        BoardComponentInstance connector = UsbMiniConnector();
        board.Components.Add(connector);

        Assert.Contains(board.VisibleFootprintPrimitives(connector), primitive => primitive.LayerName == "Top");
        Assert.Contains(board.VisibleFootprintPrimitives(connector), primitive => primitive.LayerName == "Silkscreen");
        Assert.Contains(board.VisibleFootprintPrimitives(connector), primitive => primitive.LayerName == "Keepout");

        board.SetLayerVisibility("Top", false);
        board.SetLayerVisibility("Keepout", false);

        IReadOnlyList<BoardFootprintPrimitive> visible = board.VisibleFootprintPrimitives(connector);
        Assert.DoesNotContain(visible, primitive => primitive.LayerName == "Top");
        Assert.DoesNotContain(visible, primitive => primitive.LayerName == "Keepout");
        Assert.Contains(visible, primitive => primitive.LayerName == "Silkscreen");
    }

    [Fact]
    public void SelectComponentAtUsesPadHoleAndBodyGeometryInsteadOfOnlyBounds()
    {
        BoardEditorViewModel board = new();
        BoardComponentInstance header = PinHeader();
        board.Components.Add(header);

        Assert.NotNull(board.SelectComponentAt(new CadPoint(-1_270_000, 0)));
        Assert.Equal("J1", board.SelectedComponent?.ReferenceDesignator);

        board.SelectComponentAt(new CadPoint(0, 0));
        Assert.Null(board.SelectedComponent);

        Assert.NotNull(board.SelectComponentAt(new CadPoint(1_270_000, 0)));
        Assert.Equal("J1", board.SelectedComponent?.ReferenceDesignator);
    }

    [Fact]
    public void SelectComponentAtHonorsRotatedSmdPadGeometry()
    {
        BoardEditorViewModel board = new();
        BoardComponentInstance resistor = SmdResistor() with
        {
            RotationDegrees = 90,
            Position = new CadPoint(10_000_000, 0)
        };
        board.Components.Add(resistor);

        Assert.NotNull(board.SelectComponentAt(new CadPoint(10_000_000, -1_300_000)));
        Assert.Equal("R1", board.SelectedComponent?.ReferenceDesignator);

        board.SelectComponentAt(new CadPoint(10_000_000, 0));
        Assert.Null(board.SelectedComponent);
    }

    public static TheoryData<string, BoardComponentInstance, string[]> FixtureFootprints() =>
        new()
        {
            { "TO-220", To220(), ["Pad", "Hole", "Line", "Arc", "Text"] },
            { "DIP", Dip8(), ["Pad", "Hole", "Line", "Text"] },
            { "SMD resistor", SmdResistor(), ["Smd", "Line", "Text"] },
            { "USB connector", UsbMiniConnector(), ["Smd", "Hole", "Keepout", "Line", "Arc", "Text"] },
            { "Pin header", PinHeader(), ["Pad", "Hole", "Line", "Text"] }
        };

    private static BoardComponentInstance To220() =>
        new(
            "sync-q1",
            "Q1",
            "fixture:to220",
            "TO-220",
            Position: default,
            FootprintPreview: ComponentFootprintPreview.Empty,
            FootprintPrimitives:
            [
                BoardFootprintPrimitive.Pad("1", new CadPoint(-2_540_000, 0), new CadVector(1_600_000, 1_600_000), "Round", 900_000, "Top"),
                BoardFootprintPrimitive.Pad("2", new CadPoint(0, 0), new CadVector(1_600_000, 1_600_000), "Round", 900_000, "Top"),
                BoardFootprintPrimitive.Pad("3", new CadPoint(2_540_000, 0), new CadVector(1_600_000, 1_600_000), "Round", 900_000, "Top"),
                BoardFootprintPrimitive.Hole(new CadPoint(-2_540_000, 0), 900_000, "Drills"),
                BoardFootprintPrimitive.Hole(new CadPoint(0, 0), 900_000, "Drills"),
                BoardFootprintPrimitive.Hole(new CadPoint(2_540_000, 0), 900_000, "Drills"),
                BoardFootprintPrimitive.Line(new CadPoint(-5_000_000, -3_000_000), new CadPoint(5_000_000, -3_000_000), "Silkscreen"),
                BoardFootprintPrimitive.Arc(new CadPoint(0, -3_000_000), 1_200_000, 0, 180, "Silkscreen"),
                BoardFootprintPrimitive.Text("Q1", new CadPoint(-5_000_000, 3_600_000), 1_200_000, "Names")
            ]);

    private static BoardComponentInstance Dip8() =>
        new(
            "sync-u1",
            "U1",
            "fixture:dip8",
            "DIP-8",
            Position: default,
            FootprintPreview: ComponentFootprintPreview.Empty,
            FootprintPrimitives:
            [
                BoardFootprintPrimitive.Pad("1", new CadPoint(-3_810_000, -3_810_000), new CadVector(1_300_000, 1_300_000), "Round", 800_000, "Top"),
                BoardFootprintPrimitive.Pad("4", new CadPoint(-3_810_000, 3_810_000), new CadVector(1_300_000, 1_300_000), "Round", 800_000, "Top"),
                BoardFootprintPrimitive.Pad("5", new CadPoint(3_810_000, 3_810_000), new CadVector(1_300_000, 1_300_000), "Round", 800_000, "Top"),
                BoardFootprintPrimitive.Pad("8", new CadPoint(3_810_000, -3_810_000), new CadVector(1_300_000, 1_300_000), "Round", 800_000, "Top"),
                BoardFootprintPrimitive.Hole(new CadPoint(-3_810_000, -3_810_000), 800_000, "Drills"),
                BoardFootprintPrimitive.Line(new CadPoint(-5_000_000, -5_400_000), new CadPoint(5_000_000, -5_400_000), "Silkscreen"),
                BoardFootprintPrimitive.Text("U1", new CadPoint(-5_200_000, 5_700_000), 1_000_000, "Names")
            ]);

    private static BoardComponentInstance SmdResistor() =>
        new(
            "sync-r1",
            "R1",
            "fixture:r0805",
            "0805 resistor",
            Position: default,
            FootprintPreview: ComponentFootprintPreview.Empty,
            FootprintPrimitives:
            [
                BoardFootprintPrimitive.Smd("1", new CadPoint(-1_000_000, 0), new CadVector(900_000, 1_200_000), "Rectangle", "Top"),
                BoardFootprintPrimitive.Smd("2", new CadPoint(1_000_000, 0), new CadVector(900_000, 1_200_000), "Rectangle", "Top"),
                BoardFootprintPrimitive.Line(new CadPoint(-600_000, -800_000), new CadPoint(600_000, -800_000), "Silkscreen"),
                BoardFootprintPrimitive.Text("R1", new CadPoint(-1_700_000, 1_300_000), 800_000, "Names")
            ]);

    private static BoardComponentInstance UsbMiniConnector() =>
        new(
            "sync-j2",
            "J2",
            "fixture:usb-mini",
            "USB Mini-B",
            Position: default,
            FootprintPreview: ComponentFootprintPreview.Empty,
            FootprintPrimitives:
            [
                BoardFootprintPrimitive.Smd("VBUS", new CadPoint(-1_600_000, 4_064_000), new CadVector(500_000, 2_308_000), "Rectangle", "Top"),
                BoardFootprintPrimitive.Smd("D-", new CadPoint(-800_000, 4_064_000), new CadVector(500_000, 2_308_000), "Rectangle", "Top"),
                BoardFootprintPrimitive.Smd("D+", new CadPoint(0, 4_064_000), new CadVector(500_000, 2_308_000), "Rectangle", "Top"),
                BoardFootprintPrimitive.Hole(new CadPoint(-2_200_000, 1_000_000), 1_000_000, "Drills"),
                BoardFootprintPrimitive.Hole(new CadPoint(2_200_000, 1_000_000), 1_000_000, "Drills"),
                BoardFootprintPrimitive.Keepout(new CadRectangle(-5_400_000, -4_800_000, 5_400_000, 5_100_000), "Keepout"),
                BoardFootprintPrimitive.Line(new CadPoint(-3_900_000, -4_600_000), new CadPoint(3_900_000, -4_600_000), "Silkscreen"),
                BoardFootprintPrimitive.Arc(new CadPoint(0, 4_600_000), 1_000_000, 180, 180, "Silkscreen"),
                BoardFootprintPrimitive.Text("J2", new CadPoint(-3_300_000, 5_350_000), 1_000_000, "Names")
            ]);

    private static BoardComponentInstance PinHeader() =>
        new(
            "sync-j1",
            "J1",
            "fixture:pin-header",
            "1x2 header",
            Position: default,
            FootprintPreview: ComponentFootprintPreview.Empty,
            FootprintPrimitives:
            [
                BoardFootprintPrimitive.Pad("1", new CadPoint(-1_270_000, 0), new CadVector(1_400_000, 1_400_000), "Round", 800_000, "Top"),
                BoardFootprintPrimitive.Pad("2", new CadPoint(1_270_000, 0), new CadVector(1_400_000, 1_400_000), "Round", 800_000, "Top"),
                BoardFootprintPrimitive.Hole(new CadPoint(-1_270_000, 0), 800_000, "Drills"),
                BoardFootprintPrimitive.Hole(new CadPoint(1_270_000, 0), 800_000, "Drills"),
                BoardFootprintPrimitive.Line(new CadPoint(-2_300_000, -1_300_000), new CadPoint(2_300_000, -1_300_000), "Silkscreen"),
                BoardFootprintPrimitive.Text("J1", new CadPoint(-2_300_000, 1_700_000), 800_000, "Names")
            ]);
}
