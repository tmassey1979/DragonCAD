namespace DragonCAD.Sourcing.Compliance;

public sealed record OpenHardwareAssetProvenance
{
    public OpenHardwareAssetProvenance(
        Uri sourceRepository,
        string? sourcePath,
        string? licenseName,
        string? licenseText,
        Uri? licenseUrl,
        string? attributionNotes)
    {
        SourceRepository = sourceRepository ?? throw new ArgumentNullException(nameof(sourceRepository));
        SourcePath = Normalize(sourcePath);
        LicenseName = Normalize(licenseName);
        LicenseText = Normalize(licenseText);
        LicenseUrl = licenseUrl;
        AttributionNotes = Normalize(attributionNotes);
    }

    public Uri SourceRepository { get; }

    public string SourcePath { get; }

    public string LicenseName { get; }

    public string LicenseText { get; }

    public Uri? LicenseUrl { get; }

    public string AttributionNotes { get; }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : string.Join(' ', value.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
    }
}
