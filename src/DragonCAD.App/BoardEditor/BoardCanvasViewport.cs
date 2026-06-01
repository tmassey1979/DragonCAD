using Avalonia;
using DragonCAD.Core.Geometry;

namespace DragonCAD.App.BoardEditor;

public sealed record BoardCanvasViewport(CadPoint origin, double pixelsPerInternalUnit)
{
    public Point Map(CadPoint point) =>
        new(
            (point.X - origin.X) * pixelsPerInternalUnit,
            (point.Y - origin.Y) * pixelsPerInternalUnit);

    public CadPoint ScreenToCad(Point screenPoint, Point canvasCenter) =>
        new(
            (long)Math.Round(((screenPoint.X - canvasCenter.X) / pixelsPerInternalUnit) + origin.X),
            (long)Math.Round(((screenPoint.Y - canvasCenter.Y) / pixelsPerInternalUnit) + origin.Y));
}
