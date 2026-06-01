namespace DragonCAD.Core.Components.Marketplace.Provenance;

public enum MarketplaceProvenanceKind
{
    VendorImport,
    DatasheetGenerated,
    ManualOverride,
}

public enum MarketplaceReviewState
{
    Imported,
    PendingReview,
    Approved,
    Rejected,
}

public sealed record MarketplaceComponentProvenance
{
    private MarketplaceComponentProvenance(
        CanonicalComponentKey componentKey,
        MarketplaceProvenanceKind kind,
        string sourceVendor,
        string productUrl,
        string datasheetUrl,
        string datasheetChecksum,
        string generatorName,
        MarketplaceReviewState reviewState,
        string reviewerNote,
        DateTimeOffset timestamp)
    {
        ComponentKey = componentKey;
        Kind = kind;
        SourceVendor = NormalizeRequired(sourceVendor, nameof(sourceVendor));
        ProductUrl = NormalizeRequired(productUrl, nameof(productUrl));
        DatasheetUrl = NormalizeRequired(datasheetUrl, nameof(datasheetUrl));
        DatasheetChecksum = NormalizeChecksum(datasheetChecksum);
        GeneratorName = NormalizeRequired(generatorName, nameof(generatorName));
        ReviewState = reviewState;
        ReviewerNote = NormalizeOptional(reviewerNote);
        Timestamp = timestamp;
    }

    public CanonicalComponentKey ComponentKey { get; }

    public MarketplaceProvenanceKind Kind { get; }

    public string SourceVendor { get; }

    public string ProductUrl { get; }

    public string DatasheetUrl { get; }

    public string DatasheetChecksum { get; }

    public string GeneratorName { get; }

    public MarketplaceReviewState ReviewState { get; }

    public string ReviewerNote { get; }

    public DateTimeOffset Timestamp { get; }

    public static MarketplaceComponentProvenance VendorImport(
        CanonicalComponentKey componentKey,
        string sourceVendor,
        string productUrl,
        string datasheetUrl,
        string datasheetChecksum,
        DateTimeOffset timestamp) =>
        new(
            componentKey,
            MarketplaceProvenanceKind.VendorImport,
            sourceVendor,
            productUrl,
            datasheetUrl,
            datasheetChecksum,
            generatorName: "manual",
            MarketplaceReviewState.Imported,
            reviewerNote: string.Empty,
            timestamp);

    public static MarketplaceComponentProvenance DatasheetGenerated(
        CanonicalComponentKey componentKey,
        string sourceVendor,
        string productUrl,
        string datasheetUrl,
        string datasheetChecksum,
        string generatorName,
        MarketplaceReviewState reviewState,
        DateTimeOffset timestamp) =>
        new(
            componentKey,
            MarketplaceProvenanceKind.DatasheetGenerated,
            sourceVendor,
            productUrl,
            datasheetUrl,
            datasheetChecksum,
            generatorName,
            reviewState,
            reviewerNote: string.Empty,
            timestamp);

    public MarketplaceComponentProvenance WithManualOverride(
        MarketplaceReviewState reviewState,
        string reviewerNote,
        DateTimeOffset timestamp) =>
        new(
            ComponentKey,
            MarketplaceProvenanceKind.ManualOverride,
            SourceVendor,
            ProductUrl,
            DatasheetUrl,
            DatasheetChecksum,
            generatorName: "manual",
            reviewState,
            reviewerNote,
            timestamp);

    internal string ToSummaryLine() =>
        string.Join(
            " | ",
            ComponentKey.Value,
            Kind,
            SourceVendor,
            GeneratorName,
            ReviewState,
            Timestamp.ToString("O"));

    private static string NormalizeRequired(string value, string parameterName) =>
        CanonicalComponentKey.NormalizeRequired(value, parameterName);

    private static string NormalizeOptional(string value) =>
        CanonicalComponentKey.NormalizeOptional(value);

    private static string NormalizeChecksum(string checksum)
    {
        string normalized = NormalizeRequired(checksum, nameof(checksum));
        return normalized;
    }
}

public sealed record MarketplaceProvenanceAudit
{
    private MarketplaceProvenanceAudit(IReadOnlyList<string> summaryLines)
    {
        SummaryLines = summaryLines;
    }

    public IReadOnlyList<string> SummaryLines { get; }

    public static MarketplaceProvenanceAudit Create(IReadOnlyList<MarketplaceComponentProvenance> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        string[] summaryLines = records
            .OrderBy(record => record.ComponentKey)
            .ThenBy(record => record.Timestamp)
            .ThenBy(record => record.Kind)
            .ThenBy(record => record.SourceVendor, StringComparer.Ordinal)
            .ThenBy(record => record.GeneratorName, StringComparer.Ordinal)
            .Select(record => record.ToSummaryLine())
            .ToArray();

        return new MarketplaceProvenanceAudit(summaryLines);
    }

    public override string ToString() => string.Join(Environment.NewLine, SummaryLines);
}
