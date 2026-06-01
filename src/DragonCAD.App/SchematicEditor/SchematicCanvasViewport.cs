using Avalonia;
using DragonCAD.Core.Geometry;

namespace DragonCAD.App.SchematicEditor;

public sealed class SchematicCanvasViewport
{
    private readonly CadPoint origin;
    private readonly double pixelsPerInternalUnit;

    public SchematicCanvasViewport(CadPoint origin, double pixelsPerInternalUnit)
    {
        if (pixelsPerInternalUnit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelsPerInternalUnit), "Scale must be positive.");
        }

        this.origin = origin;
        this.pixelsPerInternalUnit = pixelsPerInternalUnit;
    }

    public Point Map(CadPoint instancePosition, CadPoint localPoint)
    {
        long worldX = instancePosition.X + localPoint.X - origin.X;
        long worldY = instancePosition.Y + localPoint.Y - origin.Y;
        return new Point(worldX * pixelsPerInternalUnit, worldY * pixelsPerInternalUnit);
    }

    public CadPoint ScreenToCad(Point screenPoint, Point canvasCenter)
    {
        long x = checked(origin.X + (long)Math.Round((screenPoint.X - canvasCenter.X) / pixelsPerInternalUnit, MidpointRounding.AwayFromZero));
        long y = checked(origin.Y + (long)Math.Round((screenPoint.Y - canvasCenter.Y) / pixelsPerInternalUnit, MidpointRounding.AwayFromZero));
        return new CadPoint(x, y);
    }
}
