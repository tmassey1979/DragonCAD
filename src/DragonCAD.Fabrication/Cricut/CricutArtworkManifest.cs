namespace DragonCAD.Fabrication.Cricut;

public sealed record CricutArtworkManifest(
    string ProjectName,
    IReadOnlyList<CricutArtworkManifestEntry> Entries);
