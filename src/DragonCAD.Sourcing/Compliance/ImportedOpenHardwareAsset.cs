namespace DragonCAD.Sourcing.Compliance;

public sealed record ImportedOpenHardwareAsset
{
    public ImportedOpenHardwareAsset(
        string assetId,
        string providerId,
        string displayName,
        OpenHardwareAssetProvenance provenance)
    {
        AssetId = RequireText(assetId, nameof(assetId));
        ProviderId = RequireText(providerId, nameof(providerId));
        DisplayName = RequireText(displayName, nameof(displayName));
        Provenance = provenance ?? throw new ArgumentNullException(nameof(provenance));
    }

    public string AssetId { get; }

    public string ProviderId { get; }

    public string DisplayName { get; }

    public OpenHardwareAssetProvenance Provenance { get; }

    private static string RequireText(string value, string parameterName)
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : string.Join(' ', value.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
        if (normalized.Length == 0)
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return normalized;
    }
}
