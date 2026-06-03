using DragonCAD.Core.Geometry;

namespace DragonCAD.App.BoardEditor;

public sealed record BoardTrace(
    string TraceId,
    string LayerName,
    IReadOnlyList<CadPoint> RoutePoints,
    long WidthInternal = 250_000,
    string? StartPadSyncId = null,
    string? StartPadReferenceDesignator = null,
    string? StartPadName = null,
    string? EndPadSyncId = null,
    string? EndPadReferenceDesignator = null,
    string? EndPadName = null);
