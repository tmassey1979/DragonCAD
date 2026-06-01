using Avalonia;
using DragonCAD.Core.Geometry;

namespace DragonCAD.App.ComponentManager;

public readonly record struct ComponentPreviewViewport(CadRectangle Bounds, Size Size, double Padding)
{
    public Point Map(CadPoint point)
    {
        double availableWidth = Math.Max(1, Size.Width - (Padding * 2));
        double availableHeight = Math.Max(1, Size.Height - (Padding * 2));
        double width = Math.Max(1, Bounds.Width);
        double height = Math.Max(1, Bounds.Height);
        double scale = Math.Min(availableWidth / width, availableHeight / height);

        double drawnWidth = width * scale;
        double drawnHeight = height * scale;
        double originX = Padding + ((availableWidth - drawnWidth) / 2);
        double originY = Padding + ((availableHeight - drawnHeight) / 2);

        double x = originX + ((point.X - Bounds.Left) * scale);
        double y = originY + ((Bounds.Bottom - point.Y) * scale);
        return new Point(x, y);
    }
}
