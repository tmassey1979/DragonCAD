using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using DragonCAD.Core.Geometry;

namespace DragonCAD.App.ComponentManager;

public sealed class ComponentPreviewControl : Control
{
    public static readonly StyledProperty<ComponentManagerRow?> RowProperty =
        AvaloniaProperty.Register<ComponentPreviewControl, ComponentManagerRow?>(nameof(Row));

    public static readonly StyledProperty<ComponentPreviewKind> PreviewKindProperty =
        AvaloniaProperty.Register<ComponentPreviewControl, ComponentPreviewKind>(nameof(PreviewKind));

    private static readonly Pen GridPen = new(new SolidColorBrush(Color.FromRgb(35, 45, 56)), 1);
    private static readonly Pen SymbolPen = new(new SolidColorBrush(Color.FromRgb(78, 214, 133)), 1.5);
    private static readonly Pen PinPen = new(new SolidColorBrush(Color.FromRgb(230, 82, 77)), 1.4);
    private static readonly Pen FootprintPen = new(new SolidColorBrush(Color.FromRgb(238, 206, 86)), 1.5);
    private static readonly IBrush PadBrush = new SolidColorBrush(Color.FromArgb(170, 238, 206, 86));
    private static readonly IBrush BackgroundBrush = new SolidColorBrush(Color.FromRgb(13, 18, 24));

    static ComponentPreviewControl()
    {
        AffectsRender<ComponentPreviewControl>(RowProperty, PreviewKindProperty);
    }

    public ComponentManagerRow? Row
    {
        get => GetValue(RowProperty);
        set => SetValue(RowProperty, value);
    }

    public ComponentPreviewKind PreviewKind
    {
        get => GetValue(PreviewKindProperty);
        set => SetValue(PreviewKindProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        Rect bounds = new(Bounds.Size);
        context.DrawRectangle(BackgroundBrush, null, bounds);
        DrawGrid(context, bounds);

        if (Row is null)
        {
            return;
        }

        if (PreviewKind == ComponentPreviewKind.Symbol)
        {
            DrawSymbol(context, Row.SymbolPreview);
        }
        else
        {
            DrawFootprint(context, Row.FootprintPreview);
        }
    }

    private static void DrawGrid(DrawingContext context, Rect bounds)
    {
        const double spacing = 16;
        for (double x = 0; x <= bounds.Width; x += spacing)
        {
            context.DrawLine(GridPen, new Point(x, 0), new Point(x, bounds.Height));
        }

        for (double y = 0; y <= bounds.Height; y += spacing)
        {
            context.DrawLine(GridPen, new Point(0, y), new Point(bounds.Width, y));
        }
    }

    private void DrawSymbol(DrawingContext context, ComponentSymbolPreview preview)
    {
        ComponentPreviewViewport viewport = new(preview.Bounds, Bounds.Size, Padding: 18);
        foreach (ComponentPreviewLine line in preview.Lines)
        {
            context.DrawLine(SymbolPen, viewport.Map(line.Start), viewport.Map(line.End));
        }

        foreach (ComponentSymbolPinPreview pin in preview.Pins)
        {
            context.DrawLine(PinPen, viewport.Map(pin.ConnectPoint), viewport.Map(pin.BodyPoint));
            Point connectPoint = viewport.Map(pin.ConnectPoint);
            context.DrawEllipse(null, PinPen, connectPoint, 3, 3);
        }
    }

    private void DrawFootprint(DrawingContext context, ComponentFootprintPreview preview)
    {
        ComponentPreviewViewport viewport = new(preview.Bounds, Bounds.Size, Padding: 18);
        foreach (ComponentPreviewLine line in preview.Lines)
        {
            context.DrawLine(FootprintPen, viewport.Map(line.Start), viewport.Map(line.End));
        }

        foreach (ComponentFootprintPadPreview pad in preview.Pads)
        {
            DrawPad(context, viewport, pad);
        }
    }

    private static void DrawPad(DrawingContext context, ComponentPreviewViewport viewport, ComponentFootprintPadPreview pad)
    {
        CadPoint first = new(pad.Position.X - (pad.Size.X / 2), pad.Position.Y - (pad.Size.Y / 2));
        CadPoint second = new(pad.Position.X + (pad.Size.X / 2), pad.Position.Y + (pad.Size.Y / 2));
        Point firstPoint = viewport.Map(first);
        Point secondPoint = viewport.Map(second);
        Rect padRect = new Rect(firstPoint, secondPoint).Normalize();
        context.DrawRectangle(PadBrush, FootprintPen, padRect);
    }
}

public enum ComponentPreviewKind
{
    Symbol,
    Footprint
}
