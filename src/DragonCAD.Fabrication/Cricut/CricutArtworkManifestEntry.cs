namespace DragonCAD.Fabrication.Cricut;

public sealed record CricutArtworkManifestEntry(
    CricutArtworkOutputKind OutputKind,
    string? SourceLayerName,
    string OutputFileName,
    CricutArtworkUnits Units,
    decimal Scale,
    bool Mirror,
    IReadOnlyList<CricutArtworkBlocker> Blockers);
