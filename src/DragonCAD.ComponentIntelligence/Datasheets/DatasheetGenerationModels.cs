namespace DragonCAD.ComponentIntelligence.Datasheets;

public sealed record DatasheetAsset
{
    public DatasheetAsset(Uri sourceUrl, string checksum)
    {
        if (!sourceUrl.IsAbsoluteUri || sourceUrl.Scheme is not ("http" or "https"))
        {
            throw new ArgumentException("Datasheet source URL must be an absolute HTTP or HTTPS URL.", nameof(sourceUrl));
        }

        if (string.IsNullOrWhiteSpace(checksum))
        {
            throw new ArgumentException("Datasheet checksum is required.", nameof(checksum));
        }

        SourceUrl = sourceUrl;
        Checksum = checksum;
    }

    public Uri SourceUrl { get; }

    public string Checksum { get; }
}

public sealed record DatasheetPinFact(string Number, string Name, string Description);

public sealed record PackageDimensionFacts(long WidthInternal, long HeightInternal, long BodyLengthInternal);

public sealed record ExtractedDatasheetFacts(
    string Manufacturer,
    string ManufacturerPartNumber,
    string Description,
    string PackageName,
    IReadOnlyList<DatasheetPinFact> PinFacts,
    PackageDimensionFacts? PackageDimensions);

public enum DatasheetAiProvider
{
    Codex,
    Ollama,
}

public sealed record DatasheetComponentGenerationRequest(
    DatasheetAsset Datasheet,
    ExtractedDatasheetFacts Facts,
    DatasheetAiProvider Provider,
    string ModelName);

public sealed record GeneratedSymbolPinProposal(
    string Number,
    string Name,
    long XInternal,
    long YInternal,
    string Orientation);

public sealed record GeneratedSymbolProposal(string Name, IReadOnlyList<GeneratedSymbolPinProposal> Pins);

public sealed record GeneratedFootprintPadProposal(
    string Number,
    long XInternal,
    long YInternal,
    long DiameterInternal,
    long DrillInternal,
    string Shape);

public sealed record GeneratedFootprintProposal(string PackageName, IReadOnlyList<GeneratedFootprintPadProposal> Pads);

public enum DatasheetThreeDimensionalModelStatus
{
    Placeholder,
    GeneratedDraft,
    LinkedVendorModel,
}

public sealed record GeneratedThreeDimensionalModelProposal(
    string FileName,
    DatasheetThreeDimensionalModelStatus Status);

public enum DatasheetGenerationConfidence
{
    Low,
    Medium,
    High,
}

public enum DatasheetGenerationWarningCode
{
    MissingPackageDimensions,
}

public sealed record DatasheetGenerationWarning(DatasheetGenerationWarningCode Code, string Message);

public enum DatasheetHumanReviewStatus
{
    Required,
    Approved,
    Rejected,
}

public sealed record DatasheetGeneratedComponentPlan
{
    private DatasheetGeneratedComponentPlan(
        DatasheetComponentGenerationRequest request,
        GeneratedSymbolProposal symbol,
        GeneratedFootprintProposal footprint,
        GeneratedThreeDimensionalModelProposal threeDimensionalModel,
        DatasheetGenerationConfidence confidence,
        IReadOnlyList<DatasheetGenerationWarning> warnings)
    {
        Request = request;
        Symbol = symbol;
        Footprint = footprint;
        ThreeDimensionalModel = threeDimensionalModel;
        Confidence = confidence;
        Warnings = warnings;
    }

    public DatasheetComponentGenerationRequest Request { get; }

    public GeneratedSymbolProposal Symbol { get; }

    public GeneratedFootprintProposal Footprint { get; }

    public GeneratedThreeDimensionalModelProposal ThreeDimensionalModel { get; }

    public DatasheetGenerationConfidence Confidence { get; }

    public IReadOnlyList<DatasheetGenerationWarning> Warnings { get; }

    public DatasheetHumanReviewStatus HumanReviewStatus => DatasheetHumanReviewStatus.Required;

    public bool IsApproved => false;

    public static DatasheetGeneratedComponentPlan Create(
        DatasheetComponentGenerationRequest request,
        GeneratedSymbolProposal symbol,
        GeneratedFootprintProposal footprint,
        GeneratedThreeDimensionalModelProposal threeDimensionalModel,
        DatasheetGenerationConfidence confidence)
    {
        var warnings = new List<DatasheetGenerationWarning>();
        if (request.Facts.PackageDimensions is null)
        {
            warnings.Add(new DatasheetGenerationWarning(
                DatasheetGenerationWarningCode.MissingPackageDimensions,
                "Package dimensions were not extracted from the datasheet; footprint geometry requires human review."));
        }

        return new DatasheetGeneratedComponentPlan(
            request,
            symbol,
            footprint,
            threeDimensionalModel,
            confidence,
            warnings);
    }
}
