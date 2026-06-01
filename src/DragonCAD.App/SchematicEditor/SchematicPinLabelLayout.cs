using Avalonia;

namespace DragonCAD.App.SchematicEditor;

public static class SchematicPinLabelLayout
{
    public static Point LabelOrigin(Point pinPoint, string orientation, double labelWidth, double labelHeight) =>
        orientation switch
        {
            "Right" => new Point(pinPoint.X - labelWidth - 6, pinPoint.Y - (labelHeight / 2)),
            "Up" => new Point(pinPoint.X + 6, pinPoint.Y + 6),
            "Down" => new Point(pinPoint.X + 6, pinPoint.Y - labelHeight - 6),
            _ => new Point(pinPoint.X + 6, pinPoint.Y - (labelHeight / 2))
        };
}
