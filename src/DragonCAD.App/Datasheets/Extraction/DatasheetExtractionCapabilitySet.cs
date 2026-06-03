namespace DragonCAD.App.Datasheets.Extraction;

public sealed class DatasheetExtractionCapabilitySet
{
    private readonly HashSet<DatasheetExtractionCapability> _capabilities;

    public DatasheetExtractionCapabilitySet(IEnumerable<DatasheetExtractionCapability> capabilities)
    {
        _capabilities = [.. capabilities];
    }

    public static DatasheetExtractionCapabilitySet Empty { get; } = new([]);

    public IReadOnlyCollection<DatasheetExtractionCapability> Capabilities => _capabilities;

    public static DatasheetExtractionCapabilitySet From(params DatasheetExtractionCapability[] capabilities) =>
        new(capabilities);

    public bool Supports(DatasheetExtractionCapability capability) =>
        _capabilities.Contains(capability);

    public IReadOnlyList<DatasheetExtractionCapability> UnsupportedFrom(
        IEnumerable<DatasheetExtractionCapability> requestedCapabilities) =>
        requestedCapabilities
            .Where(capability => !_capabilities.Contains(capability))
            .Distinct()
            .ToArray();
}
