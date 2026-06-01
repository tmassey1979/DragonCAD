using DragonCAD.Fabrication.Ordering;
using DragonCAD.Fabrication.Ordering.Review;
using DragonCAD.Fabrication.Outputs;

namespace DragonCAD.Fabrication.Tests.Ordering.Review;

public sealed class FabricationOrderReviewPacketTests
{
    [Fact]
    public void Create_SummarizesPrototypeProviderAsManualReviewOnly()
    {
        ManufacturingOutputManifest manifest = ManufacturingOutputManifest.Create(
        [
            Entry(ManufacturingFileRole.Gerber, "gerbers/project.gbr"),
            Entry(ManufacturingFileRole.Drill, "drill/project.drl")
        ]);

        FabricationOrderReviewPacket packet = FabricationOrderReviewPacket.Create(
            FabricationOrderingProviders.OshParkPrototype,
            FabricationOrderMode.PrototypeBoard,
            manifest);

        Assert.Equal(FabricationProviderReviewKind.Prototype, packet.ProviderKind);
        Assert.Equal("OSH Park", packet.ProviderDisplayName);
        Assert.Equal(FabricationProviderSubmissionPolicy.ManualReviewOnly, packet.SubmissionPolicy);
        Assert.All(packet.RequiredArtifacts, artifact => Assert.True(artifact.IsPresent));
        Assert.Empty(packet.ReviewWarnings);
    }

    [Fact]
    public void Create_SummarizesProductionProviderWithRequiredArtifactsAndWarnings()
    {
        ManufacturingOutputManifest manifest = ManufacturingOutputManifest.Create(
        [
            Entry(ManufacturingFileRole.Gerber, "gerbers/project.gbr"),
            Entry(ManufacturingFileRole.Drill, "drill/project.drl"),
            Entry(ManufacturingFileRole.BillOfMaterials, "bom/project.csv")
        ]);

        FabricationOrderReviewPacket packet = FabricationOrderReviewPacket.Create(
            FabricationOrderingProviders.PcbCartProduction,
            FabricationOrderMode.AssembledBoard,
            manifest);

        Assert.Equal(FabricationProviderReviewKind.Production, packet.ProviderKind);
        Assert.Equal(
            [
                ManufacturingFileRole.Gerber,
                ManufacturingFileRole.Drill,
                ManufacturingFileRole.BillOfMaterials,
                ManufacturingFileRole.PickAndPlace
            ],
            packet.RequiredArtifacts.Select(artifact => artifact.Role));
        FabricationReviewArtifact pickAndPlace = Assert.Single(
            packet.RequiredArtifacts,
            artifact => artifact.Role == ManufacturingFileRole.PickAndPlace);
        Assert.False(pickAndPlace.IsPresent);

        FabricationReviewWarning warning = Assert.Single(packet.ReviewWarnings);
        Assert.Equal("missing-required-artifact", warning.Code);
        Assert.Equal(ManufacturingFileRole.PickAndPlace, warning.FileRole);
        Assert.Contains("PickAndPlace", warning.Message, StringComparison.Ordinal);
        Assert.Equal(FabricationProviderSubmissionPolicy.ManualReviewOnly, packet.SubmissionPolicy);
    }

    private static ManufacturingOutputEntry Entry(ManufacturingFileRole role, string path)
    {
        return new ManufacturingOutputEntry(
            role,
            ManufacturingRelativePath.Create(path),
            ManufacturingChecksum.Create($"pending:{Path.GetFileNameWithoutExtension(path)}"));
    }
}
