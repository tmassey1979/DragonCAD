namespace DragonCAD.Fabrication.Outputs.Gerber;

public sealed record GerberDrillManifestMetadata(
    int CopperLayerCount,
    int OutputFileCount,
    int ViaCount,
    int ThroughHolePadCount)
{
    public bool HasDrillData => ViaCount > 0 || ThroughHolePadCount > 0;
}
