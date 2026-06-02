namespace DragonCAD.Fabrication.Outputs.Gerber;

public sealed record GerberDrillManifest(
    string ProjectName,
    string BoardName,
    string Revision,
    GerberDrillManifestMetadata Metadata,
    IReadOnlyList<GerberDrillManifestEntry> Entries)
{
    public ManufacturingOutputManifest ToManufacturingOutputManifest()
    {
        return ManufacturingOutputManifest.Create(
            Entries.Select(entry => new ManufacturingOutputEntry(entry.Role, entry.RelativePath, entry.Checksum)));
    }
}
