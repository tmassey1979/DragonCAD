using DragonCAD.Fabrication.Bom;

namespace DragonCAD.Fabrication.Tests.Bom;

public sealed class BomCsvFormatterTests
{
    [Fact]
    public void Format_WritesDeterministicHeaderAndAggregatedRows()
    {
        BomComponent[] components =
        [
            new("R10", "RES-10K", "10k", "0603", "RC0603FR-0710KL", "Matched pair"),
            new("C1", "CAP-100N", "100nF", "0603", "CL10B104KB8NNNC"),
            new("R1", "RES-10K", "10k", "0603", "RC0603FR-0710KL", "Matched pair")
        ];

        string csv = BomCsvFormatter.Format(BomAggregator.Aggregate(components));

        Assert.Equal(
            "References,Quantity,Part,Value,Package,ManufacturerPartNumber,Notes\r\n" +
            "C1,1,CAP-100N,100nF,0603,CL10B104KB8NNNC,\r\n" +
            "R1 R10,2,RES-10K,10k,0603,RC0603FR-0710KL,Matched pair\r\n",
            csv);
    }

    [Fact]
    public void Format_EscapesCsvCellsWithCommasQuotesAndNewLines()
    {
        BomLine[] lines =
        [
            new(
                new BomPartIdentity("IC,USB", "Controller \"C\"", "QFN\n32", "MFR, \"USB\"\r\n123"),
                ["U1", "U2"],
                "Use socket, inspect \"pins\"\nsecond line")
        ];

        string csv = BomCsvFormatter.Format(lines);

        Assert.Equal(
            "References,Quantity,Part,Value,Package,ManufacturerPartNumber,Notes\r\n" +
            "U1 U2,2,\"IC,USB\",\"Controller \"\"C\"\"\",\"QFN\n32\",\"MFR, \"\"USB\"\"\r\n123\",\"Use socket, inspect \"\"pins\"\"\nsecond line\"\r\n",
            csv);
    }

    [Fact]
    public void Format_WritesEmptyOptionalFieldsForMissingManufacturerPartNumberAndNotes()
    {
        BomLine[] lines =
        [
            new(new BomPartIdentity("TP", string.Empty, string.Empty), ["TP1"])
        ];

        string csv = BomCsvFormatter.Format(lines);

        Assert.Equal(
            "References,Quantity,Part,Value,Package,ManufacturerPartNumber,Notes\r\n" +
            "TP1,1,TP,,,,\r\n",
            csv);
    }

    [Fact]
    public void Format_RejectsNullLines()
    {
        Assert.Throws<ArgumentNullException>(() => BomCsvFormatter.Format(null!));
    }
}
