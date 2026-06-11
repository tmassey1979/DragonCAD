using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia;
using DragonCAD.App.ComponentManager;
using DragonCAD.App.SchematicEditor;
using DragonCAD.Core.Geometry;

namespace DragonCAD.App.BoardEditor;

public sealed class BoardEditorViewModel : INotifyPropertyChanged
{
    private static readonly CadVector AutoPlacementStep = new(8_000_000, 0);
    private const long PadRouteHitToleranceInternal = 100_000;
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
    private bool isFreeRouteModeActive;
    private string routeCornerMode = "90";
    private readonly List<CadPoint> pendingTraceRoutePoints = [];
    private readonly Dictionary<string, BoardAirwire> retiredAirwiresByTraceId = new(StringComparer.Ordinal);
    private readonly List<BoardComponentInstance> selectedBoardComponents = [];
    private readonly List<BoardTrace> selectedBoardTraces = [];
    private readonly List<BoardVia> selectedBoardVias = [];
    private readonly List<string> selectedFootprintPrimitiveKinds = [];
    private BoardClipboardSnapshot? boardClipboard;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<BoardComponentInstance> Components { get; } = [];

    public ObservableCollection<BoardAirwire> Airwires { get; } = [];

    public ObservableCollection<BoardTrace> Traces { get; } = [];

    public ObservableCollection<BoardVia> Vias { get; } = [];

    public IReadOnlyList<BoardComponentInstance> SelectedBoardComponents => selectedBoardComponents;

    public IReadOnlyList<BoardTrace> SelectedBoardTraces => selectedBoardTraces;

    public IReadOnlyList<BoardVia> SelectedBoardVias => selectedBoardVias;

    public IReadOnlyList<string> SelectedFootprintPrimitiveKinds => selectedFootprintPrimitiveKinds;

    public int SelectedObjectCount => selectedBoardComponents.Count + selectedBoardTraces.Count + selectedBoardVias.Count;

    public ObservableCollection<BoardLayer> Layers { get; } =
    [
        new("Top", "#E63D32"),
        new("Bottom", "#2D8CFF"),
        new("Silkscreen", "#E2E8F0"),
        new("Documentation", "#22C55E"),
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
            .Where(primitive => Layers.Any(layer => layer.Name == BoardFootprintGeometry.ResolveRenderLayerName(component, primitive) && layer.IsVisible))
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

    public bool IsFreeRouteModeActive
    {
        get => isFreeRouteModeActive;
        private set
        {
            if (isFreeRouteModeActive == value)
            {
                return;
            }

            isFreeRouteModeActive = value;
            OnPropertyChanged();
        }
    }

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
            OnPropertyChanged(nameof(SelectedViaDiameterMillimeters));
            OnPropertyChanged(nameof(SelectedViaDrillMillimeters));
        }
    }

    public string SelectedViaDiameterMillimeters
    {
        get => SelectedVia is null
            ? ""
            : FormatMillimeters(SelectedVia.DiameterInternal);
        set => TrySetSelectedViaSizeMillimeters(value, isDiameter: true);
    }

    public string SelectedViaDrillMillimeters
    {
        get => SelectedVia is null
            ? ""
            : FormatMillimeters(SelectedVia.DrillInternal);
        set => TrySetSelectedViaSizeMillimeters(value, isDiameter: false);
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
        retiredAirwiresByTraceId.Clear();
        pendingTraceRoutePoints.Clear();
        PendingTraceStart = null;
        SelectedComponent = null;
        SelectedTrace = null;
        SelectedVia = null;
        SelectedTraceSegmentIndex = null;
        ClearBoardGroupSelection();
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

        RetireAirwiresForExistingTraces();

        string componentText = $"{Components.Count} board component{(Components.Count == 1 ? "" : "s")}";
        if (Airwires.Count == 0)
        {
            StatusText = $"Synchronized {componentText} from schematic.";
            return;
        }

        string airwireText = $"{Airwires.Count} airwire{(Airwires.Count == 1 ? "" : "s")}";
        StatusText = $"Synchronized {componentText} and {airwireText} from schematic.";
    }

    public BoardFootprintReplacementResult ReplaceComponentFootprintFromPackage(
        string syncId,
        ComponentFootprintPreview replacementFootprint,
        string packageLabel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(syncId);
        ArgumentNullException.ThrowIfNull(replacementFootprint);

        int componentIndex = IndexOfSyncId(syncId);
        if (componentIndex < 0)
        {
            BoardFootprintReplacementResult result = BoardFootprintReplacementResult.Failed(
                syncId,
                packageLabel,
                new BoardFootprintReplacementDiagnostic(
                    BoardFootprintReplacementDiagnosticCode.MissingComponent,
                    syncId,
                    $"Board component '{syncId}' was not found."));
            StatusText = result.Diagnostics[0].Message;
            return result;
        }

        IReadOnlyList<BoardFootprintPrimitive> replacementPrimitives = BoardFootprintPrimitive.FromPreview(replacementFootprint);
        HashSet<string> replacementPadNames = ReplacementPadNames(replacementPrimitives);
        if (replacementPadNames.Count == 0)
        {
            BoardFootprintReplacementResult result = BoardFootprintReplacementResult.Failed(
                syncId,
                packageLabel,
                new BoardFootprintReplacementDiagnostic(
                    BoardFootprintReplacementDiagnosticCode.MissingFootprintMapping,
                    syncId,
                    $"Package '{packageLabel}' does not contain board footprint pads for {Components[componentIndex].ReferenceDesignator}."));
            StatusText = result.Diagnostics[0].Message;
            return result;
        }

        string[] missingPadNames = ConnectedPadNames(syncId)
            .Where(padName => !replacementPadNames.Contains(padName))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(padName => padName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (missingPadNames.Length > 0)
        {
            string missingPads = string.Join(", ", missingPadNames);
            BoardFootprintReplacementResult result = BoardFootprintReplacementResult.Failed(
                syncId,
                packageLabel,
                new BoardFootprintReplacementDiagnostic(
                    BoardFootprintReplacementDiagnosticCode.MissingPadMapping,
                    syncId,
                    $"Package '{packageLabel}' is missing board pad mapping for {missingPads}. Existing footprint was kept."));
            StatusText = result.Diagnostics[0].Message;
            return result;
        }

        BoardComponentInstance existing = Components[componentIndex];
        BoardComponentInstance replaced = existing with
        {
            FootprintPreview = replacementFootprint,
            FootprintPrimitives = replacementPrimitives
        };
        Components[componentIndex] = replaced;
        if (SelectedComponent?.SyncId == syncId)
        {
            SelectedComponent = replaced;
        }

        RefreshAirwireEndpoints();
        OnPropertyChanged(nameof(VisibleTraces));
        StatusText = $"Replaced {replaced.ReferenceDesignator} board footprint with {packageLabel}.";
        return BoardFootprintReplacementResult.Success(syncId, packageLabel);
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

    private static HashSet<string> ReplacementPadNames(IReadOnlyList<BoardFootprintPrimitive> primitives) =>
        primitives
            .Select(PadNameOrNull)
            .Where(padName => !string.IsNullOrWhiteSpace(padName))
            .Select(padName => padName!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private IReadOnlyList<string> ConnectedPadNames(string syncId)
    {
        List<string> padNames = [];
        foreach (BoardAirwire airwire in Airwires)
        {
            AddEndpointPadName(padNames, syncId, airwire.StartSyncId, airwire.StartPinName);
            AddEndpointPadName(padNames, syncId, airwire.EndSyncId, airwire.EndPinName);
        }

        foreach (BoardAirwire airwire in retiredAirwiresByTraceId.Values)
        {
            AddEndpointPadName(padNames, syncId, airwire.StartSyncId, airwire.StartPinName);
            AddEndpointPadName(padNames, syncId, airwire.EndSyncId, airwire.EndPinName);
        }

        foreach (BoardTrace trace in Traces)
        {
            AddEndpointPadName(padNames, syncId, trace.StartPadSyncId, trace.StartPadName);
            AddEndpointPadName(padNames, syncId, trace.EndPadSyncId, trace.EndPadName);
        }

        return padNames;
    }

    private static string? PadNameOrNull(BoardFootprintPrimitive primitive) =>
        primitive switch
        {
            BoardFootprintPadPrimitive pad => pad.Name,
            BoardFootprintSmdPrimitive smd => smd.Name,
            _ => null
        };

    private static void AddEndpointPadName(List<string> padNames, string componentSyncId, string? endpointSyncId, string? padName)
    {
        if (string.Equals(componentSyncId, endpointSyncId, StringComparison.Ordinal) &&
            !string.IsNullOrWhiteSpace(padName))
        {
            padNames.Add(padName);
        }
    }

    public BoardComponentInstance? SelectComponentAt(CadPoint point)
    {
        BoardComponentInstance? candidate = FindComponentAt(point);
        if (candidate is null)
        {
            SelectedComponent = null;
            SelectedTraceSegmentIndex = null;
            ClearBoardGroupSelection();
            StatusText = "No board component selected.";
            return null;
        }

        ClearBoardGroupSelection();
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

    public BoardSelectionSnapshot SelectBoardObjectsIn(CadRectangle selectionBounds)
    {
        selectedBoardComponents.Clear();
        selectedBoardTraces.Clear();
        selectedBoardVias.Clear();
        selectedFootprintPrimitiveKinds.Clear();

        foreach (BoardComponentInstance component in Components)
        {
            if (!Intersects(selectionBounds, ComponentBounds(component)))
            {
                continue;
            }

            selectedBoardComponents.Add(component);
            AddSelectedFootprintPrimitiveKinds(selectionBounds, component);
        }

        foreach (BoardTrace trace in Traces)
        {
            if (TraceIntersects(selectionBounds, trace))
            {
                selectedBoardTraces.Add(trace);
            }
        }

        foreach (BoardVia via in Vias)
        {
            if (Intersects(selectionBounds, ViaBounds(via)))
            {
                selectedBoardVias.Add(via);
            }
        }

        SelectedComponent = selectedBoardComponents.Count == 1 && SelectedObjectCount == 1 ? selectedBoardComponents[0] : null;
        SelectedTrace = selectedBoardTraces.Count == 1 && SelectedObjectCount == 1 ? selectedBoardTraces[0] : null;
        SelectedVia = selectedBoardVias.Count == 1 && SelectedObjectCount == 1 ? selectedBoardVias[0] : null;
        SelectedTraceSegmentIndex = null;
        OnBoardSelectionChanged();

        BoardSelectionSnapshot snapshot = CreateSelectionSnapshot();
        StatusText = snapshot.TotalObjects == 1
            ? "Selected 1 board object."
            : $"Selected {snapshot.TotalObjects} board objects.";
        return snapshot;
    }

    public BoardSelectionSnapshot CopySelectedBoardObjects()
    {
        if (SelectedObjectCount == 0)
        {
            StatusText = "Select board objects before copying.";
            return CreateSelectionSnapshot();
        }

        boardClipboard = new BoardClipboardSnapshot(
            [.. selectedBoardComponents],
            [.. selectedBoardTraces],
            [.. selectedBoardVias]);
        BoardSelectionSnapshot snapshot = CreateSelectionSnapshot();
        StatusText = snapshot.TotalObjects == 1
            ? "Copied 1 board object."
            : $"Copied {snapshot.TotalObjects} board objects.";
        return snapshot;
    }

    public BoardSelectionSnapshot PasteBoardClipboard()
    {
        if (boardClipboard is null || boardClipboard.TotalObjects == 0)
        {
            StatusText = "Copy board objects before pasting.";
            return CreateSelectionSnapshot();
        }

        CadVector offset = new(GridSpacingInternal * 2, GridSpacingInternal * 2);
        selectedBoardComponents.Clear();
        selectedBoardTraces.Clear();
        selectedBoardVias.Clear();
        selectedFootprintPrimitiveKinds.Clear();

        foreach (BoardComponentInstance component in boardClipboard.Components)
        {
            BoardComponentInstance pasted = component with
            {
                SyncId = $"copy-{Guid.NewGuid():N}",
                Position = placementGrid.Snap(component.Position + offset)
            };
            Components.Add(pasted);
            selectedBoardComponents.Add(pasted);
            AddSelectedFootprintPrimitiveKinds(ComponentBounds(pasted), pasted);
        }

        foreach (BoardTrace trace in boardClipboard.Traces)
        {
            BoardTrace pasted = trace with
            {
                TraceId = Guid.NewGuid().ToString("N"),
                RoutePoints = trace.RoutePoints.Select(point => placementGrid.Snap(point + offset)).ToArray(),
                StartPadSyncId = null,
                StartPadReferenceDesignator = null,
                StartPadName = null,
                EndPadSyncId = null,
                EndPadReferenceDesignator = null,
                EndPadName = null
            };
            Traces.Add(pasted);
            selectedBoardTraces.Add(pasted);
        }

        foreach (BoardVia via in boardClipboard.Vias)
        {
            BoardVia pasted = via with
            {
                ViaId = Guid.NewGuid().ToString("N"),
                Position = placementGrid.Snap(via.Position + offset)
            };
            Vias.Add(pasted);
            selectedBoardVias.Add(pasted);
        }

        SelectedComponent = selectedBoardComponents.Count == 1 && SelectedObjectCount == 1 ? selectedBoardComponents[0] : null;
        SelectedTrace = selectedBoardTraces.Count == 1 && SelectedObjectCount == 1 ? selectedBoardTraces[0] : null;
        SelectedVia = selectedBoardVias.Count == 1 && SelectedObjectCount == 1 ? selectedBoardVias[0] : null;
        SelectedTraceSegmentIndex = null;
        OnPropertyChanged(nameof(VisibleTraces));
        OnPropertyChanged(nameof(VisibleVias));
        OnBoardSelectionChanged();

        BoardSelectionSnapshot snapshot = CreateSelectionSnapshot();
        StatusText = snapshot.TotalObjects == 1
            ? "Pasted 1 board object."
            : $"Pasted {snapshot.TotalObjects} board objects.";
        return snapshot;
    }

    public BoardSelectionSnapshot DuplicateSelectedBoardObjects()
    {
        BoardSelectionSnapshot copied = CopySelectedBoardObjects();
        if (copied.TotalObjects == 0)
        {
            return copied;
        }

        BoardSelectionSnapshot pasted = PasteBoardClipboard();
        StatusText = pasted.TotalObjects == 1
            ? "Duplicated 1 board object."
            : $"Duplicated {pasted.TotalObjects} board objects.";
        return pasted;
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

    private void TrySetSelectedViaSizeMillimeters(string value, bool isDiameter)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        string dimensionName = isDiameter ? "diameter" : "drill";
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal millimeters))
        {
            StatusText = $"Via {dimensionName} must be a number in millimeters.";
            return;
        }

        if (SelectedVia is null)
        {
            StatusText = "No board via is selected.";
            return;
        }

        long sizeInternal = (long)Math.Round(millimeters * CadUnit.InternalUnitsPerMillimeter, MidpointRounding.AwayFromZero);
        long diameterInternal = isDiameter ? sizeInternal : SelectedVia.DiameterInternal;
        long drillInternal = isDiameter ? SelectedVia.DrillInternal : sizeInternal;

        try
        {
            SetSelectedViaSizeInternal(diameterInternal, drillInternal);
        }
        catch (ArgumentOutOfRangeException error)
        {
            StatusText = error.Message;
        }
        catch (InvalidOperationException error)
        {
            StatusText = error.Message;
        }
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
        ClearBoardGroupSelection();
        StatusText = "Board route tool active.";
    }

    public void SetRouteCornerMode(string mode)
    {
        RouteCornerMode = mode;
    }

    public void SetFreeRouteMode(bool isActive)
    {
        IsFreeRouteModeActive = isActive;
        StatusText = isActive
            ? "Board free-route mode active."
            : "Board free-route mode inactive.";
    }

    public bool TraceClickAt(CadPoint point)
    {
        BoardPadHit? padHit = FindPadAt(point);
        BoardTraceEndpointHit? endpointHit = padHit is null ? FindTraceEndpointAt(point) : null;
        CadPoint routePoint = padHit?.Position ?? endpointHit?.Position ?? placementGrid.Snap(point);
        if (PendingTraceStart is null)
        {
            if (padHit is null && endpointHit is null && !IsFreeRouteModeActive)
            {
                StatusText = "Start a board trace from a pad or existing trace endpoint.";
                return false;
            }

            PendingTraceStart = routePoint;
            pendingTraceStartPad = padHit;
            pendingTraceRoutePoints.Clear();
            pendingTraceRoutePoints.Add(routePoint);
            OnPropertyChanged(nameof(PendingTraceRoutePoints));
            StatusText = StartTraceStatusText(padHit, endpointHit, routePoint);
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
        BoardTraceEndpointHit? endpointHit = endPad is null ? FindTraceEndpointAt(point) : null;
        if (endPad is null && endpointHit is null && !IsFreeRouteModeActive)
        {
            StatusText = pendingTraceStartPad is null
                ? "Finish a board trace at a pad, existing trace endpoint, or enable free-route mode."
                : $"Finish at a pad on the same airwire as {pendingTraceStartPad.ReferenceDesignator}.{pendingTraceStartPad.PadName}.";
            return false;
        }

        BoardAirwire? retiredAirwire = null;
        if (pendingTraceStartPad is not null && endPad is not null)
        {
            retiredAirwire = FindAirwireBetween(pendingTraceStartPad, endPad);
            if (retiredAirwire is null)
            {
                StatusText = $"Finish at a pad on the same airwire as {pendingTraceStartPad.ReferenceDesignator}.{pendingTraceStartPad.PadName}.";
                return false;
            }
        }

        AddRouteLeg(pendingTraceRoutePoints, endPad?.Position ?? endpointHit?.Position ?? placementGrid.Snap(point));
        if (pendingTraceRoutePoints.Count < 2)
        {
            StatusText = "Board trace needs at least two points.";
            return false;
        }

        string traceId = Guid.NewGuid().ToString("N");
        Traces.Add(new BoardTrace(
            traceId,
            ActiveLayerName,
            [.. pendingTraceRoutePoints],
            StartPadSyncId: pendingTraceStartPad?.SyncId,
            StartPadReferenceDesignator: pendingTraceStartPad?.ReferenceDesignator,
            StartPadName: pendingTraceStartPad?.PadName,
            EndPadSyncId: endPad?.SyncId,
            EndPadReferenceDesignator: endPad?.ReferenceDesignator,
            EndPadName: endPad?.PadName));
        if (retiredAirwire is not null)
        {
            Airwires.Remove(retiredAirwire);
            retiredAirwiresByTraceId[traceId] = retiredAirwire;
        }

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
            ThrowWithStatus("No board trace segment is selected.");
        }

        int traceIndex = Traces.IndexOf(SelectedTrace);
        if (traceIndex < 0)
        {
            ThrowWithStatus("The selected board trace is no longer in the document.");
        }

        int segmentIndex = SelectedTraceSegmentIndex.Value;
        List<CadPoint> routePoints = [.. SelectedTrace.RoutePoints];
        if (segmentIndex <= 0 || segmentIndex >= routePoints.Count)
        {
            ThrowWithStatus("The selected board trace segment is no longer valid.");
        }

        CadPoint start = routePoints[segmentIndex - 1];
        CadPoint end = routePoints[segmentIndex];
        CadPoint snappedPoint = SnapPointToSegmentGrid(requestedPosition, start, end);
        string fromLayer = SelectedTrace.LayerName;
        string toLayer = fromLayer == "Top" ? "Bottom" : "Top";
        BoardVia via = new(
            Guid.NewGuid().ToString("N"),
            snappedPoint,
            fromLayer,
            toLayer);

        List<CadPoint> insertedRoute = [.. routePoints.Take(segmentIndex)];
        insertedRoute.Add(snappedPoint);
        insertedRoute.AddRange(routePoints.Skip(segmentIndex));

        BoardTrace updatedTrace = SelectedTrace with { RoutePoints = RemoveAdjacentDuplicateRoutePoints(insertedRoute) };
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

    [DoesNotReturn]
    private void ThrowWithStatus(string message)
    {
        StatusText = message;
        throw new InvalidOperationException(message);
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
            BoardTrace deletedTrace = SelectedTrace;
            Traces.Remove(SelectedTrace);
            SelectedTrace = null;
            SelectedTraceSegmentIndex = null;
            RestoreAirwireForTrace(deletedTrace);
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
        OnLayerPaletteChanged();
        StatusText = $"Layer {updated.Name} {(updated.IsVisible ? "visible" : "hidden")}.";
    }

    public void SetLayerColor(string layerName, string colorHex)
    {
        int index = IndexOfLayer(layerName);
        BoardLayer updated = Layers[index] with { ColorHex = colorHex };
        Layers[index] = updated;
        OnLayerPaletteChanged();
        StatusText = $"Layer {updated.Name} color set to {updated.ColorHex}.";
    }

    public BoardLayerPaletteState ExportLayerPaletteState() =>
        new(
            ActiveLayerName,
            Layers
                .Select(layer => new BoardLayerState(layer.Name, layer.ColorHex, layer.IsVisible))
                .ToArray());

    public BoardLayerPaletteImportResult ApplyLayerPalettePreset(BoardLayerPaletteState preset) =>
        ImportLayerPaletteState(preset);

    public BoardLayerPaletteImportResult ImportLayerPaletteState(BoardLayerPaletteState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        List<string> diagnostics = ValidateLayerPaletteState(state);
        if (diagnostics.Count > 0)
        {
            StatusText = "Board layer palette import failed.";
            return new BoardLayerPaletteImportResult(false, diagnostics);
        }

        Layers.Clear();
        foreach (BoardLayerState layer in state.Layers)
        {
            Layers.Add(new BoardLayer(layer.Name, layer.ColorHex, layer.IsVisible));
        }

        ActiveLayerName = state.ActiveLayerName;
        OnLayerPaletteChanged();
        StatusText = $"Imported board layer palette with {Layers.Count} layers.";
        return new BoardLayerPaletteImportResult(true, []);
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

        ClearBoardGroupSelection();
        SelectedComponent = null;
        SelectedVia = null;
        SelectedTrace = traceHit.Trace;
        SelectedTraceSegmentIndex = traceHit.SegmentIndex;
        StatusText = $"Selected board trace on {traceHit.Trace.LayerName}.";
        return traceHit.Trace;
    }

    private BoardTraceEndpointHit? FindTraceEndpointAt(CadPoint point)
    {
        const long tolerance = 600_000;
        IReadOnlyList<BoardTrace> visibleTraces = VisibleTraces;
        for (int traceIndex = visibleTraces.Count - 1; traceIndex >= 0; traceIndex--)
        {
            BoardTrace trace = visibleTraces[traceIndex];
            if (trace.RoutePoints.Count == 0)
            {
                continue;
            }

            CadPoint start = trace.RoutePoints[0];
            if (IsEndpointHit(start, point, tolerance))
            {
                return new BoardTraceEndpointHit(start);
            }

            CadPoint end = trace.RoutePoints[^1];
            if (IsEndpointHit(end, point, tolerance))
            {
                return new BoardTraceEndpointHit(end);
            }
        }

        return null;
    }

    private BoardVia? SelectViaAt(CadPoint point)
    {
        BoardVia? via = FindViaAt(point, Vias);
        if (via is null)
        {
            return null;
        }

        ClearBoardGroupSelection();
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
                : BoardFootprintGeometry.HitTest(candidate, point))
            {
                return candidate;
            }
        }

        return null;
    }

    private void ClearBoardGroupSelection()
    {
        if (SelectedObjectCount == 0 && selectedFootprintPrimitiveKinds.Count == 0)
        {
            return;
        }

        selectedBoardComponents.Clear();
        selectedBoardTraces.Clear();
        selectedBoardVias.Clear();
        selectedFootprintPrimitiveKinds.Clear();
        OnBoardSelectionChanged();
    }

    private BoardSelectionSnapshot CreateSelectionSnapshot() =>
        new(
            selectedBoardComponents.Select(component => component.SyncId).ToArray(),
            selectedBoardTraces.Select(trace => trace.TraceId).ToArray(),
            selectedBoardVias.Select(via => via.ViaId).ToArray(),
            selectedFootprintPrimitiveKinds.Distinct(StringComparer.Ordinal).ToArray());

    private void OnBoardSelectionChanged()
    {
        OnPropertyChanged(nameof(SelectedBoardComponents));
        OnPropertyChanged(nameof(SelectedBoardTraces));
        OnPropertyChanged(nameof(SelectedBoardVias));
        OnPropertyChanged(nameof(SelectedFootprintPrimitiveKinds));
        OnPropertyChanged(nameof(SelectedObjectCount));
    }

    private void AddSelectedFootprintPrimitiveKinds(CadRectangle selectionBounds, BoardComponentInstance component)
    {
        foreach (BoardFootprintPrimitive primitive in component.FootprintPrimitives)
        {
            if (Intersects(selectionBounds, PrimitiveBounds(component, primitive)) &&
                !selectedFootprintPrimitiveKinds.Contains(primitive.Kind, StringComparer.Ordinal))
            {
                selectedFootprintPrimitiveKinds.Add(primitive.Kind);
            }
        }
    }

    private static bool TraceIntersects(CadRectangle selectionBounds, BoardTrace trace)
    {
        if (trace.RoutePoints.Count == 0)
        {
            return false;
        }

        if (trace.RoutePoints.Any(selectionBounds.Contains))
        {
            return true;
        }

        for (int index = 1; index < trace.RoutePoints.Count; index++)
        {
            if (Intersects(selectionBounds, BoundsOfPoints([trace.RoutePoints[index - 1], trace.RoutePoints[index]])))
            {
                return true;
            }
        }

        return false;
    }

    private static CadRectangle ViaBounds(BoardVia via)
    {
        long radius = via.DiameterInternal / 2;
        return new CadRectangle(
            via.Position.X - radius,
            via.Position.Y - radius,
            via.Position.X + radius,
            via.Position.Y + radius);
    }

    private static CadRectangle PrimitiveBounds(BoardComponentInstance component, BoardFootprintPrimitive primitive) =>
        primitive switch
        {
            BoardFootprintPadPrimitive pad => PadBounds(component, pad.Position, pad.Size),
            BoardFootprintSmdPrimitive smd => PadBounds(component, smd.Position, smd.Size),
            BoardFootprintHolePrimitive hole => CircleBounds(BoardFootprintGeometry.TransformLocalPoint(component, hole.Position), hole.DrillSize / 2),
            BoardFootprintKeepoutPrimitive keepout => TransformBounds(component, keepout.Bounds),
            BoardFootprintLinePrimitive line => BoundsOfPoints(
                [
                    BoardFootprintGeometry.TransformLocalPoint(component, line.Start),
                    BoardFootprintGeometry.TransformLocalPoint(component, line.End)
                ]),
            BoardFootprintArcPrimitive arc => CircleBounds(BoardFootprintGeometry.TransformLocalPoint(component, arc.Center), arc.Radius),
            BoardFootprintTextPrimitive text => TextBounds(component, text),
            _ => ComponentBounds(component)
        };

    private static CadRectangle PadBounds(BoardComponentInstance component, CadPoint localPosition, CadVector localSize)
    {
        CadPoint center = BoardFootprintGeometry.TransformLocalPoint(component, localPosition);
        CadVector size = BoardFootprintGeometry.SizeForRotation(component, localSize);
        return new CadRectangle(
            center.X - (size.X / 2),
            center.Y - (size.Y / 2),
            center.X + (size.X / 2),
            center.Y + (size.Y / 2));
    }

    private static CadRectangle TextBounds(BoardComponentInstance component, BoardFootprintTextPrimitive text)
    {
        CadPoint position = BoardFootprintGeometry.TransformLocalPoint(component, text.Position);
        long width = Math.Max(text.Size, text.Value.Length * text.Size / 2);
        CadVector size = BoardFootprintGeometry.SizeForRotation(component, new CadVector(width, text.Size));
        return new CadRectangle(position.X, position.Y, position.X + size.X, position.Y + size.Y);
    }

    private static CadRectangle CircleBounds(CadPoint center, long radius) =>
        new(center.X - radius, center.Y - radius, center.X + radius, center.Y + radius);

    private static CadRectangle TransformBounds(BoardComponentInstance component, CadRectangle bounds)
    {
        CadPoint[] corners =
        [
            BoardFootprintGeometry.TransformLocalPoint(component, new CadPoint(bounds.Left, bounds.Top)),
            BoardFootprintGeometry.TransformLocalPoint(component, new CadPoint(bounds.Right, bounds.Top)),
            BoardFootprintGeometry.TransformLocalPoint(component, new CadPoint(bounds.Right, bounds.Bottom)),
            BoardFootprintGeometry.TransformLocalPoint(component, new CadPoint(bounds.Left, bounds.Bottom))
        ];
        return new CadRectangle(
            corners.Min(point => point.X),
            corners.Min(point => point.Y),
            corners.Max(point => point.X),
            corners.Max(point => point.Y));
    }

    private static bool Intersects(CadRectangle left, CadRectangle right) =>
        left.Left <= right.Right &&
        left.Right >= right.Left &&
        left.Top <= right.Bottom &&
        left.Bottom >= right.Top;

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
            Airwires[index] = RefreshAirwireEndpoints(Airwires[index]);
        }
    }

    private BoardAirwire RefreshAirwireEndpoints(BoardAirwire airwire) =>
        airwire with
        {
            StartPosition = BoardPositionForWireEndpoint(airwire.StartSyncId, airwire.StartPinName),
            EndPosition = BoardPositionForWireEndpoint(airwire.EndSyncId, airwire.EndPinName)
        };

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
                    PadRouteHitTest(component, throughHole.Position, throughHole.Size, throughHole.Shape, point))
                {
                    CadPoint padCenter = BoardFootprintGeometry.TransformLocalPoint(component, throughHole.Position);
                    return new BoardPadHit(component.SyncId, component.ReferenceDesignator, throughHole.Name, padCenter);
                }

                if (primitive is BoardFootprintSmdPrimitive smd &&
                    PadRouteHitTest(component, smd.Position, smd.Size, smd.Shape, point))
                {
                    CadPoint padCenter = BoardFootprintGeometry.TransformLocalPoint(component, smd.Position);
                    return new BoardPadHit(component.SyncId, component.ReferenceDesignator, smd.Name, padCenter);
                }
            }
        }

        return null;
    }

    private static bool PadRouteHitTest(
        BoardComponentInstance component,
        CadPoint padPosition,
        CadVector padSize,
        string padShape,
        CadPoint point)
    {
        CadPoint center = BoardFootprintGeometry.TransformLocalPoint(component, padPosition);
        CadVector size = BoardFootprintGeometry.SizeForRotation(component, padSize);
        if (padShape is "Round" or "Oval")
        {
            double rx = Math.Max(1, (size.X / 2d) + PadRouteHitToleranceInternal);
            double ry = Math.Max(1, (size.Y / 2d) + PadRouteHitToleranceInternal);
            double dx = point.X - center.X;
            double dy = point.Y - center.Y;
            return ((dx * dx) / (rx * rx)) + ((dy * dy) / (ry * ry)) <= 1;
        }

        return new CadRectangle(
            center.X - (size.X / 2) - PadRouteHitToleranceInternal,
            center.Y - (size.Y / 2) - PadRouteHitToleranceInternal,
            center.X + (size.X / 2) + PadRouteHitToleranceInternal,
            center.Y + (size.Y / 2) + PadRouteHitToleranceInternal).Contains(point);
    }

    private BoardAirwire? FindAirwireBetween(BoardPadHit startPad, BoardPadHit endPad)
    {
        for (int index = Airwires.Count - 1; index >= 0; index--)
        {
            BoardAirwire airwire = Airwires[index];
            if (AirwireMatches(airwire, startPad, endPad))
            {
                return airwire;
            }
        }

        return null;
    }

    private BoardAirwire? FindAirwireBetween(BoardTrace trace)
    {
        if (trace.StartPadSyncId is null || trace.StartPadName is null ||
            trace.EndPadSyncId is null || trace.EndPadName is null)
        {
            return null;
        }

        for (int index = Airwires.Count - 1; index >= 0; index--)
        {
            BoardAirwire airwire = Airwires[index];
            if (AirwireMatches(airwire, trace))
            {
                return airwire;
            }
        }

        return null;
    }

    private void RetireAirwiresForExistingTraces()
    {
        retiredAirwiresByTraceId.Clear();
        foreach (BoardTrace trace in Traces)
        {
            BoardAirwire? airwire = FindAirwireBetween(trace);
            if (airwire is null)
            {
                continue;
            }

            Airwires.Remove(airwire);
            retiredAirwiresByTraceId[trace.TraceId] = airwire;
        }
    }

    private void RestoreAirwireForTrace(BoardTrace trace)
    {
        if (!retiredAirwiresByTraceId.Remove(trace.TraceId, out BoardAirwire? airwire))
        {
            return;
        }

        Airwires.Add(RefreshAirwireEndpoints(airwire));
    }

    private static bool AirwireMatches(BoardAirwire airwire, BoardPadHit startPad, BoardPadHit endPad) =>
        (EndpointMatches(airwire.StartSyncId, airwire.StartPinName, startPad) &&
            EndpointMatches(airwire.EndSyncId, airwire.EndPinName, endPad)) ||
        (EndpointMatches(airwire.StartSyncId, airwire.StartPinName, endPad) &&
            EndpointMatches(airwire.EndSyncId, airwire.EndPinName, startPad));

    private static bool AirwireMatches(BoardAirwire airwire, BoardTrace trace) =>
        EndpointMatches(airwire.StartSyncId, airwire.StartPinName, trace.StartPadSyncId, trace.StartPadName) &&
            EndpointMatches(airwire.EndSyncId, airwire.EndPinName, trace.EndPadSyncId, trace.EndPadName) ||
        EndpointMatches(airwire.StartSyncId, airwire.StartPinName, trace.EndPadSyncId, trace.EndPadName) &&
            EndpointMatches(airwire.EndSyncId, airwire.EndPinName, trace.StartPadSyncId, trace.StartPadName);

    private static bool EndpointMatches(string syncId, string pinName, BoardPadHit pad) =>
        string.Equals(syncId, pad.SyncId, StringComparison.Ordinal) &&
        string.Equals(pinName, pad.PadName, StringComparison.OrdinalIgnoreCase);

    private static bool EndpointMatches(string syncId, string pinName, string? traceSyncId, string? tracePadName) =>
        string.Equals(syncId, traceSyncId, StringComparison.Ordinal) &&
        string.Equals(pinName, tracePadName, StringComparison.OrdinalIgnoreCase);

    private static bool IsEndpointHit(CadPoint endpoint, CadPoint point, long tolerance) =>
        Math.Abs(endpoint.X - point.X) <= tolerance &&
        Math.Abs(endpoint.Y - point.Y) <= tolerance;

    private static string StartTraceStatusText(BoardPadHit? padHit, BoardTraceEndpointHit? endpointHit, CadPoint routePoint)
    {
        if (padHit is not null)
        {
            return $"Started board trace at pad {padHit.ReferenceDesignator}.{padHit.PadName}.";
        }

        return endpointHit is not null
            ? "Started board trace at existing route endpoint."
            : $"Started board trace at {FormatMillimeters(routePoint.X)} mm, {FormatMillimeters(routePoint.Y)} mm.";
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

    private static string FormatMillimeters(long internalUnits) =>
        ((decimal)internalUnits / CadUnit.InternalUnitsPerMillimeter).ToString("0.000", CultureInfo.InvariantCulture);

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

    private CadPoint SnapPointToSegmentGrid(CadPoint point, CadPoint start, CadPoint end)
    {
        if (start == end)
        {
            return placementGrid.Snap(start);
        }

        double dx = end.X - start.X;
        double dy = end.Y - start.Y;
        double t = (((point.X - start.X) * dx) + ((point.Y - start.Y) * dy)) / ((dx * dx) + (dy * dy));
        t = Math.Clamp(t, 0, 1);
        CadPoint projected = new(
            (long)Math.Round(start.X + (t * dx)),
            (long)Math.Round(start.Y + (t * dy)));
        return placementGrid.Snap(projected);
    }

    private static IReadOnlyList<CadPoint> RemoveAdjacentDuplicateRoutePoints(IReadOnlyList<CadPoint> routePoints)
    {
        List<CadPoint> route = [];
        foreach (CadPoint point in routePoints)
        {
            if (route.Count == 0 || route[^1] != point)
            {
                route.Add(point);
            }
        }

        return route;
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

    private static List<string> ValidateLayerPaletteState(BoardLayerPaletteState state)
    {
        List<string> diagnostics = [];
        HashSet<string> layerNames = new(StringComparer.Ordinal);
        foreach (BoardLayerState layer in state.Layers)
        {
            if (!layerNames.Add(layer.Name))
            {
                diagnostics.Add($"Layer name '{layer.Name}' appears more than once.");
            }
        }

        if (!layerNames.Contains(state.ActiveLayerName))
        {
            diagnostics.Add($"Active layer '{state.ActiveLayerName}' does not exist in the layer palette.");
        }

        return diagnostics;
    }

    private void OnLayerPaletteChanged()
    {
        OnPropertyChanged(nameof(Layers));
        OnPropertyChanged(nameof(VisibleTraces));
        OnPropertyChanged(nameof(VisibleVias));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private sealed record SegmentHit(double Distance, int SegmentIndex);

    private sealed record TraceHit(BoardTrace Trace, int SegmentIndex);

    private sealed record BoardClipboardSnapshot(
        IReadOnlyList<BoardComponentInstance> Components,
        IReadOnlyList<BoardTrace> Traces,
        IReadOnlyList<BoardVia> Vias)
    {
        public int TotalObjects => Components.Count + Traces.Count + Vias.Count;
    }

    private sealed record BoardPadHit(string SyncId, string ReferenceDesignator, string PadName, CadPoint Position);

    private sealed record BoardTraceEndpointHit(CadPoint Position);
}
