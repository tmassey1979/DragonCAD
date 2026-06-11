namespace DragonCAD.Fabrication.Packages;

public sealed record ManufacturingPackageBundle
{
    internal ManufacturingPackageBundle(
        string boardId,
        string sourceProjectId,
        DateTimeOffset generatedAt,
        ManufacturingPackageHandoffTarget handoffTarget,
        ManufacturingPackageArtifact[] artifacts,
        ManufacturingPackageDiagnostic[] diagnostics,
        string manifestJson)
    {
        BoardId = boardId;
        SourceProjectId = sourceProjectId;
        GeneratedAt = generatedAt;
        HandoffTarget = handoffTarget;
        Artifacts = artifacts;
        Diagnostics = diagnostics;
        ManifestJson = manifestJson;
    }

    public string BoardId { get; }

    public string SourceProjectId { get; }

    public DateTimeOffset GeneratedAt { get; }

    public ManufacturingPackageHandoffTarget HandoffTarget { get; }

    public IReadOnlyList<ManufacturingPackageArtifact> Artifacts { get; }

    public IReadOnlyList<ManufacturingPackageDiagnostic> Diagnostics { get; }

    public string ManifestJson { get; }

    public bool IsReadyForExport => Diagnostics.All(diagnostic => diagnostic.Severity != ManufacturingPackageDiagnosticSeverity.Blocker);
}
