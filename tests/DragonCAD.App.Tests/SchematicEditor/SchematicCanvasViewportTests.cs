using DragonCAD.App.SchematicEditor;
using DragonCAD.Core.Geometry;

namespace DragonCAD.App.Tests.SchematicEditor;

public sealed class SchematicCanvasViewportTests
{
    [Fact]
    public void MapAddsInstancePositionBeforeConvertingToScreenCoordinates()
    {
        SchematicCanvasViewport viewport = new(
            origin: new CadPoint(0, 0),
            pixelsPerInternalUnit: 0.001);

        Avalonia.Point point = viewport.Map(
            instancePosition: new CadPoint(1_000_000, 2_000_000),
            localPoint: new CadPoint(250_000, -500_000));

        Assert.Equal(1_250, point.X);
        Assert.Equal(1_500, point.Y);
    }

    [Fact]
    public void ScreenToCadRemovesCanvasCenterAndConvertsPixelsToInternalUnits()
    {
        SchematicCanvasViewport viewport = new(
            origin: new CadPoint(0, 0),
            pixelsPerInternalUnit: 0.00002);

        CadPoint point = viewport.ScreenToCad(
            screenPoint: new Avalonia.Point(460, 240),
            canvasCenter: new Avalonia.Point(400, 300));

        Assert.Equal(new CadPoint(3_000_000, -3_000_000), point);
    }
}
