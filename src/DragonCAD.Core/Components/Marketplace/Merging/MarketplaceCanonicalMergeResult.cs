namespace DragonCAD.Core.Components.Marketplace.Merging;

public sealed record MarketplaceCanonicalMergeResult(
    IReadOnlyList<MarketplaceCanonicalMergeDecision> Decisions,
    IReadOnlyList<MarketplaceCanonicalMergeDiagnostic> Diagnostics);

public sealed record MarketplaceCanonicalMergeDecision(
    CanonicalMarketplaceComponent Component,
    IReadOnlyList<MarketplaceComponentFact> SourceFacts,
    string MatchReason);

public sealed record MarketplaceCanonicalMergeDiagnostic(
    MarketplaceCanonicalMergeDiagnosticSeverity Severity,
    string Code,
    CanonicalComponentKey ComponentKey,
    string Message);

public enum MarketplaceCanonicalMergeDiagnosticSeverity
{
    Info,
    Warning,
    Error,
}

public static class MarketplaceCanonicalMergeDiagnosticCodes
{
    public const string ConflictingValues = "MARKETPLACE_MERGE_CONFLICTING_VALUES";
}
