namespace DragonCAD.Fabrication.Packages;

public sealed record ManufacturingPackageBundleRequest
{
    private ManufacturingPackageBundleRequest(
        string boardId,
        string sourceProjectId,
        DateTimeOffset generatedAt,
        ManufacturingPackageHandoffTarget handoffTarget,
        ManufacturingPackageArtifactSource[] artifacts)
    {
        BoardId = boardId;
        SourceProjectId = sourceProjectId;
        GeneratedAt = generatedAt;
        HandoffTarget = handoffTarget;
        Artifacts = artifacts;
    }

    public string BoardId { get; }

    public string SourceProjectId { get; }

    public DateTimeOffset GeneratedAt { get; }

    public ManufacturingPackageHandoffTarget HandoffTarget { get; }

    public IReadOnlyList<ManufacturingPackageArtifactSource> Artifacts { get; }

    public static ManufacturingPackageBundleRequest Create(
        string boardId,
        string sourceProjectId,
        DateTimeOffset generatedAt,
        ManufacturingPackageHandoffTarget handoffTarget,
        IEnumerable<ManufacturingPackageArtifactSource> artifacts)
    {
        ArgumentNullException.ThrowIfNull(artifacts);

        if (string.IsNullOrWhiteSpace(boardId))
        {
            throw new ArgumentException("Manufacturing package board id must not be empty.", nameof(boardId));
        }

        if (string.IsNullOrWhiteSpace(sourceProjectId))
        {
            throw new ArgumentException("Manufacturing package source project id must not be empty.", nameof(sourceProjectId));
        }

        ManufacturingPackageArtifactSource[] artifactArray = artifacts.ToArray();

        return new ManufacturingPackageBundleRequest(
            boardId.Trim(),
            sourceProjectId.Trim(),
            generatedAt,
            handoffTarget,
            artifactArray);
    }
}
