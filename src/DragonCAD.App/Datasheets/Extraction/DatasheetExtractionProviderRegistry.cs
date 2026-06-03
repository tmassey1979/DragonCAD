namespace DragonCAD.App.Datasheets.Extraction;

public sealed class DatasheetExtractionProviderRegistry
{
    private readonly IReadOnlyDictionary<string, IDatasheetExtractionProvider> _providersById;

    public DatasheetExtractionProviderRegistry(IEnumerable<IDatasheetExtractionProvider> providers)
    {
        _providersById = providers.ToDictionary(provider => provider.ProviderId, StringComparer.Ordinal);
    }

    public static DatasheetExtractionProviderRegistry Default { get; } = new([]);

    public IReadOnlyCollection<IDatasheetExtractionProvider> EnabledProviders => _providersById.Values.ToArray();

    public bool TryGetProvider(string providerId, out IDatasheetExtractionProvider provider) =>
        _providersById.TryGetValue(providerId, out provider!);
}
