namespace DragonCAD.ComponentIntelligence.Datasheets;

public sealed record DatasheetDraftGenerationRequest
{
    private DatasheetDraftGenerationRequest(
        string? pdfPath,
        Uri? sourceUrl,
        string? vendorProductId,
        string? manufacturerPartNumber,
        string targetPackage)
    {
        PdfPath = pdfPath;
        SourceUrl = sourceUrl;
        VendorProductId = vendorProductId;
        ManufacturerPartNumber = manufacturerPartNumber;
        TargetPackage = targetPackage;
    }

    public string? PdfPath { get; }

    public Uri? SourceUrl { get; }

    public string? VendorProductId { get; }

    public string? ManufacturerPartNumber { get; }

    public string TargetPackage { get; }

    public static DatasheetDraftGenerationRequestValidationResult Create(
        string? pdfPath,
        Uri? sourceUrl,
        string? vendorProductId,
        string? manufacturerPartNumber,
        string? targetPackage)
    {
        List<DatasheetDraftGenerationDiagnostic> diagnostics = [];
        string? normalizedPdfPath = NormalizeOptional(pdfPath);
        string? normalizedVendorProductId = NormalizeOptional(vendorProductId);
        string? normalizedManufacturerPartNumber = NormalizeOptional(manufacturerPartNumber);
        string? normalizedTargetPackage = NormalizeOptional(targetPackage);

        if (normalizedPdfPath is null && sourceUrl is null)
        {
            diagnostics.Add(new DatasheetDraftGenerationDiagnostic(
                DatasheetDraftGenerationDiagnosticCode.MissingDatasheetSource,
                "Datasheet draft generation requires a local PDF path or source URL."));
        }

        if (sourceUrl is not null && (!sourceUrl.IsAbsoluteUri || sourceUrl.Scheme is not ("http" or "https")))
        {
            diagnostics.Add(new DatasheetDraftGenerationDiagnostic(
                DatasheetDraftGenerationDiagnosticCode.UnsupportedSourceUrl,
                "Datasheet source URL must be absolute HTTP or HTTPS."));
        }

        if (normalizedVendorProductId is null && normalizedManufacturerPartNumber is null)
        {
            diagnostics.Add(new DatasheetDraftGenerationDiagnostic(
                DatasheetDraftGenerationDiagnosticCode.MissingComponentIdentifier,
                "Datasheet draft generation requires a vendor product id or manufacturer part number."));
        }

        if (normalizedTargetPackage is null)
        {
            diagnostics.Add(new DatasheetDraftGenerationDiagnostic(
                DatasheetDraftGenerationDiagnosticCode.MissingTargetPackage,
                "Datasheet draft generation requires a target package."));
        }

        if (diagnostics.Count > 0)
        {
            return new DatasheetDraftGenerationRequestValidationResult(null, diagnostics);
        }

        return new DatasheetDraftGenerationRequestValidationResult(
            new DatasheetDraftGenerationRequest(
                normalizedPdfPath,
                sourceUrl,
                normalizedVendorProductId,
                normalizedManufacturerPartNumber,
                normalizedTargetPackage!),
            Diagnostics: []);
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public enum DatasheetDraftGenerationDiagnosticCode
{
    MissingDatasheetSource,
    UnsupportedSourceUrl,
    MissingComponentIdentifier,
    MissingTargetPackage,
    UnsupportedTargetPackage,
}

public sealed record DatasheetDraftGenerationDiagnostic(
    DatasheetDraftGenerationDiagnosticCode Code,
    string Message);

public sealed record DatasheetDraftGenerationRequestValidationResult(
    DatasheetDraftGenerationRequest? Request,
    IReadOnlyList<DatasheetDraftGenerationDiagnostic> Diagnostics)
{
    public bool Accepted => Request is not null && Diagnostics.Count == 0;
}

public enum DatasheetDraftPinElectricalType
{
    Unknown,
    Input,
    Output,
    Bidirectional,
    Passive,
    PowerInput,
    PowerOutput,
    NoConnect,
}

public sealed record DatasheetDraftPinExtraction(
    string Number,
    string Name,
    DatasheetDraftPinElectricalType ElectricalType,
    decimal ConfidenceScore);

public sealed record DatasheetDraftMetadata(
    string? Manufacturer,
    string? ManufacturerPartNumber,
    string? VendorProductId,
    string? Description);

public enum DatasheetDraftWarningCode
{
    AmbiguousPinType,
    PackageDrawingAmbiguous,
    LowPinConfidence,
}

public sealed record DatasheetDraftWarning(DatasheetDraftWarningCode Code, string Message);

public sealed record DatasheetDraftExtraction(
    IReadOnlyList<DatasheetDraftPinExtraction> Pins,
    IReadOnlyList<string> PackageHints,
    DatasheetDraftMetadata Metadata,
    decimal ConfidenceScore,
    IReadOnlyList<DatasheetDraftWarning> Warnings);

public interface IDatasheetDraftExtractor
{
    DatasheetDraftExtraction Extract(DatasheetDraftGenerationRequest request);
}

public sealed record DatasheetFootprintCandidate(
    string LibraryId,
    string PackageName,
    decimal ConfidenceScore);

public interface IDatasheetPackageCatalog
{
    IReadOnlyList<DatasheetFootprintCandidate> FindFootprintCandidates(string targetPackage);
}

public sealed record DatasheetDraftPin(
    string Number,
    string Name,
    DatasheetDraftPinElectricalType ElectricalType,
    decimal ConfidenceScore);

public enum DatasheetDraftVerificationStatus
{
    Unverified,
}

public sealed record DatasheetDraftComponent(
    DatasheetDraftGenerationRequest Request,
    IReadOnlyList<DatasheetDraftPin> Pins,
    IReadOnlyList<string> PackageHints,
    IReadOnlyList<DatasheetFootprintCandidate> FootprintCandidates,
    DatasheetDraftMetadata Metadata,
    decimal ConfidenceScore,
    IReadOnlyList<DatasheetDraftWarning> Warnings)
{
    public string TargetPackage => Request.TargetPackage;

    public DatasheetDraftVerificationStatus VerificationStatus => DatasheetDraftVerificationStatus.Unverified;

    public bool IsVerified => false;
}

public sealed record DatasheetDraftGenerationResult(
    DatasheetDraftComponent? Draft,
    IReadOnlyList<DatasheetDraftGenerationDiagnostic> Diagnostics)
{
    public bool Accepted => Draft is not null && Diagnostics.Count == 0;
}

public sealed class DatasheetDraftGenerator
{
    private const decimal LowPinConfidenceThreshold = 0.50m;
    private readonly IDatasheetDraftExtractor extractor;
    private readonly IDatasheetPackageCatalog packageCatalog;

    public DatasheetDraftGenerator(
        IDatasheetDraftExtractor extractor,
        IDatasheetPackageCatalog packageCatalog)
    {
        this.extractor = extractor;
        this.packageCatalog = packageCatalog;
    }

    public DatasheetDraftGenerationResult Generate(DatasheetDraftGenerationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        IReadOnlyList<DatasheetFootprintCandidate> footprintCandidates =
            packageCatalog.FindFootprintCandidates(request.TargetPackage);
        if (footprintCandidates.Count == 0)
        {
            return new DatasheetDraftGenerationResult(
                null,
                [
                    new DatasheetDraftGenerationDiagnostic(
                        DatasheetDraftGenerationDiagnosticCode.UnsupportedTargetPackage,
                        $"Target package '{request.TargetPackage}' is not supported by the datasheet footprint catalog."),
                ]);
        }

        DatasheetDraftExtraction extraction = extractor.Extract(request);
        DatasheetDraftComponent draft = new(
            request,
            extraction.Pins.Select(ToDraftPin).ToArray(),
            extraction.PackageHints.Select(NormalizePackageHint).Where(packageHint => packageHint.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            footprintCandidates,
            MergeMetadata(request, extraction.Metadata),
            extraction.ConfidenceScore,
            BuildWarnings(extraction).ToArray());

        return new DatasheetDraftGenerationResult(draft, Diagnostics: []);
    }

    private static DatasheetDraftPin ToDraftPin(DatasheetDraftPinExtraction pin) =>
        new(pin.Number.Trim(), pin.Name.Trim(), pin.ElectricalType, pin.ConfidenceScore);

    private static DatasheetDraftMetadata MergeMetadata(
        DatasheetDraftGenerationRequest request,
        DatasheetDraftMetadata metadata) =>
        metadata with
        {
            ManufacturerPartNumber = PreferExtracted(metadata.ManufacturerPartNumber, request.ManufacturerPartNumber),
            VendorProductId = PreferExtracted(metadata.VendorProductId, request.VendorProductId),
        };

    private static string? PreferExtracted(string? extracted, string? requested) =>
        string.IsNullOrWhiteSpace(extracted) ? requested : extracted.Trim();

    private static IEnumerable<DatasheetDraftWarning> BuildWarnings(DatasheetDraftExtraction extraction)
    {
        foreach (DatasheetDraftWarning warning in extraction.Warnings)
        {
            yield return warning;
        }

        foreach (DatasheetDraftPinExtraction pin in extraction.Pins.Where(pin => pin.ConfidenceScore < LowPinConfidenceThreshold))
        {
            yield return new DatasheetDraftWarning(
                DatasheetDraftWarningCode.LowPinConfidence,
                $"Pin '{pin.Number}' was extracted below the confidence threshold.");
        }
    }

    private static string NormalizePackageHint(string packageHint) =>
        string.IsNullOrWhiteSpace(packageHint) ? string.Empty : packageHint.Trim();
}
