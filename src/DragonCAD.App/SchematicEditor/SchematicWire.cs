using DragonCAD.Core.Geometry;

namespace DragonCAD.App.SchematicEditor;

public sealed record SchematicPinEndpoint(
    string InstanceId,
    string ReferenceDesignator,
    string PinName,
    CadPoint Position);

public sealed record SchematicWire(
    string WireId,
    SchematicPinEndpoint Start,
    SchematicPinEndpoint End,
    IReadOnlyList<CadPoint> RoutePoints,
    string NetName = "",
    string ManualNetName = "",
    string LabelNetName = "");

public sealed record SchematicWireVertexHandle(
    string WireId,
    int VertexIndex,
    CadPoint Position,
    bool IsSelected,
    bool IsEndpoint = false);

public sealed record SchematicNet(
    string Name,
    IReadOnlyList<string> PinNames,
    IReadOnlyList<string> WireIds);

public sealed record SchematicNetLabel(
    string LabelId,
    string NetName,
    CadPoint Position,
    string AssociatedWireId = "");

public sealed record SchematicNetLabelDiagnostic(
    string Code,
    string NetName,
    string Message,
    IReadOnlyList<string> LabelIds);

public sealed record SchematicNetLabelRenderItem(
    string LabelId,
    string NetName,
    CadPoint Position,
    bool IsSelected);
