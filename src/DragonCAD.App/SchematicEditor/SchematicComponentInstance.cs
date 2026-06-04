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
    string ActivePackageLabel = "No package")
{
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
