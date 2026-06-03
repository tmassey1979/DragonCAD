using DragonCAD.Core.Components.Definitions;
using DragonCAD.Core.Components.Identity;

namespace DragonCAD.Core.Libraries.Review;

public sealed class ComponentReviewQueue
{
    private readonly List<ComponentReviewItem> items = [];
    private readonly List<ComponentPromotionRecord> promotionRecords = [];

    public IReadOnlyList<ComponentReviewItem> Items => items;

    public IReadOnlyList<ComponentPromotionRecord> PromotionRecords => promotionRecords;

    public ComponentReviewItem Enqueue(
        ComponentDefinition candidate,
        ComponentReviewCandidateSource source,
        ComponentReviewPreviewStatus symbolPreview,
        ComponentReviewPreviewStatus footprintPreview,
        IReadOnlyList<ComponentReviewConflict> conflicts,
        IReadOnlyList<ComponentReviewWarning> warnings)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(symbolPreview);
        ArgumentNullException.ThrowIfNull(footprintPreview);
        ArgumentNullException.ThrowIfNull(conflicts);
        ArgumentNullException.ThrowIfNull(warnings);

        candidate.Validate();

        ComponentReviewItem item = new(
            new ComponentReviewItemId($"review:{items.Count + 1}"),
            candidate,
            source,
            ComponentReviewMetadata.From(candidate),
            symbolPreview,
            footprintPreview,
            conflicts.ToArray(),
            warnings.ToArray(),
            ComponentReviewItemState.PendingReview);
        items.Add(item);

        return item;
    }

    public ComponentReviewItem Get(ComponentReviewItemId itemId) =>
        Find(itemId) ?? throw new InvalidOperationException($"Review item '{itemId}' was not found.");

    public ComponentPromotionRecord PromoteNew(
        ComponentReviewItemId itemId,
        ComponentId canonicalComponentId,
        string reviewer,
        DateTimeOffset reviewedAt,
        IReadOnlyList<string> changedFields,
        bool conflictReviewed = false) =>
        Promote(
            itemId,
            ComponentPromotionDecision.CreateCanonical,
            canonicalComponentId,
            reviewer,
            reviewedAt,
            changedFields,
            rejectionReason: null,
            conflictReviewed);

    public ComponentPromotionRecord LinkExisting(
        ComponentReviewItemId itemId,
        ComponentId canonicalComponentId,
        string reviewer,
        DateTimeOffset reviewedAt,
        IReadOnlyList<string> changedFields,
        bool conflictReviewed = false) =>
        Promote(
            itemId,
            ComponentPromotionDecision.LinkExistingCanonical,
            canonicalComponentId,
            reviewer,
            reviewedAt,
            changedFields,
            rejectionReason: null,
            conflictReviewed);

    public ComponentPromotionRecord Reject(
        ComponentReviewItemId itemId,
        string reviewer,
        DateTimeOffset reviewedAt,
        string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        return Promote(
            itemId,
            ComponentPromotionDecision.RejectCandidate,
            canonicalComponentId: null,
            reviewer,
            reviewedAt,
            ["State", "RejectionReason"],
            reason.Trim(),
            conflictReviewed: true);
    }

    private ComponentPromotionRecord Promote(
        ComponentReviewItemId itemId,
        ComponentPromotionDecision decision,
        ComponentId? canonicalComponentId,
        string reviewer,
        DateTimeOffset reviewedAt,
        IReadOnlyList<string> changedFields,
        string? rejectionReason,
        bool conflictReviewed)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reviewer);
        ArgumentNullException.ThrowIfNull(changedFields);

        ComponentReviewItem item = Get(itemId);
        if (item.State != ComponentReviewItemState.PendingReview)
        {
            throw new InvalidOperationException($"Review item '{itemId}' has already been resolved.");
        }

        if (item.Conflicts.Count > 0 && !conflictReviewed)
        {
            throw new InvalidOperationException("Promotion requires explicit conflict review before resolving this candidate.");
        }

        string[] normalizedChangedFields = NormalizeChangedFields(changedFields);
        if (normalizedChangedFields.Length == 0)
        {
            throw new ArgumentException("Promotion records require at least one changed field.", nameof(changedFields));
        }

        ComponentPromotionRecord record = new(
            new ComponentPromotionRecordId($"promotion:{promotionRecords.Count + 1}"),
            item.Id,
            item.Candidate.Id,
            canonicalComponentId,
            decision,
            reviewer.Trim(),
            reviewedAt,
            item.Source,
            normalizedChangedFields,
            rejectionReason);
        promotionRecords.Add(record);

        Replace(item, item with
        {
            State = decision == ComponentPromotionDecision.RejectCandidate
                ? ComponentReviewItemState.Rejected
                : ComponentReviewItemState.Promoted
        });

        return record;
    }

    private ComponentReviewItem? Find(ComponentReviewItemId itemId) =>
        items.FirstOrDefault(item => item.Id == itemId);

    private void Replace(ComponentReviewItem previous, ComponentReviewItem replacement)
    {
        int index = items.IndexOf(previous);
        items[index] = replacement;
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value.Trim();
    }

    private static string[] NormalizeChangedFields(IReadOnlyList<string> changedFields)
    {
        HashSet<string> seen = new(StringComparer.Ordinal);
        List<string> normalizedFields = [];

        foreach (string changedField in changedFields)
        {
            string normalizedField = NormalizeRequired(changedField, nameof(changedFields));
            if (seen.Add(normalizedField))
            {
                normalizedFields.Add(normalizedField);
            }
        }

        return normalizedFields.ToArray();
    }
}

public readonly record struct ComponentReviewItemId(string Value)
{
    public override string ToString() => Value;
}

public readonly record struct ComponentPromotionRecordId(string Value)
{
    public override string ToString() => Value;
}

public sealed record ComponentReviewItem(
    ComponentReviewItemId Id,
    ComponentDefinition Candidate,
    ComponentReviewCandidateSource Source,
    ComponentReviewMetadata Metadata,
    ComponentReviewPreviewStatus SymbolPreview,
    ComponentReviewPreviewStatus FootprintPreview,
    IReadOnlyList<ComponentReviewConflict> Conflicts,
    IReadOnlyList<ComponentReviewWarning> Warnings,
    ComponentReviewItemState State);

public enum ComponentReviewItemState
{
    PendingReview,
    Promoted,
    Rejected
}

public sealed record ComponentReviewCandidateSource(
    ComponentReviewSourceKind Kind,
    string SourceName,
    string SourceLocation,
    DateTimeOffset ImportedAt)
{
    public string SourceName { get; init; } = NormalizeRequired(SourceName, nameof(SourceName));

    public string SourceLocation { get; init; } = NormalizeRequired(SourceLocation, nameof(SourceLocation));

    public static ComponentReviewCandidateSource ImportedLibrary(
        string sourceName,
        string sourceLocation,
        DateTimeOffset importedAt) =>
        new(ComponentReviewSourceKind.ImportedLibrary, sourceName, sourceLocation, importedAt);

    public static ComponentReviewCandidateSource Generated(
        string sourceName,
        string sourceLocation,
        DateTimeOffset importedAt) =>
        new(ComponentReviewSourceKind.Generated, sourceName, sourceLocation, importedAt);

    private static string NormalizeRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value.Trim();
    }
}

public enum ComponentReviewSourceKind
{
    ImportedLibrary,
    Generated,
    Manual
}

public sealed record ComponentReviewMetadata(
    string DisplayName,
    string Manufacturer,
    string ManufacturerPartNumber,
    string Description,
    IReadOnlyList<ComponentAttribute> Attributes)
{
    public static ComponentReviewMetadata From(ComponentDefinition candidate) =>
        new(
            candidate.DisplayName,
            candidate.Manufacturer,
            candidate.ManufacturerPartNumber,
            candidate.Description,
            candidate.Attributes.ToArray());
}

public sealed record ComponentReviewPreviewStatus(ComponentReviewPreviewState State, string Detail)
{
    public string Detail { get; init; } = NormalizeRequired(Detail, nameof(Detail));

    public static ComponentReviewPreviewStatus Ready(string detail) =>
        new(ComponentReviewPreviewState.Ready, detail);

    public static ComponentReviewPreviewStatus Failed(string detail) =>
        new(ComponentReviewPreviewState.Failed, detail);

    private static string NormalizeRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value.Trim();
    }
}

public enum ComponentReviewPreviewState
{
    NotAvailable,
    Ready,
    Warning,
    Failed
}

public sealed record ComponentReviewConflict(string Field, string Message)
{
    public string Field { get; init; } = NormalizeRequired(Field, nameof(Field));

    public string Message { get; init; } = NormalizeRequired(Message, nameof(Message));

    private static string NormalizeRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value.Trim();
    }
}

public sealed record ComponentReviewWarning(string Field, string Message)
{
    public string Field { get; init; } = NormalizeRequired(Field, nameof(Field));

    public string Message { get; init; } = NormalizeRequired(Message, nameof(Message));

    private static string NormalizeRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value.Trim();
    }
}

public sealed record ComponentPromotionRecord(
    ComponentPromotionRecordId Id,
    ComponentReviewItemId ReviewItemId,
    ComponentId CandidateComponentId,
    ComponentId? CanonicalComponentId,
    ComponentPromotionDecision Decision,
    string Reviewer,
    DateTimeOffset ReviewedAt,
    ComponentReviewCandidateSource Source,
    IReadOnlyList<string> ChangedFields,
    string? RejectionReason);

public enum ComponentPromotionDecision
{
    CreateCanonical,
    LinkExistingCanonical,
    RejectCandidate
}
