using DragonCAD.Fabrication.Packages;

namespace DragonCAD.Fabrication.Tests.Packages;

public sealed class ManufacturingPackageBundleBuilderTests
{
    private static readonly DateTimeOffset GeneratedAt = new(2026, 06, 10, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Build_CreatesFullAssemblyBundleWithManifestMetadataAndDiagnostics()
    {
        ManufacturingPackageBundle bundle = ManufacturingPackageBundleBuilder.Build(
            ManufacturingPackageBundleRequest.Create(
                boardId: "board-a",
                sourceProjectId: "project-42",
                generatedAt: GeneratedAt,
                handoffTarget: ManufacturingPackageHandoffTarget.Assembly,
                artifacts:
                [
                    Artifact(ManufacturingPackageArtifactKind.PickAndPlace, "assembly/project-pnp.csv", "ref,x,y\nU1,1,2\n"),
                    Artifact(ManufacturingPackageArtifactKind.Gerber, "gerbers/top.gbr", "G04 top*"),
                    Artifact(ManufacturingPackageArtifactKind.BillOfMaterials, "bom/project.csv", "ref,mpn\nU1,STM32\n"),
                    Artifact(ManufacturingPackageArtifactKind.Drill, "drill/project.drl", "M48\n"),
                    Artifact(ManufacturingPackageArtifactKind.SolderPaste, "paste/top.gtp", "G04 paste*")
                ]));

        Assert.True(bundle.IsReadyForExport);
        Assert.Equal("board-a", bundle.BoardId);
        Assert.Equal("project-42", bundle.SourceProjectId);
        Assert.Equal(GeneratedAt, bundle.GeneratedAt);
        Assert.Empty(bundle.Diagnostics);
        Assert.Contains(bundle.Artifacts, artifact => artifact.Kind == ManufacturingPackageArtifactKind.Manifest);
        Assert.Contains(bundle.Artifacts, artifact => artifact.Kind == ManufacturingPackageArtifactKind.BillOfMaterials);
        Assert.Contains("\"boardId\":\"board-a\"", bundle.ManifestJson, StringComparison.Ordinal);
        Assert.Contains("\"sourceProjectId\":\"project-42\"", bundle.ManifestJson, StringComparison.Ordinal);
        Assert.All(bundle.Artifacts, artifact => Assert.StartsWith("sha256:", artifact.Checksum.Value, StringComparison.Ordinal));
    }

    [Fact]
    public void Build_ReportsMissingDrillBlockerForPrototypeTarget()
    {
        ManufacturingPackageBundle bundle = ManufacturingPackageBundleBuilder.Build(
            ManufacturingPackageBundleRequest.Create(
                "board-a",
                "project-42",
                GeneratedAt,
                ManufacturingPackageHandoffTarget.GerberPrototype,
                [Artifact(ManufacturingPackageArtifactKind.Gerber, "gerbers/top.gbr", "G04 top*")]));

        ManufacturingPackageDiagnostic diagnostic = Assert.Single(bundle.Diagnostics);
        Assert.False(bundle.IsReadyForExport);
        Assert.Equal(ManufacturingPackageDiagnosticSeverity.Blocker, diagnostic.Severity);
        Assert.Equal("missing-required-artifact", diagnostic.Code);
        Assert.Equal(ManufacturingPackageArtifactKind.Drill, diagnostic.ArtifactKind);
        Assert.Equal(ManufacturingPackageHandoffTarget.GerberPrototype, diagnostic.HandoffTarget);
    }

    [Fact]
    public void Build_ReportsMissingBomBlockerForAssemblyTarget()
    {
        ManufacturingPackageBundle bundle = ManufacturingPackageBundleBuilder.Build(
            ManufacturingPackageBundleRequest.Create(
                "board-a",
                "project-42",
                GeneratedAt,
                ManufacturingPackageHandoffTarget.Assembly,
                [
                    Artifact(ManufacturingPackageArtifactKind.Gerber, "gerbers/top.gbr", "G04 top*"),
                    Artifact(ManufacturingPackageArtifactKind.Drill, "drill/project.drl", "M48\n"),
                    Artifact(ManufacturingPackageArtifactKind.PickAndPlace, "assembly/project-pnp.csv", "ref,x,y\nU1,1,2\n")
                ]));

        ManufacturingPackageDiagnostic diagnostic = Assert.Single(bundle.Diagnostics);
        Assert.False(bundle.IsReadyForExport);
        Assert.Equal(ManufacturingPackageDiagnosticSeverity.Blocker, diagnostic.Severity);
        Assert.Equal(ManufacturingPackageArtifactKind.BillOfMaterials, diagnostic.ArtifactKind);
        Assert.Equal(ManufacturingPackageHandoffTarget.Assembly, diagnostic.HandoffTarget);
    }

    [Fact]
    public void Build_IncludesOptionalCricutAndPasteWithoutBlockingPrototypeTarget()
    {
        ManufacturingPackageBundle bundle = ManufacturingPackageBundleBuilder.Build(
            ManufacturingPackageBundleRequest.Create(
                "board-a",
                "project-42",
                GeneratedAt,
                ManufacturingPackageHandoffTarget.GerberPrototype,
                [
                    Artifact(ManufacturingPackageArtifactKind.Gerber, "gerbers/top.gbr", "G04 top*"),
                    Artifact(ManufacturingPackageArtifactKind.Drill, "drill/project.drl", "M48\n"),
                    Artifact(ManufacturingPackageArtifactKind.CricutArtwork, "cricut/solder-mask.svg", "<svg />"),
                    Artifact(ManufacturingPackageArtifactKind.SolderPaste, "paste/top.gtp", "G04 paste*")
                ]));

        Assert.True(bundle.IsReadyForExport);
        Assert.Empty(bundle.Diagnostics);
        Assert.Contains(bundle.Artifacts, artifact => artifact.Kind == ManufacturingPackageArtifactKind.CricutArtwork);
        Assert.Contains(bundle.Artifacts, artifact => artifact.Kind == ManufacturingPackageArtifactKind.SolderPaste);
    }

    [Fact]
    public void Build_ProducesStableHashesForEquivalentContent()
    {
        ManufacturingPackageBundle first = ManufacturingPackageBundleBuilder.Build(PrototypeRequest());
        ManufacturingPackageBundle second = ManufacturingPackageBundleBuilder.Build(PrototypeRequest());

        Assert.Equal(
            first.Artifacts.Select(artifact => artifact.Checksum.Value).ToArray(),
            second.Artifacts.Select(artifact => artifact.Checksum.Value).ToArray());
        Assert.Equal(first.ManifestJson, second.ManifestJson);
    }

    [Fact]
    public void Build_OrdersArtifactsDeterministicallyByKindAndPath()
    {
        ManufacturingPackageBundle bundle = ManufacturingPackageBundleBuilder.Build(
            ManufacturingPackageBundleRequest.Create(
                "board-a",
                "project-42",
                GeneratedAt,
                ManufacturingPackageHandoffTarget.Assembly,
                [
                    Artifact(ManufacturingPackageArtifactKind.PickAndPlace, "assembly/project-pnp.csv", "ref,x,y\nU1,1,2\n"),
                    Artifact(ManufacturingPackageArtifactKind.Gerber, "gerbers/top.gbr", "G04 top*"),
                    Artifact(ManufacturingPackageArtifactKind.Gerber, "gerbers/bottom.gbr", "G04 bottom*"),
                    Artifact(ManufacturingPackageArtifactKind.BillOfMaterials, "bom/project.csv", "ref,mpn\nU1,STM32\n"),
                    Artifact(ManufacturingPackageArtifactKind.Drill, "drill/project.drl", "M48\n")
                ]));

        Assert.Equal(
            [
                "gerbers/bottom.gbr",
                "gerbers/top.gbr",
                "drill/project.drl",
                "bom/project.csv",
                "assembly/project-pnp.csv",
                "manifest/manufacturing-package.json"
            ],
            bundle.Artifacts.Select(artifact => artifact.RelativePath.Value).ToArray());
    }

    private static ManufacturingPackageBundleRequest PrototypeRequest()
    {
        return ManufacturingPackageBundleRequest.Create(
            "board-a",
            "project-42",
            GeneratedAt,
            ManufacturingPackageHandoffTarget.GerberPrototype,
            [
                Artifact(ManufacturingPackageArtifactKind.Gerber, "gerbers/top.gbr", "G04 top*"),
                Artifact(ManufacturingPackageArtifactKind.Drill, "drill/project.drl", "M48\n")
            ]);
    }

    private static ManufacturingPackageArtifactSource Artifact(
        ManufacturingPackageArtifactKind kind,
        string relativePath,
        string content)
    {
        return ManufacturingPackageArtifactSource.Create(kind, relativePath, content);
    }
}
