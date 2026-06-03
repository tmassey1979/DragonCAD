using DragonCAD.Sourcing.ProjectPlanning;

namespace DragonCAD.Sourcing.Tests.ProjectPlanning;

public sealed class ProjectFabricationHandoffPlannerTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 3, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Plan_ConsumesCurrentGerberDrillBomAndPickAndPlaceArtifacts()
    {
        ProjectPlanningResult result = ProjectDerivedBomPlanner.Plan(
            Request(
                artifacts:
                [
                    Artifact(ProjectFabricationArtifactRole.Gerber, "fabrication/gerbers.zip", sourceRevision: "rev-a"),
                    Artifact(ProjectFabricationArtifactRole.Drill, "fabrication/drill.zip", sourceRevision: "rev-a"),
                    Artifact(ProjectFabricationArtifactRole.BillOfMaterials, "fabrication/bom.csv", sourceRevision: "rev-a"),
                    Artifact(ProjectFabricationArtifactRole.PickAndPlace, "fabrication/pnp.csv", sourceRevision: "rev-a")
                ]),
            Now);

        Assert.True(result.FabricationReadiness.IsReady);
        Assert.Empty(result.FabricationReadiness.Blockers);
        Assert.Equal(
            [
                "fabrication/gerbers.zip",
                "fabrication/drill.zip",
                "fabrication/bom.csv",
                "fabrication/pnp.csv"
            ],
            result.FabricationReadiness.RequiredArtifacts.SelectMany(artifact => artifact.Files).Select(file => file.RelativePath));
    }

    [Fact]
    public void Plan_BlocksFabricationPacketWhenRequiredArtifactsAreMissing()
    {
        ProjectPlanningResult result = ProjectDerivedBomPlanner.Plan(
            Request(
                artifacts:
                [
                    Artifact(ProjectFabricationArtifactRole.Gerber, "fabrication/gerbers.zip", sourceRevision: "rev-a"),
                    Artifact(ProjectFabricationArtifactRole.Drill, "fabrication/drill.zip", sourceRevision: "rev-a")
                ]),
            Now);

        Assert.False(result.FabricationReadiness.IsReady);
        Assert.Equal(
            [
                ProjectFabricationReadinessBlockerCodes.MissingBillOfMaterials,
                ProjectFabricationReadinessBlockerCodes.MissingPickAndPlace
            ],
            result.FabricationReadiness.Blockers.Select(blocker => blocker.Code));
        Assert.All(
            result.FabricationReadiness.RequiredArtifacts.Where(artifact => artifact.Role is ProjectFabricationArtifactRole.BillOfMaterials or ProjectFabricationArtifactRole.PickAndPlace),
            artifact => Assert.Empty(artifact.Files));
    }

    [Fact]
    public void Plan_BlocksFabricationPacketWhenArtifactsComeFromStaleDesignRevision()
    {
        ProjectPlanningResult result = ProjectDerivedBomPlanner.Plan(
            Request(
                artifacts:
                [
                    Artifact(ProjectFabricationArtifactRole.Gerber, "fabrication/gerbers.zip", sourceRevision: "rev-a"),
                    Artifact(ProjectFabricationArtifactRole.Drill, "fabrication/drill.zip", sourceRevision: "rev-old"),
                    Artifact(ProjectFabricationArtifactRole.BillOfMaterials, "fabrication/bom.csv", sourceRevision: "rev-a"),
                    Artifact(ProjectFabricationArtifactRole.PickAndPlace, "fabrication/pnp.csv", sourceRevision: "rev-a")
                ]),
            Now);

        Assert.False(result.FabricationReadiness.IsReady);
        ProjectFabricationReadinessBlocker blocker = Assert.Single(result.FabricationReadiness.Blockers);
        Assert.Equal(ProjectFabricationReadinessBlockerCodes.StaleArtifact, blocker.Code);
        Assert.Equal(ProjectFabricationArtifactRole.Drill, blocker.Role);
        Assert.Equal("fabrication/drill.zip", blocker.RelativePath);
    }

    private static ProjectPlanningRequest Request(IReadOnlyList<ProjectFabricationArtifact> artifacts)
    {
        return new ProjectPlanningRequest(
            DesignRevision: "rev-a",
            Components: [new ProjectDesignComponent("U1", "timer", "555", IsPlaced: true)],
            PackageSelections: [new ProjectPackageSelection("U1", "SOIC-8", "NE555DR", DoNotSubstitute: false, Alternates: [])],
            VendorOffers: [new ProjectVendorOffer("NE555DR", "Mouser", "595-NE555DR", Stock: 10, Money.Usd(0.25m), Now.AddDays(-1), Now.AddDays(5))],
            Artifacts: artifacts);
    }

    private static ProjectFabricationArtifact Artifact(
        ProjectFabricationArtifactRole role,
        string relativePath,
        string sourceRevision)
    {
        return new ProjectFabricationArtifact(
            role,
            relativePath,
            sourceRevision,
            GeneratedAt: Now.AddMinutes(-5));
    }
}
