using DragonCAD.Core.Geometry;

namespace DragonCAD.App.SchematicEditor;

public interface ISchematicPlacementTarget
{
    void HandleSchematicCanvasClick(CadPoint point);

    void HandleSchematicPointerPressed(CadPoint point);

    void HandleSchematicPointerMoved(CadPoint point);

    void HandleSchematicPointerReleased(CadPoint point);

    bool IsDraggingSchematicComponent { get; }

    bool IsDraggingSchematicWireSegment { get; }
}
