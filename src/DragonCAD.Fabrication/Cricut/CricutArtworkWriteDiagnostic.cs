namespace DragonCAD.Fabrication.Cricut;

public sealed record CricutArtworkWriteDiagnostic(
    string ProjectName,
    string? OutputFileName,
    string Code,
    string Message,
    string? SourceLayerName);
