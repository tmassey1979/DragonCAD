using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Avalonia;
using DragonCAD.App.ComponentManager;
using DragonCAD.App.Placement;
using DragonCAD.Core.Geometry;

namespace DragonCAD.App.SchematicEditor;

public sealed class SchematicEditorViewModel : INotifyPropertyChanged
{
    private const long PinEndpointHitTolerance = 1_250_000;
    private const double PinLeadHitTolerance = 350_000;
    private const double WireSegmentHitTolerance = 700_000;
    private const long ComponentTextHitTolerance = 700_000;
    private static readonly IReadOnlyDictionary<string, string> EmptyComponentAttributes =
        new Dictionary<string, string>(StringComparer.Ordinal);

    private CadGrid placementGrid = new(new CadVector(CadUnit.InternalUnitsPerMillimeter, CadUnit.InternalUnitsPerMillimeter));
    private int nextComponentNumber = 1;
    private string statusText = "Schematic ready.";
    private bool isGridVisible = true;
    private string gridStyle = "Dots";
    private long gridSpacingInternal = CadUnit.InternalUnitsPerMillimeter;
    private SchematicComponentInstance? selectedComponent;
    private SchematicWire? selectedWire;
    private int? selectedWireSegmentIndex;
    private int? selectedWireVertexIndex;
    private SchematicPinEndpoint? pendingWireStart;
    private SchematicPinEndpoint? hoveredPin;
    private SchematicComponentInstance? hoveredComponent;
    private SchematicWire? hoveredWire;
    private int? hoveredWireSegmentIndex;
    private SchematicNetLabel? hoveredNetLabel;
    private string hoverTargetText = "No hover target";
    private ComponentPlacementIntent? activePlacementCandidate;
    private SchematicPinEndpoint? selectedPinEndpoint;
    private SchematicNetLabel? selectedNetLabel;
    private SchematicComponentTextLabel? selectedComponentTextLabel;
    private CadPoint? pendingWirePreviewPoint;
    private readonly List<CadPoint> pendingWireRoutePoints = [];
    private double zoomLevel = 1.0;
    private CadPoint viewportOrigin = new(0, 0);
    private bool isDirty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<SchematicComponentInstance> Components { get; } = [];

    public ObservableCollection<SchematicWire> Wires { get; } = [];

    public ObservableCollection<SchematicNetLabel> NetLabels { get; } = [];

    public ObservableCollection<SchematicNet> Nets { get; } = [];

    public ObservableCollection<SchematicNetLabelDiagnostic> NetLabelDiagnostics { get; } = [];

    public ObservableCollection<SchematicSelectedComponentMetadataDiagnostic> SelectedComponentMetadataDiagnostics { get; } = [];

    public bool IsDirty
    {
        get => isDirty;
        private set
        {
            if (isDirty == value)
            {
                return;
            }

            isDirty = value;
            OnPropertyChanged();
        }
    }

    public IEnumerable<SchematicWireSegmentRenderItem> RenderableWireSegments =>
        Wires.SelectMany(wire =>
            wire.RoutePoints
                .Skip(1)
                .Select((point, index) => new SchematicWireSegmentRenderItem(
                    wire.WireId,
                    index + 1,
                    wire.RoutePoints[index],
                    point,
                    wire.NetName,
                    wire.WireId == SelectedWire?.WireId && SelectedWireSegmentIndex == index + 1,
                    wire.WireId == HoveredWire?.WireId && HoveredWireSegmentIndex == index + 1)));

    public IEnumerable<SchematicWireVertexHandle> SelectedWireVertexHandles
    {
        get
        {
            if (SelectedWire is null)
            {
                yield break;
            }

            for (int index = 0; index < SelectedWire.RoutePoints.Count; index++)
            {
                yield return new SchematicWireVertexHandle(
                    SelectedWire.WireId,
                    index,
                    SelectedWire.RoutePoints[index],
                    index == SelectedWireVertexIndex,
                    IsEndpointVertexIndex(SelectedWire.RoutePoints, index));
            }
        }
    }

    public IEnumerable<SchematicNetLabelRenderItem> RenderableNetLabels =>
        NetLabels.Select(label => new SchematicNetLabelRenderItem(
            label.LabelId,
            label.NetName,
            label.Position,
            label.LabelId == SelectedNetLabel?.LabelId,
            label.LabelId == HoveredNetLabel?.LabelId,
            label.RotationDegrees));

    public IEnumerable<SchematicComponentTextLabel> RenderableComponentTextLabels =>
        Components.SelectMany(ComponentTextLabelsFor);

    public CadRectangle SheetBounds { get; } =
        new(-140_000_000, -100_000_000, 140_000_000, 100_000_000);

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

    public SchematicPinEndpoint? PendingWireStart
    {
        get => pendingWireStart;
        private set
        {
            if (pendingWireStart == value)
            {
                return;
            }

            pendingWireStart = value;
            OnPropertyChanged();
        }
    }

    public IReadOnlyList<CadPoint> PendingWireRoutePoints => pendingWireRoutePoints;

    public CadPoint? PendingWirePreviewPoint
    {
        get => pendingWirePreviewPoint;
        private set
        {
            if (pendingWirePreviewPoint == value)
            {
                return;
            }

            pendingWirePreviewPoint = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PendingWirePreviewRoutePoints));
        }
    }

    public IReadOnlyList<CadPoint> PendingWirePreviewRoutePoints
    {
        get
        {
            if (PendingWirePreviewPoint is null || pendingWireRoutePoints.Count == 0)
            {
                return pendingWireRoutePoints;
            }

            List<CadPoint> route = [..pendingWireRoutePoints];
            AddOrthogonalLeg(route, PendingWirePreviewPoint.Value);
            return route;
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

    public SchematicComponentInstance? SelectedComponent
    {
        get => selectedComponent;
        set
        {
            if (selectedComponent == value)
            {
                return;
            }

            selectedComponent = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectionSummary));
            OnPropertyChanged(nameof(SelectedComponentMetadata));
        }
    }

    public SchematicSelectedComponentMetadata? SelectedComponentMetadata =>
        SelectedComponent is null
            ? null
            : new SchematicSelectedComponentMetadata(
                SelectedComponent.ReferenceDesignator,
                SelectedComponent.DisplayName,
                SelectedComponent.Value,
                SelectedComponent.Attributes ?? EmptyComponentAttributes,
                SelectedComponent.ActivePackageVariantId,
                SelectedComponent.ActivePackageFootprintId,
                ActivePackageLabelFor(SelectedComponent));

    public SchematicWire? SelectedWire
    {
        get => selectedWire;
        private set
        {
            if (selectedWire == value)
            {
                return;
            }

            selectedWire = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectionSummary));
            OnPropertyChanged(nameof(SelectedWireVertexHandles));
            OnPropertyChanged(nameof(RenderableWireSegments));
        }
    }

    public int? SelectedWireSegmentIndex
    {
        get => selectedWireSegmentIndex;
        private set
        {
            if (selectedWireSegmentIndex == value)
            {
                return;
            }

            selectedWireSegmentIndex = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectionSummary));
            OnPropertyChanged(nameof(RenderableWireSegments));
        }
    }

    public int? SelectedWireVertexIndex
    {
        get => selectedWireVertexIndex;
        private set
        {
            if (selectedWireVertexIndex == value)
            {
                return;
            }

            selectedWireVertexIndex = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectionSummary));
            OnPropertyChanged(nameof(SelectedWireVertexHandles));
        }
    }

    public SchematicPinEndpoint? HoveredPin
    {
        get => hoveredPin;
        private set
        {
            if (hoveredPin == value)
            {
                return;
            }

            hoveredPin = value;
            OnPropertyChanged();
        }
    }

    public SchematicComponentInstance? HoveredComponent
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

    public SchematicWire? HoveredWire
    {
        get => hoveredWire;
        private set
        {
            if (hoveredWire == value)
            {
                return;
            }

            hoveredWire = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(RenderableWireSegments));
        }
    }

    public int? HoveredWireSegmentIndex
    {
        get => hoveredWireSegmentIndex;
        private set
        {
            if (hoveredWireSegmentIndex == value)
            {
                return;
            }

            hoveredWireSegmentIndex = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(RenderableWireSegments));
        }
    }

    public SchematicNetLabel? HoveredNetLabel
    {
        get => hoveredNetLabel;
        private set
        {
            if (hoveredNetLabel == value)
            {
                return;
            }

            hoveredNetLabel = value;
            OnPropertyChanged();
        }
    }

    public string HoverTargetText
    {
        get => hoverTargetText;
        private set
        {
            if (hoverTargetText == value)
            {
                return;
            }

            hoverTargetText = value;
            OnPropertyChanged();
        }
    }

    public SchematicPinEndpoint? SelectedPinEndpoint
    {
        get => selectedPinEndpoint;
        private set
        {
            if (selectedPinEndpoint == value)
            {
                return;
            }

            selectedPinEndpoint = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectionSummary));
        }
    }

    public SchematicNetLabel? SelectedNetLabel
    {
        get => selectedNetLabel;
        private set
        {
            if (selectedNetLabel == value)
            {
                return;
            }

            selectedNetLabel = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectionSummary));
        }
    }

    public SchematicComponentTextLabel? SelectedComponentTextLabel
    {
        get => selectedComponentTextLabel;
        private set
        {
            if (selectedComponentTextLabel == value)
            {
                return;
            }

            selectedComponentTextLabel = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectionSummary));
        }
    }

    public string SelectionSummary
    {
        get
        {
            if (SelectedComponentTextLabel is not null)
            {
                string kind = SelectedComponentTextLabel.Kind == SchematicComponentTextKind.Name ? "name" : "value";
                return $"Component text {SelectedComponentTextLabel.ReferenceDesignator} {kind}";
            }

            if (SelectedComponent is not null)
            {
                string valueText = string.IsNullOrWhiteSpace(SelectedComponent.Value)
                    ? ""
                    : $" value {SelectedComponent.Value}";
                string activePackageLabel = ActivePackageLabelFor(SelectedComponent);
                IReadOnlyDictionary<string, string> attributes = SelectedComponent.Attributes ?? EmptyComponentAttributes;
                string packageText = activePackageLabel == "No package"
                    ? ""
                    : $" package {activePackageLabel}";
                string attributesText = attributes.Count == 0
                    ? ""
                    : $" attributes {string.Join(", ", attributes.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => $"{pair.Key}={pair.Value}"))}";
                return $"Component {SelectedComponent.ReferenceDesignator}: {SelectedComponent.DisplayName}{valueText}{packageText}{attributesText}";
            }

            if (SelectedPinEndpoint is not null)
            {
                return $"Pin {SelectedPinEndpoint.ReferenceDesignator}.{SelectedPinEndpoint.PinName}";
            }

            if (SelectedNetLabel is not null)
            {
                return $"Net label {SelectedNetLabel.NetName}";
            }

            if (SelectedWire is not null && SelectedWireVertexIndex is { } vertexIndex)
            {
                return $"Wire {SelectedWire.NetName} vertex {vertexIndex}";
            }

            if (SelectedWire is not null && SelectedWireSegmentIndex is { } segmentIndex)
            {
                return $"Wire {SelectedWire.NetName} segment {segmentIndex}";
            }

            return SelectedWire is not null
                ? $"Wire {SelectedWire.NetName}"
                : "No schematic object selected";
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

    public ComponentPlacementIntent? ActivePlacementCandidate
    {
        get => activePlacementCandidate;
        private set
        {
            if (activePlacementCandidate == value)
            {
                return;
            }

            activePlacementCandidate = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasActivePlacementCandidate));
        }
    }

    public bool HasActivePlacementCandidate => ActivePlacementCandidate is not null;

    public bool TryArmComponentPlacement(ComponentPlacementIntent intent)
    {
        ArgumentNullException.ThrowIfNull(intent);

        if (intent.SymbolCount <= 0 || intent.FootprintCount <= 0)
        {
            ActivePlacementCandidate = null;
            StatusText = $"{intent.DisplayName} cannot be placed because it is missing verified schematic or footprint geometry.";
            return false;
        }

        ActivePlacementCandidate = intent;
        StatusText = $"Placement armed: {intent.DisplayName}. Click the schematic to place; press Escape to cancel.";
        return true;
    }

    public SchematicComponentInstance? PlaceArmedComponentAt(CadPoint requestedPosition)
    {
        if (ActivePlacementCandidate is null)
        {
            StatusText = "Choose a trusted placeable component before dropping it on the schematic.";
            return null;
        }

        return PlaceComponent(ActivePlacementCandidate, requestedPosition);
    }

    public bool CancelComponentPlacement()
    {
        if (ActivePlacementCandidate is null)
        {
            return false;
        }

        ActivePlacementCandidate = null;
        StatusText = "Placement cancelled. Click a schematic object to select it.";
        return true;
    }

    public SchematicComponentInstance PlaceComponent(ComponentPlacementIntent intent, CadPoint requestedPosition)
    {
        ArgumentNullException.ThrowIfNull(intent);

        string referenceDesignator = $"U{nextComponentNumber++}";
        SchematicComponentInstance instance = new(
            Guid.NewGuid().ToString("N"),
            referenceDesignator,
            intent.ComponentId,
            intent.DisplayName,
            placementGrid.Snap(requestedPosition),
            NormalizeSymbolPreview(intent.SymbolPreview),
            intent.FootprintPreview ?? ComponentFootprintPreview.Empty,
            "",
            0,
            false,
            intent.SymbolPreview is null ? null : SchematicSymbolRenderPreview.FromComponentPreview(intent.SymbolPreview));
        Components.Add(instance);
        SelectedComponent = instance;
        SelectedWire = null;
        SelectedWireVertexIndex = null;
        SelectedNetLabel = null;
        StatusText = $"Placed {referenceDesignator}: {intent.DisplayName}";
        return instance;
    }

    public void Clear()
    {
        Components.Clear();
        Wires.Clear();
        NetLabels.Clear();
        Nets.Clear();
        NetLabelDiagnostics.Clear();
        SelectedComponentMetadataDiagnostics.Clear();
        ClearPendingRoutePoints();
        ActivePlacementCandidate = null;
        PendingWireStart = null;
        PendingWirePreviewPoint = null;
        HoveredPin = null;
        HoveredNetLabel = null;
        SelectedPinEndpoint = null;
        SelectedNetLabel = null;
        SelectedComponentTextLabel = null;
        SelectedComponent = null;
        SelectedWire = null;
        SelectedWireSegmentIndex = null;
        SelectedWireVertexIndex = null;
        nextComponentNumber = 1;
        IsDirty = false;
        StatusText = "Schematic cleared.";
    }

    public SchematicComponentInstance? SelectComponentAt(CadPoint point)
    {
        if (SelectComponentTextLabelAt(point) is not null)
        {
            return null;
        }

        for (int index = Components.Count - 1; index >= 0; index--)
        {
            SchematicComponentInstance candidate = Components[index];
            if (Contains(candidate, point))
            {
                SelectedComponent = candidate;
                SelectedWire = null;
                SelectedWireSegmentIndex = null;
                SelectedWireVertexIndex = null;
                SelectedPinEndpoint = null;
                SelectedNetLabel = null;
                SelectedComponentTextLabel = null;
                StatusText = $"Selected {candidate.ReferenceDesignator}: {candidate.DisplayName}";
                return candidate;
            }
        }

        SelectedComponent = null;
        SelectedPinEndpoint = null;
        SelectedNetLabel = null;
        SelectedComponentTextLabel = null;
        SelectedWireVertexIndex = null;
        if (SelectNetLabelAt(point) is null)
        {
            SelectWireAt(point);
        }

        return null;
    }

    public SchematicPinEndpoint? SelectPinEndpointAt(CadPoint point)
    {
        SelectedPinEndpoint = FindPinAt(point);
        if (SelectedPinEndpoint is null)
        {
            return null;
        }

        SelectedComponent = null;
        SelectedWire = null;
        SelectedWireSegmentIndex = null;
        SelectedWireVertexIndex = null;
        SelectedNetLabel = null;
        SelectedComponentTextLabel = null;
        string connectedText = SelectedPinEndpoint.IsConnected
            ? $" connected to {NetNameForEndpoint(SelectedPinEndpoint)}"
            : "";
        StatusText = $"Selected pin {SelectedPinEndpoint.ReferenceDesignator}.{SelectedPinEndpoint.PinName}{connectedText}";
        return SelectedPinEndpoint;
    }

    public SchematicNetLabel PlaceNetLabel(string netName, CadPoint requestedPosition)
    {
        string normalizedNetName = netName.Trim();
        if (string.IsNullOrWhiteSpace(normalizedNetName))
        {
            throw new InvalidOperationException("Net label name is required.");
        }

        CadPoint snappedPosition = placementGrid.Snap(requestedPosition);
        SchematicNetLabel label = new(
            Guid.NewGuid().ToString("N"),
            normalizedNetName,
            snappedPosition,
            FindAttachedWireId(snappedPosition));
        NetLabels.Add(label);
        SelectedNetLabel = label;
        SelectedComponent = null;
        SelectedWire = null;
        SelectedWireSegmentIndex = null;
        SelectedWireVertexIndex = null;
        SelectedPinEndpoint = null;
        SelectedComponentTextLabel = null;
        RebuildNets();
        StatusText = $"Placed net label {label.NetName} at {FormatMillimeters(label.Position.X)} mm, {FormatMillimeters(label.Position.Y)} mm.";
        return label;
    }

    public SchematicNetLabel? SelectNetLabelAt(CadPoint point)
    {
        SchematicNetLabel? nearest = FindNetLabelAt(point);
        SelectedNetLabel = nearest;
        if (nearest is null)
        {
            StatusText = "No schematic object selected.";
            return null;
        }

        SelectedComponent = null;
        SelectedWire = null;
        SelectedWireSegmentIndex = null;
        SelectedWireVertexIndex = null;
        SelectedPinEndpoint = null;
        SelectedComponentTextLabel = null;
        StatusText = $"Selected net label {nearest.NetName}.";
        return nearest;
    }

    public SchematicNetLabel MoveSelectedNetLabelTo(CadPoint requestedPosition)
    {
        if (SelectedNetLabel is null)
        {
            throw new InvalidOperationException("No schematic net label is selected.");
        }

        int index = NetLabels.IndexOf(SelectedNetLabel);
        if (index < 0)
        {
            throw new InvalidOperationException("The selected schematic net label is no longer in the document.");
        }

        SchematicNetLabel moved = SelectedNetLabel with
        {
            Position = placementGrid.Snap(requestedPosition)
        };
        moved = moved with { AssociatedWireId = FindAttachedWireId(moved.Position) };
        NetLabels[index] = moved;
        SelectedNetLabel = moved;
        RebuildNets();
        StatusText = $"Moved net label {moved.NetName} to {FormatMillimeters(moved.Position.X)} mm, {FormatMillimeters(moved.Position.Y)} mm.";
        return moved;
    }

    public SchematicNetLabel RotateSelectedNetLabelClockwise()
    {
        if (SelectedNetLabel is null)
        {
            throw new InvalidOperationException("No schematic net label is selected.");
        }

        int index = NetLabels.IndexOf(SelectedNetLabel);
        if (index < 0)
        {
            throw new InvalidOperationException("The selected schematic net label is no longer in the document.");
        }

        SchematicNetLabel rotated = SelectedNetLabel with
        {
            RotationDegrees = NormalizeRotation(SelectedNetLabel.RotationDegrees + 90)
        };
        NetLabels[index] = rotated;
        SelectedNetLabel = rotated;
        StatusText = $"Rotated net label {rotated.NetName} to {rotated.RotationDegrees} degrees.";
        return rotated;
    }

    public SchematicNetLabel RenameSelectedNetLabel(string netName)
    {
        if (SelectedNetLabel is null)
        {
            throw new InvalidOperationException("No schematic net label is selected.");
        }

        string normalizedNetName = netName.Trim();
        if (string.IsNullOrWhiteSpace(normalizedNetName))
        {
            throw new InvalidOperationException("Net label name is required.");
        }

        int index = NetLabels.IndexOf(SelectedNetLabel);
        if (index < 0)
        {
            throw new InvalidOperationException("The selected schematic net label is no longer in the document.");
        }

        SchematicNetLabel renamed = SelectedNetLabel with { NetName = normalizedNetName };
        NetLabels[index] = renamed;
        SelectedNetLabel = renamed;
        RebuildNets();
        StatusText = $"Renamed net label to {renamed.NetName}.";
        return renamed;
    }

    public bool DeleteSelectedNetLabel()
    {
        if (SelectedNetLabel is null)
        {
            StatusText = "Select a net label before deleting it.";
            return false;
        }

        string deletedName = SelectedNetLabel.NetName;
        NetLabels.Remove(SelectedNetLabel);
        SelectedNetLabel = null;
        RebuildNets();
        StatusText = $"Deleted net label {deletedName}.";
        return true;
    }

    public SchematicComponentTextLabel? SelectComponentTextLabelAt(CadPoint point)
    {
        SchematicComponentTextLabel? label = FindComponentTextLabelAt(point);
        if (label is null)
        {
            return null;
        }

        SelectedComponentTextLabel = label;
        SelectedComponent = null;
        SelectedWire = null;
        SelectedWireSegmentIndex = null;
        SelectedWireVertexIndex = null;
        SelectedPinEndpoint = null;
        SelectedNetLabel = null;
        StatusText = $"Selected {label.ReferenceDesignator} {TextKindLabel(label.Kind)} text.";
        return label;
    }

    public SchematicComponentTextLabel MoveSelectedComponentNameTextTo(CadPoint requestedPosition) =>
        MoveSelectedComponentTextTo(SchematicComponentTextKind.Name, requestedPosition);

    public SchematicComponentTextLabel MoveSelectedComponentValueTextTo(CadPoint requestedPosition) =>
        MoveSelectedComponentTextTo(SchematicComponentTextKind.Value, requestedPosition);

    public SchematicComponentInstance ResetSelectedComponentTextPositions()
    {
        SchematicComponentInstance selected = RequireSelectedComponentInDocument(out int index);
        SchematicComponentInstance reset = selected with
        {
            NameTextPosition = null,
            ValueTextPosition = null
        };
        ReplaceSelectedComponent(index, reset);
        SelectedComponentTextLabel = null;
        IsDirty = true;
        StatusText = $"Reset {reset.ReferenceDesignator} text positions.";
        return reset;
    }

    public SchematicComponentInstance MoveSelectedComponentTo(CadPoint requestedPosition)
    {
        if (SelectedComponent is null)
        {
            throw new InvalidOperationException("No schematic component is selected.");
        }

        int index = Components.IndexOf(SelectedComponent);
        if (index < 0)
        {
            throw new InvalidOperationException("The selected schematic component is no longer in the document.");
        }

        SchematicComponentInstance moved = SelectedComponent with
        {
            Position = placementGrid.Snap(requestedPosition)
        };
        Components[index] = moved;
        SelectedComponent = moved;
        SelectedComponentTextLabel = null;
        RefreshWireEndpointsForMovedComponent(moved);
        StatusText = $"Moved {moved.ReferenceDesignator} to {FormatMillimeters(moved.Position.X)} mm, {FormatMillimeters(moved.Position.Y)} mm";
        return moved;
    }

    public SchematicComponentInstance RotateSelectedComponentClockwise()
    {
        if (SelectedComponent is null)
        {
            throw new InvalidOperationException("No schematic component is selected.");
        }

        int index = Components.IndexOf(SelectedComponent);
        if (index < 0)
        {
            throw new InvalidOperationException("The selected schematic component is no longer in the document.");
        }

        int nextRotation = NormalizeRotation(SelectedComponent.RotationDegrees + 90);
        SchematicComponentInstance rotated = SelectedComponent with { RotationDegrees = nextRotation };
        Components[index] = rotated;
        SelectedComponent = rotated;
        RefreshWireEndpointsForMovedComponent(rotated);
        StatusText = $"Rotated {rotated.ReferenceDesignator} to {rotated.RotationDegrees} degrees.";
        return rotated;
    }

    public SchematicComponentInstance MirrorSelectedComponent()
    {
        if (SelectedComponent is null)
        {
            throw new InvalidOperationException("No schematic component is selected.");
        }

        int index = Components.IndexOf(SelectedComponent);
        if (index < 0)
        {
            throw new InvalidOperationException("The selected schematic component is no longer in the document.");
        }

        SchematicComponentInstance mirrored = SelectedComponent with { IsMirrored = !SelectedComponent.IsMirrored };
        Components[index] = mirrored;
        SelectedComponent = mirrored;
        RefreshWireEndpointsForMovedComponent(mirrored);
        StatusText = mirrored.IsMirrored
            ? $"Mirrored {mirrored.ReferenceDesignator}."
            : $"Unmirrored {mirrored.ReferenceDesignator}.";
        return mirrored;
    }

    public SchematicComponentInstance DuplicateSelectedComponent()
    {
        if (SelectedComponent is null)
        {
            throw new InvalidOperationException("No schematic component is selected.");
        }

        SchematicComponentInstance source = SelectedComponent;
        string referenceDesignator = $"U{nextComponentNumber++}";
        SchematicComponentInstance duplicate = source with
        {
            InstanceId = Guid.NewGuid().ToString("N"),
            ReferenceDesignator = referenceDesignator,
            Position = placementGrid.Snap(new CadPoint(
                source.Position.X + (5 * CadUnit.InternalUnitsPerMillimeter),
                source.Position.Y + (5 * CadUnit.InternalUnitsPerMillimeter)))
        };

        Components.Add(duplicate);
        SelectedComponent = duplicate;
        SelectedWire = null;
        SelectedWireSegmentIndex = null;
        SelectedWireVertexIndex = null;
        StatusText = $"Duplicated {source.ReferenceDesignator} as {duplicate.ReferenceDesignator}.";
        return duplicate;
    }

    public SchematicComponentInstance UpdateSelectedComponentProperties(
        string referenceDesignator,
        string displayName,
        string value)
    {
        if (SelectedComponent is null)
        {
            throw new InvalidOperationException("No schematic component is selected.");
        }

        string normalizedReference = referenceDesignator.Trim();
        if (string.IsNullOrWhiteSpace(normalizedReference))
        {
            throw new InvalidOperationException("Reference designator is required.");
        }

        int index = Components.IndexOf(SelectedComponent);
        if (index < 0)
        {
            throw new InvalidOperationException("The selected schematic component is no longer in the document.");
        }

        SchematicComponentInstance updated = SelectedComponent with
        {
            ReferenceDesignator = normalizedReference,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? SelectedComponent.DisplayName : displayName.Trim(),
            Value = value.Trim()
        };
        Components[index] = updated;
        SelectedComponent = updated;
        RefreshWireEndpointReferenceDesignators(updated);
        RebuildNets();
        IsDirty = true;
        StatusText = $"Updated {updated.ReferenceDesignator} properties.";
        return updated;
    }

    public void MarkClean() => IsDirty = false;

    public SchematicComponentInstance SetSelectedComponentAttribute(string name, string value)
    {
        string normalizedName = name.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new InvalidOperationException("Attribute name is required.");
        }

        SchematicComponentInstance selected = RequireSelectedComponentInDocument(out int index);
        Dictionary<string, string> attributes = new(selected.Attributes ?? EmptyComponentAttributes, StringComparer.Ordinal)
        {
            [normalizedName] = value.Trim()
        };

        SchematicComponentInstance updated = selected with { Attributes = attributes };
        ReplaceSelectedComponent(index, updated);
        IsDirty = true;
        StatusText = $"Updated {updated.ReferenceDesignator} attribute {normalizedName}.";
        return updated;
    }

    public SchematicComponentInstance RemoveSelectedComponentAttribute(string name)
    {
        string normalizedName = name.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new InvalidOperationException("Attribute name is required.");
        }

        SchematicComponentInstance selected = RequireSelectedComponentInDocument(out int index);
        Dictionary<string, string> attributes = new(selected.Attributes ?? EmptyComponentAttributes, StringComparer.Ordinal);
        attributes.Remove(normalizedName);

        SchematicComponentInstance updated = selected with { Attributes = attributes };
        ReplaceSelectedComponent(index, updated);
        IsDirty = true;
        StatusText = $"Removed {updated.ReferenceDesignator} attribute {normalizedName}.";
        return updated;
    }

    public bool TryApplySelectedComponentPackage(ComponentPackageOption option)
    {
        ArgumentNullException.ThrowIfNull(option);

        SchematicComponentInstance selected = RequireSelectedComponentInDocument(out int index);
        SelectedComponentMetadataDiagnostics.Clear();
        if (string.IsNullOrWhiteSpace(option.FootprintId) ||
            option.PadCount <= 0 ||
            option.FootprintPreview.Pads.Count == 0)
        {
            string packageLabel = string.IsNullOrWhiteSpace(option.Label) ? "Selected package" : option.Label.Trim();
            string message = $"{packageLabel} cannot be applied because no footprint mapping exists; preserved {ActivePackageLabelFor(selected)}.";
            SelectedComponentMetadataDiagnostics.Add(new SchematicSelectedComponentMetadataDiagnostic(
                "DragonCAD.Schematic.InvalidPackageMapping",
                packageLabel,
                message));
            StatusText = message;
            return false;
        }

        SchematicComponentInstance updated = selected with
        {
            FootprintPreview = option.FootprintPreview,
            ActivePackageVariantId = option.VariantId.Trim(),
            ActivePackageFootprintId = option.FootprintId.Trim(),
            ActivePackageLabel = option.Label.Trim()
        };
        ReplaceSelectedComponent(index, updated);
        IsDirty = true;
        StatusText = $"Updated {updated.ReferenceDesignator} package to {updated.ActivePackageLabel}.";
        return true;
    }

    public void ZoomIn()
    {
        ZoomLevel = Math.Min(8.0, Math.Round(ZoomLevel * 1.25, 4));
        StatusText = $"Schematic zoom {ZoomLevel:0.##}x.";
    }

    public void ZoomOut()
    {
        ZoomLevel = Math.Max(0.25, Math.Round(ZoomLevel / 1.25, 4));
        StatusText = $"Schematic zoom {ZoomLevel:0.##}x.";
    }

    public void ZoomAt(CadPoint cursorCadPoint, bool zoomIn)
    {
        double oldZoom = ZoomLevel;
        double nextZoom = zoomIn
            ? Math.Min(8.0, Math.Round(ZoomLevel * 1.25, 4))
            : Math.Max(0.25, Math.Round(ZoomLevel / 1.25, 4));
        if (Math.Abs(oldZoom - nextZoom) < 0.0001)
        {
            StatusText = $"Schematic zoom {ZoomLevel:0.##}x.";
            return;
        }

        double ratio = oldZoom / nextZoom;
        ViewportOrigin = new CadPoint(
            cursorCadPoint.X - (long)Math.Round((cursorCadPoint.X - ViewportOrigin.X) * ratio, MidpointRounding.AwayFromZero),
            cursorCadPoint.Y - (long)Math.Round((cursorCadPoint.Y - ViewportOrigin.Y) * ratio, MidpointRounding.AwayFromZero));
        ZoomLevel = nextZoom;
        StatusText = $"Schematic zoom {ZoomLevel:0.##}x.";
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
        StatusText = $"Schematic pan {FormatMillimeters(ViewportOrigin.X)} mm, {FormatMillimeters(ViewportOrigin.Y)} mm.";
    }

    public void CenterSheetInViewport()
    {
        ViewportOrigin = CenterOf(SheetBounds);
        StatusText = "Centered schematic sheet.";
    }

    public void FitSheetToViewport(double viewportWidthPixels, double viewportHeightPixels, double paddingPixels)
    {
        ZoomLevel = CalculateFitZoom(SheetBounds, viewportWidthPixels, viewportHeightPixels, paddingPixels, basePixelsPerInternalUnit: 0.00002);
        ViewportOrigin = CenterOf(SheetBounds);
        StatusText = $"Fit schematic sheet at {ZoomLevel:0.##}x.";
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

    public bool ConnectPinAt(CadPoint point)
    {
        SchematicPinEndpoint? endpoint = FindPinAt(point);
        if (endpoint is null)
        {
            StatusText = "No pin at cursor.";
            return false;
        }

        if (PendingWireStart is null)
        {
            PendingWireStart = endpoint;
            StatusText = $"Started wire at {endpoint.ReferenceDesignator}.{endpoint.PinName}";
            return true;
        }

        if (PendingWireStart == endpoint)
        {
            StatusText = "Choose a different pin to complete the wire.";
            return false;
        }

        AddWire(PendingWireStart, endpoint, NormalizeCompletedRoute([PendingWireStart.Position], endpoint.Position));
        PendingWireStart = null;
        ClearPendingRoutePoints();
        return true;
    }

    public bool TraceClickAt(CadPoint point)
    {
        SchematicPinEndpoint? endpoint = FindPinAt(point);
        if (PendingWireStart is null)
        {
            if (endpoint is null)
            {
                StatusText = "Start schematic trace on a component pin.";
                return false;
            }

            PendingWireStart = endpoint;
            SetPendingRoutePoints([endpoint.Position]);
            PendingWirePreviewPoint = null;
            StatusText = $"Started wire at {endpoint.ReferenceDesignator}.{endpoint.PinName}";
            return true;
        }

        if (endpoint is not null && endpoint != PendingWireStart)
        {
            List<CadPoint> route = NormalizeCompletedRoute(pendingWireRoutePoints, endpoint.Position);
            AddWire(PendingWireStart, endpoint, route);
            PendingWireStart = null;
            ClearPendingRoutePoints();
            PendingWirePreviewPoint = null;
            return true;
        }

        CadPoint snappedPoint = placementGrid.Snap(point);
        AddOrthogonalLeg(pendingWireRoutePoints, snappedPoint);
        OnPropertyChanged(nameof(PendingWireRoutePoints));
        OnPropertyChanged(nameof(PendingWirePreviewRoutePoints));

        StatusText = $"Added wire segment at {FormatMillimeters(snappedPoint.X)} mm, {FormatMillimeters(snappedPoint.Y)} mm";
        return true;
    }

    public SchematicPinEndpoint? UpdateHoveredPinAt(CadPoint point)
    {
        HoveredPin = FindPinAt(point);
        if (HoveredPin is not null)
        {
            HoveredComponent = null;
            HoveredWire = null;
            HoveredWireSegmentIndex = null;
            HoveredNetLabel = null;
            HoverTargetText = PendingWireStart is not null && HoveredPin != PendingWireStart
                ? $"Connect to pin {HoveredPin.ReferenceDesignator}.{HoveredPin.PinName}"
                : $"Pin {HoveredPin.ReferenceDesignator}.{HoveredPin.PinName}";
        }
        else
        {
            HoveredComponent = null;
            HoveredWire = null;
            HoveredWireSegmentIndex = null;
            HoveredNetLabel = null;
            HoverTargetText = "No hover target";
        }

        return HoveredPin;
    }

    public string UpdateHoverTargetAt(CadPoint point)
    {
        if (UpdateHoveredPinAt(point) is { } hoveredEndpoint)
        {
            return HoverTargetText;
        }

        HoveredComponent = null;
        HoveredWire = null;
        HoveredWireSegmentIndex = null;
        HoveredNetLabel = null;

        for (int componentIndex = Components.Count - 1; componentIndex >= 0; componentIndex--)
        {
            SchematicComponentInstance candidate = Components[componentIndex];
            if (Contains(candidate, point))
            {
                HoveredComponent = candidate;
                HoverTargetText = $"Component {candidate.ReferenceDesignator}: {candidate.DisplayName}";
                StatusText = HoverTargetText;
                return HoverTargetText;
            }
        }

        if (FindNetLabelAt(point) is { } hoveredLabel)
        {
            HoveredNetLabel = hoveredLabel;
            HoverTargetText = $"Net label {hoveredLabel.NetName}";
            StatusText = HoverTargetText;
            return HoverTargetText;
        }

        double nearestDistance = double.MaxValue;
        SchematicWire? nearestWire = null;
        int? nearestSegmentIndex = null;
        for (int wireIndex = Wires.Count - 1; wireIndex >= 0; wireIndex--)
        {
            SchematicWire wire = Wires[wireIndex];
            int? segmentIndex = NearestSegmentIndex(point, wire.RoutePoints, WireSegmentHitTolerance, out double distance);
            if (segmentIndex is not null && distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestWire = wire;
                nearestSegmentIndex = segmentIndex;
            }
        }

        if (nearestWire is not null)
        {
            HoveredWire = nearestWire;
            HoveredWireSegmentIndex = nearestSegmentIndex;
            HoverTargetText = $"Wire {nearestWire.NetName} segment {nearestSegmentIndex}";
            StatusText = HoverTargetText;
            return HoverTargetText;
        }

        HoverTargetText = "No hover target";
        return HoverTargetText;
    }

    public void UpdateTracePreviewAt(CadPoint point)
    {
        if (PendingWireStart is null)
        {
            PendingWirePreviewPoint = null;
            return;
        }

        SchematicPinEndpoint? endpoint = UpdateHoveredPinAt(point);
        PendingWirePreviewPoint = endpoint is not null && endpoint != PendingWireStart
            ? endpoint.Position
            : placementGrid.Snap(point);
    }

    public bool CancelPendingWire()
    {
        if (PendingWireStart is null && pendingWireRoutePoints.Count == 0 && PendingWirePreviewPoint is null)
        {
            StatusText = "No pending wire to cancel.";
            return false;
        }

        PendingWireStart = null;
        ClearPendingRoutePoints();
        PendingWirePreviewPoint = null;
        HoveredPin = null;
        StatusText = "Cancelled pending wire.";
        return true;
    }

    public SchematicWire? SelectWireAt(CadPoint point)
    {
        double nearestDistance = double.MaxValue;
        SchematicWire? nearestWire = null;
        int? nearestSegmentIndex = null;
        for (int wireIndex = Wires.Count - 1; wireIndex >= 0; wireIndex--)
        {
            SchematicWire wire = Wires[wireIndex];
            int? segmentIndex = NearestSegmentIndex(point, wire.RoutePoints, WireSegmentHitTolerance, out double distance);
            if (segmentIndex is not null && distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestWire = wire;
                nearestSegmentIndex = segmentIndex;
            }
        }

        if (nearestWire is not null)
        {
            SelectedComponent = null;
            SelectedWire = nearestWire;
            SelectedWireSegmentIndex = nearestSegmentIndex;
            SelectedWireVertexIndex = null;
            SelectedPinEndpoint = null;
            SelectedNetLabel = null;
            StatusText = $"Selected wire {nearestWire.NetName}.";
            return nearestWire;
        }

        SelectedWire = null;
        SelectedWireSegmentIndex = null;
        SelectedWireVertexIndex = null;
        SelectNetLabelAt(point);
        return null;
    }

    public SchematicWireVertexHandle? SelectWireVertexAt(CadPoint point)
    {
        double nearestDistance = double.MaxValue;
        SchematicWire? nearestWire = null;
        int? nearestVertexIndex = null;
        for (int wireIndex = Wires.Count - 1; wireIndex >= 0; wireIndex--)
        {
            SchematicWire wire = Wires[wireIndex];
            for (int vertexIndex = 0; vertexIndex < wire.RoutePoints.Count; vertexIndex++)
            {
                double distance = Distance(point, wire.RoutePoints[vertexIndex]);
                if (distance <= WireSegmentHitTolerance && distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestWire = wire;
                    nearestVertexIndex = vertexIndex;
                }
            }
        }

        if (nearestWire is null || nearestVertexIndex is null)
        {
            SelectedWireVertexIndex = null;
            return null;
        }

        SelectedComponent = null;
        SelectedWire = nearestWire;
        SelectedWireSegmentIndex = null;
        SelectedWireVertexIndex = nearestVertexIndex;
        SelectedPinEndpoint = null;
        SelectedNetLabel = null;
        StatusText = $"Selected wire vertex {nearestVertexIndex} on {nearestWire.NetName}.";
        return new SchematicWireVertexHandle(
            nearestWire.WireId,
            nearestVertexIndex.Value,
            nearestWire.RoutePoints[nearestVertexIndex.Value],
            true,
            IsEndpointVertexIndex(nearestWire.RoutePoints, nearestVertexIndex.Value));
    }

    public bool IsSelectedWireVertexEndpoint() =>
        SelectedWire is not null &&
        SelectedWireVertexIndex is { } vertexIndex &&
        IsEndpointVertexIndex(SelectedWire.RoutePoints, vertexIndex);

    public SchematicWire MoveSelectedWireSegmentTo(CadPoint requestedPosition)
    {
        if (SelectedWire is null || SelectedWireSegmentIndex is null)
        {
            throw new InvalidOperationException("No schematic wire segment is selected.");
        }

        int wireIndex = Wires.IndexOf(SelectedWire);
        if (wireIndex < 0)
        {
            throw new InvalidOperationException("The selected schematic wire is no longer in the document.");
        }

        int segmentIndex = SelectedWireSegmentIndex.Value;
        if (segmentIndex <= 0 || segmentIndex >= SelectedWire.RoutePoints.Count)
        {
            throw new InvalidOperationException("The selected schematic wire segment is invalid.");
        }

        CadPoint snapped = placementGrid.Snap(requestedPosition);
        List<CadPoint> routePoints = [..SelectedWire.RoutePoints];
        CadPoint start = routePoints[segmentIndex - 1];
        CadPoint end = routePoints[segmentIndex];
        if (start.Y == end.Y)
        {
            routePoints[segmentIndex - 1] = new CadPoint(start.X, snapped.Y);
            routePoints[segmentIndex] = new CadPoint(end.X, snapped.Y);
        }
        else if (start.X == end.X)
        {
            routePoints[segmentIndex - 1] = new CadPoint(snapped.X, start.Y);
            routePoints[segmentIndex] = new CadPoint(snapped.X, end.Y);
        }
        else
        {
            throw new InvalidOperationException("Only orthogonal wire segments can be moved.");
        }

        SchematicWire moved = SelectedWire with { RoutePoints = routePoints };
        Wires[wireIndex] = moved;
        SelectedWire = moved;
        SelectedWireVertexIndex = null;
        StatusText = $"Moved wire segment {segmentIndex} on {moved.NetName}.";
        return moved;
    }

    public SchematicWire MoveSelectedWireVertexTo(CadPoint requestedPosition)
    {
        if (SelectedWire is null || SelectedWireVertexIndex is null)
        {
            throw new InvalidOperationException("No schematic wire vertex is selected.");
        }

        int wireIndex = Wires.IndexOf(SelectedWire);
        if (wireIndex < 0)
        {
            throw new InvalidOperationException("The selected schematic wire is no longer in the document.");
        }

        int vertexIndex = SelectedWireVertexIndex.Value;
        if (vertexIndex <= 0 || vertexIndex >= SelectedWire.RoutePoints.Count - 1)
        {
            throw new InvalidOperationException("Only interior schematic wire vertices can be moved.");
        }

        List<CadPoint> routePoints = [..SelectedWire.RoutePoints];
        CadPoint previous = routePoints[vertexIndex - 1];
        CadPoint current = routePoints[vertexIndex];
        CadPoint next = routePoints[vertexIndex + 1];
        CadPoint snapped = placementGrid.Snap(requestedPosition);

        routePoints[vertexIndex] = snapped;
        routePoints[vertexIndex - 1] = previous.X == current.X
            ? new CadPoint(snapped.X, previous.Y)
            : new CadPoint(previous.X, snapped.Y);
        routePoints[vertexIndex + 1] = next.X == current.X
            ? new CadPoint(snapped.X, next.Y)
            : new CadPoint(next.X, snapped.Y);

        routePoints = CompactRoute(routePoints);
        SchematicWire moved = SelectedWire with { RoutePoints = routePoints };
        Wires[wireIndex] = moved;
        SelectedWire = moved;
        int updatedVertexIndex = routePoints.IndexOf(snapped);
        SelectedWireVertexIndex = updatedVertexIndex > 0 && updatedVertexIndex < routePoints.Count - 1
            ? updatedVertexIndex
            : null;
        StatusText = $"Moved wire vertex {vertexIndex} on {moved.NetName}.";
        return moved;
    }

    public SchematicWire CompleteSelectedWireEndpointDrag(CadPoint releasePosition)
    {
        if (SelectedWire is null || SelectedWireVertexIndex is null)
        {
            throw new InvalidOperationException("No schematic wire endpoint is selected.");
        }

        int vertexIndex = SelectedWireVertexIndex.Value;
        if (!IsEndpointVertexIndex(SelectedWire.RoutePoints, vertexIndex))
        {
            throw new InvalidOperationException("Only schematic wire endpoints can be reconnected on release.");
        }

        int wireIndex = Wires.IndexOf(SelectedWire);
        if (wireIndex < 0)
        {
            throw new InvalidOperationException("The selected schematic wire is no longer in the document.");
        }

        SchematicPinEndpoint? endpoint = FindPinAt(releasePosition);
        bool isStartEndpoint = vertexIndex == 0;
        SchematicPinEndpoint fixedEndpoint = isStartEndpoint ? SelectedWire.End : SelectedWire.Start;
        if (endpoint is null || PinKey(endpoint) == PinKey(fixedEndpoint))
        {
            StatusText = "Wire endpoint must be released over a compatible pin.";
            return SelectedWire;
        }

        List<CadPoint> routePoints = [..SelectedWire.RoutePoints];
        int endpointIndex = isStartEndpoint ? 0 : routePoints.Count - 1;
        routePoints[endpointIndex] = endpoint.Position;
        RepairAdjacentEndpointSegment(routePoints, endpointIndex);
        routePoints = CompactRoute(OrthogonalizeRoute(routePoints));

        SchematicPinEndpoint connectedEndpoint = endpoint with { IsConnected = true };
        SchematicWire reconnected = isStartEndpoint
            ? SelectedWire with { Start = connectedEndpoint, RoutePoints = routePoints }
            : SelectedWire with { End = connectedEndpoint, RoutePoints = routePoints };
        Wires[wireIndex] = reconnected;
        SelectedWire = reconnected;
        SelectedWireVertexIndex = isStartEndpoint ? 0 : reconnected.RoutePoints.Count - 1;
        RebuildNets();
        SelectedWire = Wires.Single(wire => wire.WireId == reconnected.WireId);
        SelectedWireVertexIndex = isStartEndpoint ? 0 : SelectedWire.RoutePoints.Count - 1;
        StatusText = $"Reconnected wire endpoint to {endpoint.ReferenceDesignator}.{endpoint.PinName}.";
        return SelectedWire;
    }

    public SchematicWire InsertVertexIntoSelectedWireSegment(CadPoint requestedPosition)
    {
        if (SelectedWire is null || SelectedWireSegmentIndex is null)
        {
            throw new InvalidOperationException("No schematic wire segment is selected.");
        }

        int wireIndex = Wires.IndexOf(SelectedWire);
        if (wireIndex < 0)
        {
            throw new InvalidOperationException("The selected schematic wire is no longer in the document.");
        }

        int segmentIndex = SelectedWireSegmentIndex.Value;
        if (segmentIndex <= 0 || segmentIndex >= SelectedWire.RoutePoints.Count)
        {
            throw new InvalidOperationException("The selected schematic wire segment is invalid.");
        }

        CadPoint snapped = placementGrid.Snap(requestedPosition);
        IReadOnlyList<CadPoint> routePoints = SelectedWire.RoutePoints;
        CadPoint start = routePoints[segmentIndex - 1];
        CadPoint end = routePoints[segmentIndex];
        if (start.X != end.X && start.Y != end.Y)
        {
            throw new InvalidOperationException("Only orthogonal wire segments can accept inserted vertices.");
        }

        List<CadPoint> updatedRoute = [];
        for (int index = 0; index < segmentIndex; index++)
        {
            updatedRoute.Add(routePoints[index]);
        }

        AddInsertedOrthogonalVertex(updatedRoute, start, end, snapped);

        for (int index = segmentIndex + 1; index < routePoints.Count; index++)
        {
            updatedRoute.Add(routePoints[index]);
        }

        updatedRoute = CompactRoute(updatedRoute);
        SchematicWire updated = SelectedWire with { RoutePoints = updatedRoute };
        Wires[wireIndex] = updated;
        SelectedWire = updated;
        SelectedWireSegmentIndex = NearestSegmentIndex(snapped, updated.RoutePoints, 0, out _) ?? segmentIndex;
        SelectedWireVertexIndex = null;
        StatusText = $"Inserted wire vertex on {updated.NetName}.";
        return updated;
    }

    public SchematicWire DeleteSelectedWireSegment()
    {
        if (SelectedWire is null || SelectedWireSegmentIndex is null)
        {
            throw new InvalidOperationException("No schematic wire segment is selected.");
        }

        int wireIndex = Wires.IndexOf(SelectedWire);
        if (wireIndex < 0)
        {
            throw new InvalidOperationException("The selected schematic wire is no longer in the document.");
        }

        int segmentIndex = SelectedWireSegmentIndex.Value;
        if (segmentIndex <= 0 || segmentIndex >= SelectedWire.RoutePoints.Count)
        {
            throw new InvalidOperationException("The selected schematic wire segment is invalid.");
        }

        IReadOnlyList<CadPoint> routePoints = SelectedWire.RoutePoints;
        if (routePoints.Count <= 2)
        {
            throw new InvalidOperationException("Use wire delete for a single-segment schematic wire.");
        }

        CadPoint deletedStart = routePoints[segmentIndex - 1];
        CadPoint deletedEnd = routePoints[segmentIndex];
        int anchorBeforeIndex = Math.Max(0, segmentIndex - 2);
        int anchorAfterIndex = Math.Min(routePoints.Count - 1, segmentIndex + 1);

        List<CadPoint> updatedRoute = [];
        for (int index = 0; index <= anchorBeforeIndex; index++)
        {
            updatedRoute.Add(routePoints[index]);
        }

        AddOrthogonalLegAvoidingDeletedSegment(
            updatedRoute,
            routePoints[anchorAfterIndex],
            deletedStart,
            deletedEnd);

        for (int index = anchorAfterIndex + 1; index < routePoints.Count; index++)
        {
            updatedRoute.Add(routePoints[index]);
        }

        updatedRoute = CompactRoute(updatedRoute);
        SchematicWire updated = SelectedWire with { RoutePoints = updatedRoute };
        Wires[wireIndex] = updated;
        SelectedWire = updated;
        SelectedWireSegmentIndex = null;
        SelectedWireVertexIndex = null;
        RebuildNets();
        SelectedWire = Wires.Single(wire => wire.WireId == updated.WireId);
        StatusText = $"Deleted wire segment {segmentIndex} on {SelectedWire.NetName}.";
        return SelectedWire;
    }

    public bool DeleteSelectedWireVertex()
    {
        if (SelectedWire is null || SelectedWireVertexIndex is null)
        {
            StatusText = "Select a wire vertex before deleting it.";
            return false;
        }

        int wireIndex = Wires.IndexOf(SelectedWire);
        if (wireIndex < 0)
        {
            throw new InvalidOperationException("The selected schematic wire is no longer in the document.");
        }

        int vertexIndex = SelectedWireVertexIndex.Value;
        if (IsEndpointVertexIndex(SelectedWire.RoutePoints, vertexIndex))
        {
            StatusText = "Wire endpoint vertices cannot be deleted; reconnect or delete the wire instead.";
            return false;
        }

        if (!TryBuildRouteWithoutVertex(SelectedWire.RoutePoints, vertexIndex, out List<CadPoint>? updatedRoute))
        {
            StatusText = $"Wire vertex {vertexIndex} cannot be deleted because the route would become invalid.";
            return false;
        }

        SchematicWire updated = SelectedWire with { RoutePoints = updatedRoute };
        Wires[wireIndex] = updated;
        SelectedWire = updated;
        SelectedWireVertexIndex = null;
        SelectedWireSegmentIndex = null;
        RebuildNets();
        SelectedWire = Wires.Single(wire => wire.WireId == updated.WireId);
        StatusText = $"Deleted wire vertex {vertexIndex} on {SelectedWire.NetName}.";
        return true;
    }

    public SchematicWire RenameSelectedWireNet(string netName)
    {
        if (SelectedWire is null)
        {
            throw new InvalidOperationException("No schematic wire is selected.");
        }

        string normalizedNetName = netName.Trim();
        if (string.IsNullOrWhiteSpace(normalizedNetName))
        {
            throw new InvalidOperationException("Net name is required.");
        }

        int wireIndex = Wires.IndexOf(SelectedWire);
        if (wireIndex < 0)
        {
            throw new InvalidOperationException("The selected schematic wire is no longer in the document.");
        }

        SchematicWire renamed = SelectedWire with
        {
            NetName = normalizedNetName,
            ManualNetName = normalizedNetName
        };
        Wires[wireIndex] = renamed;
        SelectedWire = renamed;
        RebuildNets();
        SelectedWire = Wires.Single(wire => wire.WireId == renamed.WireId);
        StatusText = $"Renamed selected net to {SelectedWire.NetName}.";
        return SelectedWire;
    }

    public bool DeleteSelectedWire()
    {
        if (SelectedWire is null)
        {
            StatusText = "Select a wire before deleting it.";
            return false;
        }

        Wires.Remove(SelectedWire);
        SelectedWire = null;
        SelectedWireSegmentIndex = null;
        SelectedWireVertexIndex = null;
        RebuildNets();
        StatusText = "Deleted selected wire.";
        return true;
    }

    public bool DeleteSelectedWireObject()
    {
        if (SelectedWire is null)
        {
            StatusText = "Select a wire before deleting it.";
            return false;
        }

        if (SelectedWireVertexIndex is not null)
        {
            return DeleteSelectedWireVertex();
        }

        if (SelectedWireSegmentIndex is not null && SelectedWire.RoutePoints.Count > 2)
        {
            DeleteSelectedWireSegment();
            return true;
        }

        return DeleteSelectedWire();
    }

    public bool DeleteSelectedComponent()
    {
        if (SelectedComponent is null)
        {
            StatusText = "Select a schematic component before deleting it.";
            return false;
        }

        SchematicComponentInstance deleted = SelectedComponent;
        int attachedWireCount = Wires.Count(wire =>
            wire.Start.InstanceId == deleted.InstanceId ||
            wire.End.InstanceId == deleted.InstanceId);

        Components.Remove(deleted);
        for (int index = Wires.Count - 1; index >= 0; index--)
        {
            SchematicWire wire = Wires[index];
            if (wire.Start.InstanceId == deleted.InstanceId ||
                wire.End.InstanceId == deleted.InstanceId)
            {
                Wires.RemoveAt(index);
            }
        }

        SelectedComponent = null;
        SelectedWire = null;
        SelectedWireSegmentIndex = null;
        RebuildNets();
        string wireText = $"{attachedWireCount} attached wire{(attachedWireCount == 1 ? "" : "s")}";
        StatusText = $"Deleted {deleted.ReferenceDesignator} and {wireText}.";
        return true;
    }

    private static bool Contains(SchematicComponentInstance instance, CadPoint point)
    {
        CadRectangle placedBounds = RotatedBounds(instance);
        return placedBounds.Contains(point);
    }

    private SchematicPinEndpoint? FindPinAt(CadPoint point)
    {
        SchematicPinEndpoint? nearest = null;
        double nearestDistance = double.MaxValue;
        for (int componentIndex = Components.Count - 1; componentIndex >= 0; componentIndex--)
        {
            SchematicComponentInstance instance = Components[componentIndex];
            bool pointInsideComponentBody = Contains(instance, point);
            foreach (ComponentSymbolPinPreview pin in instance.SymbolPreview.Pins)
            {
                CadPoint pinPosition = TransformLocalPoint(instance, pin.ConnectPoint);
                CadPoint bodyPosition = TransformLocalPoint(instance, pin.BodyPoint);
                double endpointDistance = Distance(point, pinPosition);
                double leadDistance = pinPosition == bodyPosition
                    ? endpointDistance
                    : DistanceToSegment(point, pinPosition, bodyPosition);
                if (endpointDistance <= PinEndpointHitTolerance ||
                    (!pointInsideComponentBody && leadDistance <= PinLeadHitTolerance))
                {
                    double distance = Math.Min(endpointDistance, leadDistance);
                    if (distance >= nearestDistance)
                    {
                        continue;
                    }

                    nearestDistance = distance;
                    nearest = CreatePinEndpoint(instance, pin, pinPosition, IsPinConnected(instance.InstanceId, pin.Name));
                }
            }
        }

        return nearest;
    }

    private void AddWire(SchematicPinEndpoint start, SchematicPinEndpoint end, IReadOnlyList<CadPoint> routePoints)
    {
        SchematicPinEndpoint connectedStart = start with { IsConnected = true };
        SchematicPinEndpoint connectedEnd = end with { IsConnected = true };
        Wires.Add(new SchematicWire(Guid.NewGuid().ToString("N"), connectedStart, connectedEnd, routePoints));
        RebuildNets();
        SchematicWire completedWire = Wires[Wires.Count - 1];
        StatusText = $"Connected {start.ReferenceDesignator}.{start.PinName} to {end.ReferenceDesignator}.{end.PinName}. Net {completedWire.NetName}.";
    }

    private void RefreshWireEndpointsForMovedComponent(SchematicComponentInstance moved)
    {
        for (int index = 0; index < Wires.Count; index++)
        {
            SchematicWire wire = Wires[index];
            SchematicWire updated = wire;
            List<CadPoint> routePoints = [..wire.RoutePoints];

            if (wire.Start.InstanceId == moved.InstanceId &&
                TryCreateEndpointForMovedPin(moved, wire.Start.PinName, out SchematicPinEndpoint start))
            {
                updated = updated with { Start = start };
                if (routePoints.Count > 0)
                {
                    routePoints[0] = start.Position;
                    RepairAdjacentEndpointSegment(routePoints, 0);
                }
            }

            if (wire.End.InstanceId == moved.InstanceId &&
                TryCreateEndpointForMovedPin(moved, wire.End.PinName, out SchematicPinEndpoint end))
            {
                updated = updated with { End = end };
                if (routePoints.Count > 0)
                {
                    routePoints[^1] = end.Position;
                    RepairAdjacentEndpointSegment(routePoints, routePoints.Count - 1);
                }
            }

            routePoints = OrthogonalizeRoute(routePoints);
            if (!ReferenceEquals(updated, wire) || !routePoints.SequenceEqual(wire.RoutePoints))
            {
                Wires[index] = updated with { RoutePoints = routePoints };
            }
        }

        RebuildNets();
    }

    private void RefreshWireEndpointReferenceDesignators(SchematicComponentInstance updatedComponent)
    {
        for (int index = 0; index < Wires.Count; index++)
        {
            SchematicWire wire = Wires[index];
            SchematicWire updated = wire;
            if (wire.Start.InstanceId == updatedComponent.InstanceId)
            {
                updated = updated with
                {
                    Start = wire.Start with { ReferenceDesignator = updatedComponent.ReferenceDesignator }
                };
            }

            if (wire.End.InstanceId == updatedComponent.InstanceId)
            {
                updated = updated with
                {
                    End = updated.End with { ReferenceDesignator = updatedComponent.ReferenceDesignator }
                };
            }

            if (!ReferenceEquals(updated, wire))
            {
                Wires[index] = updated;
            }
        }
    }

    private static bool TryCreateEndpointForMovedPin(
        SchematicComponentInstance moved,
        string pinName,
        out SchematicPinEndpoint endpoint)
    {
        ComponentSymbolPinPreview? pin = moved.SymbolPreview.Pins
            .FirstOrDefault(candidate => candidate.Name == pinName);
        if (pin is null)
        {
            endpoint = default!;
            return false;
        }

        endpoint = CreatePinEndpoint(moved, pin, TransformLocalPoint(moved, pin.ConnectPoint), isConnected: true);
        return true;
    }

    private static SchematicPinEndpoint CreatePinEndpoint(
        SchematicComponentInstance instance,
        ComponentSymbolPinPreview pin,
        CadPoint connectionPoint,
        bool isConnected) =>
        new(
            instance.InstanceId,
            instance.ReferenceDesignator,
            pin.Name,
            connectionPoint,
            PinNumberFor(pin),
            isConnected);

    private static string PinNumberFor(ComponentSymbolPinPreview pin) =>
        pin.Name;

    private bool IsPinConnected(string instanceId, string pinNumber) =>
        Wires.Any(wire =>
            IsSameEndpoint(wire.Start, instanceId, pinNumber) ||
            IsSameEndpoint(wire.End, instanceId, pinNumber));

    private string NetNameForEndpoint(SchematicPinEndpoint endpoint) =>
        Wires.FirstOrDefault(wire =>
            IsSameEndpoint(wire.Start, endpoint.InstanceId, endpoint.StablePinNumber) ||
            IsSameEndpoint(wire.End, endpoint.InstanceId, endpoint.StablePinNumber))?.NetName ?? "unassigned net";

    private static bool IsSameEndpoint(SchematicPinEndpoint endpoint, string instanceId, string pinNumber) =>
        endpoint.InstanceId == instanceId &&
        endpoint.StablePinNumber == (string.IsNullOrWhiteSpace(pinNumber) ? endpoint.PinName : pinNumber);

    public static CadPoint TransformLocalPoint(SchematicComponentInstance instance, CadPoint localPoint)
    {
        CadPoint mirrored = instance.IsMirrored
            ? new CadPoint(-localPoint.X, localPoint.Y)
            : localPoint;
        CadPoint rotated = RotateLocalPoint(mirrored, instance.RotationDegrees);
        return new CadPoint(instance.Position.X + rotated.X, instance.Position.Y + rotated.Y);
    }

    private static CadRectangle RotatedBounds(SchematicComponentInstance instance)
    {
        CadRectangle bounds = instance.SymbolPreview.Bounds;
        CadPoint[] corners =
        [
            TransformLocalPoint(instance, new CadPoint(bounds.Left, bounds.Top)),
            TransformLocalPoint(instance, new CadPoint(bounds.Right, bounds.Top)),
            TransformLocalPoint(instance, new CadPoint(bounds.Right, bounds.Bottom)),
            TransformLocalPoint(instance, new CadPoint(bounds.Left, bounds.Bottom))
        ];
        return new CadRectangle(
            corners.Min(point => point.X),
            corners.Min(point => point.Y),
            corners.Max(point => point.X),
            corners.Max(point => point.Y));
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

    private static List<CadPoint> NormalizeCompletedRoute(IReadOnlyList<CadPoint> currentRoutePoints, CadPoint endpoint)
    {
        List<CadPoint> route = [..currentRoutePoints];
        if (route.Count != 1)
        {
            AddOrthogonalLeg(route, endpoint);
            return route;
        }

        CadPoint start = route[0];
        long dx = Math.Abs(endpoint.X - start.X);
        long dy = Math.Abs(endpoint.Y - start.Y);
        if (Math.Max(dx, dy) >= 2_000_000)
        {
            AddOrthogonalLeg(route, endpoint);
            return route;
        }

        long doglegOffset = start.Y <= endpoint.Y ? -2_000_000 : 2_000_000;
        route.Add(new CadPoint(start.X, start.Y + doglegOffset));
        route.Add(new CadPoint(endpoint.X, start.Y + doglegOffset));
        route.Add(endpoint);
        return route;
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

    private static void AddOrthogonalLegAvoidingDeletedSegment(
        List<CadPoint> route,
        CadPoint target,
        CadPoint deletedStart,
        CadPoint deletedEnd)
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
            CadPoint horizontalThenVerticalCorner = new(target.X, last.Y);
            CadPoint verticalThenHorizontalCorner = new(last.X, target.Y);
            CadPoint corner = horizontalThenVerticalCorner == deletedStart ||
                horizontalThenVerticalCorner == deletedEnd
                    ? verticalThenHorizontalCorner
                    : horizontalThenVerticalCorner;

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

    private static void AddInsertedOrthogonalVertex(
        List<CadPoint> route,
        CadPoint segmentStart,
        CadPoint segmentEnd,
        CadPoint insertedVertex)
    {
        if (segmentStart.Y == segmentEnd.Y)
        {
            AddRoutePoint(route, new CadPoint(insertedVertex.X, segmentStart.Y));
            AddRoutePoint(route, insertedVertex);
            AddRoutePoint(route, new CadPoint(segmentEnd.X, insertedVertex.Y));
            AddRoutePoint(route, segmentEnd);
            return;
        }

        AddRoutePoint(route, new CadPoint(segmentStart.X, insertedVertex.Y));
        AddRoutePoint(route, insertedVertex);
        AddRoutePoint(route, new CadPoint(insertedVertex.X, segmentEnd.Y));
        AddRoutePoint(route, segmentEnd);
    }

    private static void AddRoutePoint(List<CadPoint> route, CadPoint point)
    {
        if (route.Count == 0 || route[^1] != point)
        {
            route.Add(point);
        }
    }

    private static List<CadPoint> CompactRoute(IReadOnlyList<CadPoint> routePoints)
    {
        List<CadPoint> compacted = [];
        foreach (CadPoint point in routePoints)
        {
            if (compacted.Count == 0 || compacted[^1] != point)
            {
                compacted.Add(point);
            }
        }

        bool removedPoint;
        do
        {
            removedPoint = false;
            for (int index = 1; index < compacted.Count - 1; index++)
            {
                CadPoint previous = compacted[index - 1];
                CadPoint current = compacted[index];
                CadPoint next = compacted[index + 1];
                if ((previous.X == current.X && current.X == next.X) ||
                    (previous.Y == current.Y && current.Y == next.Y))
                {
                    compacted.RemoveAt(index);
                    removedPoint = true;
                    break;
                }
            }

            for (int index = 1; index < compacted.Count; index++)
            {
                if (compacted[index - 1] == compacted[index])
                {
                    compacted.RemoveAt(index);
                    removedPoint = true;
                    break;
                }
            }
        }
        while (removedPoint);

        return compacted;
    }

    private static bool TryBuildRouteWithoutVertex(
        IReadOnlyList<CadPoint> routePoints,
        int vertexIndex,
        [NotNullWhen(true)] out List<CadPoint>? updatedRoute)
    {
        updatedRoute = null;
        if (vertexIndex <= 0 || vertexIndex >= routePoints.Count - 1)
        {
            return false;
        }

        CadPoint previous = routePoints[vertexIndex - 1];
        CadPoint current = routePoints[vertexIndex];
        CadPoint next = routePoints[vertexIndex + 1];
        CadPoint adjustedNext = next;
        if (previous.X != next.X && previous.Y != next.Y)
        {
            if (vertexIndex + 1 == routePoints.Count - 1)
            {
                return false;
            }

            if (previous.X == current.X && current.Y == next.Y)
            {
                adjustedNext = new CadPoint(next.X, previous.Y);
            }
            else if (previous.Y == current.Y && current.X == next.X)
            {
                adjustedNext = new CadPoint(previous.X, next.Y);
            }
            else
            {
                return false;
            }
        }

        updatedRoute = [];
        for (int index = 0; index < routePoints.Count; index++)
        {
            if (index == vertexIndex)
            {
                continue;
            }

            CadPoint point = index == vertexIndex + 1 ? adjustedNext : routePoints[index];
            if (updatedRoute.Count == 0 || updatedRoute[^1] != point)
            {
                updatedRoute.Add(point);
            }
        }

        return updatedRoute.Count >= 2 &&
            updatedRoute[0] == routePoints[0] &&
            updatedRoute[^1] == routePoints[^1] &&
            updatedRoute.Zip(updatedRoute.Skip(1)).All(pair => pair.First.X == pair.Second.X || pair.First.Y == pair.Second.Y);
    }

    private static bool IsEndpointVertexIndex(IReadOnlyList<CadPoint> routePoints, int vertexIndex) =>
        vertexIndex == 0 || vertexIndex == routePoints.Count - 1;

    private static void RepairAdjacentEndpointSegment(List<CadPoint> routePoints, int endpointIndex)
    {
        if (routePoints.Count < 2)
        {
            return;
        }

        if (endpointIndex == 0)
        {
            CadPoint endpoint = routePoints[0];
            CadPoint adjacent = routePoints[1];
            routePoints[1] = Math.Abs(adjacent.X - endpoint.X) <= Math.Abs(adjacent.Y - endpoint.Y)
                ? new CadPoint(endpoint.X, adjacent.Y)
                : new CadPoint(adjacent.X, endpoint.Y);
            return;
        }

        CadPoint previous = routePoints[endpointIndex - 1];
        CadPoint end = routePoints[endpointIndex];
        routePoints[endpointIndex - 1] = Math.Abs(previous.X - end.X) <= Math.Abs(previous.Y - end.Y)
            ? new CadPoint(end.X, previous.Y)
            : new CadPoint(previous.X, end.Y);
    }

    private static List<CadPoint> OrthogonalizeRoute(IReadOnlyList<CadPoint> routePoints)
    {
        if (routePoints.Count < 2)
        {
            return [..routePoints];
        }

        List<CadPoint> orthogonal = [routePoints[0]];
        for (int index = 1; index < routePoints.Count; index++)
        {
            AddOrthogonalLeg(orthogonal, routePoints[index]);
        }

        return orthogonal;
    }

    private void RebuildNets()
    {
        Nets.Clear();
        NetLabelDiagnostics.Clear();
        if (Wires.Count == 0)
        {
            return;
        }

        Dictionary<string, SchematicPinEndpoint> endpointsByKey = [];
        Dictionary<string, List<string>> adjacency = [];
        foreach (SchematicWire wire in Wires)
        {
            string startKey = PinKey(wire.Start);
            string endKey = PinKey(wire.End);
            endpointsByKey[startKey] = wire.Start;
            endpointsByKey[endKey] = wire.End;
            AddEdge(adjacency, startKey, endKey);
            AddEdge(adjacency, endKey, startKey);
        }

        Dictionary<string, string> netByPinKey = [];
        HashSet<string> visited = [];
        int netNumber = 1;
        foreach (string pinKey in endpointsByKey.Keys.Order(StringComparer.Ordinal))
        {
            if (!visited.Add(pinKey))
            {
                continue;
            }

            List<string> componentPins = [];
            Queue<string> queue = new([pinKey]);
            while (queue.Count > 0)
            {
                string current = queue.Dequeue();
                componentPins.Add(current);
                foreach (string next in adjacency[current])
                {
                    if (visited.Add(next))
                    {
                        queue.Enqueue(next);
                    }
                }
            }

            string[] wireIds = Wires
                .Where(wire => componentPins.Contains(PinKey(wire.Start), StringComparer.Ordinal) ||
                               componentPins.Contains(PinKey(wire.End), StringComparer.Ordinal))
                .Select(wire => wire.WireId)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            string? labelNetName = NetLabels
                .Where(label => wireIds.Contains(label.AssociatedWireId, StringComparer.Ordinal))
                .Select(label => label.NetName)
                .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name));
            string netName = labelNetName ??
                Wires
                    .Where(wire => wireIds.Contains(wire.WireId, StringComparer.Ordinal))
                    .Select(EffectiveManualNetName)
                    .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name)) ??
                $"N${netNumber++}";
            foreach (string componentPin in componentPins)
            {
                netByPinKey[componentPin] = netName;
            }

            string[] pinNames = componentPins
                .Select(pin => EndpointLabel(endpointsByKey[pin]))
                .Order(StringComparer.Ordinal)
                .ToArray();
            Nets.Add(new SchematicNet(netName, pinNames, wireIds));
        }

        for (int index = 0; index < Wires.Count; index++)
        {
            SchematicWire wire = Wires[index];
            string? labelNetName = NetLabels
                .Where(label => label.AssociatedWireId == wire.WireId)
                .Select(label => label.NetName)
                .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name));
            Wires[index] = wire with
            {
                NetName = netByPinKey[PinKey(wire.Start)],
                ManualNetName = labelNetName ?? (string.IsNullOrWhiteSpace(wire.LabelNetName) ? wire.ManualNetName : ""),
                LabelNetName = labelNetName ?? ""
            };
        }

        RebuildNetLabelDiagnostics();
    }

    private SchematicComponentInstance RequireSelectedComponentInDocument(out int index)
    {
        SchematicComponentInstance? selected = SelectedComponent;
        if (selected is null && SelectedComponentTextLabel is not null)
        {
            selected = Components.FirstOrDefault(component => component.InstanceId == SelectedComponentTextLabel.InstanceId);
        }

        if (selected is null)
        {
            throw new InvalidOperationException("No schematic component is selected.");
        }

        index = Components.IndexOf(selected);
        if (index < 0)
        {
            throw new InvalidOperationException("The selected schematic component is no longer in the document.");
        }

        return selected;
    }

    private SchematicComponentTextLabel MoveSelectedComponentTextTo(
        SchematicComponentTextKind kind,
        CadPoint requestedPosition)
    {
        SchematicComponentInstance selected = RequireSelectedComponentInDocument(out int index);
        CadPoint snapped = placementGrid.Snap(requestedPosition);
        SchematicComponentInstance updated = kind == SchematicComponentTextKind.Name
            ? selected with { NameTextPosition = snapped }
            : selected with { ValueTextPosition = snapped };

        ReplaceSelectedComponent(index, updated);
        SchematicComponentTextLabel label = ComponentTextLabelFor(updated, kind);
        SelectedComponentTextLabel = label;
        IsDirty = true;
        StatusText = $"Moved {updated.ReferenceDesignator} {TextKindLabel(kind)} text to {FormatMillimeters(snapped.X)} mm, {FormatMillimeters(snapped.Y)} mm.";
        return label;
    }

    private void ReplaceSelectedComponent(int index, SchematicComponentInstance updated)
    {
        Components[index] = updated;
        SelectedComponent = updated;
        OnPropertyChanged(nameof(RenderableComponentTextLabels));
        OnPropertyChanged(nameof(SelectionSummary));
        OnPropertyChanged(nameof(SelectedComponentMetadata));
    }

    private static IEnumerable<SchematicComponentTextLabel> ComponentTextLabelsFor(SchematicComponentInstance component)
    {
        yield return ComponentTextLabelFor(component, SchematicComponentTextKind.Name);
        if (!string.IsNullOrWhiteSpace(component.Value))
        {
            yield return ComponentTextLabelFor(component, SchematicComponentTextKind.Value);
        }
    }

    private static SchematicComponentTextLabel ComponentTextLabelFor(
        SchematicComponentInstance component,
        SchematicComponentTextKind kind) =>
        new(
            component.InstanceId,
            component.ReferenceDesignator,
            kind,
            kind == SchematicComponentTextKind.Name ? component.ReferenceDesignator : component.Value,
            kind == SchematicComponentTextKind.Name
                ? component.NameTextPositionOrDefault
                : component.ValueTextPositionOrDefault);

    private SchematicComponentTextLabel? FindComponentTextLabelAt(CadPoint point)
    {
        SchematicComponentTextLabel? nearest = null;
        long nearestDistanceSquared = long.MaxValue;
        for (int componentIndex = Components.Count - 1; componentIndex >= 0; componentIndex--)
        {
            SchematicComponentInstance component = Components[componentIndex];
            foreach (SchematicComponentTextLabel label in ComponentTextLabelsFor(component))
            {
                if (Math.Abs(label.Position.X - point.X) > ComponentTextHitTolerance ||
                    Math.Abs(label.Position.Y - point.Y) > ComponentTextHitTolerance)
                {
                    continue;
                }

                long dx = label.Position.X - point.X;
                long dy = label.Position.Y - point.Y;
                long distanceSquared = (dx * dx) + (dy * dy);
                if (distanceSquared >= nearestDistanceSquared)
                {
                    continue;
                }

                nearestDistanceSquared = distanceSquared;
                nearest = label;
            }
        }

        return nearest;
    }

    private static string TextKindLabel(SchematicComponentTextKind kind) =>
        kind == SchematicComponentTextKind.Name ? "name" : "value";

    private void RebuildNetLabelDiagnostics()
    {
        IEnumerable<IGrouping<string, SchematicNetLabel>> duplicateGroups = NetLabels
            .Where(label => !string.IsNullOrWhiteSpace(label.AssociatedWireId))
            .GroupBy(label => label.NetName, StringComparer.OrdinalIgnoreCase)
            .Where(group => group
                .Select(NetIdentityForLabel)
                .Where(identity => !string.IsNullOrWhiteSpace(identity))
                .Distinct(StringComparer.Ordinal)
                .Count() > 1);

        foreach (IGrouping<string, SchematicNetLabel> group in duplicateGroups)
        {
            string netName = group.First().NetName;
            NetLabelDiagnostics.Add(new SchematicNetLabelDiagnostic(
                "DragonCAD.Schematic.DuplicateNetLabel",
                netName,
                $"Net label {netName} appears on disconnected nets.",
                group.Select(label => label.LabelId).ToArray()));
        }
    }

    private string NetIdentityForLabel(SchematicNetLabel label)
    {
        return Nets.FirstOrDefault(net => net.WireIds.Contains(label.AssociatedWireId, StringComparer.Ordinal)) is { } net
            ? string.Join("|", net.WireIds.Order(StringComparer.Ordinal))
            : "";
    }

    private static string EffectiveManualNetName(SchematicWire wire) =>
        !string.IsNullOrWhiteSpace(wire.LabelNetName) && wire.ManualNetName == wire.LabelNetName
            ? ""
            : wire.ManualNetName;

    private static string ActivePackageLabelFor(SchematicComponentInstance component) =>
        string.IsNullOrWhiteSpace(component.ActivePackageLabel)
            ? "No package"
            : component.ActivePackageLabel;

    private static void AddEdge(Dictionary<string, List<string>> adjacency, string start, string end)
    {
        if (!adjacency.TryGetValue(start, out List<string>? connected))
        {
            connected = [];
            adjacency[start] = connected;
        }

        connected.Add(end);
    }

    private static string PinKey(SchematicPinEndpoint endpoint) =>
        $"{endpoint.InstanceId}:{endpoint.StablePinNumber}";

    private static string EndpointLabel(SchematicPinEndpoint endpoint) =>
        $"{endpoint.ReferenceDesignator}.{endpoint.PinName}";

    private string FindAttachedWireId(CadPoint point)
    {
        double nearestDistance = double.MaxValue;
        string attachedWireId = "";
        for (int wireIndex = Wires.Count - 1; wireIndex >= 0; wireIndex--)
        {
            SchematicWire wire = Wires[wireIndex];
            if (NearestSegmentIndex(point, wire.RoutePoints, WireSegmentHitTolerance, out double distance) is not null &&
                distance < nearestDistance)
            {
                nearestDistance = distance;
                attachedWireId = wire.WireId;
            }
        }

        return attachedWireId;
    }

    private SchematicNetLabel? FindNetLabelAt(CadPoint point)
    {
        const long tolerance = 500_000;
        SchematicNetLabel? nearest = null;
        long nearestDistanceSquared = long.MaxValue;
        for (int index = NetLabels.Count - 1; index >= 0; index--)
        {
            SchematicNetLabel label = NetLabels[index];
            if (Math.Abs(label.Position.X - point.X) > tolerance ||
                Math.Abs(label.Position.Y - point.Y) > tolerance)
            {
                continue;
            }

            long dx = label.Position.X - point.X;
            long dy = label.Position.Y - point.Y;
            long distanceSquared = (dx * dx) + (dy * dy);
            if (distanceSquared >= nearestDistanceSquared)
            {
                continue;
            }

            nearestDistanceSquared = distanceSquared;
            nearest = label;
        }

        return nearest;
    }

    private static int? NearestSegmentIndex(CadPoint point, IReadOnlyList<CadPoint> routePoints, double tolerance, out double nearestDistance)
    {
        nearestDistance = double.MaxValue;
        if (routePoints.Count < 2)
        {
            return null;
        }

        int? nearestIndex = null;
        for (int index = 1; index < routePoints.Count; index++)
        {
            double distance = DistanceToSegment(point, routePoints[index - 1], routePoints[index]);
            if (distance <= tolerance && distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestIndex = index;
            }
        }

        return nearestIndex;
    }

    private static double DistanceToSegment(CadPoint point, CadPoint start, CadPoint end)
    {
        double dx = end.X - start.X;
        double dy = end.Y - start.Y;
        if (Math.Abs(dx) < double.Epsilon && Math.Abs(dy) < double.Epsilon)
        {
            return Distance(point, start);
        }

        double t = ((point.X - start.X) * dx + (point.Y - start.Y) * dy) / ((dx * dx) + (dy * dy));
        t = Math.Clamp(t, 0, 1);
        double closestX = start.X + (t * dx);
        double closestY = start.Y + (t * dy);
        return Math.Sqrt(Math.Pow(point.X - closestX, 2) + Math.Pow(point.Y - closestY, 2));
    }

    private static double Distance(CadPoint first, CadPoint second) =>
        Math.Sqrt(Math.Pow(first.X - second.X, 2) + Math.Pow(first.Y - second.Y, 2));

    private void SetPendingRoutePoints(IReadOnlyList<CadPoint> routePoints)
    {
        pendingWireRoutePoints.Clear();
        pendingWireRoutePoints.AddRange(routePoints);
        OnPropertyChanged(nameof(PendingWireRoutePoints));
        OnPropertyChanged(nameof(PendingWirePreviewRoutePoints));
    }

    private void ClearPendingRoutePoints()
    {
        pendingWireRoutePoints.Clear();
        OnPropertyChanged(nameof(PendingWireRoutePoints));
        OnPropertyChanged(nameof(PendingWirePreviewRoutePoints));
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

    private static ComponentSymbolPreview NormalizeSymbolPreview(ComponentSymbolPreview? preview)
    {
        if (preview is not null && (preview.Lines.Count > 0 || preview.Pins.Count > 0))
        {
            return preview;
        }

        CadRectangle bounds = new(-2_000_000, -1_000_000, 2_000_000, 1_000_000);
        return new ComponentSymbolPreview(
            bounds,
            [
                new ComponentPreviewLine(new CadPoint(bounds.Left, bounds.Top), new CadPoint(bounds.Right, bounds.Top)),
                new ComponentPreviewLine(new CadPoint(bounds.Right, bounds.Top), new CadPoint(bounds.Right, bounds.Bottom)),
                new ComponentPreviewLine(new CadPoint(bounds.Right, bounds.Bottom), new CadPoint(bounds.Left, bounds.Bottom)),
                new ComponentPreviewLine(new CadPoint(bounds.Left, bounds.Bottom), new CadPoint(bounds.Left, bounds.Top))
            ],
            []);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
