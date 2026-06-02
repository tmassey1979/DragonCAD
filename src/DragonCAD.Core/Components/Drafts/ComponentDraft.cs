using DragonCAD.Core.Components.Identity;
using DragonCAD.Core.Geometry;

namespace DragonCAD.Core.Components.Drafts;

public sealed record ComponentDraft(
    ComponentId Id,
    string DisplayName,
    ComponentDraftPackage Package,
    IReadOnlyList<ComponentDraftAttribute> Attributes,
    IReadOnlyList<ComponentDraftPin> Pins,
    IReadOnlyList<ComponentDraftSymbol> Symbols,
    IReadOnlyList<ComponentDraftFootprint> Footprints,
    IReadOnlyList<ComponentDraftDeviceMapping> DeviceMappings);

public sealed record ComponentDraftPackage(
    string Name,
    string ReferencePrefix,
    IReadOnlyList<ComponentDraftAttribute> Metadata);

public sealed record ComponentDraftAttribute(string Name, string Value);

public sealed record ComponentDraftPin(
    ComponentPinId Id,
    string Name,
    string Number,
    ComponentDraftPinElectricalType ElectricalType);

public enum ComponentDraftPinElectricalType
{
    Passive,
    Input,
    Output,
    Bidirectional,
    Power,
    NoConnect
}

public sealed record ComponentDraftSymbol(
    ComponentSymbolId Id,
    string Name,
    IReadOnlyList<ComponentDraftSymbolPin> Pins,
    IReadOnlyList<ComponentDraftSymbolPrimitive> Primitives);

public sealed record ComponentDraftSymbolPin(
    ComponentPinId PinId,
    CadPoint Start,
    CadPoint End,
    ComponentDraftPinOrientation Orientation);

public enum ComponentDraftPinOrientation
{
    Left,
    Right,
    Up,
    Down
}

public sealed record ComponentDraftSymbolPrimitive(
    ComponentDraftPrimitiveKind Kind,
    CadPoint Start,
    CadPoint End);

public enum ComponentDraftPrimitiveKind
{
    Line,
    Rectangle,
    Circle,
    Arc,
    Text
}

public sealed record ComponentDraftFootprint(
    ComponentFootprintId Id,
    string Name,
    IReadOnlyList<ComponentDraftPad> Pads,
    IReadOnlyList<ComponentDraftFootprintPrimitive> Silkscreen,
    IReadOnlyList<ComponentDraftFootprintPrimitive> Courtyard);

public sealed record ComponentDraftPad(
    ComponentPadId Id,
    string Name,
    CadPoint Position,
    CadVector Size,
    ComponentDraftPadTechnology Technology,
    ComponentDraftPadShape Shape,
    long? DrillSize = null);

public enum ComponentDraftPadTechnology
{
    ThroughHole,
    SurfaceMount
}

public enum ComponentDraftPadShape
{
    Round,
    Rectangle,
    RoundedRectangle,
    Oval
}

public sealed record ComponentDraftFootprintPrimitive(
    ComponentDraftPrimitiveKind Kind,
    CadPoint Start,
    CadPoint End);

public sealed record ComponentDraftDeviceMapping(
    ComponentPinId PinId,
    ComponentFootprintId FootprintId,
    ComponentPadId PadId);
