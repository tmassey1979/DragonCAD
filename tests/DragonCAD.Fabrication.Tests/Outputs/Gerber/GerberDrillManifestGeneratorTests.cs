using DragonCAD.Fabrication.Outputs;
using DragonCAD.Fabrication.Outputs.Gerber;

namespace DragonCAD.Fabrication.Tests.Outputs.Gerber;

public sealed class GerberDrillManifestGeneratorTests
{
    [Fact]
    public void Generate_CreatesDeterministicGerberAndDrillEntriesForTwoLayerBoard()
    {
        GerberDrillManifestRequest request = new(
            ProjectName: "Motor Controller Rev A",
            BoardName: "Main Board",
            Revision: "A",
            Layers:
            [
                GerberBoardLayer.BottomCopper("Bottom Copper"),
                GerberBoardLayer.TopSilkscreen("Top Legend"),
                GerberBoardLayer.TopCopper("Top Copper"),
                GerberBoardLayer.BottomSolderMask("Bottom Mask"),
                GerberBoardLayer.TopSolderMask("Top Mask")
            ],
            ViaCount: 3,
            ThroughHolePadCount: 12);

        GerberDrillManifest manifest = GerberDrillManifestGenerator.Generate(request);

        Assert.Equal("Motor Controller Rev A", manifest.ProjectName);
        Assert.Equal("Main Board", manifest.BoardName);
        Assert.Equal("A", manifest.Revision);
        Assert.Equal(2, manifest.Metadata.CopperLayerCount);
        Assert.True(manifest.Metadata.HasDrillData);
        Assert.Equal(
            [
                "gerbers/motor-controller-rev-a.GTL",
                "gerbers/motor-controller-rev-a.GBL",
                "gerbers/motor-controller-rev-a.GTS",
                "gerbers/motor-controller-rev-a.GBS",
                "gerbers/motor-controller-rev-a.GTO",
                "drill/motor-controller-rev-a.drl"
            ],
            manifest.Entries.Select(entry => entry.RelativePath.Value).ToArray());
        Assert.Equal(ManufacturingFileRole.Drill, manifest.Entries[^1].Role);
        Assert.Equal("pending:drill-motor-controller-rev-a-plated", manifest.Entries[^1].Checksum.Value);
    }

    [Fact]
    public void Generate_ProjectsToExistingManufacturingManifestContract()
    {
        GerberDrillManifest manifest = GerberDrillManifestGenerator.Generate(
            new GerberDrillManifestRequest(
                ProjectName: "Sensor Node",
                BoardName: "Controller",
                Revision: "1",
                Layers:
                [
                    GerberBoardLayer.TopCopper("Top Copper"),
                    GerberBoardLayer.BottomCopper("Bottom Copper")
                ],
                ViaCount: 1,
                ThroughHolePadCount: 0));

        ManufacturingOutputManifest manufacturingManifest = manifest.ToManufacturingOutputManifest();

        Assert.Equal(
            [
                ManufacturingFileRole.Gerber,
                ManufacturingFileRole.Gerber,
                ManufacturingFileRole.Drill
            ],
            manufacturingManifest.Entries.Select(entry => entry.Role).ToArray());
        Assert.Equal(
            [
                "gerbers/sensor-node.GBL",
                "gerbers/sensor-node.GTL",
                "drill/sensor-node.drl"
            ],
            manufacturingManifest.Entries.Select(entry => entry.RelativePath.Value).ToArray());
    }

    [Fact]
    public void Generate_OmitsDrillEntryWhenBoardHasNoDrillRelevantFeatures()
    {
        GerberDrillManifest manifest = GerberDrillManifestGenerator.Generate(
            new GerberDrillManifestRequest(
                ProjectName: "Flex Sensor",
                BoardName: "Flex",
                Revision: "",
                Layers:
                [
                    GerberBoardLayer.TopCopper("Top Copper")
                ],
                ViaCount: 0,
                ThroughHolePadCount: 0));

        Assert.False(manifest.Metadata.HasDrillData);
        Assert.Single(manifest.Entries);
        Assert.Equal("gerbers/flex-sensor.GTL", manifest.Entries[0].RelativePath.Value);
    }

    [Fact]
    public void Generate_UsesEmptyBoardMetadataWhenNoLayersOrDrillsExist()
    {
        GerberDrillManifest manifest = GerberDrillManifestGenerator.Generate(
            new GerberDrillManifestRequest(
                ProjectName: "Untitled",
                BoardName: "",
                Revision: "",
                Layers: [],
                ViaCount: 0,
                ThroughHolePadCount: 0));

        Assert.Empty(manifest.Entries);
        Assert.Equal(0, manifest.Metadata.CopperLayerCount);
        Assert.Equal(0, manifest.Metadata.OutputFileCount);
        Assert.False(manifest.Metadata.HasDrillData);
    }

    [Fact]
    public void Generate_RejectsAmbiguousProjectNames()
    {
        Assert.Throws<ArgumentException>(
            () => GerberDrillManifestGenerator.Generate(
                new GerberDrillManifestRequest(
                    ProjectName: "",
                    BoardName: "Board",
                    Revision: "",
                    Layers: [],
                    ViaCount: 0,
                    ThroughHolePadCount: 0)));
    }
}
