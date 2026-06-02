using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using DragonCAD.App.ComponentManager;
using DragonCAD.App.Diagnostics;
using DragonCAD.Core.Geometry;

namespace DragonCAD.App.SchematicEditor;

public sealed class SchematicCanvasControl : Control
{
    public static readonly StyledProperty<SchematicEditorViewModel?> EditorProperty =
        AvaloniaProperty.Register<SchematicCanvasControl, SchematicEditorViewModel?>(nameof(Editor));

    public static readonly StyledProperty<ISchematicPlacementTarget?> PlacementTargetProperty =
        AvaloniaProperty.Register<SchematicCanvasControl, ISchematicPlacementTarget?>(nameof(PlacementTarget));

    private static readonly IBrush BackgroundBrush = new SolidColorBrush(Color.FromRgb(250, 247, 238));
    private static readonly Pen GridPen = new(new SolidColorBrush(Color.FromRgb(220, 216, 204)), 1);
    private static readonly Pen SymbolPen = new(new SolidColorBrush(Color.FromRgb(23, 128, 77)), 1.4);
    private static readonly Pen PinPen = new(new SolidColorBrush(Color.FromRgb(184, 56, 54)), 1.3);
    private static readonly Pen WirePen = new(new SolidColorBrush(Color.FromRgb(0, 92, 210)), 3.0);
    private static readonly Pen PendingWirePen = new(new SolidColorBrush(Color.FromRgb(48, 130, 220)), 2.4);
    private static readonly Pen SheetPen = new(new SolidColorBrush(Color.FromRgb(122, 132, 145)), 1.2);
    private static readonly Pen SelectionPen = new(new SolidColorBrush(Color.FromRgb(40, 130, 255)), 1.6);
    private static readonly Pen HoverSelectionPen = new(new SolidColorBrush(Color.FromRgb(70, 180, 255)), 1.4);
    private static readonly Pen WireSelectionPen = new(new SolidColorBrush(Color.FromRgb(255, 176, 52)), 3.0);
    private static readonly Pen HoverWirePen = new(new SolidColorBrush(Color.FromRgb(70, 180, 255)), 3.2);
    private static readonly Pen NetLabelSelectionPen = new(new SolidColorBrush(Color.FromRgb(255, 176, 52)), 1.4);
    private static readonly IBrush HoverPinBrush = new SolidColorBrush(Color.FromRgb(255, 213, 74));
    private static readonly IBrush WireNodeBrush = new SolidColorBrush(Color.FromRgb(0, 92, 210));
    private static readonly IBrush NetLabelBrush = new SolidColorBrush(Color.FromRgb(0, 70, 160));
    private static readonly IBrush TextBrush = new SolidColorBrush(Color.FromRgb(22, 37, 52));
    private static readonly IBrush PinLabelBrush = new SolidColorBrush(Color.FromRgb(73, 43, 32));
    private bool isPanningViewport;
    private Point lastPanScreenPoint;

    static SchematicCanvasControl()
    {
        AffectsRender<SchematicCanvasControl>(EditorProperty);
    }

    public SchematicEditorViewModel? Editor
    {
        get => GetValue(EditorProperty);
        set => SetValue(EditorProperty, value);
    }

    public ISchematicPlacementTarget? PlacementTarget
    {
        get => GetValue(PlacementTargetProperty);
        set => SetValue(PlacementTargetProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == EditorProperty)
        {
            if (change.OldValue is SchematicEditorViewModel oldEditor)
            {
                oldEditor.Components.CollectionChanged -= ComponentsChanged;
                oldEditor.Wires.CollectionChanged -= WiresChanged;
                oldEditor.NetLabels.CollectionChanged -= NetLabelsChanged;
                oldEditor.PropertyChanged -= EditorPropertyChanged;
            }

            if (change.NewValue is SchematicEditorViewModel newEditor)
            {
                newEditor.Components.CollectionChanged += ComponentsChanged;
                newEditor.Wires.CollectionChanged += WiresChanged;
                newEditor.NetLabels.CollectionChanged += NetLabelsChanged;
                newEditor.PropertyChanged += EditorPropertyChanged;
            }
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        Rect bounds = new(Bounds.Size);
        context.DrawRectangle(BackgroundBrush, null, bounds);

        if (Editor is null)
        {
            return;
        }

        SchematicCanvasViewport viewport = CreateViewport();
        DrawGrid(context, bounds, bounds.Center, viewport, Editor);
        DrawSheet(context, viewport, bounds.Center, Editor.SheetBounds);
        foreach (SchematicComponentInstance instance in Editor.Components)
        {
            DrawInstance(
                context,
                viewport,
                bounds.Center,
                instance,
                ReferenceEquals(instance, Editor.SelectedComponent),
                ReferenceEquals(instance, Editor.HoveredComponent));
        }

        foreach (SchematicWire wire in Editor.Wires)
        {
            bool isSelectedWire = Editor.SelectedWire?.WireId == wire.WireId;
            bool isHoveredWire = Editor.HoveredWire?.WireId == wire.WireId;
            DrawCompletedWire(
                context,
                viewport,
                bounds.Center,
                wire,
                isSelectedWire ? WireSelectionPen : isHoveredWire ? HoverWirePen : WirePen,
                isSelectedWire ? Editor.SelectedWireSegmentIndex : isHoveredWire ? Editor.HoveredWireSegmentIndex : null);
        }

        foreach (SchematicNetLabelRenderItem label in Editor.RenderableNetLabels)
        {
            DrawNetLabel(context, viewport, bounds.Center, label);
        }

        DrawPendingWire(context, viewport, bounds.Center, Editor.PendingWirePreviewRoutePoints);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (PlacementTarget is null)
        {
            return;
        }

        CadPoint point = CreateViewport().ScreenToCad(e.GetPosition(this), Bounds.Center);
        DragonCadLog.Info($"schematic-canvas pointer-pressed position={e.GetPosition(this)} cad=({point.X},{point.Y}) placementTarget={PlacementTarget.GetType().Name}");
        PlacementTarget.HandleSchematicPointerPressed(point);
        if (PlacementTarget.IsDraggingSchematicComponent || PlacementTarget.IsDraggingSchematicWireSegment)
        {
            Cursor = new Cursor(StandardCursorType.SizeAll);
            e.Pointer.Capture(this);
        }
        else if (PlacementTarget.CanPanSchematicViewport &&
            Editor?.SelectedComponent is null &&
            Editor?.SelectedWire is null &&
            Editor?.SelectedNetLabel is null)
        {
            isPanningViewport = true;
            lastPanScreenPoint = e.GetPosition(this);
            Cursor = new Cursor(StandardCursorType.SizeAll);
            e.Pointer.Capture(this);
        }

        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (PlacementTarget is null)
        {
            return;
        }

        if (isPanningViewport && Editor is not null)
        {
            Point currentPoint = e.GetPosition(this);
            Editor.PanViewportByScreenDelta(currentPoint - lastPanScreenPoint, 0.00002 * Editor.ZoomLevel);
            lastPanScreenPoint = currentPoint;
            Cursor = new Cursor(StandardCursorType.SizeAll);
            e.Handled = true;
            return;
        }

        CadPoint point = CreateViewport().ScreenToCad(e.GetPosition(this), Bounds.Center);
        PlacementTarget.HandleSchematicPointerMoved(point);
        Cursor = PlacementTarget.IsDraggingSchematicComponent || PlacementTarget.IsDraggingSchematicWireSegment
            ? new Cursor(StandardCursorType.SizeAll)
            : CursorForEditorState();
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (PlacementTarget is null)
        {
            return;
        }

        if (isPanningViewport)
        {
            isPanningViewport = false;
            Cursor = null;
            e.Pointer.Capture(null);
            e.Handled = true;
            return;
        }

        CadPoint point = CreateViewport().ScreenToCad(e.GetPosition(this), Bounds.Center);
        PlacementTarget.HandleSchematicPointerReleased(point);
        Cursor = null;
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

    private static void DrawGrid(
        DrawingContext context,
        Rect bounds,
        Point center,
        SchematicCanvasViewport viewport,
        SchematicEditorViewModel editor)
    {
        if (!editor.IsGridVisible)
        {
            return;
        }

        double spacing = Math.Max(4, Math.Abs(
            viewport.Map(new CadPoint(0, 0), new CadPoint(editor.GridSpacingInternal, 0)).X -
            viewport.Map(new CadPoint(0, 0), new CadPoint(0, 0)).X));
        double startX = center.X % spacing;
        double startY = center.Y % spacing;
        if (editor.GridStyle == "Lines")
        {
            for (double x = startX; x <= bounds.Width; x += spacing)
            {
                context.DrawLine(GridPen, new Point(x, 0), new Point(x, bounds.Height));
            }

            for (double y = startY; y <= bounds.Height; y += spacing)
            {
                context.DrawLine(GridPen, new Point(0, y), new Point(bounds.Width, y));
            }

            return;
        }

        for (double x = startX; x <= bounds.Width; x += spacing)
        {
            for (double y = startY; y <= bounds.Height; y += spacing)
            {
                context.DrawEllipse(GridPen.Brush, null, new Point(x, y), 1.1, 1.1);
            }
        }
    }

    private static void DrawRoutedWire(
        DrawingContext context,
        SchematicCanvasViewport viewport,
        Point center,
        IReadOnlyList<CadPoint> routePoints,
        Pen pen)
    {
        for (int index = 1; index < routePoints.Count; index++)
        {
            context.DrawLine(
                pen,
                Translate(viewport.Map(new CadPoint(0, 0), routePoints[index - 1]), center),
                Translate(viewport.Map(new CadPoint(0, 0), routePoints[index]), center));
        }
    }

    private static void DrawCompletedWire(
        DrawingContext context,
        SchematicCanvasViewport viewport,
        Point center,
        SchematicWire wire,
        Pen pen,
        int? selectedSegmentIndex)
    {
        DrawRoutedWire(context, viewport, center, wire.RoutePoints, pen);
        if (selectedSegmentIndex is { } segmentIndex &&
            segmentIndex > 0 &&
            segmentIndex < wire.RoutePoints.Count)
        {
            context.DrawLine(
                WireSelectionPen,
                Translate(viewport.Map(new CadPoint(0, 0), wire.RoutePoints[segmentIndex - 1]), center),
                Translate(viewport.Map(new CadPoint(0, 0), wire.RoutePoints[segmentIndex]), center));
        }

        foreach (CadPoint routePoint in wire.RoutePoints)
        {
            Point point = Translate(viewport.Map(new CadPoint(0, 0), routePoint), center);
            context.DrawEllipse(WireNodeBrush, null, point, 3.8, 3.8);
        }

        if (!string.IsNullOrWhiteSpace(wire.NetName) && wire.RoutePoints.Count > 1)
        {
            CadPoint labelPoint = wire.RoutePoints[wire.RoutePoints.Count / 2];
            FormattedText label = new(
                wire.NetName,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                Typeface.Default,
                11,
                NetLabelBrush);
            Point mapped = Translate(viewport.Map(new CadPoint(0, 0), labelPoint), center);
            context.DrawText(label, new Point(mapped.X + 5, mapped.Y - label.Height - 5));
        }
    }

    private static void DrawPendingWire(
        DrawingContext context,
        SchematicCanvasViewport viewport,
        Point center,
        IReadOnlyList<CadPoint> routePoints)
    {
        if (routePoints.Count < 2)
        {
            return;
        }

        DrawRoutedWire(context, viewport, center, routePoints, PendingWirePen);
    }

    private static void DrawNetLabel(
        DrawingContext context,
        SchematicCanvasViewport viewport,
        Point center,
        SchematicNetLabelRenderItem label)
    {
        Point mapped = Translate(viewport.Map(new CadPoint(0, 0), label.Position), center);
        FormattedText text = new(
            label.NetName,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            12,
            NetLabelBrush);
        Point origin = new(mapped.X + 6, mapped.Y - (text.Height / 2));
        if (label.IsSelected)
        {
            Rect highlightBounds = new(
                new Point(origin.X - 4, origin.Y - 2),
                new Size(text.Width + 8, text.Height + 4));
            context.DrawRectangle(null, NetLabelSelectionPen, highlightBounds);
        }

        context.DrawLine(PinPen, new Point(mapped.X - 4, mapped.Y), new Point(mapped.X + 4, mapped.Y));
        context.DrawLine(PinPen, new Point(mapped.X, mapped.Y - 4), new Point(mapped.X, mapped.Y + 4));
        context.DrawText(text, origin);
    }

    private void DrawInstance(
        DrawingContext context,
        SchematicCanvasViewport viewport,
        Point center,
        SchematicComponentInstance instance,
        bool isSelected,
        bool isHovered)
    {
        if (isSelected)
        {
            DrawSelection(context, viewport, center, instance, SelectionPen);
        }
        else if (isHovered)
        {
            DrawSelection(context, viewport, center, instance, HoverSelectionPen);
        }

        foreach (ComponentPreviewLine line in instance.SymbolPreview.Lines)
        {
            context.DrawLine(
                SymbolPen,
                Translate(viewport.Map(new CadPoint(0, 0), SchematicEditorViewModel.TransformLocalPoint(instance, line.Start)), center),
                Translate(viewport.Map(new CadPoint(0, 0), SchematicEditorViewModel.TransformLocalPoint(instance, line.End)), center));
        }

        foreach (ComponentSymbolPinPreview pin in instance.SymbolPreview.Pins)
        {
            Point connectPoint = Translate(viewport.Map(new CadPoint(0, 0), SchematicEditorViewModel.TransformLocalPoint(instance, pin.ConnectPoint)), center);
            bool isHoveredPin = IsHoveredPin(instance, pin);
            context.DrawLine(
                isHoveredPin ? WireSelectionPen : PinPen,
                connectPoint,
                Translate(viewport.Map(new CadPoint(0, 0), SchematicEditorViewModel.TransformLocalPoint(instance, pin.BodyPoint)), center));
            context.DrawEllipse(isHoveredPin ? HoverPinBrush : null, isHoveredPin ? WireSelectionPen : PinPen, connectPoint, isHoveredPin ? 4.5 : 3, isHoveredPin ? 4.5 : 3);
            DrawPinLabel(context, connectPoint, pin);
        }

        FormattedText label = new(
            instance.ReferenceDesignator,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            12,
            TextBrush);
        context.DrawText(label, Translate(viewport.Map(instance.Position, new CadPoint(0, -6_500_000)), center));

        if (!string.IsNullOrWhiteSpace(instance.Value))
        {
            FormattedText valueLabel = new(
                instance.Value,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                Typeface.Default,
                11,
                TextBrush);
            context.DrawText(valueLabel, Translate(viewport.Map(instance.Position, new CadPoint(0, 7_200_000)), center));
        }
    }

    private static void DrawPinLabel(DrawingContext context, Point connectPoint, ComponentSymbolPinPreview pin)
    {
        if (string.IsNullOrWhiteSpace(pin.Name))
        {
            return;
        }

        FormattedText label = new(
            pin.Name,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            10,
            PinLabelBrush);
        context.DrawText(label, SchematicPinLabelLayout.LabelOrigin(connectPoint, pin.Orientation, label.Width, label.Height));
    }

    private static void DrawSelection(
        DrawingContext context,
        SchematicCanvasViewport viewport,
        Point center,
        SchematicComponentInstance instance,
        Pen pen)
    {
        CadRectangle bounds = instance.SymbolPreview.Bounds;
        Point[] corners =
        [
            Translate(viewport.Map(new CadPoint(0, 0), SchematicEditorViewModel.TransformLocalPoint(instance, new CadPoint(bounds.Left - 250_000, bounds.Top - 250_000))), center),
            Translate(viewport.Map(new CadPoint(0, 0), SchematicEditorViewModel.TransformLocalPoint(instance, new CadPoint(bounds.Right + 250_000, bounds.Top - 250_000))), center),
            Translate(viewport.Map(new CadPoint(0, 0), SchematicEditorViewModel.TransformLocalPoint(instance, new CadPoint(bounds.Right + 250_000, bounds.Bottom + 250_000))), center),
            Translate(viewport.Map(new CadPoint(0, 0), SchematicEditorViewModel.TransformLocalPoint(instance, new CadPoint(bounds.Left - 250_000, bounds.Bottom + 250_000))), center)
        ];
        Rect selectionBounds = new(
            new Point(corners.Min(point => point.X), corners.Min(point => point.Y)),
            new Point(corners.Max(point => point.X), corners.Max(point => point.Y)));
        context.DrawRectangle(null, pen, selectionBounds.Normalize());
    }

    private bool IsHoveredPin(SchematicComponentInstance instance, ComponentSymbolPinPreview pin) =>
        IsMatchingPin(Editor?.HoveredPin, instance, pin) ||
        IsMatchingPin(Editor?.SelectedPinEndpoint, instance, pin);

    private static bool IsMatchingPin(
        SchematicPinEndpoint? endpoint,
        SchematicComponentInstance instance,
        ComponentSymbolPinPreview pin) =>
        endpoint is not null &&
        endpoint.InstanceId == instance.InstanceId &&
        endpoint.PinName == pin.Name;

    private void ComponentsChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        InvalidateVisual();

    private void WiresChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        InvalidateVisual();

    private void NetLabelsChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        InvalidateVisual();

    private void EditorPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SchematicEditorViewModel.ZoomLevel) or
            nameof(SchematicEditorViewModel.ViewportOrigin) or
            nameof(SchematicEditorViewModel.IsGridVisible) or
            nameof(SchematicEditorViewModel.GridStyle) or
            nameof(SchematicEditorViewModel.GridSpacingInternal) or
            nameof(SchematicEditorViewModel.SelectedComponent) or
            nameof(SchematicEditorViewModel.SelectedWire) or
            nameof(SchematicEditorViewModel.SelectedWireSegmentIndex) or
            nameof(SchematicEditorViewModel.SelectedNetLabel) or
            nameof(SchematicEditorViewModel.SelectedPinEndpoint) or
            nameof(SchematicEditorViewModel.HoveredPin) or
            nameof(SchematicEditorViewModel.HoveredComponent) or
            nameof(SchematicEditorViewModel.HoveredWire) or
            nameof(SchematicEditorViewModel.HoveredWireSegmentIndex) or
            nameof(SchematicEditorViewModel.PendingWireRoutePoints) or
            nameof(SchematicEditorViewModel.PendingWirePreviewPoint) or
            nameof(SchematicEditorViewModel.PendingWirePreviewRoutePoints))
        {
            InvalidateVisual();
        }
    }

    private static void DrawSheet(DrawingContext context, SchematicCanvasViewport viewport, Point center, CadRectangle sheet)
    {
        Point topLeft = Translate(viewport.Map(new CadPoint(0, 0), new CadPoint(sheet.Left, sheet.Top)), center);
        Point bottomRight = Translate(viewport.Map(new CadPoint(0, 0), new CadPoint(sheet.Right, sheet.Bottom)), center);
        context.DrawRectangle(null, SheetPen, new Rect(topLeft, bottomRight).Normalize());
    }

    private SchematicCanvasViewport CreateViewport()
    {
        double zoom = Editor?.ZoomLevel ?? 1.0;
        return new SchematicCanvasViewport(Editor?.ViewportOrigin ?? new CadPoint(0, 0), pixelsPerInternalUnit: 0.00002 * zoom);
    }

    private static Point Translate(Point point, Point offset) =>
        new(point.X + offset.X, point.Y + offset.Y);

    private Cursor? CursorForEditorState()
    {
        if (Editor?.PendingWireStart is not null)
        {
            return new Cursor(StandardCursorType.Cross);
        }

        if (Editor?.HoveredPin is not null)
        {
            return new Cursor(StandardCursorType.Hand);
        }

        if (Editor?.HoveredComponent is not null ||
            Editor?.HoveredWire is not null)
        {
            return new Cursor(StandardCursorType.SizeAll);
        }

        return null;
    }
}
