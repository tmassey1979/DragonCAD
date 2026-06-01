namespace DragonCAD.App.BoardEditor;

using DragonCAD.App.ComponentManager;
using DragonCAD.Core.Geometry;

public sealed record BoardComponentInstance(
    string SyncId,
    string ReferenceDesignator,
    string ComponentId,
    string DisplayName,
    CadPoint Position,
    ComponentFootprintPreview FootprintPreview,
    string Value = "",
    int RotationDegrees = 0,
    bool IsMirrored = false)
{
    public BoardComponentInstance(
        string syncId,
        string referenceDesignator,
        string componentId,
        string displayName,
        CadPoint position = default)
        : this(syncId, referenceDesignator, componentId, displayName, position, ComponentFootprintPreview.Empty)
    {
    }
}
