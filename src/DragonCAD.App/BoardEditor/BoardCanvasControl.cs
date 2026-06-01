using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using DragonCAD.App.ComponentManager;
using DragonCAD.Core.Geometry;

namespace DragonCAD.App.BoardEditor;

public sealed class BoardCanvasControl : Control
{
    public static readonly StyledProperty<BoardEditorViewModel?> EditorProperty =
        AvaloniaProperty.Register<BoardCanvasControl, BoardEditorViewModel?>(nameof(Editor));

    private static readonly IBrush BackgroundBrush = new SolidColorBrush(Color.FromRgb(6, 8, 10));
    private static readonly IBrush ComponentFillBrush = new SolidColorBrush(Color.FromRgb(14, 21, 27));
    private static readonly IBrush TextBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240));
    private static readonly IBrush PadBrush = new SolidColorBrush(Color.FromRgb(239, 197, 74));
    private static readonly IBrush SmdPadBrush = new SolidColorBrush(Color.FromRgb(211, 95, 49));
    private static readonly Pen GridPen = new(new SolidColorBrush(Color.FromRgb(31, 37, 45)), 1);
    private static readonly Pen AirwirePen = new(new SolidColorBrush(Color.FromRgb(78, 171, 247)), 1.6);
    private static readonly Pen TracePen = new(new SolidColorBrush(Color.FromRgb(230, 61, 50)), 3.0);
    private static readonly Pen PendingTracePen = new(new SolidColorBrush(Color.FromRgb(255, 176, 52)), 2.4);
    private static readonly Pen ComponentPen = new(new SolidColorBrush(Color.FromRgb(210, 216, 226)), 1.4);
    private static readonly Pen SilkscreenPen = new(new SolidColorBrush(Color.FromRgb(226, 232, 240)), 1.2);
    private static readonly Pen PadPen = new(new SolidColorBrush(Color.FromRgb(239, 197, 74)), 1.0);
    private static readonly Pen SelectedComponentPen = new(new SolidColorBrush(Color.FromRgb(255, 176, 52)), 2.2);
    private CadVector dragOffset;
    private BoardDragMode dragMode;

    static BoardCanvasControl()
    {
        AffectsRender<BoardCanvasControl>(EditorProperty);
    }

    public BoardEditorViewModel? Editor
    {
        get => GetValue(EditorProperty);
        set => SetValue(EditorProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == EditorProperty)
        {
            if (change.OldValue is BoardEditorViewModel oldEditor)
            {
                oldEditor.Components.CollectionChanged -= ItemsChanged;
                oldEditor.Airwires.CollectionChanged -= ItemsChanged;
                oldEditor.Traces.CollectionChanged -= ItemsChanged;
                oldEditor.Vias.CollectionChanged -= ItemsChanged;
                oldEditor.PropertyChanged -= EditorPropertyChanged;
            }

            if (change.NewValue is BoardEditorViewModel newEditor)
            {
                newEditor.Components.CollectionChanged += ItemsChanged;
                newEditor.Airwires.CollectionChanged += ItemsChanged;
                newEditor.Traces.CollectionChanged += ItemsChanged;
                newEditor.Vias.CollectionChanged += ItemsChanged;
                newEditor.PropertyChanged += EditorPropertyChanged;
            }
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        Rect bounds = new(Bounds.Size);
        Point center = bounds.Center;
        context.DrawRectangle(BackgroundBrush, null, bounds);

        if (Editor is null)
        {
            return;
        }

        BoardCanvasViewport viewport = CreateViewport();
        DrawGrid(context, bounds, center, viewport, Editor);
        foreach (BoardTrace trace in Editor.VisibleTraces)
        {
            bool isSelectedTrace = Editor.SelectedTrace?.TraceId == trace.TraceId;
            DrawTrace(context, viewport, center, trace, isSelectedTrace);
        }

        DrawRoute(context, viewport, center, Editor.PendingTraceRoutePoints, PendingTracePen);
        foreach (BoardVia via in Editor.Vias)
        {
            DrawVia(context, viewport, center, via, Editor.SelectedVia?.ViaId == via.ViaId);
        }

        foreach (BoardAirwire airwire in Editor.Airwires)
        {
            context.DrawLine(
                AirwirePen,
                Translate(viewport.Map(airwire.StartPosition), center),
                Translate(viewport.Map(airwire.EndPosition), center));
        }

        foreach (BoardComponentInstance component in Editor.Components)
        {
            DrawComponent(
                context,
                viewport,
                center,
                component,
                Editor.SelectedComponent?.SyncId == component.SyncId);
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (Editor is null)
        {
            return;
        }

        CadPoint point = CreateViewport().ScreenToCad(e.GetPosition(this), Bounds.Center);
        if (Editor.ActiveTool == "Route")
        {
            Editor.TraceClickAt(point);
            Cursor = new Cursor(StandardCursorType.Cross);
            e.Handled = true;
            return;
        }

        bool selected = Editor.SelectAt(point);
        if (!selected)
        {
            dragMode = BoardDragMode.None;
            Cursor = null;
            e.Handled = true;
            return;
        }

        if (Editor.SelectedComponent is not null)
        {
            dragOffset = Editor.SelectedComponent.Position - point;
            dragMode = BoardDragMode.Component;
            Cursor = new Cursor(StandardCursorType.SizeAll);
            e.Pointer.Capture(this);
        }
        else if (Editor.SelectedVia is not null)
        {
            dragOffset = Editor.SelectedVia.Position - point;
            dragMode = BoardDragMode.Via;
            Cursor = new Cursor(StandardCursorType.SizeAll);
            e.Pointer.Capture(this);
        }
        else if (Editor.SelectedTrace is not null)
        {
            dragOffset = default;
            dragMode = BoardDragMode.TraceSegment;
            Cursor = new Cursor(StandardCursorType.SizeAll);
            e.Pointer.Capture(this);
        }

        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (Editor is null || dragMode == BoardDragMode.None)
        {
            Cursor = Editor?.ActiveTool == "Route" ? new Cursor(StandardCursorType.Cross) : null;
            return;
        }

        CadPoint point = CreateViewport().ScreenToCad(e.GetPosition(this), Bounds.Center);
        MoveSelectionTo(point);
        Cursor = new Cursor(StandardCursorType.SizeAll);
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (Editor is null)
        {
            return;
        }

        if (dragMode != BoardDragMode.None)
        {
            CadPoint point = CreateViewport().ScreenToCad(e.GetPosition(this), Bounds.Center);
            MoveSelectionTo(point);
        }

        dragMode = BoardDragMode.None;
        Cursor = null;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private void MoveSelectionTo(CadPoint point)
    {
        if (Editor is null)
        {
            return;
        }

        switch (dragMode)
        {
            case BoardDragMode.Component:
                Editor.MoveSelectedComponentTo(point + dragOffset);
                break;
            case BoardDragMode.Via:
                Editor.MoveSelectedViaTo(point + dragOffset);
                break;
            case BoardDragMode.TraceSegment:
                Editor.MoveSelectedTraceSegmentTo(point);
                break;
        }
    }

    private static void DrawGrid(
        DrawingContext context,
        Rect bounds,
        Point center,
        BoardCanvasViewport viewport,
        BoardEditorViewModel editor)
    {
        if (!editor.IsGridVisible)
        {
            return;
        }

        double spacing = Math.Max(4, Math.Abs(
            viewport.Map(new CadPoint(editor.GridSpacingInternal, 0)).X -
            viewport.Map(new CadPoint(0, 0)).X));
        double startX = center.X % spacing;
        double startY = center.Y % spacing;
        if (editor.GridStyle == "Dots")
        {
            for (double x = startX; x <= bounds.Width; x += spacing)
            {
                for (double y = startY; y <= bounds.Height; y += spacing)
                {
                    context.DrawEllipse(GridPen.Brush, null, new Point(x, y), 1.1, 1.1);
                }
            }

            return;
        }

        for (double x = startX; x <= bounds.Width; x += spacing)
        {
            context.DrawLine(GridPen, new Point(x, 0), new Point(x, bounds.Height));
        }

        for (double y = startY; y <= bounds.Height; y += spacing)
        {
            context.DrawLine(GridPen, new Point(0, y), new Point(bounds.Width, y));
        }
    }

    private static void DrawComponent(
        DrawingContext context,
        BoardCanvasViewport viewport,
        Point center,
        BoardComponentInstance component,
        bool isSelected)
    {
        Point position = Translate(viewport.Map(component.Position), center);
        if (component.FootprintPreview.Lines.Count == 0 && component.FootprintPreview.Pads.Count == 0)
        {
            DrawFallbackComponent(context, position, isSelected);
        }
        else
        {
            DrawFootprintGeometry(context, viewport, center, component, isSelected);
        }

        FormattedText label = new(
            component.ReferenceDesignator,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            12,
            TextBrush);
        context.DrawText(label, new Point(position.X - (label.Width / 2), position.Y - 30));

        if (!string.IsNullOrWhiteSpace(component.Value))
        {
            FormattedText valueLabel = new(
                component.Value,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                Typeface.Default,
                11,
                TextBrush);
            context.DrawText(valueLabel, new Point(position.X - (valueLabel.Width / 2), position.Y + 20));
        }
    }

    private static void DrawFallbackComponent(DrawingContext context, Point position, bool isSelected)
    {
        Rect body = new(position.X - 28, position.Y - 18, 56, 36);
        context.DrawRectangle(ComponentFillBrush, isSelected ? SelectedComponentPen : ComponentPen, body, radiusX: 3, radiusY: 3);
        context.DrawEllipse(PadBrush, null, new Point(body.Left + 7, position.Y), 3.5, 3.5);
        context.DrawEllipse(PadBrush, null, new Point(body.Right - 7, position.Y), 3.5, 3.5);
    }

    private static void DrawFootprintGeometry(
        DrawingContext context,
        BoardCanvasViewport viewport,
        Point center,
        BoardComponentInstance component,
        bool isSelected)
    {
        foreach (ComponentPreviewLine line in component.FootprintPreview.Lines)
        {
            context.DrawLine(
                SilkscreenPen,
                Translate(viewport.Map(TransformLocalPoint(component, line.Start)), center),
                Translate(viewport.Map(TransformLocalPoint(component, line.End)), center));
        }

        foreach (ComponentFootprintPadPreview pad in component.FootprintPreview.Pads)
        {
            DrawPad(context, viewport, center, component, pad);
        }

        if (isSelected)
        {
            CadRectangle bounds = component.FootprintPreview.Bounds;
            Point[] corners =
            [
                Translate(viewport.Map(TransformLocalPoint(component, new CadPoint(bounds.Left, bounds.Top))), center),
                Translate(viewport.Map(TransformLocalPoint(component, new CadPoint(bounds.Right, bounds.Top))), center),
                Translate(viewport.Map(TransformLocalPoint(component, new CadPoint(bounds.Right, bounds.Bottom))), center),
                Translate(viewport.Map(TransformLocalPoint(component, new CadPoint(bounds.Left, bounds.Bottom))), center)
            ];
            Rect selectionBounds = new(
                new Point(corners.Min(point => point.X), corners.Min(point => point.Y)),
                new Point(corners.Max(point => point.X), corners.Max(point => point.Y)));
            context.DrawRectangle(null, SelectedComponentPen, selectionBounds.Normalize());
        }
    }

    private static void DrawPad(
        DrawingContext context,
        BoardCanvasViewport viewport,
        Point center,
        BoardComponentInstance component,
        ComponentFootprintPadPreview pad)
    {
        CadVector padSize = NormalizeRotation(component.RotationDegrees) is 90 or 270
            ? new CadVector(pad.Size.Y, pad.Size.X)
            : pad.Size;
        CadPoint padCenter = TransformLocalPoint(component, pad.Position);
        CadPoint first = new(padCenter.X - (padSize.X / 2), padCenter.Y - (padSize.Y / 2));
        CadPoint second = new(padCenter.X + (padSize.X / 2), padCenter.Y + (padSize.Y / 2));
        Rect padRect = new Rect(
            Translate(viewport.Map(first), center),
            Translate(viewport.Map(second), center)).Normalize();
        IBrush fill = pad.Technology == "SurfaceMount" ? SmdPadBrush : PadBrush;
        context.DrawRectangle(fill, PadPen, padRect, radiusX: 2, radiusY: 2);
    }

    private static void DrawTrace(
        DrawingContext context,
        BoardCanvasViewport viewport,
        Point center,
        BoardTrace trace,
        bool isSelected)
    {
        Pen basePen = TracePenForLayer(trace.LayerName);
        double width = Math.Max(1.5, trace.WidthInternal * 0.000025);
        Pen tracePen = isSelected
            ? new Pen(SelectedComponentPen.Brush, width + 1.2)
            : new Pen(basePen.Brush, width);
        DrawRoute(context, viewport, center, trace.RoutePoints, tracePen);
    }

    private static void DrawRoute(
        DrawingContext context,
        BoardCanvasViewport viewport,
        Point center,
        IReadOnlyList<CadPoint> routePoints,
        Pen pen)
    {
        for (int index = 1; index < routePoints.Count; index++)
        {
            context.DrawLine(
                pen,
                Translate(viewport.Map(routePoints[index - 1]), center),
                Translate(viewport.Map(routePoints[index]), center));
        }
    }

    private static void DrawVia(
        DrawingContext context,
        BoardCanvasViewport viewport,
        Point center,
        BoardVia via,
        bool isSelected)
    {
        Point position = Translate(viewport.Map(via.Position), center);
        double radius = Math.Max(3.5, via.DiameterInternal * 0.000025 / 2);
        double drillRadius = Math.Max(1.5, via.DrillInternal * 0.000025 / 2);
        context.DrawEllipse(PadBrush, isSelected ? SelectedComponentPen : PadPen, position, radius, radius);
        context.DrawEllipse(BackgroundBrush, null, position, drillRadius, drillRadius);
    }

    private static Pen TracePenForLayer(string layerName) =>
        layerName == "Bottom"
            ? new Pen(new SolidColorBrush(Color.FromRgb(45, 140, 255)), 3.0)
            : TracePen;

    private static CadPoint TransformLocalPoint(BoardComponentInstance component, CadPoint localPoint)
    {
        CadPoint mirrored = component.IsMirrored
            ? new CadPoint(-localPoint.X, localPoint.Y)
            : localPoint;
        CadPoint rotated = RotateLocalPoint(mirrored, component.RotationDegrees);
        return new CadPoint(component.Position.X + rotated.X, component.Position.Y + rotated.Y);
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

    private static Point Translate(Point point, Point offset) =>
        new(point.X + offset.X, point.Y + offset.Y);

    private static BoardCanvasViewport CreateViewport() =>
        new(new CadPoint(4_000_000, 0), 0.000025);

    private void ItemsChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        InvalidateVisual();

    private void EditorPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(BoardEditorViewModel.SelectedComponent) or
            nameof(BoardEditorViewModel.SelectedTraceSegmentIndex) or
            nameof(BoardEditorViewModel.IsGridVisible) or
            nameof(BoardEditorViewModel.GridStyle) or
            nameof(BoardEditorViewModel.GridSpacingInternal) or
            nameof(BoardEditorViewModel.PendingTraceStart) or
            nameof(BoardEditorViewModel.PendingTraceRoutePoints) or
            nameof(BoardEditorViewModel.ActiveTool) or
            nameof(BoardEditorViewModel.VisibleTraces) or
            nameof(BoardEditorViewModel.ActiveLayerName) or
            nameof(BoardEditorViewModel.SelectedTrace) or
            nameof(BoardEditorViewModel.SelectedVia))
        {
            InvalidateVisual();
        }
    }

    private enum BoardDragMode
    {
        None,
        Component,
        Via,
        TraceSegment
    }
}
