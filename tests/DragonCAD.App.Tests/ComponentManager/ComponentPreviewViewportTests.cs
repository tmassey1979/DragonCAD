using Avalonia;
using DragonCAD.App.ComponentManager;
using DragonCAD.Core.Geometry;

namespace DragonCAD.App.Tests.ComponentManager;

public sealed class ComponentPreviewViewportTests
{
    [Fact]
    public void MapsCadBoundsIntoAvailablePixelsWithPaddingAndYFlip()
    {
        ComponentPreviewViewport viewport = new(
            new CadRectangle(-100, -50, 100, 50),
            new Size(240, 140),
            Padding: 20);

        Assert.Equal(new Point(20, 120), viewport.Map(new CadPoint(-100, -50)));
        Assert.Equal(new Point(220, 20), viewport.Map(new CadPoint(100, 50)));
        Assert.Equal(new Point(120, 70), viewport.Map(new CadPoint(0, 0)));
    }
}
