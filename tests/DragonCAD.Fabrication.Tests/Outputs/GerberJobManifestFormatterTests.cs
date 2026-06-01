using DragonCAD.Fabrication.Outputs;

namespace DragonCAD.Fabrication.Tests.Outputs;

public sealed class GerberJobManifestFormatterTests
{
    [Fact]
    public void FormatCsv_EmitsDeterministicFileSummaryRows()
    {
        ManufacturingOutputManifest manifest = CreateUnsortedManifest();

        string csv = GerberJobManifestFormatter.FormatCsv(manifest);

        Assert.Equal(
            "Role,RelativePath,Checksum\r\n"
            + "Gerber,gerbers/bottom-copper.gbr,pending:bottom\r\n"
            + "Gerber,gerbers/top-copper.gbr,pending:top\r\n"
            + "BillOfMaterials,\"bom/main,board.csv\",pending:bom\r\n"
            + "PickAndPlace,assembly/placements.csv,pending:pnp\r\n",
            csv);
    }

    [Fact]
    public void FormatJson_EmitsDeterministicCountsAndFiles()
    {
        ManufacturingOutputManifest manifest = CreateUnsortedManifest();

        string json = GerberJobManifestFormatter.FormatJson(manifest);

        Assert.Equal(
            "{\"fileCount\":4,\"roles\":[{\"role\":\"Gerber\",\"count\":2},{\"role\":\"BillOfMaterials\",\"count\":1},{\"role\":\"PickAndPlace\",\"count\":1}],\"files\":[{\"role\":\"Gerber\",\"relativePath\":\"gerbers/bottom-copper.gbr\",\"checksum\":\"pending:bottom\"},{\"role\":\"Gerber\",\"relativePath\":\"gerbers/top-copper.gbr\",\"checksum\":\"pending:top\"},{\"role\":\"BillOfMaterials\",\"relativePath\":\"bom/main,board.csv\",\"checksum\":\"pending:bom\"},{\"role\":\"PickAndPlace\",\"relativePath\":\"assembly/placements.csv\",\"checksum\":\"pending:pnp\"}]}",
            json);
    }

    [Fact]
    public void Formatters_RejectNullManifest()
    {
        Assert.Throws<ArgumentNullException>(() => GerberJobManifestFormatter.FormatCsv(null!));
        Assert.Throws<ArgumentNullException>(() => GerberJobManifestFormatter.FormatJson(null!));
    }

    private static ManufacturingOutputManifest CreateUnsortedManifest()
    {
        return ManufacturingOutputManifest.Create(
        [
            new ManufacturingOutputEntry(
                ManufacturingFileRole.PickAndPlace,
                ManufacturingRelativePath.Create("assembly/placements.csv"),
                ManufacturingChecksum.Create("pending:pnp")),
            new ManufacturingOutputEntry(
                ManufacturingFileRole.Gerber,
                ManufacturingRelativePath.Create("gerbers/top-copper.gbr"),
                ManufacturingChecksum.Create("pending:top")),
            new ManufacturingOutputEntry(
                ManufacturingFileRole.BillOfMaterials,
                ManufacturingRelativePath.Create("bom/main,board.csv"),
                ManufacturingChecksum.Create("pending:bom")),
            new ManufacturingOutputEntry(
                ManufacturingFileRole.Gerber,
                ManufacturingRelativePath.Create("gerbers/bottom-copper.gbr"),
                ManufacturingChecksum.Create("pending:bottom"))
        ]);
    }
}
