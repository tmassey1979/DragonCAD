namespace DragonCAD.Fabrication.Cricut;

public sealed record CricutArtworkSourceLayer(
    string Name,
    CricutArtworkSourceLayerKind Kind,
    CricutArtworkBoardSide Side,
    bool HasGeometry);
