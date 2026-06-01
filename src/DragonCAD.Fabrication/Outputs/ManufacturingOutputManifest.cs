namespace DragonCAD.Fabrication.Outputs;

public sealed record ManufacturingOutputManifest
{
    private ManufacturingOutputManifest(ManufacturingOutputEntry[] entries)
    {
        Entries = entries;
    }

    public IReadOnlyList<ManufacturingOutputEntry> Entries { get; }

    public static ManufacturingOutputManifest Create(IEnumerable<ManufacturingOutputEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        ManufacturingOutputEntry[] sortedEntries = entries
            .OrderBy(entry => entry.Role)
            .ThenBy(entry => entry.RelativePath.Value, StringComparer.Ordinal)
            .ToArray();

        return new ManufacturingOutputManifest(sortedEntries);
    }
}
