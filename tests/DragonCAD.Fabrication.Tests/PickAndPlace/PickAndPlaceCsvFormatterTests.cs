using DragonCAD.Fabrication.PickAndPlace;

namespace DragonCAD.Fabrication.Tests.PickAndPlace;

public sealed class PickAndPlaceCsvFormatterTests
{
    [Fact]
    public void Format_WritesDeterministicPlacementRows()
    {
        ComponentPlacementRow[] rows =
        [
            new("U2", "MCU", "QFN-32", 2_540_000, 5_080_000, 90, PlacementSide.Top),
            new("C1", "100nF", "0603", -1_270_000, 0, 270, PlacementSide.Bottom),
            new("U1", "Regulator", "TO-220", 0, 1_270_000, 0, PlacementSide.Top)
        ];

        string csv = PickAndPlaceCsvFormatter.Format(rows);

        Assert.Equal(
            "Reference,Value,Package,X,Y,Rotation,Side\r\n" +
            "C1,100nF,0603,-1270000,0,270,Bottom\r\n" +
            "U1,Regulator,TO-220,0,1270000,0,Top\r\n" +
            "U2,MCU,QFN-32,2540000,5080000,90,Top\r\n",
            csv);
    }

    [Fact]
    public void Format_EscapesCsvCellsWithCommasQuotesAndNewLines()
    {
        ComponentPlacementRow[] rows =
        [
            new("U1", "USB, Controller \"A\"", "QFN\n32", 0, 0, 180, PlacementSide.Top)
        ];

        string csv = PickAndPlaceCsvFormatter.Format(rows);

        Assert.Equal(
            "Reference,Value,Package,X,Y,Rotation,Side\r\n" +
            "U1,\"USB, Controller \"\"A\"\"\",\"QFN\n32\",0,0,180,Top\r\n",
            csv);
    }

    [Fact]
    public void ComponentPlacementRow_RejectsMissingReference()
    {
        Assert.Throws<ArgumentException>(() => new ComponentPlacementRow(" ", "10k", "0603", 0, 0, 0, PlacementSide.Top));
    }

    [Theory]
    [InlineData(-90)]
    [InlineData(45)]
    [InlineData(360)]
    public void ComponentPlacementRow_RejectsUnsupportedRotation(int rotationDegrees)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ComponentPlacementRow("R1", "10k", "0603", 0, 0, rotationDegrees, PlacementSide.Top));
    }

    [Fact]
    public void Format_RejectsNullRows()
    {
        Assert.Throws<ArgumentNullException>(() => PickAndPlaceCsvFormatter.Format(null!));
    }
}
