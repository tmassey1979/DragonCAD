namespace DragonCAD.Core.Timeline;

public sealed record RevisionTimelineEvent
{
    private RevisionTimelineEvent(
        RevisionTimelineEventId id,
        DateTimeOffset occurredAt,
        string actor,
        RevisionTimelineArea area,
        string summary,
        IReadOnlyList<RevisionObjectRef> changedObjectRefs,
        IReadOnlyList<RevisionArtifactRef> artifactRefs,
        GitCommitId? gitCommitId)
    {
        Id = id;
        OccurredAt = occurredAt;
        Actor = RevisionTimelineEventId.Required(actor, nameof(actor));
        Area = area;
        Summary = RevisionTimelineEventId.Required(summary, nameof(summary));
        ChangedObjectRefs = NormalizeRefs(changedObjectRefs, nameof(changedObjectRefs));
        ArtifactRefs = NormalizeRefs(artifactRefs, nameof(artifactRefs));
        GitCommitId = gitCommitId;
    }

    public RevisionTimelineEventId Id { get; }

    public DateTimeOffset OccurredAt { get; }

    public string Actor { get; }

    public RevisionTimelineArea Area { get; }

    public string Summary { get; }

    public IReadOnlyList<RevisionObjectRef> ChangedObjectRefs { get; }

    public IReadOnlyList<RevisionArtifactRef> ArtifactRefs { get; }

    public GitCommitId? GitCommitId { get; }

    public static RevisionTimelineEvent Save(
        RevisionTimelineEventId id,
        DateTimeOffset occurredAt,
        string actor,
        string summary,
        IReadOnlyList<RevisionObjectRef> changedObjectRefs,
        IReadOnlyList<RevisionArtifactRef> artifactRefs,
        GitCommitId? gitCommitId = null) =>
        Create(id, occurredAt, actor, RevisionTimelineArea.Save, summary, changedObjectRefs, artifactRefs, gitCommitId);

    public static RevisionTimelineEvent Import(
        RevisionTimelineEventId id,
        DateTimeOffset occurredAt,
        string actor,
        string summary,
        IReadOnlyList<RevisionObjectRef> changedObjectRefs,
        IReadOnlyList<RevisionArtifactRef> artifactRefs,
        GitCommitId? gitCommitId = null) =>
        Create(id, occurredAt, actor, RevisionTimelineArea.Import, summary, changedObjectRefs, artifactRefs, gitCommitId);

    public static RevisionTimelineEvent Promotion(
        RevisionTimelineEventId id,
        DateTimeOffset occurredAt,
        string actor,
        string summary,
        IReadOnlyList<RevisionObjectRef> changedObjectRefs,
        IReadOnlyList<RevisionArtifactRef> artifactRefs,
        GitCommitId? gitCommitId = null) =>
        Create(id, occurredAt, actor, RevisionTimelineArea.Promotion, summary, changedObjectRefs, artifactRefs, gitCommitId);

    public static RevisionTimelineEvent FabricationExport(
        RevisionTimelineEventId id,
        DateTimeOffset occurredAt,
        string actor,
        string summary,
        IReadOnlyList<RevisionObjectRef> changedObjectRefs,
        IReadOnlyList<RevisionArtifactRef> artifactRefs,
        GitCommitId? gitCommitId = null) =>
        Create(id, occurredAt, actor, RevisionTimelineArea.FabricationExport, summary, changedObjectRefs, artifactRefs, gitCommitId);

    public static RevisionTimelineEvent OrderingReview(
        RevisionTimelineEventId id,
        DateTimeOffset occurredAt,
        string actor,
        string summary,
        IReadOnlyList<RevisionObjectRef> changedObjectRefs,
        IReadOnlyList<RevisionArtifactRef> artifactRefs,
        GitCommitId? gitCommitId = null) =>
        Create(id, occurredAt, actor, RevisionTimelineArea.OrderingReview, summary, changedObjectRefs, artifactRefs, gitCommitId);

    private static RevisionTimelineEvent Create(
        RevisionTimelineEventId id,
        DateTimeOffset occurredAt,
        string actor,
        RevisionTimelineArea area,
        string summary,
        IReadOnlyList<RevisionObjectRef> changedObjectRefs,
        IReadOnlyList<RevisionArtifactRef> artifactRefs,
        GitCommitId? gitCommitId) =>
        new(id, occurredAt, actor, area, summary, changedObjectRefs, artifactRefs, gitCommitId);

    private static IReadOnlyList<T> NormalizeRefs<T>(IReadOnlyList<T> refs, string parameterName)
        where T : IComparable<T>
    {
        ArgumentNullException.ThrowIfNull(refs);
        if (refs.Count == 0)
        {
            throw new ArgumentException("At least one reference is required.", parameterName);
        }

        return refs
            .Order()
            .ToArray();
    }
}
