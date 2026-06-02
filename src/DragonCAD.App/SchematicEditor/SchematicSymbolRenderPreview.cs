using DragonCAD.App.ComponentManager;
using DragonCAD.Core.Components.Definitions;
using DragonCAD.Core.Geometry;

namespace DragonCAD.App.SchematicEditor;

public sealed record SchematicSymbolRenderPreview(
    CadRectangle Bounds,
    IReadOnlyList<SchematicSymbolPrimitivePreview> Primitives,
    IReadOnlyList<ComponentSymbolPinPreview> Pins)
{
    public static SchematicSymbolRenderPreview Empty { get; } =
        new(new CadRectangle(0, 0, 0, 0), [], []);

    public static SchematicSymbolRenderPreview FromComponentPreview(ComponentSymbolPreview preview)
    {
        SchematicSymbolLine[] primitives = preview.Lines
            .Select(line => new SchematicSymbolLine(line.Start, line.End, "94", "green"))
            .ToArray();
        return new SchematicSymbolRenderPreview(preview.Bounds, primitives, preview.Pins);
    }
}

public abstract record SchematicSymbolPrimitivePreview(string Layer, string Color);

public sealed record SchematicSymbolLine(CadPoint Start, CadPoint End, string Layer, string Color)
    : SchematicSymbolPrimitivePreview(Layer, Color);

public sealed record SchematicSymbolArc(
    CadPoint Center,
    long Radius,
    int StartAngleDegrees,
    int SweepAngleDegrees,
    string Layer,
    string Color)
    : SchematicSymbolPrimitivePreview(Layer, Color);

public sealed record SchematicSymbolRectangle(CadRectangle Bounds, string Layer, string Color)
    : SchematicSymbolPrimitivePreview(Layer, Color);

public sealed record SchematicSymbolCircle(CadPoint Center, long Radius, string Layer, string Color)
    : SchematicSymbolPrimitivePreview(Layer, Color);

public sealed record SchematicSymbolText(
    ComponentSymbolTextKind Kind,
    string Value,
    CadPoint Position,
    string Layer,
    string Color)
    : SchematicSymbolPrimitivePreview(Layer, Color);

public static class SchematicSymbolPrimitiveMapper
{
    public static SchematicSymbolRenderPreview FromDefinition(ComponentDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        ComponentSymbol? symbol = definition.Symbols.FirstOrDefault();
        if (symbol is null)
        {
            return SchematicSymbolRenderPreview.Empty;
        }

        Dictionary<string, ComponentPin> pinsById = definition.Pins.ToDictionary(pin => pin.Id.Value, StringComparer.Ordinal);
        ComponentSymbolPinPreview[] pins = symbol.Pins
            .Select(pin =>
            {
                pinsById.TryGetValue(pin.PinId.Value, out ComponentPin? componentPin);
                return new ComponentSymbolPinPreview(
                    componentPin?.Name ?? pin.PinId.Value,
                    pin.Position,
                    BodyPointForPin(pin.Position, pin.Orientation),
                    pin.Orientation.ToString());
            })
            .ToArray();
        SchematicSymbolPrimitivePreview[] primitives = MapPrimitives(symbol).ToArray();

        return new SchematicSymbolRenderPreview(CalculateBounds(primitives, pins), primitives, pins);
    }

    private static IEnumerable<SchematicSymbolPrimitivePreview> MapPrimitives(ComponentSymbol symbol)
    {
        if (symbol.Primitives.Count == 0)
        {
            foreach (ComponentLine line in symbol.Lines)
            {
                yield return new SchematicSymbolLine(line.Start, line.End, "94", "green");
            }

            foreach (ComponentSymbolText text in symbol.Texts)
            {
                yield return new SchematicSymbolText(text.Kind, text.Value, text.Position, "95", "brown");
            }

            yield break;
        }

        foreach (ComponentSymbolPrimitive primitive in symbol.Primitives)
        {
            yield return primitive switch
            {
                ComponentSymbolLinePrimitive line => new SchematicSymbolLine(line.Start, line.End, line.Layer, line.Color),
                ComponentSymbolArcPrimitive arc => new SchematicSymbolArc(arc.Center, arc.Radius, arc.StartAngleDegrees, arc.SweepAngleDegrees, arc.Layer, arc.Color),
                ComponentSymbolRectanglePrimitive rectangle => new SchematicSymbolRectangle(rectangle.Bounds, rectangle.Layer, rectangle.Color),
                ComponentSymbolCirclePrimitive circle => new SchematicSymbolCircle(circle.Center, circle.Radius, circle.Layer, circle.Color),
                ComponentSymbolTextPrimitive text => new SchematicSymbolText(text.Kind, text.Value, text.Position, text.Layer, text.Color),
                _ => throw new InvalidOperationException($"Unsupported schematic primitive '{primitive.GetType().Name}'.")
            };
        }
    }

    private static CadPoint BodyPointForPin(CadPoint connectPoint, ComponentPinOrientation orientation) =>
        orientation switch
        {
            ComponentPinOrientation.Left => connectPoint + new CadVector(-50_000, 0),
            ComponentPinOrientation.Right => connectPoint + new CadVector(50_000, 0),
            ComponentPinOrientation.Up => connectPoint + new CadVector(0, -50_000),
            ComponentPinOrientation.Down => connectPoint + new CadVector(0, 50_000),
            _ => connectPoint
        };

    private static CadRectangle CalculateBounds(
        IReadOnlyList<SchematicSymbolPrimitivePreview> primitives,
        IReadOnlyList<ComponentSymbolPinPreview> pins)
    {
        List<CadPoint> points = [];
        foreach (SchematicSymbolPrimitivePreview primitive in primitives)
        {
            AddPrimitiveBounds(points, primitive);
        }

        foreach (ComponentSymbolPinPreview pin in pins)
        {
            points.Add(pin.ConnectPoint);
            points.Add(pin.BodyPoint);
        }

        if (points.Count == 0)
        {
            return new CadRectangle(0, 0, 0, 0);
        }

        return new CadRectangle(
            points.Min(point => point.X),
            points.Min(point => point.Y),
            points.Max(point => point.X),
            points.Max(point => point.Y));
    }

    private static void AddPrimitiveBounds(List<CadPoint> points, SchematicSymbolPrimitivePreview primitive)
    {
        switch (primitive)
        {
            case SchematicSymbolLine line:
                points.Add(line.Start);
                points.Add(line.End);
                break;
            case SchematicSymbolArc arc:
                points.Add(new CadPoint(arc.Center.X - arc.Radius, arc.Center.Y - arc.Radius));
                points.Add(new CadPoint(arc.Center.X + arc.Radius, arc.Center.Y + arc.Radius));
                break;
            case SchematicSymbolRectangle rectangle:
                points.Add(new CadPoint(rectangle.Bounds.Left, rectangle.Bounds.Top));
                points.Add(new CadPoint(rectangle.Bounds.Right, rectangle.Bounds.Bottom));
                break;
            case SchematicSymbolCircle circle:
                points.Add(new CadPoint(circle.Center.X - circle.Radius, circle.Center.Y - circle.Radius));
                points.Add(new CadPoint(circle.Center.X + circle.Radius, circle.Center.Y + circle.Radius));
                break;
            case SchematicSymbolText text:
                points.Add(text.Position);
                break;
        }
    }
}
