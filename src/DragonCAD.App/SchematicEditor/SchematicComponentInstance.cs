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
    SchematicSymbolRenderPreview? SymbolRenderPreview = null)
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
