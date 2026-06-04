using DragonCAD.App.ComponentManager;
using DragonCAD.Core.Geometry;

namespace DragonCAD.App.BoardEditor;

public abstract record BoardFootprintPrimitive(string Kind, string LayerName)
{
    public static BoardFootprintPadPrimitive Pad(
        string name,
        CadPoint position,
        CadVector size,
        string shape,
        long drillSize,
        string layerName) =>
        new(name, position, size, shape, drillSize, layerName);

    public static BoardFootprintSmdPrimitive Smd(
        string name,
        CadPoint position,
        CadVector size,
        string shape,
        string layerName) =>
        new(name, position, size, shape, layerName);

    public static BoardFootprintHolePrimitive Hole(CadPoint position, long drillSize, string layerName) =>
        new(position, drillSize, layerName);

    public static BoardFootprintKeepoutPrimitive Keepout(CadRectangle bounds, string layerName) =>
        new(bounds, layerName);

    public static BoardFootprintLinePrimitive Line(CadPoint start, CadPoint end, string layerName) =>
        new(start, end, layerName);

    public static BoardFootprintArcPrimitive Arc(
        CadPoint center,
        long radius,
        int startAngleDegrees,
        int sweepAngleDegrees,
        string layerName) =>
        new(center, radius, startAngleDegrees, sweepAngleDegrees, layerName);

    public static BoardFootprintTextPrimitive Text(string value, CadPoint position, long size, string layerName) =>
        new(value, position, size, layerName);

    public static IReadOnlyList<BoardFootprintPrimitive> FromPreview(ComponentFootprintPreview preview)
    {
        List<BoardFootprintPrimitive> primitives = [];
        foreach (ComponentPreviewLine line in preview.Lines)
        {
            primitives.Add(Line(line.Start, line.End, "Silkscreen"));
        }

        foreach (ComponentFootprintPadPreview pad in preview.Pads)
        {
            if (string.Equals(pad.Technology, "SurfaceMount", StringComparison.OrdinalIgnoreCase))
            {
                primitives.Add(Smd(pad.Name, pad.Position, pad.Size, pad.Shape, "Top"));
            }
            else
            {
                long drillSize = Math.Min(pad.Size.X, pad.Size.Y) / 2;
                primitives.Add(Pad(pad.Name, pad.Position, pad.Size, pad.Shape, drillSize, "Top"));
            }
        }

        return primitives;
    }
}

public sealed record BoardFootprintPadPrimitive(
    string Name,
    CadPoint Position,
    CadVector Size,
    string Shape,
    long DrillSize,
    string LayerName)
    : BoardFootprintPrimitive("Pad", LayerName);

public sealed record BoardFootprintSmdPrimitive(
    string Name,
    CadPoint Position,
    CadVector Size,
    string Shape,
    string LayerName)
    : BoardFootprintPrimitive("Smd", LayerName);

public sealed record BoardFootprintHolePrimitive(CadPoint Position, long DrillSize, string LayerName)
    : BoardFootprintPrimitive("Hole", LayerName);

public sealed record BoardFootprintKeepoutPrimitive(CadRectangle Bounds, string LayerName)
    : BoardFootprintPrimitive("Keepout", LayerName);

public sealed record BoardFootprintLinePrimitive(CadPoint Start, CadPoint End, string LayerName)
    : BoardFootprintPrimitive("Line", LayerName);

public sealed record BoardFootprintArcPrimitive(
    CadPoint Center,
    long Radius,
    int StartAngleDegrees,
    int SweepAngleDegrees,
    string LayerName)
    : BoardFootprintPrimitive("Arc", LayerName);

public sealed record BoardFootprintTextPrimitive(string Value, CadPoint Position, long Size, string LayerName)
    : BoardFootprintPrimitive("Text", LayerName);

public static class BoardFootprintGeometry
{
    public static CadRectangle CalculateBounds(IReadOnlyList<BoardFootprintPrimitive> primitives)
    {
        List<CadPoint> points = [];
        foreach (BoardFootprintPrimitive primitive in primitives)
        {
            switch (primitive)
            {
                case BoardFootprintPadPrimitive pad:
                    AddBox(points, pad.Position, pad.Size);
                    break;
                case BoardFootprintSmdPrimitive smd:
                    AddBox(points, smd.Position, smd.Size);
                    break;
                case BoardFootprintHolePrimitive hole:
                    AddCircle(points, hole.Position, hole.DrillSize / 2);
                    break;
                case BoardFootprintKeepoutPrimitive keepout:
                    points.Add(new CadPoint(keepout.Bounds.Left, keepout.Bounds.Top));
                    points.Add(new CadPoint(keepout.Bounds.Right, keepout.Bounds.Bottom));
                    break;
                case BoardFootprintLinePrimitive line:
                    points.Add(line.Start);
                    points.Add(line.End);
                    break;
                case BoardFootprintArcPrimitive arc:
                    AddCircle(points, arc.Center, arc.Radius);
                    break;
                case BoardFootprintTextPrimitive text:
                    points.Add(text.Position);
                    points.Add(new CadPoint(text.Position.X + EstimateTextWidth(text), text.Position.Y + text.Size));
                    break;
            }
        }

        return points.Count == 0
            ? new CadRectangle(0, 0, 0, 0)
            : new CadRectangle(
                points.Min(point => point.X),
                points.Min(point => point.Y),
                points.Max(point => point.X),
                points.Max(point => point.Y));
    }

    public static bool HitTest(BoardComponentInstance component, CadPoint point)
    {
        for (int index = component.FootprintPrimitives.Count - 1; index >= 0; index--)
        {
            if (PrimitiveHitTest(component, component.FootprintPrimitives[index], point))
            {
                return true;
            }
        }

        return false;
    }

    public static bool PrimitiveHitTest(BoardComponentInstance component, BoardFootprintPrimitive primitive, CadPoint point) =>
        primitive switch
        {
            BoardFootprintPadPrimitive pad => PadContains(component, pad.Position, pad.Size, pad.Shape, point),
            BoardFootprintSmdPrimitive smd => PadContains(component, smd.Position, smd.Size, smd.Shape, point),
            BoardFootprintHolePrimitive hole => CircleContains(TransformLocalPoint(component, hole.Position), hole.DrillSize / 2, point),
            BoardFootprintKeepoutPrimitive keepout => TransformBounds(component, keepout.Bounds).Contains(point),
            BoardFootprintLinePrimitive line => DistanceToSegment(
                point,
                TransformLocalPoint(component, line.Start),
                TransformLocalPoint(component, line.End)) <= 200_000,
            BoardFootprintArcPrimitive arc => ArcContains(component, arc, point),
            BoardFootprintTextPrimitive text => TextBounds(component, text).Contains(point),
            _ => false
        };

    public static CadPoint TransformLocalPoint(BoardComponentInstance component, CadPoint localPoint)
    {
        CadPoint mirrored = component.IsMirrored
            ? new CadPoint(-localPoint.X, localPoint.Y)
            : localPoint;
        CadPoint rotated = RotateLocalPoint(mirrored, component.RotationDegrees);
        return new CadPoint(component.Position.X + rotated.X, component.Position.Y + rotated.Y);
    }

    public static CadVector SizeForRotation(BoardComponentInstance component, CadVector size) =>
        NormalizeRotation(component.RotationDegrees) is 90 or 270
            ? new CadVector(size.Y, size.X)
            : size;

    public static string ResolveRenderLayerName(BoardComponentInstance component, BoardFootprintPrimitive primitive) =>
        component.IsMirrored && primitive is BoardFootprintSmdPrimitive
            ? OppositeCopperLayerName(primitive.LayerName)
            : primitive.LayerName;

    private static string OppositeCopperLayerName(string layerName) =>
        layerName switch
        {
            "Top" => "Bottom",
            "Bottom" => "Top",
            _ => layerName
        };

    private static bool PadContains(BoardComponentInstance component, CadPoint position, CadVector size, string shape, CadPoint point)
    {
        CadPoint center = TransformLocalPoint(component, position);
        CadVector rotatedSize = SizeForRotation(component, size);
        if (shape is "Round" or "Oval")
        {
            double rx = Math.Max(1, rotatedSize.X / 2d);
            double ry = Math.Max(1, rotatedSize.Y / 2d);
            double dx = point.X - center.X;
            double dy = point.Y - center.Y;
            return ((dx * dx) / (rx * rx)) + ((dy * dy) / (ry * ry)) <= 1;
        }

        return new CadRectangle(
            center.X - (rotatedSize.X / 2),
            center.Y - (rotatedSize.Y / 2),
            center.X + (rotatedSize.X / 2),
            center.Y + (rotatedSize.Y / 2)).Contains(point);
    }

    private static bool CircleContains(CadPoint center, long radius, CadPoint point)
    {
        long dx = point.X - center.X;
        long dy = point.Y - center.Y;
        return (dx * dx) + (dy * dy) <= radius * radius;
    }

    private static CadRectangle TransformBounds(BoardComponentInstance component, CadRectangle bounds)
    {
        CadPoint[] corners =
        [
            TransformLocalPoint(component, new CadPoint(bounds.Left, bounds.Top)),
            TransformLocalPoint(component, new CadPoint(bounds.Right, bounds.Top)),
            TransformLocalPoint(component, new CadPoint(bounds.Right, bounds.Bottom)),
            TransformLocalPoint(component, new CadPoint(bounds.Left, bounds.Bottom))
        ];
        return new CadRectangle(
            corners.Min(point => point.X),
            corners.Min(point => point.Y),
            corners.Max(point => point.X),
            corners.Max(point => point.Y));
    }

    private static CadRectangle TextBounds(BoardComponentInstance component, BoardFootprintTextPrimitive text)
    {
        CadPoint position = TransformLocalPoint(component, text.Position);
        long width = EstimateTextWidth(text);
        CadVector size = SizeForRotation(component, new CadVector(width, text.Size));
        return new CadRectangle(position.X, position.Y, position.X + size.X, position.Y + size.Y);
    }

    private static bool ArcContains(BoardComponentInstance component, BoardFootprintArcPrimitive arc, CadPoint point)
    {
        CadPoint center = TransformLocalPoint(component, arc.Center);
        double distance = Math.Sqrt(Math.Pow(point.X - center.X, 2) + Math.Pow(point.Y - center.Y, 2));
        if (Math.Abs(distance - arc.Radius) > 250_000)
        {
            return false;
        }

        double angle = Math.Atan2(point.Y - center.Y, point.X - center.X) * 180 / Math.PI;
        if (angle < 0)
        {
            angle += 360;
        }

        return AngleInSweep(angle, arc.StartAngleDegrees, arc.SweepAngleDegrees);
    }

    private static bool AngleInSweep(double angle, int startAngleDegrees, int sweepAngleDegrees)
    {
        int sweepSign = Math.Sign(sweepAngleDegrees);
        double sweep = Math.Abs(sweepAngleDegrees);
        double start = NormalizeAngle(startAngleDegrees);
        double delta = sweepSign >= 0
            ? NormalizeAngle(angle - start)
            : NormalizeAngle(start - angle);
        return delta <= sweep;
    }

    private static double NormalizeAngle(double angle)
    {
        double normalized = angle % 360;
        return normalized < 0 ? normalized + 360 : normalized;
    }

    private static CadPoint RotateLocalPoint(CadPoint point, int rotationDegrees) =>
        NormalizeRotation(rotationDegrees) switch
        {
            90 => new CadPoint(-point.Y, point.X),
            180 => new CadPoint(-point.X, -point.Y),
            270 => new CadPoint(point.Y, -point.X),
            _ => point
        };

    private static int NormalizeRotation(int rotationDegrees)
    {
        int normalized = rotationDegrees % 360;
        return normalized < 0 ? normalized + 360 : normalized;
    }

    private static void AddBox(List<CadPoint> points, CadPoint center, CadVector size)
    {
        points.Add(new CadPoint(center.X - (size.X / 2), center.Y - (size.Y / 2)));
        points.Add(new CadPoint(center.X + (size.X / 2), center.Y + (size.Y / 2)));
    }

    private static void AddCircle(List<CadPoint> points, CadPoint center, long radius)
    {
        points.Add(new CadPoint(center.X - radius, center.Y - radius));
        points.Add(new CadPoint(center.X + radius, center.Y + radius));
    }

    private static long EstimateTextWidth(BoardFootprintTextPrimitive text) =>
        Math.Max(text.Size, text.Value.Length * text.Size / 2);

    private static double DistanceToSegment(CadPoint point, CadPoint start, CadPoint end)
    {
        double dx = end.X - start.X;
        double dy = end.Y - start.Y;
        if (Math.Abs(dx) < 0.001 && Math.Abs(dy) < 0.001)
        {
            return Math.Sqrt(Math.Pow(point.X - start.X, 2) + Math.Pow(point.Y - start.Y, 2));
        }

        double t = (((point.X - start.X) * dx) + ((point.Y - start.Y) * dy)) / ((dx * dx) + (dy * dy));
        t = Math.Clamp(t, 0, 1);
        double nearestX = start.X + (t * dx);
        double nearestY = start.Y + (t * dy);
        return Math.Sqrt(Math.Pow(point.X - nearestX, 2) + Math.Pow(point.Y - nearestY, 2));
    }
}
