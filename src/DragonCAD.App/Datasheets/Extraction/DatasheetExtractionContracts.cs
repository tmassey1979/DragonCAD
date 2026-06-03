namespace DragonCAD.App.Datasheets.Extraction;

public sealed record DatasheetExtractionRequest(
    string DatasheetId,
    string SourceName,
    IReadOnlyCollection<DatasheetExtractionCapability> RequestedCapabilities,
    IReadOnlyList<DatasheetSourceReference> SourceReferences);

public sealed record DatasheetSourceReference(
    string Location,
    string Description);

public sealed record DatasheetExtractionConfidence(
    decimal Score,
    string Rationale);

public sealed record DatasheetExtractedPin(
    string Number,
    string Name,
    string Function,
    DatasheetSourceReference SourceReference);

public sealed record DatasheetExtractedPackage(
    string PackageName,
    string FootprintHint,
    DatasheetSourceReference SourceReference);

public sealed record DatasheetExtractedFact(
    string Name,
    string Value,
    DatasheetSourceReference SourceReference);

public sealed record DatasheetThreeDimensionalModelProposal(
    string ModelFileName,
    string PackageName,
    DatasheetSourceReference SourceReference);

public enum DatasheetExtractionDiagnosticSeverity
{
    Info,
    Warning,
    Error
}

public sealed record DatasheetExtractionDiagnostic(
    DatasheetExtractionDiagnosticSeverity Severity,
    string Code,
    string Message,
    DatasheetSourceReference? SourceReference = null);

public sealed record DatasheetUnsupportedFeatureWarning(
    DatasheetExtractionCapability Capability,
    string Reason);
