using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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
    private string activeTool = "Select";
    private string activeLayerName = "Top";
    private CadPoint? pendingTraceStart;
    private BoardTrace? selectedTrace;
    private BoardVia? selectedVia;
    private int? selectedTraceSegmentIndex;
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
        new("Dimension", "#A3E635")
    ];

    public IReadOnlyList<BoardTrace> VisibleTraces =>
        Traces
            .Where(trace => Layers.Any(layer => layer.Name == trace.LayerName && layer.IsVisible))
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
            CadPoint startPosition = BoardPositionForWireEndpoint(wire.Start.InstanceId);
            CadPoint endPosition = BoardPositionForWireEndpoint(wire.End.InstanceId);
            Airwires.Add(new BoardAirwire(
                wire.NetName,
                wire.Start.InstanceId,
                wire.Start.ReferenceDesignator,
                wire.Start.PinName,
                startPosition,
                wire.End.InstanceId,
                wire.End.ReferenceDesignator,
                wire.End.PinName,
                endPosition));
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
        for (int index = Components.Count - 1; index >= 0; index--)
        {
            BoardComponentInstance candidate = Components[index];
            if (ComponentBounds(candidate).Contains(point))
            {
                SelectedComponent = candidate;
                SelectedTrace = null;
                SelectedVia = null;
                SelectedTraceSegmentIndex = null;
                StatusText = $"Selected board component {candidate.ReferenceDesignator}.";
                return candidate;
            }
        }

        SelectedComponent = null;
        SelectedTraceSegmentIndex = null;
        StatusText = "No board component selected.";
        return null;
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

    public void ActivateSelectTool()
    {
        ActiveTool = "Select";
        pendingTraceRoutePoints.Clear();
        PendingTraceStart = null;
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

    public bool TraceClickAt(CadPoint point)
    {
        CadPoint snappedPoint = placementGrid.Snap(point);
        if (PendingTraceStart is null)
        {
            PendingTraceStart = snappedPoint;
            pendingTraceRoutePoints.Clear();
            pendingTraceRoutePoints.Add(snappedPoint);
            OnPropertyChanged(nameof(PendingTraceRoutePoints));
            StatusText = $"Started board trace at {FormatMillimeters(snappedPoint.X)} mm, {FormatMillimeters(snappedPoint.Y)} mm.";
            return true;
        }

        AddOrthogonalLeg(pendingTraceRoutePoints, snappedPoint);
        OnPropertyChanged(nameof(PendingTraceRoutePoints));
        StatusText = $"Added board trace segment at {FormatMillimeters(snappedPoint.X)} mm, {FormatMillimeters(snappedPoint.Y)} mm.";
        return true;
    }

    public bool CompleteTraceAt(CadPoint point)
    {
        if (PendingTraceStart is null)
        {
            StatusText = "Start a board trace before finishing it.";
            return false;
        }

        AddOrthogonalLeg(pendingTraceRoutePoints, placementGrid.Snap(point));
        if (pendingTraceRoutePoints.Count < 2)
        {
            StatusText = "Board trace needs at least two points.";
            return false;
        }

        Traces.Add(new BoardTrace(Guid.NewGuid().ToString("N"), ActiveLayerName, [.. pendingTraceRoutePoints]));
        pendingTraceRoutePoints.Clear();
        PendingTraceStart = null;
        OnPropertyChanged(nameof(PendingTraceRoutePoints));
        OnPropertyChanged(nameof(VisibleTraces));
        StatusText = $"Routed board trace on {ActiveLayerName}.";
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

        if (PendingTraceStart is not null)
        {
            AddOrthogonalLeg(pendingTraceRoutePoints, snappedPoint);
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
        const double tolerance = 350_000;
        double nearestDistance = double.MaxValue;
        BoardTrace? nearestTrace = null;
        int? nearestSegmentIndex = null;
        for (int traceIndex = VisibleTraces.Count - 1; traceIndex >= 0; traceIndex--)
        {
            BoardTrace trace = VisibleTraces[traceIndex];
            SegmentHit? hit = NearestSegmentHit(point, trace.RoutePoints);
            if (hit is not null && hit.Distance <= tolerance && hit.Distance < nearestDistance)
            {
                nearestDistance = hit.Distance;
                nearestTrace = trace;
                nearestSegmentIndex = hit.SegmentIndex;
            }
        }

        if (nearestTrace is null)
        {
            return null;
        }

        SelectedComponent = null;
        SelectedVia = null;
        SelectedTrace = nearestTrace;
        SelectedTraceSegmentIndex = nearestSegmentIndex;
        StatusText = $"Selected board trace on {nearestTrace.LayerName}.";
        return nearestTrace;
    }

    private BoardVia? SelectViaAt(CadPoint point)
    {
        const long tolerance = 600_000;
        for (int viaIndex = Vias.Count - 1; viaIndex >= 0; viaIndex--)
        {
            BoardVia via = Vias[viaIndex];
            if (Math.Abs(via.Position.X - point.X) <= tolerance &&
                Math.Abs(via.Position.Y - point.Y) <= tolerance)
            {
                SelectedComponent = null;
                SelectedTrace = null;
                SelectedTraceSegmentIndex = null;
                SelectedVia = via;
                StatusText = $"Selected via {via.FromLayerName}->{via.ToLayerName}.";
                return via;
            }
        }

        return null;
    }

    private CadPoint BoardPositionForWireEndpoint(string syncId)
    {
        int index = IndexOfSyncId(syncId);
        return index >= 0 ? Components[index].Position : default;
    }

    private void RefreshAirwireEndpoints()
    {
        for (int index = 0; index < Airwires.Count; index++)
        {
            BoardAirwire airwire = Airwires[index];
            Airwires[index] = airwire with
            {
                StartPosition = BoardPositionForWireEndpoint(airwire.StartSyncId),
                EndPosition = BoardPositionForWireEndpoint(airwire.EndSyncId)
            };
        }
    }

    private static CadRectangle ComponentBounds(BoardComponentInstance component) =>
        component.FootprintPreview.Bounds.Width > 0 || component.FootprintPreview.Bounds.Height > 0
            ? new CadRectangle(
                component.Position.X + component.FootprintPreview.Bounds.Left,
                component.Position.Y + component.FootprintPreview.Bounds.Top,
                component.Position.X + component.FootprintPreview.Bounds.Right,
                component.Position.Y + component.FootprintPreview.Bounds.Bottom)
            : new CadRectangle(
                component.Position.X - 1_500_000,
                component.Position.Y - 1_000_000,
                component.Position.X + 1_500_000,
                component.Position.Y + 1_000_000);

    private static string FormatMillimeters(long internalUnits) =>
        ((decimal)internalUnits / CadUnit.InternalUnitsPerMillimeter).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);

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
}
