using DragonCAD.Core.Geometry;

namespace DragonCAD.App.BoardEditor;

public sealed record BoardVia(
    string ViaId,
    CadPoint Position,
    string FromLayerName,
    string ToLayerName,
    long DiameterInternal = 800_000,
    long DrillInternal = 350_000);
