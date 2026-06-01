using DragonCAD.App.BoardEditor;
using DragonCAD.Core.Geometry;

namespace DragonCAD.App.Tests.BoardEditor;

public sealed class BoardCanvasViewportTests
{
    [Fact]
    public void MapConvertsBoardCadPointToScreenCoordinates()
    {
        BoardCanvasViewport viewport = new(
            origin: new CadPoint(0, 0),
            pixelsPerInternalUnit: 0.00002);

        Avalonia.Point point = viewport.Map(new CadPoint(2_000_000, -1_000_000));

        Assert.Equal(40, point.X);
        Assert.Equal(-20, point.Y);
    }

    [Fact]
    public void ScreenToCadRemovesCenterAndUsesBoardScale()
    {
        BoardCanvasViewport viewport = new(
            origin: new CadPoint(0, 0),
            pixelsPerInternalUnit: 0.00002);

        CadPoint point = viewport.ScreenToCad(
            screenPoint: new Avalonia.Point(460, 240),
            canvasCenter: new Avalonia.Point(400, 300));

        Assert.Equal(new CadPoint(3_000_000, -3_000_000), point);
    }
}
