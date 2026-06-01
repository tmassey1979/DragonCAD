using DragonCAD.Core.Components.Marketplace;
using DragonCAD.Core.Components.Marketplace.Provenance;

namespace DragonCAD.Core.Components.Promotion;

public enum PromotionAssetKind
{
    Footprint,
    Model3D,
    Symbol,
}

public enum PromotionDiagnosticSeverity
{
    Info,
    Warning,
    Error,
}

public sealed record PromotionAssetSummary
{
    public PromotionAssetSummary(
        PromotionAssetKind kind,
        string assetId,
        string displayName,
        string checksum)
    {
        Kind = kind;
        AssetId = CanonicalComponentKey.NormalizeRequired(assetId, nameof(assetId));
        DisplayName = CanonicalComponentKey.NormalizeRequired(displayName, nameof(displayName));
        Checksum = CanonicalComponentKey.NormalizeRequired(checksum, nameof(checksum));
    }

    public PromotionAssetKind Kind { get; }

    public string AssetId { get; }

    public string DisplayName { get; }

    public string Checksum { get; }

    internal string ToSummaryLine() => $"{Kind}:{AssetId}";
}

public sealed record PromotionDiagnostic
{
    public PromotionDiagnostic(PromotionDiagnosticSeverity severity, string message)
    {
        Severity = severity;
        Message = CanonicalComponentKey.NormalizeRequired(message, nameof(message));
    }

    public PromotionDiagnosticSeverity Severity { get; }

    public string Message { get; }

    internal string ToSummaryLine() => $"{Severity}: {Message}";
}

public sealed record LibraryPromotionCandidate
{
    private LibraryPromotionCandidate(
        CanonicalComponentKey componentId,
        string componentName,
        string sourceProvenanceId,
        string targetLibraryId,
        MarketplaceReviewState reviewState,
        IReadOnlyList<PromotionAssetSummary> assets,
        IReadOnlyList<PromotionDiagnostic> diagnostics)
    {
        ComponentId = componentId;
        ComponentName = NormalizeRequired(componentName, nameof(componentName));
        SourceProvenanceId = NormalizeRequired(sourceProvenanceId, nameof(sourceProvenanceId));
        TargetLibraryId = NormalizeRequired(targetLibraryId, nameof(targetLibraryId));
        ReviewState = reviewState;
        Assets = SortAssets(assets);
        Diagnostics = SortDiagnostics(diagnostics);
    }

    public CanonicalComponentKey ComponentId { get; }

    public string ComponentName { get; }

    public string SourceProvenanceId { get; }

    public string TargetLibraryId { get; }

    public MarketplaceReviewState ReviewState { get; }

    public IReadOnlyList<PromotionAssetSummary> Assets { get; }

    public IReadOnlyList<PromotionDiagnostic> Diagnostics { get; }

    public static LibraryPromotionCandidate Create(
        CanonicalComponentKey componentId,
        string componentName,
        string sourceProvenanceId,
        string targetLibraryId,
        MarketplaceReviewState reviewState,
        IReadOnlyList<PromotionAssetSummary> assets,
        IReadOnlyList<PromotionDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(assets);
        ArgumentNullException.ThrowIfNull(diagnostics);

        return new LibraryPromotionCandidate(
            componentId,
            componentName,
            sourceProvenanceId,
            targetLibraryId,
            reviewState,
            assets,
            diagnostics);
    }

    public LibraryPromotionPackage ToPackage()
    {
        bool hasBlockingDiagnostic = Diagnostics.Any(diagnostic => diagnostic.Severity == PromotionDiagnosticSeverity.Error);
        bool canPromote = ReviewState == MarketplaceReviewState.Approved && !hasBlockingDiagnostic;

        return new LibraryPromotionPackage(
            ComponentId,
            ComponentName,
            SourceProvenanceId,
            TargetLibraryId,
            ReviewState,
            canPromote,
            Assets.Select(asset => asset.ToSummaryLine()).ToArray(),
            Diagnostics.Select(diagnostic => diagnostic.ToSummaryLine()).ToArray());
    }

    private static IReadOnlyList<PromotionAssetSummary> SortAssets(IReadOnlyList<PromotionAssetSummary> assets) =>
        assets
            .OrderBy(asset => asset.Kind)
            .ThenBy(asset => asset.AssetId, StringComparer.Ordinal)
            .ThenBy(asset => asset.Checksum, StringComparer.Ordinal)
            .ToArray();

    private static IReadOnlyList<PromotionDiagnostic> SortDiagnostics(IReadOnlyList<PromotionDiagnostic> diagnostics) =>
        diagnostics
            .OrderByDescending(diagnostic => diagnostic.Severity)
            .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal)
            .ToArray();

    private static string NormalizeRequired(string value, string parameterName) =>
        CanonicalComponentKey.NormalizeRequired(value, parameterName);
}

public sealed record LibraryPromotionPackage
{
    public LibraryPromotionPackage(
        CanonicalComponentKey componentId,
        string componentName,
        string sourceProvenanceId,
        string targetLibraryId,
        MarketplaceReviewState reviewState,
        bool canPromote,
        IReadOnlyList<string> assetLines,
        IReadOnlyList<string> diagnosticLines)
    {
        ComponentId = componentId;
        ComponentName = CanonicalComponentKey.NormalizeRequired(componentName, nameof(componentName));
        SourceProvenanceId = CanonicalComponentKey.NormalizeRequired(sourceProvenanceId, nameof(sourceProvenanceId));
        TargetLibraryId = CanonicalComponentKey.NormalizeRequired(targetLibraryId, nameof(targetLibraryId));
        ReviewState = reviewState;
        CanPromote = canPromote;
        AssetLines = assetLines.ToArray();
        DiagnosticLines = diagnosticLines.ToArray();
    }

    public CanonicalComponentKey ComponentId { get; }

    public string ComponentName { get; }

    public string SourceProvenanceId { get; }

    public string TargetLibraryId { get; }

    public MarketplaceReviewState ReviewState { get; }

    public bool CanPromote { get; }

    public bool MutatesLibrary => false;

    public IReadOnlyList<string> AssetLines { get; }

    public IReadOnlyList<string> DiagnosticLines { get; }

    public string Summary =>
        $"{ComponentId.Value} -> {TargetLibraryId} | {(CanPromote ? "Approved" : "Blocked")} | {AssetLines.Count} assets | {DiagnosticLines.Count} diagnostics";
}
