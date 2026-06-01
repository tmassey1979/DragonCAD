namespace DragonCAD.App.BoardEditor;

using DragonCAD.Core.Geometry;

public sealed record BoardAirwire(
    string NetName,
    string StartSyncId,
    string StartReferenceDesignator,
    string StartPinName,
    CadPoint StartPosition,
    string EndSyncId,
    string EndReferenceDesignator,
    string EndPinName,
    CadPoint EndPosition);
