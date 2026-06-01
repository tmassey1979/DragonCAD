using Avalonia;
using DragonCAD.App.SchematicEditor;

namespace DragonCAD.App.Tests.SchematicEditor;

public sealed class SchematicPinLabelLayoutTests
{
    [Theory]
    [InlineData("Left", 6, -7)]
    [InlineData("Right", -24, -7)]
    [InlineData("Up", 6, 6)]
    [InlineData("Down", 6, -20)]
    public void LabelOriginOffsetsAwayFromPinByOrientation(string orientation, double expectedXOffset, double expectedYOffset)
    {
        Point pinPoint = new(100, 200);

        Point labelOrigin = SchematicPinLabelLayout.LabelOrigin(pinPoint, orientation, labelWidth: 18, labelHeight: 14);

        Assert.Equal(new Point(100 + expectedXOffset, 200 + expectedYOffset), labelOrigin);
    }
}
