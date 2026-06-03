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
    bool IsMirrored = false,
    IReadOnlyList<BoardFootprintPrimitive>? FootprintPrimitives = null)
{
    public IReadOnlyList<BoardFootprintPrimitive> FootprintPrimitives { get; init; } =
        FootprintPrimitives ?? BoardFootprintPrimitive.FromPreview(FootprintPreview);

    public CadRectangle FootprintBounds => MergeBounds(FootprintPreview.Bounds, BoardFootprintGeometry.CalculateBounds(FootprintPrimitives));

    public BoardComponentInstance(
        string syncId,
        string referenceDesignator,
        string componentId,
        string displayName,
        CadPoint position = default)
        : this(syncId, referenceDesignator, componentId, displayName, position, ComponentFootprintPreview.Empty)
    {
    }

    private static CadRectangle MergeBounds(CadRectangle previewBounds, CadRectangle primitiveBounds)
    {
        bool hasPreviewBounds = previewBounds.Width > 0 || previewBounds.Height > 0;
        bool hasPrimitiveBounds = primitiveBounds.Width > 0 || primitiveBounds.Height > 0;
        if (!hasPreviewBounds)
        {
            return primitiveBounds;
        }

        if (!hasPrimitiveBounds)
        {
            return previewBounds;
        }

        return new CadRectangle(
            Math.Min(previewBounds.Left, primitiveBounds.Left),
            Math.Min(previewBounds.Top, primitiveBounds.Top),
            Math.Max(previewBounds.Right, primitiveBounds.Right),
            Math.Max(previewBounds.Bottom, primitiveBounds.Bottom));
    }
}
