using DragonCAD.Core.Components.Drafts;
using DragonCAD.Core.Components.Marketplace.Provenance;

namespace DragonCAD.Core.Components.Promotion.Persistence;

public enum TrustedLibraryPromotionAction
{
    PromoteNew,
    LinkExisting,
    Reject,
}

public enum TrustedLibraryPromotionStatus
{
    Promoted,
    LinkedExisting,
    Rejected,
    Blocked,
}

public enum TrustedLibraryPromotionRecordState
{
    Trusted,
}

public enum TrustedLibraryPromotionAuditKind
{
    PromotedNew,
    LinkedExisting,
    Rejected,
}

public enum TrustedLibraryPromotionConflictKind
{
    VerifiedGeometry,
}

public enum TrustedLibraryPromotionConflictDecision
{
    KeepExistingVerifiedGeometry,
}

public sealed record TrustedLibraryPromotionRequest(
    TrustedLibraryPromotionAction Action,
    ComponentDraft Draft,
    string TargetLibraryId,
    string Reviewer,
    string DecisionId,
    DateTimeOffset ReviewedAt,
    string SourceProvenanceId,
    MarketplaceComponentProvenance SourceProvenance,
    string? ExistingComponentId,
    IReadOnlyDictionary<string, TrustedLibraryPromotionConflictDecision> ConflictDecisions);

public sealed record TrustedLibraryPromotionResult(
    TrustedLibraryPromotionStatus Status,
    IReadOnlyList<TrustedLibraryPromotionConflict> Conflicts)
{
    public bool IsBlocked => Status == TrustedLibraryPromotionStatus.Blocked;
}

public sealed record TrustedLibraryPromotionConflict(
    TrustedLibraryPromotionConflictKind Kind,
    string Key,
    string ComponentId,
    string Message);

public sealed record TrustedLibraryPromotionLibrary(
    IReadOnlyList<TrustedLibraryPromotionRecord> Records,
    IReadOnlyList<TrustedLibraryPromotionAuditRecord> AuditRecords)
{
    public static TrustedLibraryPromotionLibrary Empty { get; } = new([], []);
}

public sealed record TrustedLibraryPromotionRecord(
    string ComponentId,
    string DisplayName,
    string TargetLibraryId,
    string SourceProvenanceId,
    TrustedLibraryPromotionProvenance Provenance,
    string Reviewer,
    DateTimeOffset ReviewedAt,
    string LastDecisionId,
    TrustedLibraryPromotionPackage Package,
    string VerifiedGeometryFingerprint,
    TrustedLibraryPromotionRecordState State,
    IReadOnlyList<string> RollbackActions);

public sealed record TrustedLibraryPromotionPackage(
    string Name,
    string ReferencePrefix,
    IReadOnlyList<string> FootprintMappings);

public sealed record TrustedLibraryPromotionProvenance(
    string Kind,
    string SourceVendor,
    string ProductUrl,
    string DatasheetUrl,
    string DatasheetChecksum,
    string GeneratorName,
    string ReviewState,
    DateTimeOffset Timestamp);

public sealed record TrustedLibraryPromotionAuditRecord(
    TrustedLibraryPromotionAuditKind Kind,
    string ComponentId,
    string TargetLibraryId,
    string SourceProvenanceId,
    string Reviewer,
    string DecisionId,
    DateTimeOffset ReviewedAt,
    string ReviewState,
    TrustedLibraryPromotionRecord? RecordSnapshot);
