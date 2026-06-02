namespace DragonCAD.Fabrication.Outputs.Gerber;

public sealed record GerberDrillManifestRequest(
    string ProjectName,
    string BoardName,
    string Revision,
    IReadOnlyList<GerberBoardLayer> Layers,
    int ViaCount,
    int ThroughHolePadCount);
