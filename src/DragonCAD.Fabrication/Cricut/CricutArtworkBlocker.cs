namespace DragonCAD.Fabrication.Cricut;

public sealed record CricutArtworkBlocker(
    string Code,
    string Message,
    string? SourceLayerName);
