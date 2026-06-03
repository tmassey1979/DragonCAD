namespace DragonCAD.App.Datasheets.Extraction;

public interface IDatasheetExtractionProvider
{
    string ProviderId { get; }

    string DisplayName { get; }

    DatasheetExtractionCapabilitySet Capabilities { get; }

    Task<DatasheetExtractionResult> ExtractAsync(
        DatasheetExtractionRequest request,
        CancellationToken cancellationToken = default);
}
