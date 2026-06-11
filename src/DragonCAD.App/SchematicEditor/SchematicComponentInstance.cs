using DragonCAD.App.ComponentManager;
using DragonCAD.Core.Geometry;

namespace DragonCAD.App.SchematicEditor;

public sealed record SchematicComponentInstance(
    string InstanceId,
    string ReferenceDesignator,
    string ComponentId,
    string DisplayName,
    CadPoint Position,
    ComponentSymbolPreview SymbolPreview,
    ComponentFootprintPreview FootprintPreview,
    string Value = "",
    int RotationDegrees = 0,
    bool IsMirrored = false,
    SchematicSymbolRenderPreview? SymbolRenderPreview = null,
    IReadOnlyDictionary<string, string>? Attributes = null,
    string ActivePackageVariantId = "",
    string ActivePackageFootprintId = "",
    string ActivePackageLabel = "No package",
    string PhysicalComponentId = "",
    string UnitId = "",
    string UnitName = "",
    bool IsRequiredUnit = true,
    bool CanPlaceUnitMultiple = false,
    CadPoint? NameTextPosition = null,
    CadPoint? ValueTextPosition = null)
{
    public CadPoint NameTextPositionOrDefault =>
        NameTextPosition ?? new CadPoint(Position.X, Position.Y - 6_500_000);

    public CadPoint ValueTextPositionOrDefault =>
        ValueTextPosition ?? new CadPoint(Position.X, Position.Y + 7_200_000);

    public SchematicComponentInstance(
        string instanceId,
        string referenceDesignator,
        string componentId,
        string displayName,
        CadPoint position,
        ComponentSymbolPreview symbolPreview)
        : this(instanceId, referenceDesignator, componentId, displayName, position, symbolPreview, ComponentFootprintPreview.Empty)
    {
    }
}

public enum SchematicComponentTextKind
{
    Name,
    Value
}

public sealed record SchematicComponentTextLabel(
    string InstanceId,
    string ReferenceDesignator,
    SchematicComponentTextKind Kind,
    string Text,
    CadPoint Position);

public sealed record SchematicSelectedComponentMetadata(
    string ReferenceDesignator,
    string DisplayName,
    string Value,
    IReadOnlyDictionary<string, string> Attributes,
    string ActivePackageVariantId,
    string ActivePackageFootprintId,
    string ActivePackageLabel);

public sealed record SchematicSelectedComponentMetadataDiagnostic(
    string Code,
    string Target,
    string Message);
