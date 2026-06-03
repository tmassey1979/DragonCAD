using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using DragonCAD.App.ComponentManager;
using DragonCAD.App.SchematicEditor;
using DragonCAD.Core.Geometry;

namespace DragonCAD.App.BoardEditor;

public sealed class BoardEditorViewModel : INotifyPropertyChanged
{
    private static readonly CadVector AutoPlacementStep = new(8_000_000, 0);
    private CadGrid placementGrid = new(new CadVector(CadUnit.InternalUnitsPerMillimeter, CadUnit.InternalUnitsPerMillimeter));
    private string statusText = "Board ready.";
    private BoardComponentInstance? selectedComponent;
    private bool isGridVisible = true;
    private string gridStyle = "Dots";
    private long gridSpacingInternal = CadUnit.InternalUnitsPerMillimeter;
    private double zoomLevel = 1.0;
    private CadPoint viewportOrigin = new(4_000_000, 0);
    private string activeTool = "Select";
    private string activeLayerName = "Top";
    private CadPoint? pendingTraceStart;
    private BoardTrace? selectedTrace;
    private BoardVia? selectedVia;
    private int? selectedTraceSegmentIndex;
    private BoardComponentInstance? hoveredComponent;
    private BoardTrace? hoveredTrace;
    private BoardVia? hoveredVia;
    private int? hoveredTraceSegmentIndex;
    private BoardPadHit? pendingTraceStartPad;
    private string routeCornerMode = "90";
    private readonly List<CadPoint> pendingTraceRoutePoints = [];

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<BoardComponentInstance> Components { get; } = [];

    public ObservableCollection<BoardAirwire> Airwires { get; } = [];

    public ObservableCollection<BoardTrace> Traces { get; } = [];

    public ObservableCollection<BoardVia> Vias { get; } = [];

    public ObservableCollection<BoardLayer> Layers { get; } =
    [
        new("Top", "#E63D32"),
        new("Bottom", "#2D8CFF"),
        new("Silkscreen", "#E2E8F0"),
        new("Dimension", "#A3E635"),
        new("Keepout", "#F43F5E"),
        new("Names", "#F8FAFC"),
        new("Values", "#CBD5E1"),
        new("Drills", "#94A3B8")
    ];

    public IReadOnlyList<BoardTrace> VisibleTraces =>
        Traces
            .Where(trace => Layers.Any(layer => layer.Name == trace.LayerName && layer.IsVisible))
            .ToArray();

    public IReadOnlyList<BoardVia> VisibleVias =>
        Vias
            .Where(via =>
                Layers.Any(layer => layer.Name == via.FromLayerName && layer.IsVisible) ||
                Layers.Any(layer => layer.Name == via.ToLayerName && layer.IsVisible))
            .ToArray();

    public IReadOnlyList<BoardFootprintPrimitive> VisibleFootprintPrimitives(BoardComponentInstance component) =>
        component.FootprintPrimitives
            .Where(primitive => Layers.Any(layer => layer.Name == primitive.LayerName && layer.IsVisible))
            .ToArray();

    public string ActiveLayerName
    {
        get => activeLayerName;
        private set
        {
            if (activeLayerName == value)
            {
                return;
            }

            activeLayerName = value;
            OnPropertyChanged();
        }
    }

    public string ActiveTool
    {
        get => activeTool;
        private set
        {
            if (activeTool == value)
            {
                return;
            }

            activeTool = value;
            OnPropertyChanged();
        }
    }

    public CadPoint? PendingTraceStart
    {
        get => pendingTraceStart;
        private set
        {
            if (pendingTraceStart == value)
            {
                return;
            }

            pendingTraceStart = value;
            OnPropertyChanged();
        }
    }

    public IReadOnlyList<CadPoint> PendingTraceRoutePoints => pendingTraceRoutePoints;

    public IReadOnlyList<string> RouteCornerModes { get; } = ["90", "45"];

    public string RouteCornerMode
    {
        get => routeCornerMode;
        set
        {
            string normalized = NormalizeRouteCornerMode(value);
            if (routeCornerMode == normalized)
            {
                return;
            }

            routeCornerMode = normalized;
            OnPropertyChanged();
            StatusText = $"Board route corner mode {RouteCornerMode} degrees.";
        }
    }

    public bool IsGridVisible
    {
        get => isGridVisible;
        private set
        {
            if (isGridVisible == value)
            {
                return;
            }

            isGridVisible = value;
            OnPropertyChanged();
        }
    }

    public string GridStyle
    {
        get => gridStyle;
        private set
        {
            if (gridStyle == value)
            {
                return;
            }

            gridStyle = value;
            OnPropertyChanged();
        }
    }

    public long GridSpacingInternal
    {
        get => gridSpacingInternal;
        private set
        {
            if (gridSpacingInternal == value)
            {
                return;
            }

            gridSpacingInternal = value;
            placementGrid = new CadGrid(new CadVector(value, value));
            OnPropertyChanged();
        }
    }

    public double ZoomLevel
    {
        get => zoomLevel;
        private set
        {
            if (Math.Abs(zoomLevel - value) < 0.0001)
            {
                return;
            }

            zoomLevel = value;
            OnPropertyChanged();
        }
    }

    public CadPoint ViewportOrigin
    {
        get => viewportOrigin;
        private set
        {
            if (viewportOrigin == value)
            {
                return;
            }

            viewportOrigin = value;
            OnPropertyChanged();
        }
    }

    public BoardComponentInstance? SelectedComponent
    {
        get => selectedComponent;
        private set
        {
            if (selectedComponent == value)
            {
                return;
            }

            selectedComponent = value;
            OnPropertyChanged();
        }
    }

    public BoardTrace? SelectedTrace
    {
        get => selectedTrace;
        private set
        {
            if (selectedTrace == value)
            {
                return;
            }

            selectedTrace = value;
            if (value is null)
            {
                SelectedTraceSegmentIndex = null;
            }

            OnPropertyChanged();
        }
    }

    public int? SelectedTraceSegmentIndex
    {
        get => selectedTraceSegmentIndex;
        private set
        {
            if (selectedTraceSegmentIndex == value)
            {
                return;
            }

            selectedTraceSegmentIndex = value;
            OnPropertyChanged();
        }
    }

    public BoardVia? SelectedVia
    {
        get => selectedVia;
        private set
        {
            if (selectedVia == value)
            {
                return;
            }

            selectedVia = value;
            OnPropertyChanged();
        }
    }

    public BoardComponentInstance? HoveredComponent
    {
        get => hoveredComponent;
        private set
        {
            if (hoveredComponent == value)
            {
                return;
            }

            hoveredComponent = value;
            OnPropertyChanged();
        }
    }

    public BoardTrace? HoveredTrace
    {
        get => hoveredTrace;
        private set
        {
            if (hoveredTrace == value)
            {
                return;
            }

            hoveredTrace = value;
            if (value is null)
            {
                HoveredTraceSegmentIndex = null;
            }

            OnPropertyChanged();
        }
    }

    public int? HoveredTraceSegmentIndex
    {
        get => hoveredTraceSegmentIndex;
        private set
        {
            if (hoveredTraceSegmentIndex == value)
            {
                return;
            }

            hoveredTraceSegmentIndex = value;
            OnPropertyChanged();
        }
    }

    public BoardVia? HoveredVia
    {
        get => hoveredVia;
        private set
        {
            if (hoveredVia == value)
            {
                return;
            }

            hoveredVia = value;
            OnPropertyChanged();
        }
    }

    public string StatusText
    {
        get => statusText;
        private set
        {
            if (statusText == value)
            {
                return;
            }

            statusText = value;
            OnPropertyChanged();
        }
    }

    public void SynchronizeFromSchematic(IReadOnlyList<SchematicComponentInstance> schematicComponents)
    {
        SynchronizeFromSchematic(schematicComponents, []);
    }

    public void Clear()
    {
        Components.Clear();
        Airwires.Clear();
        Traces.Clear();
        Vias.Clear();
        pendingTraceRoutePoints.Clear();
        PendingTraceStart = null;
        SelectedComponent = null;
        SelectedTrace = null;
        SelectedVia = null;
        SelectedTraceSegmentIndex = null;
        ClearHover();
        StatusText = "Board cleared.";
    }

    public void SynchronizeFromSchematic(
        IReadOnlyList<SchematicComponentInstance> schematicComponents,
        IReadOnlyList<SchematicWire> schematicWires)
    {
        ArgumentNullException.ThrowIfNull(schematicComponents);
        ArgumentNullException.ThrowIfNull(schematicWires);

        foreach (SchematicComponentInstance schematicComponent in schematicComponents)
        {
            BoardComponentInstance boardComponent = new(
                schematicComponent.InstanceId,
                schematicComponent.ReferenceDesignator,
                schematicComponent.ComponentId,
                schematicComponent.DisplayName,
                BoardPositionFor(schematicComponent.InstanceId, Components.Count),
                schematicComponent.FootprintPreview,
                schematicComponent.Value,
                schematicComponent.RotationDegrees,
                schematicComponent.IsMirrored);

            int existingIndex = IndexOfSyncId(schematicComponent.InstanceId);
            if (existingIndex >= 0)
            {
                Components[existingIndex] = boardComponent;
            }
            else
            {
                Components.Add(boardComponent);
            }
        }

        HashSet<string> liveSyncIds = schematicComponents
            .Select(component => component.InstanceId)
            .ToHashSet(StringComparer.Ordinal);
        for (int index = Components.Count - 1; index >= 0; index--)
        {
            if (!liveSyncIds.Contains(Components[index].SyncId))
            {
                Components.RemoveAt(index);
            }
        }

        Airwires.Clear();
        foreach (SchematicWire wire in schematicWires)
        {
            Airwires.Add(new BoardAirwire(
                wire.NetName,
                wire.Start.InstanceId,
                wire.Start.ReferenceDesignator,
                wire.Start.PinName,
                BoardPositionForWireEndpoint(wire.Start.InstanceId, wire.Start.PinName),
                wire.End.InstanceId,
                wire.End.ReferenceDesignator,
                wire.End.PinName,
                BoardPositionForWireEndpoint(wire.End.InstanceId, wire.End.PinName)));
        }

        string componentText = $"{Components.Count} board component{(Components.Count == 1 ? "" : "s")}";
        if (Airwires.Count == 0)
        {
            StatusText = $"Synchronized {componentText} from schematic.";
            return;
        }

        string airwireText = $"{Airwires.Count} airwire{(Airwires.Count == 1 ? "" : "s")}";
        StatusText = $"Synchronized {componentText} and {airwireText} from schematic.";
    }

    private int IndexOfSyncId(string syncId)
    {
        for (int index = 0; index < Components.Count; index++)
        {
            if (Components[index].SyncId == syncId)
            {
                return index;
            }
        }

        return -1;
    }

    public BoardComponentInstance? SelectComponentAt(CadPoint point)
    {
        BoardComponentInstance? candidate = FindComponentAt(point);
        if (candidate is null)
        {
            SelectedComponent = null;
            SelectedTraceSegmentIndex = null;
            StatusText = "No board component selected.";
            return null;
        }

        SelectedComponent = candidate;
        SelectedTrace = null;
        SelectedVia = null;
        SelectedTraceSegmentIndex = null;
        StatusText = $"Selected board component {candidate.ReferenceDesignator}.";
        return candidate;
    }

    public bool SelectAt(CadPoint point)
    {
        if (SelectViaAt(point) is not null)
        {
            return true;
        }

        if (SelectTraceAt(point) is not null)
        {
            return true;
        }

        return SelectComponentAt(point) is not null;
    }

    public void UpdateHoverAt(CadPoint point)
    {
        if (ActiveTool == "Route")
        {
            ClearHover();
            return;
        }

        BoardVia? via = FindViaAt(point, VisibleVias);
        if (via is not null)
        {
            HoveredVia = via;
            HoveredTrace = null;
            HoveredComponent = null;
            return;
        }

        TraceHit? traceHit = FindTraceAt(point);
        if (traceHit is not null)
        {
            HoveredVia = null;
            HoveredTrace = traceHit.Trace;
            HoveredTraceSegmentIndex = traceHit.SegmentIndex;
            HoveredComponent = null;
            return;
        }

        BoardComponentInstance? component = FindComponentAt(point);
        if (component is not null)
        {
            HoveredVia = null;
            HoveredTrace = null;
            HoveredComponent = component;
            return;
        }

        ClearHover();
    }

    public void ClearHover()
    {
        HoveredComponent = null;
        HoveredTrace = null;
        HoveredTraceSegmentIndex = null;
        HoveredVia = null;
    }

    public BoardComponentInstance MoveSelectedComponentTo(CadPoint requestedPosition)
    {
        if (SelectedComponent is null)
        {
            throw new InvalidOperationException("No board component is selected.");
        }

        int index = Components.IndexOf(SelectedComponent);
        if (index < 0)
        {
            throw new InvalidOperationException("The selected board component is no longer in the document.");
        }

        BoardComponentInstance moved = SelectedComponent with
        {
            Position = placementGrid.Snap(requestedPosition)
        };
        Components[index] = moved;
        SelectedComponent = moved;
        RefreshAirwireEndpoints();
        StatusText = $"Moved {moved.ReferenceDesignator} to {FormatMillimeters(moved.Position.X)} mm, {FormatMillimeters(moved.Position.Y)} mm.";
        return moved;
    }

    public BoardComponentInstance RotateSelectedComponentClockwise()
    {
        if (SelectedComponent is null)
        {
            throw new InvalidOperationException("No board component is selected.");
        }

        int index = Components.IndexOf(SelectedComponent);
        if (index < 0)
        {
            throw new InvalidOperationException("The selected board component is no longer in the document.");
        }

        BoardComponentInstance rotated = SelectedComponent with
        {
            RotationDegrees = (SelectedComponent.RotationDegrees + 90) % 360
        };
        Components[index] = rotated;
        SelectedComponent = rotated;
        RefreshAirwireEndpoints();
        StatusText = $"Rotated board component {rotated.ReferenceDesignator} to {rotated.RotationDegrees} degrees.";
        return rotated;
    }

    public BoardComponentInstance MirrorSelectedComponent()
    {
        if (SelectedComponent is null)
        {
            throw new InvalidOperationException("No board component is selected.");
        }

        int index = Components.IndexOf(SelectedComponent);
        if (index < 0)
        {
            throw new InvalidOperationException("The selected board component is no longer in the document.");
        }

        BoardComponentInstance mirrored = SelectedComponent with
        {
            IsMirrored = !SelectedComponent.IsMirrored
        };
        Components[index] = mirrored;
        SelectedComponent = mirrored;
        RefreshAirwireEndpoints();
        StatusText = $"Mirrored board component {mirrored.ReferenceDesignator}.";
        return mirrored;
    }

    public BoardVia MoveSelectedViaTo(CadPoint requestedPosition)
    {
        if (SelectedVia is null)
        {
            throw new InvalidOperationException("No board via is selected.");
        }

        int index = Vias.IndexOf(SelectedVia);
        if (index < 0)
        {
            throw new InvalidOperationException("The selected via is no longer in the document.");
        }

        BoardVia moved = SelectedVia with
        {
            Position = placementGrid.Snap(requestedPosition)
        };
        Vias[index] = moved;
        SelectedVia = moved;
        StatusText = $"Moved via to {FormatMillimeters(moved.Position.X)} mm, {FormatMillimeters(moved.Position.Y)} mm.";
        return moved;
    }

    public BoardVia SetSelectedViaSizeInternal(long diameterInternal, long drillInternal)
    {
        if (diameterInternal <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(diameterInternal), "Via diameter must be positive.");
        }

        if (drillInternal <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(drillInternal), "Via drill must be positive.");
        }

        if (drillInternal >= diameterInternal)
        {
            throw new ArgumentOutOfRangeException(nameof(drillInternal), "Via drill must be smaller than the diameter.");
        }

        if (SelectedVia is null)
        {
            throw new InvalidOperationException("No board via is selected.");
        }

        int index = Vias.IndexOf(SelectedVia);
        if (index < 0)
        {
            throw new InvalidOperationException("The selected via is no longer in the document.");
        }

        BoardVia resized = SelectedVia with
        {
            DiameterInternal = diameterInternal,
            DrillInternal = drillInternal
        };
        Vias[index] = resized;
        SelectedVia = resized;
        StatusText = $"Set selected via size to {FormatMillimeters(diameterInternal)} mm diameter, {FormatMillimeters(drillInternal)} mm drill.";
        return resized;
    }

    public BoardTrace MoveSelectedTraceSegmentTo(CadPoint requestedPosition)
    {
        if (SelectedTrace is null || SelectedTraceSegmentIndex is null)
        {
            throw new InvalidOperationException("No board trace segment is selected.");
        }

        int traceIndex = Traces.IndexOf(SelectedTrace);
        if (traceIndex < 0)
        {
            throw new InvalidOperationException("The selected board trace is no longer in the document.");
        }

        int segmentIndex = SelectedTraceSegmentIndex.Value;
        List<CadPoint> routePoints = [.. SelectedTrace.RoutePoints];
        if (segmentIndex <= 0 || segmentIndex >= routePoints.Count)
        {
            throw new InvalidOperationException("The selected board trace segment is no longer valid.");
        }

        CadPoint snappedPoint = placementGrid.Snap(requestedPosition);
        CadPoint start = routePoints[segmentIndex - 1];
        CadPoint end = routePoints[segmentIndex];
        if (start.X == end.X)
        {
            routePoints[segmentIndex - 1] = new CadPoint(snappedPoint.X, start.Y);
            routePoints[segmentIndex] = new CadPoint(snappedPoint.X, end.Y);
        }
        else if (start.Y == end.Y)
        {
            routePoints[segmentIndex - 1] = new CadPoint(start.X, snappedPoint.Y);
            routePoints[segmentIndex] = new CadPoint(end.X, snappedPoint.Y);
        }
        else
        {
            routePoints[segmentIndex - 1] = snappedPoint;
            routePoints[segmentIndex] = snappedPoint;
        }

        BoardTrace moved = SelectedTrace with { RoutePoints = routePoints };
        Traces[traceIndex] = moved;
        SelectedTrace = moved;
        SelectedTraceSegmentIndex = segmentIndex;
        OnPropertyChanged(nameof(VisibleTraces));
        StatusText = "Moved selected board trace segment.";
        return moved;
    }

    public BoardTrace MoveSelectedTraceToActiveLayer()
    {
        if (SelectedTrace is null)
        {
            throw new InvalidOperationException("No board trace is selected.");
        }

        int index = Traces.IndexOf(SelectedTrace);
        if (index < 0)
        {
            throw new InvalidOperationException("The selected board trace is no longer in the document.");
        }

        BoardTrace moved = SelectedTrace with { LayerName = ActiveLayerName };
        Traces[index] = moved;
        SelectedTrace = moved;
        OnPropertyChanged(nameof(VisibleTraces));
        StatusText = $"Moved selected board trace to {ActiveLayerName}.";
        return moved;
    }

    public BoardTrace SetSelectedTraceWidthInternal(long widthInternal)
    {
        if (widthInternal <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(widthInternal), "Trace width must be positive.");
        }

        if (SelectedTrace is null)
        {
            throw new InvalidOperationException("No board trace is selected.");
        }

        int index = Traces.IndexOf(SelectedTrace);
        if (index < 0)
        {
            throw new InvalidOperationException("The selected board trace is no longer in the document.");
        }

        BoardTrace resized = SelectedTrace with { WidthInternal = widthInternal };
        Traces[index] = resized;
        SelectedTrace = resized;
        OnPropertyChanged(nameof(VisibleTraces));
        StatusText = $"Set selected board trace width to {FormatMillimeters(widthInternal)} mm.";
        return resized;
    }

    public void ToggleGridVisibility()
    {
        IsGridVisible = !IsGridVisible;
        StatusText = IsGridVisible ? "Grid visible." : "Grid hidden.";
    }

    public void ToggleGridStyle()
    {
        GridStyle = GridStyle == "Dots" ? "Lines" : "Dots";
        StatusText = $"Grid style set to {GridStyle}.";
    }

    public void SetGridSpacingMillimeters(decimal millimeters)
    {
        if (millimeters <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(millimeters), "Grid spacing must be positive.");
        }

        decimal bounded = Math.Clamp(millimeters, 0.1m, 25.4m);
        GridSpacingInternal = (long)Math.Round(bounded * CadUnit.InternalUnitsPerMillimeter, MidpointRounding.AwayFromZero);
        StatusText = $"Grid spacing set to {FormatMillimeters(GridSpacingInternal)} mm.";
    }

    public void ZoomIn()
    {
        ZoomLevel = Math.Min(8.0, Math.Round(ZoomLevel * 1.25, 4));
        StatusText = $"Board zoom {ZoomLevel:0.##}x.";
    }

    public void ZoomOut()
    {
        ZoomLevel = Math.Max(0.25, Math.Round(ZoomLevel / 1.25, 4));
        StatusText = $"Board zoom {ZoomLevel:0.##}x.";
    }

    public void ZoomAt(CadPoint cursorCadPoint, bool zoomIn)
    {
        double oldZoom = ZoomLevel;
        double nextZoom = zoomIn
            ? Math.Min(8.0, Math.Round(ZoomLevel * 1.25, 4))
            : Math.Max(0.25, Math.Round(ZoomLevel / 1.25, 4));
        if (Math.Abs(oldZoom - nextZoom) < 0.0001)
        {
            StatusText = $"Board zoom {ZoomLevel:0.##}x.";
            return;
        }

        double ratio = oldZoom / nextZoom;
        ViewportOrigin = new CadPoint(
            cursorCadPoint.X - (long)Math.Round((cursorCadPoint.X - ViewportOrigin.X) * ratio, MidpointRounding.AwayFromZero),
            cursorCadPoint.Y - (long)Math.Round((cursorCadPoint.Y - ViewportOrigin.Y) * ratio, MidpointRounding.AwayFromZero));
        ZoomLevel = nextZoom;
        StatusText = $"Board zoom {ZoomLevel:0.##}x.";
    }

    public void PanViewportByScreenDelta(Vector screenDelta, double pixelsPerInternalUnit)
    {
        if (pixelsPerInternalUnit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelsPerInternalUnit), "Viewport scale must be positive.");
        }

        long deltaX = (long)Math.Round(screenDelta.X / pixelsPerInternalUnit, MidpointRounding.AwayFromZero);
        long deltaY = (long)Math.Round(screenDelta.Y / pixelsPerInternalUnit, MidpointRounding.AwayFromZero);
        ViewportOrigin = new CadPoint(ViewportOrigin.X - deltaX, ViewportOrigin.Y - deltaY);
        StatusText = $"Board pan {FormatMillimeters(ViewportOrigin.X)} mm, {FormatMillimeters(ViewportOrigin.Y)} mm.";
    }

    public void CenterBoardContentsInViewport()
    {
        CadRectangle bounds = BoardContentsBounds();
        ViewportOrigin = CenterOf(bounds);
        StatusText = "Centered board contents.";
    }

    public void FitBoardContentsToViewport(double viewportWidthPixels, double viewportHeightPixels, double paddingPixels)
    {
        CadRectangle bounds = BoardContentsBounds();
        ZoomLevel = CalculateFitZoom(bounds, viewportWidthPixels, viewportHeightPixels, paddingPixels, basePixelsPerInternalUnit: 0.000025);
        ViewportOrigin = CenterOf(bounds);
        StatusText = $"Fit board contents at {ZoomLevel:0.##}x.";
    }

    public void ActivateSelectTool()
    {
        ActiveTool = "Select";
        pendingTraceRoutePoints.Clear();
        PendingTraceStart = null;
        pendingTraceStartPad = null;
        StatusText = "Board select tool active.";
    }

    public void ActivateRouteTool()
    {
        ActiveTool = "Route";
        SelectedComponent = null;
        SelectedTrace = null;
        SelectedVia = null;
        SelectedTraceSegmentIndex = null;
        StatusText = "Board route tool active.";
    }

    public void SetRouteCornerMode(string mode)
    {
        RouteCornerMode = mode;
    }

    public bool TraceClickAt(CadPoint point)
    {
        BoardPadHit? padHit = FindPadAt(point);
        CadPoint routePoint = padHit?.Position ?? placementGrid.Snap(point);
        if (PendingTraceStart is null)
        {
            PendingTraceStart = routePoint;
            pendingTraceStartPad = padHit;
            pendingTraceRoutePoints.Clear();
            pendingTraceRoutePoints.Add(routePoint);
            OnPropertyChanged(nameof(PendingTraceRoutePoints));
            StatusText = padHit is null
                ? $"Started board trace at {FormatMillimeters(routePoint.X)} mm, {FormatMillimeters(routePoint.Y)} mm."
                : $"Started board trace at pad {padHit.ReferenceDesignator}.{padHit.PadName}.";
            return true;
        }

        AddRouteLeg(pendingTraceRoutePoints, routePoint);
        OnPropertyChanged(nameof(PendingTraceRoutePoints));
        StatusText = padHit is null
            ? $"Added board trace segment at {FormatMillimeters(routePoint.X)} mm, {FormatMillimeters(routePoint.Y)} mm."
            : $"Added board trace segment at pad {padHit.ReferenceDesignator}.{padHit.PadName}.";
        return true;
    }

    public bool CompleteTraceAt(CadPoint point)
    {
        if (PendingTraceStart is null)
        {
            StatusText = "Start a board trace before finishing it.";
            return false;
        }

        BoardPadHit? endPad = FindPadAt(point);
        AddRouteLeg(pendingTraceRoutePoints, endPad?.Position ?? placementGrid.Snap(point));
        if (pendingTraceRoutePoints.Count < 2)
        {
            StatusText = "Board trace needs at least two points.";
            return false;
        }

        BoardAirwire? retiredAirwire = RetireAirwireBetween(pendingTraceStartPad, endPad);
        Traces.Add(new BoardTrace(Guid.NewGuid().ToString("N"), ActiveLayerName, [.. pendingTraceRoutePoints]));
        pendingTraceRoutePoints.Clear();
        PendingTraceStart = null;
        pendingTraceStartPad = null;
        OnPropertyChanged(nameof(PendingTraceRoutePoints));
        OnPropertyChanged(nameof(VisibleTraces));
        StatusText = retiredAirwire is null
            ? $"Routed board trace on {ActiveLayerName}."
            : $"Routed board trace on {ActiveLayerName} and retired airwire {retiredAirwire.NetName}.";
        return true;
    }

    public BoardVia PlaceViaAt(CadPoint point)
    {
        string fromLayer = ActiveLayerName;
        string toLayer = fromLayer == "Top" ? "Bottom" : "Top";
        CadPoint snappedPoint = placementGrid.Snap(point);
        BoardVia via = new(
            Guid.NewGuid().ToString("N"),
            snappedPoint,
            fromLayer,
            toLayer);
        Vias.Add(via);
        OnPropertyChanged(nameof(VisibleVias));

        if (PendingTraceStart is not null)
        {
            AddRouteLeg(pendingTraceRoutePoints, snappedPoint);
            OnPropertyChanged(nameof(PendingTraceRoutePoints));
        }

        ActiveLayerName = toLayer;
        StatusText = $"Placed via and switched routing layer to {toLayer}.";
        return via;
    }

    public BoardVia InsertViaIntoSelectedTraceSegment(CadPoint requestedPosition)
    {
        if (SelectedTrace is null || SelectedTraceSegmentIndex is null)
        {
            throw new InvalidOperationException("No board trace segment is selected.");
        }

        int traceIndex = Traces.IndexOf(SelectedTrace);
        if (traceIndex < 0)
        {
            throw new InvalidOperationException("The selected board trace is no longer in the document.");
        }

        int segmentIndex = SelectedTraceSegmentIndex.Value;
        List<CadPoint> routePoints = [.. SelectedTrace.RoutePoints];
        if (segmentIndex <= 0 || segmentIndex >= routePoints.Count)
        {
            throw new InvalidOperationException("The selected board trace segment is no longer valid.");
        }

        CadPoint snappedPoint = placementGrid.Snap(requestedPosition);
        string fromLayer = SelectedTrace.LayerName;
        string toLayer = fromLayer == "Top" ? "Bottom" : "Top";
        BoardVia via = new(
            Guid.NewGuid().ToString("N"),
            snappedPoint,
            fromLayer,
            toLayer);

        CadPoint start = routePoints[segmentIndex - 1];
        CadPoint end = routePoints[segmentIndex];
        List<CadPoint> insertedRoute = [.. routePoints.Take(segmentIndex)];
        AddOrthogonalLeg(insertedRoute, snappedPoint);
        AddOrthogonalLeg(insertedRoute, end);
        insertedRoute.AddRange(routePoints.Skip(segmentIndex + 1));

        BoardTrace updatedTrace = SelectedTrace with { RoutePoints = CompactRoute(insertedRoute) };
        Traces[traceIndex] = updatedTrace;
        Vias.Add(via);
        ActiveLayerName = toLayer;
        SelectedTrace = updatedTrace;
        SelectedTraceSegmentIndex = Math.Min(segmentIndex + 1, updatedTrace.RoutePoints.Count - 1);
        SelectedVia = null;
        SelectedComponent = null;
        OnPropertyChanged(nameof(VisibleTraces));
        OnPropertyChanged(nameof(VisibleVias));
        StatusText = $"Inserted via into selected board trace and switched routing layer to {toLayer}.";
        return via;
    }

    public bool DeleteSelectedBoardObject()
    {
        if (SelectedComponent is not null)
        {
            string syncId = SelectedComponent.SyncId;
            string referenceDesignator = SelectedComponent.ReferenceDesignator;
            Components.Remove(SelectedComponent);
            SelectedComponent = null;

            int removedAirwireCount = 0;
            for (int index = Airwires.Count - 1; index >= 0; index--)
            {
                if (Airwires[index].StartSyncId == syncId || Airwires[index].EndSyncId == syncId)
                {
                    Airwires.RemoveAt(index);
                    removedAirwireCount++;
                }
            }

            StatusText = removedAirwireCount == 1
                ? $"Deleted board component {referenceDesignator} and 1 related airwire."
                : $"Deleted board component {referenceDesignator} and {removedAirwireCount} related airwires.";
            return true;
        }

        if (SelectedTrace is not null)
        {
            Traces.Remove(SelectedTrace);
            SelectedTrace = null;
            SelectedTraceSegmentIndex = null;
            OnPropertyChanged(nameof(VisibleTraces));
            StatusText = "Deleted selected board trace.";
            return true;
        }

        if (SelectedVia is not null)
        {
            Vias.Remove(SelectedVia);
            SelectedVia = null;
            OnPropertyChanged(nameof(VisibleVias));
            StatusText = "Deleted selected via.";
            return true;
        }

        StatusText = "Select a board component, trace, or via before deleting it.";
        return false;
    }

    public void SetActiveLayer(string layerName)
    {
        BoardLayer layer = Layers.FirstOrDefault(candidate => candidate.Name == layerName)
            ?? throw new InvalidOperationException($"Unknown board layer '{layerName}'.");
        ActiveLayerName = layer.Name;
        StatusText = $"Active board layer set to {layer.Name}.";
    }

    public void SetLayerVisibility(string layerName, bool isVisible)
    {
        int index = IndexOfLayer(layerName);
        BoardLayer updated = Layers[index] with { IsVisible = isVisible };
        Layers[index] = updated;
        OnPropertyChanged(nameof(VisibleTraces));
        OnPropertyChanged(nameof(VisibleVias));
        StatusText = $"Layer {updated.Name} {(updated.IsVisible ? "visible" : "hidden")}.";
    }

    private CadPoint BoardPositionFor(string syncId, int newComponentIndex)
    {
        int existingIndex = IndexOfSyncId(syncId);
        if (existingIndex >= 0)
        {
            return Components[existingIndex].Position;
        }

        return new CadPoint(newComponentIndex * AutoPlacementStep.X, newComponentIndex * AutoPlacementStep.Y);
    }

    private BoardTrace? SelectTraceAt(CadPoint point)
    {
        TraceHit? traceHit = FindTraceAt(point);
        if (traceHit is null)
        {
            return null;
        }

        SelectedComponent = null;
        SelectedVia = null;
        SelectedTrace = traceHit.Trace;
        SelectedTraceSegmentIndex = traceHit.SegmentIndex;
        StatusText = $"Selected board trace on {traceHit.Trace.LayerName}.";
        return traceHit.Trace;
    }

    private BoardVia? SelectViaAt(CadPoint point)
    {
        BoardVia? via = FindViaAt(point, Vias);
        if (via is null)
        {
            return null;
        }

        SelectedComponent = null;
        SelectedTrace = null;
        SelectedTraceSegmentIndex = null;
        SelectedVia = via;
        StatusText = $"Selected via {via.FromLayerName}->{via.ToLayerName}.";
        return via;
    }

    private BoardComponentInstance? FindComponentAt(CadPoint point)
    {
        for (int index = Components.Count - 1; index >= 0; index--)
        {
            BoardComponentInstance candidate = Components[index];
            if (candidate.FootprintPrimitives.Count == 0
                ? ComponentBounds(candidate).Contains(point)
                : BoardFootprintGeometry.HitTest(candidate, point) || ComponentPreviewBoundsContains(candidate, point))
            {
                return candidate;
            }
        }

        return null;
    }

    private TraceHit? FindTraceAt(CadPoint point)
    {
        const double tolerance = 350_000;
        double nearestDistance = double.MaxValue;
        BoardTrace? nearestTrace = null;
        int? nearestSegmentIndex = null;
        IReadOnlyList<BoardTrace> visibleTraces = VisibleTraces;
        for (int traceIndex = visibleTraces.Count - 1; traceIndex >= 0; traceIndex--)
        {
            BoardTrace trace = visibleTraces[traceIndex];
            SegmentHit? hit = NearestSegmentHit(point, trace.RoutePoints);
            if (hit is not null && hit.Distance <= tolerance && hit.Distance < nearestDistance)
            {
                nearestDistance = hit.Distance;
                nearestTrace = trace;
                nearestSegmentIndex = hit.SegmentIndex;
            }
        }

        return nearestTrace is null || nearestSegmentIndex is null
            ? null
            : new TraceHit(nearestTrace, nearestSegmentIndex.Value);
    }

    private static BoardVia? FindViaAt(CadPoint point, IReadOnlyList<BoardVia> vias)
    {
        const long tolerance = 600_000;
        for (int viaIndex = vias.Count - 1; viaIndex >= 0; viaIndex--)
        {
            BoardVia via = vias[viaIndex];
            if (Math.Abs(via.Position.X - point.X) <= tolerance &&
                Math.Abs(via.Position.Y - point.Y) <= tolerance)
            {
                return via;
            }
        }

        return null;
    }

    private CadPoint BoardPositionForWireEndpoint(string syncId, string pinName)
    {
        int index = IndexOfSyncId(syncId);
        if (index < 0)
        {
            return default;
        }

        BoardComponentInstance component = Components[index];
        BoardFootprintPrimitive? pad = component.FootprintPrimitives.FirstOrDefault(candidate =>
            candidate is BoardFootprintPadPrimitive throughHole &&
                string.Equals(throughHole.Name, pinName, StringComparison.OrdinalIgnoreCase) ||
            candidate is BoardFootprintSmdPrimitive smd &&
                string.Equals(smd.Name, pinName, StringComparison.OrdinalIgnoreCase));
        return pad switch
        {
            BoardFootprintPadPrimitive throughHole => BoardFootprintGeometry.TransformLocalPoint(component, throughHole.Position),
            BoardFootprintSmdPrimitive smd => BoardFootprintGeometry.TransformLocalPoint(component, smd.Position),
            _ => component.Position
        };
    }

    private void RefreshAirwireEndpoints()
    {
        for (int index = 0; index < Airwires.Count; index++)
        {
            BoardAirwire airwire = Airwires[index];
            Airwires[index] = airwire with
            {
                StartPosition = BoardPositionForWireEndpoint(airwire.StartSyncId, airwire.StartPinName),
                EndPosition = BoardPositionForWireEndpoint(airwire.EndSyncId, airwire.EndPinName)
            };
        }
    }

    private static CadRectangle ComponentBounds(BoardComponentInstance component) =>
        component.FootprintBounds.Width > 0 || component.FootprintBounds.Height > 0
            ? new CadRectangle(
                component.Position.X + component.FootprintBounds.Left,
                component.Position.Y + component.FootprintBounds.Top,
                component.Position.X + component.FootprintBounds.Right,
                component.Position.Y + component.FootprintBounds.Bottom)
            : new CadRectangle(
                component.Position.X - 1_500_000,
                component.Position.Y - 1_000_000,
                component.Position.X + 1_500_000,
                component.Position.Y + 1_000_000);

    private static bool ComponentPreviewBoundsContains(BoardComponentInstance component, CadPoint point)
    {
        CadRectangle bounds = component.FootprintPreview.Bounds;
        if (bounds.Width <= 0 && bounds.Height <= 0)
        {
            return false;
        }

        CadPoint[] corners =
        [
            BoardFootprintGeometry.TransformLocalPoint(component, new CadPoint(bounds.Left, bounds.Top)),
            BoardFootprintGeometry.TransformLocalPoint(component, new CadPoint(bounds.Right, bounds.Top)),
            BoardFootprintGeometry.TransformLocalPoint(component, new CadPoint(bounds.Right, bounds.Bottom)),
            BoardFootprintGeometry.TransformLocalPoint(component, new CadPoint(bounds.Left, bounds.Bottom))
        ];
        CadRectangle transformedBounds = new(
            corners.Min(candidate => candidate.X),
            corners.Min(candidate => candidate.Y),
            corners.Max(candidate => candidate.X),
            corners.Max(candidate => candidate.Y));
        return transformedBounds.Contains(point);
    }

    private CadRectangle BoardContentsBounds()
    {
        List<CadRectangle> rectangles = [];
        rectangles.AddRange(Components.Select(ComponentBounds));
        rectangles.AddRange(Traces.Where(trace => trace.RoutePoints.Count > 0).Select(trace => BoundsOfPoints(trace.RoutePoints)));
        rectangles.AddRange(Vias.Select(via => new CadRectangle(
            via.Position.X - (via.DiameterInternal / 2),
            via.Position.Y - (via.DiameterInternal / 2),
            via.Position.X + (via.DiameterInternal / 2),
            via.Position.Y + (via.DiameterInternal / 2))));
        rectangles.AddRange(Airwires.Select(airwire => BoundsOfPoints([airwire.StartPosition, airwire.EndPosition])));

        if (rectangles.Count == 0)
        {
            return new CadRectangle(-10_000_000, -10_000_000, 10_000_000, 10_000_000);
        }

        return new CadRectangle(
            rectangles.Min(rectangle => rectangle.Left),
            rectangles.Min(rectangle => rectangle.Top),
            rectangles.Max(rectangle => rectangle.Right),
            rectangles.Max(rectangle => rectangle.Bottom));
    }

    private static CadRectangle BoundsOfPoints(IReadOnlyList<CadPoint> points) =>
        new(
            points.Min(point => point.X),
            points.Min(point => point.Y),
            points.Max(point => point.X),
            points.Max(point => point.Y));

    private BoardPadHit? FindPadAt(CadPoint point)
    {
        for (int componentIndex = Components.Count - 1; componentIndex >= 0; componentIndex--)
        {
            BoardComponentInstance component = Components[componentIndex];
            for (int padIndex = component.FootprintPrimitives.Count - 1; padIndex >= 0; padIndex--)
            {
                BoardFootprintPrimitive primitive = component.FootprintPrimitives[padIndex];
                if (primitive is BoardFootprintPadPrimitive throughHole &&
                    BoardFootprintGeometry.PrimitiveHitTest(component, primitive, point))
                {
                    CadPoint padCenter = BoardFootprintGeometry.TransformLocalPoint(component, throughHole.Position);
                    return new BoardPadHit(component.SyncId, component.ReferenceDesignator, throughHole.Name, padCenter);
                }

                if (primitive is BoardFootprintSmdPrimitive smd &&
                    BoardFootprintGeometry.PrimitiveHitTest(component, primitive, point))
                {
                    CadPoint padCenter = BoardFootprintGeometry.TransformLocalPoint(component, smd.Position);
                    return new BoardPadHit(component.SyncId, component.ReferenceDesignator, smd.Name, padCenter);
                }
            }
        }

        return null;
    }

    private BoardAirwire? RetireAirwireBetween(BoardPadHit? startPad, BoardPadHit? endPad)
    {
        if (startPad is null || endPad is null)
        {
            return null;
        }

        for (int index = Airwires.Count - 1; index >= 0; index--)
        {
            BoardAirwire airwire = Airwires[index];
            if (AirwireMatches(airwire, startPad, endPad))
            {
                Airwires.RemoveAt(index);
                return airwire;
            }
        }

        return null;
    }

    private static bool AirwireMatches(BoardAirwire airwire, BoardPadHit startPad, BoardPadHit endPad) =>
        (EndpointMatches(airwire.StartSyncId, airwire.StartPinName, startPad) &&
            EndpointMatches(airwire.EndSyncId, airwire.EndPinName, endPad)) ||
        (EndpointMatches(airwire.StartSyncId, airwire.StartPinName, endPad) &&
            EndpointMatches(airwire.EndSyncId, airwire.EndPinName, startPad));

    private static bool EndpointMatches(string syncId, string pinName, BoardPadHit pad) =>
        string.Equals(syncId, pad.SyncId, StringComparison.Ordinal) &&
        string.Equals(pinName, pad.PadName, StringComparison.OrdinalIgnoreCase);

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

    private static string FormatMillimeters(long internalUnits) =>
        ((decimal)internalUnits / CadUnit.InternalUnitsPerMillimeter).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);

    private static CadPoint CenterOf(CadRectangle bounds) =>
        new(bounds.Left + (bounds.Width / 2), bounds.Top + (bounds.Height / 2));

    private static double CalculateFitZoom(
        CadRectangle bounds,
        double viewportWidthPixels,
        double viewportHeightPixels,
        double paddingPixels,
        double basePixelsPerInternalUnit)
    {
        if (viewportWidthPixels <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(viewportWidthPixels), "Viewport width must be positive.");
        }

        if (viewportHeightPixels <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(viewportHeightPixels), "Viewport height must be positive.");
        }

        double availableWidth = Math.Max(1, viewportWidthPixels - (paddingPixels * 2));
        double availableHeight = Math.Max(1, viewportHeightPixels - (paddingPixels * 2));
        double contentWidth = Math.Max(1, bounds.Width * basePixelsPerInternalUnit);
        double contentHeight = Math.Max(1, bounds.Height * basePixelsPerInternalUnit);
        return Math.Clamp(Math.Round(Math.Min(availableWidth / contentWidth, availableHeight / contentHeight), 4), 0.05, 8.0);
    }

    private void AddRouteLeg(List<CadPoint> route, CadPoint target)
    {
        if (RouteCornerMode == "45")
        {
            AddFortyFiveLeg(route, target);
            return;
        }

        AddOrthogonalLeg(route, target);
    }

    private static string NormalizeRouteCornerMode(string mode)
    {
        ArgumentNullException.ThrowIfNull(mode);
        return mode.Trim() switch
        {
            "45" => "45",
            "90" => "90",
            _ => throw new ArgumentOutOfRangeException(nameof(mode), "Route corner mode must be 45 or 90.")
        };
    }

    private static void AddFortyFiveLeg(List<CadPoint> route, CadPoint target)
    {
        if (route.Count == 0)
        {
            route.Add(target);
            return;
        }

        CadPoint last = route[^1];
        if (last == target)
        {
            return;
        }

        long dx = target.X - last.X;
        long dy = target.Y - last.Y;
        long absDx = Math.Abs(dx);
        long absDy = Math.Abs(dy);
        if (absDx == 0 || absDy == 0 || absDx == absDy)
        {
            route.Add(target);
            return;
        }

        CadPoint diagonalCorner = absDx > absDy
            ? new CadPoint(target.X - (Math.Sign(dx) * absDy), last.Y)
            : new CadPoint(last.X, target.Y - (Math.Sign(dy) * absDx));
        if (route[^1] != diagonalCorner)
        {
            route.Add(diagonalCorner);
        }

        if (route[^1] != target)
        {
            route.Add(target);
        }
    }

    private static void AddOrthogonalLeg(List<CadPoint> route, CadPoint target)
    {
        if (route.Count == 0)
        {
            route.Add(target);
            return;
        }

        CadPoint last = route[^1];
        if (last == target)
        {
            return;
        }

        if (last.X != target.X && last.Y != target.Y)
        {
            CadPoint corner = new(target.X, last.Y);
            if (route[^1] != corner)
            {
                route.Add(corner);
            }
        }

        if (route[^1] != target)
        {
            route.Add(target);
        }
    }

    private static IReadOnlyList<CadPoint> CompactRoute(IReadOnlyList<CadPoint> routePoints)
    {
        List<CadPoint> compacted = [];
        foreach (CadPoint point in routePoints)
        {
            if (compacted.Count == 0 || compacted[^1] != point)
            {
                compacted.Add(point);
            }
        }

        for (int index = compacted.Count - 2; index > 0; index--)
        {
            CadPoint previous = compacted[index - 1];
            CadPoint current = compacted[index];
            CadPoint next = compacted[index + 1];
            if ((previous.X == current.X && current.X == next.X) ||
                (previous.Y == current.Y && current.Y == next.Y))
            {
                compacted.RemoveAt(index);
            }
        }

        return compacted;
    }

    private static SegmentHit? NearestSegmentHit(CadPoint point, IReadOnlyList<CadPoint> routePoints)
    {
        if (routePoints.Count < 2)
        {
            return null;
        }

        double nearest = double.MaxValue;
        int? nearestSegmentIndex = null;
        for (int index = 1; index < routePoints.Count; index++)
        {
            CadPoint start = routePoints[index - 1];
            CadPoint end = routePoints[index];
            double distance = DistanceToSegment(point, start, end);
            if (distance < nearest)
            {
                nearest = distance;
                nearestSegmentIndex = index;
            }
        }

        return nearestSegmentIndex is null ? null : new SegmentHit(nearest, nearestSegmentIndex.Value);
    }

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

    private int IndexOfLayer(string layerName)
    {
        for (int index = 0; index < Layers.Count; index++)
        {
            if (Layers[index].Name == layerName)
            {
                return index;
            }
        }

        throw new InvalidOperationException($"Unknown board layer '{layerName}'.");
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private sealed record SegmentHit(double Distance, int SegmentIndex);

    private sealed record TraceHit(BoardTrace Trace, int SegmentIndex);

    private sealed record BoardPadHit(string SyncId, string ReferenceDesignator, string PadName, CadPoint Position);
}
