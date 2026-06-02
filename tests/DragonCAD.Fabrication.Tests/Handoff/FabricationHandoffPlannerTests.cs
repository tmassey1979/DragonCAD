using DragonCAD.Fabrication.Handoff;
using DragonCAD.Fabrication.Outputs;

namespace DragonCAD.Fabrication.Tests.Handoff;

public sealed class FabricationHandoffPlannerTests
{
    [Fact]
    public void Plan_CreatesReadyOshParkPrototypeHandoffWithDeterministicHashes()
    {
        FabricationHandoffPlan plan = FabricationHandoffPlanner.Plan(
            OshParkRequest(
                CompleteBoardOutputs(),
                boardOutlinePresent: true,
                widthMillimeters: 42.25m,
                heightMillimeters: 17.5m,
                warnings:
                [
                    new FabricationHandoffWarning("silkscreen-near-edge", "Silkscreen is near the board edge.")
                ],
                acceptedWarningCodes: ["silkscreen-near-edge"]));

        Assert.True(plan.IsActionEnabled);
        Assert.Empty(plan.Blockers);
        Assert.Equal(FabricationHandoffProvider.OshPark, plan.Provider);
        Assert.Equal("OSH Park", plan.ProviderDisplayName);
        Assert.Equal(FabricationHandoffActionKind.PrepareManualHandoff, plan.Action.Kind);
        Assert.False(plan.Action.AllowsUpload);
        Assert.Null(plan.Action.UploadEndpoint);
        Assert.Equal(
            [
                ManufacturingFileRole.Gerber,
                ManufacturingFileRole.Drill
            ],
            plan.RequiredArtifacts.Select(artifact => artifact.Role));
        Assert.All(plan.RequiredArtifacts, artifact => Assert.True(artifact.IsPresent));
        Assert.Equal(
            "sha256:ee6757bc72b16a947e7d43100821f717c11e1135042bacca6df346eabacbd6b5",
            plan.PackageHash.Value);
    }

    [Fact]
    public void Plan_BlocksOshParkPrototypeWhenDrillArtifactIsMissing()
    {
        FabricationHandoffPlan plan = FabricationHandoffPlanner.Plan(
            OshParkRequest(
                ManufacturingOutputManifest.Create(
                [
                    Entry(ManufacturingFileRole.Gerber, "gerbers/top-copper.gbr", "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")
                ]),
                boardOutlinePresent: true,
                widthMillimeters: 42.25m,
                heightMillimeters: 17.5m));

        Assert.False(plan.IsActionEnabled);
        FabricationHandoffBlocker blocker = Assert.Single(plan.Blockers);
        Assert.Equal(FabricationHandoffBlockerCodes.MissingDrill, blocker.Code);
        Assert.Equal(ManufacturingFileRole.Drill, blocker.FileRole);
        Assert.Contains("drill", blocker.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Plan_BlocksWarningsUntilTheyAreAccepted()
    {
        FabricationHandoffWarning warning = new("soldermask-swell", "Soldermask swell exceeds the prototype preference.");
        FabricationHandoffRequest unaccepted = OshParkRequest(
            CompleteBoardOutputs(),
            boardOutlinePresent: true,
            widthMillimeters: 42.25m,
            heightMillimeters: 17.5m,
            warnings: [warning]);

        FabricationHandoffPlan blockedPlan = FabricationHandoffPlanner.Plan(unaccepted);

        Assert.False(blockedPlan.IsActionEnabled);
        FabricationHandoffBlocker blocker = Assert.Single(blockedPlan.Blockers);
        Assert.Equal(FabricationHandoffBlockerCodes.UnacceptedWarning, blocker.Code);
        Assert.Equal("soldermask-swell", blocker.WarningCode);

        FabricationHandoffPlan acceptedPlan = FabricationHandoffPlanner.Plan(
            unaccepted with { AcceptedWarningCodes = ["soldermask-swell"] });

        Assert.True(acceptedPlan.IsActionEnabled);
        Assert.Empty(acceptedPlan.Blockers);
        Assert.All(acceptedPlan.Warnings, planWarning => Assert.True(planWarning.IsAccepted));
    }

    [Fact]
    public void Plan_CreatesReadyPcbCartProductionHandoffWithAssemblyRequirements()
    {
        FabricationHandoffPlan plan = FabricationHandoffPlanner.Plan(
            PcbCartRequest(
                CompleteAssemblyOutputs(),
                stackup: "4-layer FR4 1.6mm ENIG",
                quantity: 25,
                assemblySide: FabricationAssemblySide.Top));

        Assert.True(plan.IsActionEnabled);
        Assert.Empty(plan.Blockers);
        Assert.Equal(FabricationHandoffProvider.PcbCart, plan.Provider);
        Assert.Equal("PCBCart", plan.ProviderDisplayName);
        Assert.Equal(
            [
                ManufacturingFileRole.Gerber,
                ManufacturingFileRole.Drill,
                ManufacturingFileRole.BillOfMaterials,
                ManufacturingFileRole.PickAndPlace
            ],
            plan.RequiredArtifacts.Select(artifact => artifact.Role));
        Assert.All(plan.RequiredArtifacts, artifact => Assert.True(artifact.IsPresent));
        Assert.Equal("4-layer FR4 1.6mm ENIG", plan.Stackup);
        Assert.Equal(25, plan.Quantity);
        Assert.Equal(FabricationAssemblySide.Top, plan.AssemblySide);
        Assert.Equal(
            "sha256:39df7ed5f9da6fb0d133e4f79c25fa1b47f1251dcd8c85826ea2ab53a43964f4",
            plan.PackageHash.Value);
    }

    [Fact]
    public void Plan_NeverCreatesUploadActionOrEndpoint()
    {
        FabricationHandoffPlan plan = FabricationHandoffPlanner.Plan(
            PcbCartRequest(
                CompleteAssemblyOutputs(),
                stackup: "4-layer FR4 1.6mm ENIG",
                quantity: 25,
                assemblySide: FabricationAssemblySide.Both));

        Assert.Equal(FabricationHandoffActionKind.PrepareManualHandoff, plan.Action.Kind);
        Assert.False(plan.Action.AllowsUpload);
        Assert.Null(plan.Action.UploadEndpoint);
        Assert.DoesNotContain("upload", plan.Action.Label, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("checkout", plan.Action.Label, StringComparison.OrdinalIgnoreCase);
    }

    private static FabricationHandoffRequest OshParkRequest(
        ManufacturingOutputManifest manifest,
        bool boardOutlinePresent,
        decimal widthMillimeters,
        decimal heightMillimeters,
        IReadOnlyList<FabricationHandoffWarning>? warnings = null,
        IReadOnlyList<string>? acceptedWarningCodes = null)
    {
        return new FabricationHandoffRequest(
            FabricationHandoffProvider.OshPark,
            manifest,
            new FabricationBoardHandoffDetails(boardOutlinePresent, widthMillimeters, heightMillimeters),
            productionDetails: null,
            warnings ?? [],
            acceptedWarningCodes ?? []);
    }

    private static FabricationHandoffRequest PcbCartRequest(
        ManufacturingOutputManifest manifest,
        string stackup,
        int quantity,
        FabricationAssemblySide assemblySide)
    {
        return new FabricationHandoffRequest(
            FabricationHandoffProvider.PcbCart,
            manifest,
            new FabricationBoardHandoffDetails(BoardOutlinePresent: true, WidthMillimeters: 42.25m, HeightMillimeters: 17.5m),
            new FabricationProductionHandoffDetails(stackup, quantity, assemblySide),
            warnings: [],
            acceptedWarningCodes: []);
    }

    private static ManufacturingOutputManifest CompleteBoardOutputs()
    {
        return ManufacturingOutputManifest.Create(
        [
            Entry(ManufacturingFileRole.Gerber, "gerbers/top-copper.gbr", "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
            Entry(ManufacturingFileRole.Gerber, "gerbers/bottom-copper.gbr", "sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"),
            Entry(ManufacturingFileRole.Drill, "drill/plated.drl", "sha256:cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc")
        ]);
    }

    private static ManufacturingOutputManifest CompleteAssemblyOutputs()
    {
        return ManufacturingOutputManifest.Create(
        [
            Entry(ManufacturingFileRole.Gerber, "gerbers/top-copper.gbr", "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
            Entry(ManufacturingFileRole.Gerber, "gerbers/bottom-copper.gbr", "sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"),
            Entry(ManufacturingFileRole.Drill, "drill/plated.drl", "sha256:cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc"),
            Entry(ManufacturingFileRole.BillOfMaterials, "bom/assembly.csv", "sha256:dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd"),
            Entry(ManufacturingFileRole.PickAndPlace, "assembly/top-pos.csv", "sha256:eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee")
        ]);
    }

    private static ManufacturingOutputEntry Entry(ManufacturingFileRole role, string path, string checksum)
    {
        return new ManufacturingOutputEntry(
            role,
            ManufacturingRelativePath.Create(path),
            ManufacturingChecksum.Create(checksum));
    }
}
