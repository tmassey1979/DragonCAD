using DragonCAD.Fabrication.Bom;

namespace DragonCAD.Fabrication.Tests.Bom;

public sealed class BomCsvFormatterTests
{
    [Fact]
    public void Format_WritesDeterministicHeaderAndAggregatedRows()
    {
        BomComponent[] components =
        [
            new("R10", "RES-10K", "10k", "0603"),
            new("C1", "CAP-100N", "100nF", "0603"),
            new("R1", "RES-10K", "10k", "0603")
        ];

        string csv = BomCsvFormatter.Format(BomAggregator.Aggregate(components));

        Assert.Equal(
            "References,Quantity,Part,Value,Package\r\n" +
            "C1,1,CAP-100N,100nF,0603\r\n" +
            "R1 R10,2,RES-10K,10k,0603\r\n",
            csv);
    }

    [Fact]
    public void Format_EscapesCsvCellsWithCommasQuotesAndNewLines()
    {
        BomLine[] lines =
        [
            new(
                new BomPartIdentity("IC,USB", "Controller \"C\"", "QFN\n32"),
                ["U1", "U2"])
        ];

        string csv = BomCsvFormatter.Format(lines);

        Assert.Equal(
            "References,Quantity,Part,Value,Package\r\n" +
            "U1 U2,2,\"IC,USB\",\"Controller \"\"C\"\"\",\"QFN\n32\"\r\n",
            csv);
    }

    [Fact]
    public void Format_RejectsNullLines()
    {
        Assert.Throws<ArgumentNullException>(() => BomCsvFormatter.Format(null!));
    }
}
