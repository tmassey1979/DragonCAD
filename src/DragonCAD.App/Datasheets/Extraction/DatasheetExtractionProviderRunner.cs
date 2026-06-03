namespace DragonCAD.App.Datasheets.Extraction;

public sealed class DatasheetExtractionProviderRunner
{
    private readonly DatasheetExtractionProviderRegistry _registry;

    public DatasheetExtractionProviderRunner(DatasheetExtractionProviderRegistry registry)
    {
        _registry = registry;
    }

    public async Task<DatasheetExtractionResult> ExtractAsync(
        string providerId,
        DatasheetExtractionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_registry.TryGetProvider(providerId, out IDatasheetExtractionProvider provider))
        {
            return DatasheetExtractionResult.Disabled(providerId, request.RequestedCapabilities);
        }

        DatasheetExtractionResult providerResult = await provider
            .ExtractAsync(request, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<DatasheetUnsupportedFeatureWarning> unsupportedFeatures = provider.Capabilities
            .UnsupportedFrom(request.RequestedCapabilities)
            .Select(capability => new DatasheetUnsupportedFeatureWarning(
                capability,
                $"Provider '{provider.ProviderId}' does not support {CapabilityDisplayName(capability)}."))
            .ToArray();

        return providerResult.WithUnsupportedFeatures(unsupportedFeatures);
    }

    private static string CapabilityDisplayName(DatasheetExtractionCapability capability) =>
        capability switch
        {
            DatasheetExtractionCapability.PinExtraction => "pin extraction",
            DatasheetExtractionCapability.PackageFootprintExtraction => "package/footprint extraction",
            DatasheetExtractionCapability.ComponentFactsExtraction => "component facts extraction",
            DatasheetExtractionCapability.ThreeDimensionalModelProposal => "3D model proposal",
            _ => capability.ToString()
        };
}
