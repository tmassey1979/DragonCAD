namespace DragonCAD.App.Datasheets.Extraction;

public sealed record DatasheetExtractionResult(
    string ProviderId,
    bool IsSupported,
    DatasheetExtractionConfidence Confidence,
    IReadOnlyList<DatasheetSourceReference> SourceReferences,
    IReadOnlyList<DatasheetExtractedPin> Pins,
    IReadOnlyList<DatasheetExtractedPackage> Packages,
    IReadOnlyList<DatasheetExtractedFact> ComponentFacts,
    IReadOnlyList<DatasheetThreeDimensionalModelProposal> ThreeDimensionalModelProposals,
    IReadOnlyList<DatasheetExtractionDiagnostic> Diagnostics,
    IReadOnlyList<DatasheetUnsupportedFeatureWarning> UnsupportedFeatures)
{
    public static DatasheetExtractionResult Supported(
        string providerId,
        DatasheetExtractionConfidence confidence,
        IReadOnlyList<DatasheetSourceReference> sourceReferences,
        IReadOnlyList<DatasheetExtractedPin>? pins = null,
        IReadOnlyList<DatasheetExtractedPackage>? packages = null,
        IReadOnlyList<DatasheetExtractedFact>? componentFacts = null,
        IReadOnlyList<DatasheetThreeDimensionalModelProposal>? threeDimensionalModelProposals = null,
        IReadOnlyList<DatasheetExtractionDiagnostic>? diagnostics = null,
        IReadOnlyList<DatasheetUnsupportedFeatureWarning>? unsupportedFeatures = null) =>
        new(
            providerId,
            IsSupported: true,
            confidence,
            sourceReferences,
            pins ?? [],
            packages ?? [],
            componentFacts ?? [],
            threeDimensionalModelProposals ?? [],
            diagnostics ?? [],
            unsupportedFeatures ?? []);

    public DatasheetExtractionResult WithUnsupportedFeatures(
        IReadOnlyList<DatasheetUnsupportedFeatureWarning> unsupportedFeatures) =>
        this with
        {
            UnsupportedFeatures = UnsupportedFeatures.Concat(unsupportedFeatures).ToArray()
        };

    public static DatasheetExtractionResult Disabled(
        string providerId,
        IReadOnlyCollection<DatasheetExtractionCapability> requestedCapabilities) =>
        new(
            providerId,
            IsSupported: false,
            new DatasheetExtractionConfidence(0m, "Provider is not enabled."),
            SourceReferences: [],
            Pins: [],
            Packages: [],
            ComponentFacts: [],
            ThreeDimensionalModelProposals: [],
            Diagnostics:
            [
                new DatasheetExtractionDiagnostic(
                    DatasheetExtractionDiagnosticSeverity.Warning,
                    "PROVIDER_NOT_CONFIGURED",
                    $"No datasheet extraction provider is configured for '{providerId}'.")
            ],
            UnsupportedFeatures: requestedCapabilities
                .Distinct()
                .Select(capability => new DatasheetUnsupportedFeatureWarning(capability, "Provider is not enabled."))
                .ToArray());
}
