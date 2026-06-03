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
    private static readonly Pen PendingTracePen = new(new SolidColorBrush(Color.FromRgb(255, 176, 52)), 2.4);
    private static readonly Pen ComponentPen = new(new SolidColorBrush(Color.FromRgb(210, 216, 226)), 1.4);
    private static readonly Pen SilkscreenPen = new(new SolidColorBrush(Color.FromRgb(226, 232, 240)), 1.2);
    private static readonly Pen PadPen = new(new SolidColorBrush(Color.FromRgb(239, 197, 74)), 1.0);
    private static readonly Pen SelectedComponentPen = new(new SolidColorBrush(Color.FromRgb(255, 176, 52)), 2.2);
    private static readonly Pen HoverSelectionPen = new(new SolidColorBrush(Color.FromRgb(70, 180, 255)), 1.7);
    private static readonly Pen HoverTracePen = new(new SolidColorBrush(Color.FromRgb(70, 180, 255)), 3.2);
    private CadVector dragOffset;
    private BoardDragMode dragMode;
    private Point lastPanScreenPoint;

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
                oldEditor.Layers.CollectionChanged -= ItemsChanged;
                oldEditor.PropertyChanged -= EditorPropertyChanged;
            }

            if (change.NewValue is BoardEditorViewModel newEditor)
            {
                newEditor.Components.CollectionChanged += ItemsChanged;
                newEditor.Airwires.CollectionChanged += ItemsChanged;
                newEditor.Traces.CollectionChanged += ItemsChanged;
                newEditor.Vias.CollectionChanged += ItemsChanged;
                newEditor.Layers.CollectionChanged += ItemsChanged;
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
            bool isHoveredTrace = Editor.HoveredTrace?.TraceId == trace.TraceId;
            DrawTrace(context, viewport, center, Editor, trace, isSelectedTrace, isHoveredTrace);
        }

        DrawRoute(context, viewport, center, Editor.PendingTraceRoutePoints, PendingTracePen);
        foreach (BoardVia via in Editor.VisibleVias)
        {
            bool isHoveredVia = Editor.HoveredVia?.ViaId == via.ViaId;
            DrawVia(context, viewport, center, Editor, via, Editor.SelectedVia?.ViaId == via.ViaId, isHoveredVia);
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
                Editor,
                component,
                Editor.SelectedComponent?.SyncId == component.SyncId,
                ReferenceEquals(component, Editor.HoveredComponent));
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
            dragMode = BoardDragMode.Pan;
            lastPanScreenPoint = e.GetPosition(this);
            Editor.ClearHover();
            Cursor = new Cursor(StandardCursorType.SizeAll);
            e.Pointer.Capture(this);
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
        if (Editor is null)
        {
            Cursor = null;
            return;
        }

        CadPoint point = CreateViewport().ScreenToCad(e.GetPosition(this), Bounds.Center);
        if (dragMode == BoardDragMode.None)
        {
            Editor.UpdateHoverAt(point);
            Cursor = CursorForEditorState();
            e.Handled = true;
            return;
        }

        if (dragMode == BoardDragMode.Pan)
        {
            Point currentPoint = e.GetPosition(this);
            Editor.PanViewportByScreenDelta(currentPoint - lastPanScreenPoint, 0.000025 * Editor.ZoomLevel);
            lastPanScreenPoint = currentPoint;
            Cursor = new Cursor(StandardCursorType.SizeAll);
            e.Handled = true;
            return;
        }

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

        if (dragMode != BoardDragMode.None && dragMode != BoardDragMode.Pan)
        {
            CadPoint point = CreateViewport().ScreenToCad(e.GetPosition(this), Bounds.Center);
            MoveSelectionTo(point);
        }

        dragMode = BoardDragMode.None;
        Editor.ClearHover();
        Cursor = CursorForEditorState();
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (Editor is null)
        {
            return;
        }

        CadPoint cursorCadPoint = CreateViewport().ScreenToCad(e.GetPosition(this), Bounds.Center);
        Editor.ZoomAt(cursorCadPoint, e.Delta.Y > 0);
        Cursor = new Cursor(StandardCursorType.SizeNorthSouth);
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
        BoardEditorViewModel editor,
        BoardComponentInstance component,
        bool isSelected,
        bool isHovered)
    {
        Point position = Translate(viewport.Map(component.Position), center);
        if (component.FootprintPrimitives.Count == 0)
        {
            DrawFallbackComponent(context, position, isSelected, isHovered);
        }
        else
        {
            DrawFootprintGeometry(context, viewport, center, editor, component, isSelected, isHovered);
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

    private static void DrawFallbackComponent(DrawingContext context, Point position, bool isSelected, bool isHovered)
    {
        Rect body = new(position.X - 28, position.Y - 18, 56, 36);
        context.DrawRectangle(ComponentFillBrush, isSelected ? SelectedComponentPen : isHovered ? HoverSelectionPen : ComponentPen, body, radiusX: 3, radiusY: 3);
        context.DrawEllipse(PadBrush, null, new Point(body.Left + 7, position.Y), 3.5, 3.5);
        context.DrawEllipse(PadBrush, null, new Point(body.Right - 7, position.Y), 3.5, 3.5);
    }

    private static void DrawFootprintGeometry(
        DrawingContext context,
        BoardCanvasViewport viewport,
        Point center,
        BoardEditorViewModel editor,
        BoardComponentInstance component,
        bool isSelected,
        bool isHovered)
    {
        foreach (BoardFootprintPrimitive primitive in editor.VisibleFootprintPrimitives(component))
        {
            DrawFootprintPrimitive(context, viewport, center, editor, component, primitive);
        }

        if (isSelected || isHovered)
        {
            CadRectangle bounds = component.FootprintBounds;
            Point[] corners =
            [
                Translate(viewport.Map(BoardFootprintGeometry.TransformLocalPoint(component, new CadPoint(bounds.Left, bounds.Top))), center),
                Translate(viewport.Map(BoardFootprintGeometry.TransformLocalPoint(component, new CadPoint(bounds.Right, bounds.Top))), center),
                Translate(viewport.Map(BoardFootprintGeometry.TransformLocalPoint(component, new CadPoint(bounds.Right, bounds.Bottom))), center),
                Translate(viewport.Map(BoardFootprintGeometry.TransformLocalPoint(component, new CadPoint(bounds.Left, bounds.Bottom))), center)
            ];
            Rect selectionBounds = new(
                new Point(corners.Min(point => point.X), corners.Min(point => point.Y)),
                new Point(corners.Max(point => point.X), corners.Max(point => point.Y)));
            context.DrawRectangle(null, isSelected ? SelectedComponentPen : isHovered ? HoverSelectionPen : ComponentPen, selectionBounds.Normalize());
        }
    }

    private static void DrawFootprintPrimitive(
        DrawingContext context,
        BoardCanvasViewport viewport,
        Point center,
        BoardEditorViewModel editor,
        BoardComponentInstance component,
        BoardFootprintPrimitive primitive)
    {
        IBrush brush = LayerBrushFor(editor, primitive.LayerName, Color.FromRgb(226, 232, 240));
        Pen pen = new(brush, 1.2);
        switch (primitive)
        {
            case BoardFootprintPadPrimitive pad:
                DrawPrimitivePad(context, viewport, center, component, pad.Position, pad.Size, pad.Shape, pad.DrillSize, brush, pen);
                break;
            case BoardFootprintSmdPrimitive smd:
                DrawPrimitivePad(context, viewport, center, component, smd.Position, smd.Size, smd.Shape, drillSize: 0, brush, pen);
                break;
            case BoardFootprintHolePrimitive hole:
                DrawHole(context, viewport, center, component, hole, brush, pen);
                break;
            case BoardFootprintKeepoutPrimitive keepout:
                DrawKeepout(context, viewport, center, component, keepout, pen);
                break;
            case BoardFootprintLinePrimitive line:
                context.DrawLine(
                    pen,
                    Translate(viewport.Map(BoardFootprintGeometry.TransformLocalPoint(component, line.Start)), center),
                    Translate(viewport.Map(BoardFootprintGeometry.TransformLocalPoint(component, line.End)), center));
                break;
            case BoardFootprintArcPrimitive arc:
                DrawArc(context, viewport, center, component, arc, pen);
                break;
            case BoardFootprintTextPrimitive text:
                DrawFootprintText(context, viewport, center, component, text, brush);
                break;
        }
    }

    private static void DrawPrimitivePad(
        DrawingContext context,
        BoardCanvasViewport viewport,
        Point center,
        BoardComponentInstance component,
        CadPoint position,
        CadVector size,
        string shape,
        long drillSize,
        IBrush brush,
        Pen pen)
    {
        CadVector padSize = BoardFootprintGeometry.SizeForRotation(component, size);
        CadPoint padCenter = BoardFootprintGeometry.TransformLocalPoint(component, position);
        CadPoint first = new(padCenter.X - (padSize.X / 2), padCenter.Y - (padSize.Y / 2));
        CadPoint second = new(padCenter.X + (padSize.X / 2), padCenter.Y + (padSize.Y / 2));
        Rect padRect = new Rect(
            Translate(viewport.Map(first), center),
            Translate(viewport.Map(second), center)).Normalize();
        if (shape is "Round" or "Oval")
        {
            context.DrawEllipse(brush, pen, padRect.Center, padRect.Width / 2, padRect.Height / 2);
        }
        else
        {
            context.DrawRectangle(brush, pen, padRect, radiusX: shape == "RoundedRectangle" ? 2 : 0, radiusY: shape == "RoundedRectangle" ? 2 : 0);
        }

        if (drillSize > 0)
        {
            Point drillCenter = Translate(viewport.Map(padCenter), center);
            double radius = Math.Max(1.2, drillSize * 0.000025 / 2);
            context.DrawEllipse(BackgroundBrush, null, drillCenter, radius, radius);
        }
    }

    private static void DrawHole(
        DrawingContext context,
        BoardCanvasViewport viewport,
        Point center,
        BoardComponentInstance component,
        BoardFootprintHolePrimitive hole,
        IBrush brush,
        Pen pen)
    {
        Point position = Translate(viewport.Map(BoardFootprintGeometry.TransformLocalPoint(component, hole.Position)), center);
        double radius = Math.Max(1.2, hole.DrillSize * 0.000025 / 2);
        context.DrawEllipse(BackgroundBrush, pen, position, radius, radius);
    }

    private static void DrawKeepout(
        DrawingContext context,
        BoardCanvasViewport viewport,
        Point center,
        BoardComponentInstance component,
        BoardFootprintKeepoutPrimitive keepout,
        Pen pen)
    {
        Rect rect = new(
            Translate(viewport.Map(BoardFootprintGeometry.TransformLocalPoint(component, new CadPoint(keepout.Bounds.Left, keepout.Bounds.Top))), center),
            Translate(viewport.Map(BoardFootprintGeometry.TransformLocalPoint(component, new CadPoint(keepout.Bounds.Right, keepout.Bounds.Bottom))), center));
        context.DrawRectangle(null, pen, rect.Normalize());
    }

    private static void DrawArc(
        DrawingContext context,
        BoardCanvasViewport viewport,
        Point center,
        BoardComponentInstance component,
        BoardFootprintArcPrimitive arc,
        Pen pen)
    {
        const int segmentCount = 24;
        CadPoint? previous = null;
        for (int index = 0; index <= segmentCount; index++)
        {
            double angle = (arc.StartAngleDegrees + (arc.SweepAngleDegrees * (index / (double)segmentCount))) * Math.PI / 180;
            CadPoint point = new(
                arc.Center.X + (long)Math.Round(Math.Cos(angle) * arc.Radius),
                arc.Center.Y + (long)Math.Round(Math.Sin(angle) * arc.Radius));
            if (previous is not null)
            {
                context.DrawLine(
                    pen,
                    Translate(viewport.Map(BoardFootprintGeometry.TransformLocalPoint(component, previous.Value)), center),
                    Translate(viewport.Map(BoardFootprintGeometry.TransformLocalPoint(component, point)), center));
            }

            previous = point;
        }
    }

    private static void DrawFootprintText(
        DrawingContext context,
        BoardCanvasViewport viewport,
        Point center,
        BoardComponentInstance component,
        BoardFootprintTextPrimitive text,
        IBrush brush)
    {
        Point position = Translate(viewport.Map(BoardFootprintGeometry.TransformLocalPoint(component, text.Position)), center);
        FormattedText label = new(
            text.Value,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            Math.Max(8, text.Size * 0.000025),
            brush);
        context.DrawText(label, position);
    }

    private static void DrawTrace(
        DrawingContext context,
        BoardCanvasViewport viewport,
        Point center,
        BoardEditorViewModel editor,
        BoardTrace trace,
        bool isSelected,
        bool isHoveredTrace)
    {
        IBrush layerBrush = LayerBrushFor(editor, trace.LayerName, Color.FromRgb(230, 61, 50));
        double width = Math.Max(1.5, trace.WidthInternal * 0.000025);
        Pen tracePen = isSelected
            ? new Pen(SelectedComponentPen.Brush, width + 1.2)
            : isHoveredTrace ? HoverTracePen : new Pen(layerBrush, width);
        DrawRoute(context, viewport, center, trace.RoutePoints, tracePen);
        int? highlightedSegmentIndex = isSelected
            ? editor.SelectedTraceSegmentIndex
            : isHoveredTrace ? editor.HoveredTraceSegmentIndex : null;
        if (highlightedSegmentIndex is { } segmentIndex &&
            segmentIndex > 0 &&
            segmentIndex < trace.RoutePoints.Count)
        {
            context.DrawLine(
                isSelected ? SelectedComponentPen : HoverTracePen,
                Translate(viewport.Map(trace.RoutePoints[segmentIndex - 1]), center),
                Translate(viewport.Map(trace.RoutePoints[segmentIndex]), center));
        }
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
        BoardEditorViewModel editor,
        BoardVia via,
        bool isSelected,
        bool isHoveredVia)
    {
        Point position = Translate(viewport.Map(via.Position), center);
        BoardViaRenderState renderState = CreateViaRenderState(via);
        IBrush viaBrush = LayerBrushFor(editor, renderState.FromLayerName, Color.FromRgb(239, 197, 74));
        context.DrawEllipse(viaBrush, isSelected ? SelectedComponentPen : isHoveredVia ? HoverSelectionPen : PadPen, position, renderState.Radius, renderState.Radius);
        context.DrawEllipse(BackgroundBrush, null, position, renderState.DrillRadius, renderState.DrillRadius);
    }

    public static BoardViaRenderState CreateViaRenderState(BoardVia via) =>
        new(
            Math.Max(3.5, via.DiameterInternal * 0.000025 / 2),
            Math.Max(1.5, via.DrillInternal * 0.000025 / 2),
            via.FromLayerName,
            via.ToLayerName);

    private static IBrush LayerBrushFor(BoardEditorViewModel editor, string layerName, Color fallback)
    {
        BoardLayer? layer = editor.Layers.FirstOrDefault(candidate => candidate.Name == layerName);
        return layer is null
            ? new SolidColorBrush(fallback)
            : new SolidColorBrush(Color.Parse(layer.ColorHex));
    }

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

    private BoardCanvasViewport CreateViewport()
    {
        double zoom = Editor?.ZoomLevel ?? 1.0;
        return new BoardCanvasViewport(Editor?.ViewportOrigin ?? new CadPoint(4_000_000, 0), 0.000025 * zoom);
    }

    private void ItemsChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        InvalidateVisual();

    private void EditorPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(BoardEditorViewModel.SelectedComponent) or
            nameof(BoardEditorViewModel.SelectedTraceSegmentIndex) or
            nameof(BoardEditorViewModel.IsGridVisible) or
            nameof(BoardEditorViewModel.GridStyle) or
            nameof(BoardEditorViewModel.GridSpacingInternal) or
            nameof(BoardEditorViewModel.ZoomLevel) or
            nameof(BoardEditorViewModel.ViewportOrigin) or
            nameof(BoardEditorViewModel.PendingTraceStart) or
            nameof(BoardEditorViewModel.PendingTraceRoutePoints) or
            nameof(BoardEditorViewModel.ActiveTool) or
            nameof(BoardEditorViewModel.HoveredComponent) or
            nameof(BoardEditorViewModel.HoveredTrace) or
            nameof(BoardEditorViewModel.HoveredTraceSegmentIndex) or
            nameof(BoardEditorViewModel.HoveredVia) or
            nameof(BoardEditorViewModel.VisibleTraces) or
            nameof(BoardEditorViewModel.VisibleVias) or
            nameof(BoardEditorViewModel.ActiveLayerName) or
            nameof(BoardEditorViewModel.SelectedTrace) or
            nameof(BoardEditorViewModel.SelectedVia))
        {
            InvalidateVisual();
        }
    }

    private Cursor? CursorForEditorState()
    {
        if (Editor?.ActiveTool == "Route")
        {
            return new Cursor(StandardCursorType.Cross);
        }

        if (Editor?.HoveredComponent is not null ||
            Editor?.HoveredTrace is not null ||
            Editor?.HoveredVia is not null)
        {
            return new Cursor(StandardCursorType.SizeAll);
        }

        return null;
    }

    private enum BoardDragMode
    {
        None,
        Pan,
        Component,
        Via,
        TraceSegment
    }
}

public sealed record BoardViaRenderState(
    double Radius,
    double DrillRadius,
    string FromLayerName,
    string ToLayerName);
