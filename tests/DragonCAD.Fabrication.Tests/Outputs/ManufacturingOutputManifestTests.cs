using DragonCAD.Fabrication.Outputs;

namespace DragonCAD.Fabrication.Tests.Outputs;

public sealed class ManufacturingOutputManifestTests
{
    [Fact]
    public void RelativePath_NormalizesSeparatorsAndRejectsUnsafePaths()
    {
        ManufacturingRelativePath path = ManufacturingRelativePath.Create("gerbers\\top-copper.gbr");

        Assert.Equal("gerbers/top-copper.gbr", path.Value);
        Assert.Throws<ArgumentException>(() => ManufacturingRelativePath.Create("C:/temp/top.gbr"));
        Assert.Throws<ArgumentException>(() => ManufacturingRelativePath.Create("../top.gbr"));
        Assert.Throws<ArgumentException>(() => ManufacturingRelativePath.Create("gerbers/../top.gbr"));
        Assert.Throws<ArgumentException>(() => ManufacturingRelativePath.Create("gerbers//top.gbr"));
    }

    [Theory]
    [InlineData("sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef")]
    [InlineData("pending:gerber-top-copper")]
    public void ChecksumPlaceholder_AcceptsDeterministicChecksumOrPendingToken(string value)
    {
        ManufacturingChecksum checksum = ManufacturingChecksum.Create(value);

        Assert.Equal(value, checksum.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("sha256:1234")]
    [InlineData("sha1:0123456789abcdef0123456789abcdef01234567")]
    [InlineData("pending:")]
    [InlineData("pending:top copper")]
    public void ChecksumPlaceholder_RejectsAmbiguousValues(string value)
    {
        Assert.Throws<ArgumentException>(() => ManufacturingChecksum.Create(value));
    }

    [Fact]
    public void Create_SortsEntriesByRoleThenRelativePath()
    {
        ManufacturingOutputManifest manifest = ManufacturingOutputManifest.Create(
        [
            new ManufacturingOutputEntry(
                ManufacturingFileRole.BillOfMaterials,
                ManufacturingRelativePath.Create("bom/project.csv"),
                ManufacturingChecksum.Create("pending:bom")),
            new ManufacturingOutputEntry(
                ManufacturingFileRole.Gerber,
                ManufacturingRelativePath.Create("gerbers/top-copper.gbr"),
                ManufacturingChecksum.Create("pending:top")),
            new ManufacturingOutputEntry(
                ManufacturingFileRole.Gerber,
                ManufacturingRelativePath.Create("gerbers/bottom-copper.gbr"),
                ManufacturingChecksum.Create("pending:bottom")),
            new ManufacturingOutputEntry(
                ManufacturingFileRole.PickAndPlace,
                ManufacturingRelativePath.Create("assembly/placements.csv"),
                ManufacturingChecksum.Create("pending:pnp"))
        ]);

        Assert.Equal(
            [
                ManufacturingFileRole.Gerber,
                ManufacturingFileRole.Gerber,
                ManufacturingFileRole.BillOfMaterials,
                ManufacturingFileRole.PickAndPlace
            ],
            manifest.Entries.Select(entry => entry.Role).ToArray());
        Assert.Equal(
            [
                "gerbers/bottom-copper.gbr",
                "gerbers/top-copper.gbr",
                "bom/project.csv",
                "assembly/placements.csv"
            ],
            manifest.Entries.Select(entry => entry.RelativePath.Value).ToArray());
    }
}
