namespace DragonCAD.Fabrication.Cricut;

public sealed record CricutArtworkWriteResult(
    IReadOnlyList<CricutArtworkSvgFile> Files,
    IReadOnlyList<CricutArtworkWriteDiagnostic> Diagnostics);
