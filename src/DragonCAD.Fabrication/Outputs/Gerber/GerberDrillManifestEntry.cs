namespace DragonCAD.Fabrication.Outputs.Gerber;

public sealed record GerberDrillManifestEntry(
    ManufacturingFileRole Role,
    ManufacturingRelativePath RelativePath,
    ManufacturingChecksum Checksum,
    string OutputName,
    string? SourceLayerName,
    GerberBoardLayerKind? LayerKind,
    GerberBoardSide? Side,
    IReadOnlyDictionary<string, string> Metadata);
