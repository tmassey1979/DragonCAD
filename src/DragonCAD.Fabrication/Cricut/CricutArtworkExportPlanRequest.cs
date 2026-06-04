namespace DragonCAD.Fabrication.Cricut;

public sealed record CricutArtworkExportPlanRequest(
    string ProjectName,
    IReadOnlyList<CricutArtworkSourceLayer> SourceLayers,
    CricutArtworkUnits Units,
    decimal Scale,
    bool IncludeCopperVinyl,
    bool IncludeSolderPaste,
    bool IncludeRegistrationMarks);
