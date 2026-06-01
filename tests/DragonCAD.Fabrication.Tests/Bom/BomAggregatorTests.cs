using DragonCAD.Fabrication.Bom;

namespace DragonCAD.Fabrication.Tests.Bom;

public sealed class BomAggregatorTests
{
    [Fact]
    public void Aggregate_GroupsReferencesByPartValueAndPackage()
    {
        BomComponent[] components =
        [
            new("R10", "RES-10K", "10k", "0603"),
            new("R1", "RES-10K", "10k", "0603"),
            new("C1", "CAP-100N", "100nF", "0603"),
            new("R2", "RES-10K", "10k", "0805")
        ];

        BomLine[] lines = BomAggregator.Aggregate(components);

        Assert.Collection(
            lines,
            line =>
            {
                Assert.Equal(new BomPartIdentity("CAP-100N", "100nF", "0603"), line.Identity);
                Assert.Equal(["C1"], line.References);
                Assert.Equal(1, line.Quantity);
            },
            line =>
            {
                Assert.Equal(new BomPartIdentity("RES-10K", "10k", "0603"), line.Identity);
                Assert.Equal(["R1", "R10"], line.References);
                Assert.Equal(2, line.Quantity);
            },
            line =>
            {
                Assert.Equal(new BomPartIdentity("RES-10K", "10k", "0805"), line.Identity);
                Assert.Equal(["R2"], line.References);
                Assert.Equal(1, line.Quantity);
            });
    }

    [Fact]
    public void Aggregate_TrimsIdentityFieldsAndUsesCanonicalGrouping()
    {
        BomComponent[] components =
        [
            new(" U2 ", " MCP1700 ", " 3.3V ", " SOT-23 "),
            new("U1", "MCP1700", "3.3V", "SOT-23")
        ];

        BomLine[] lines = BomAggregator.Aggregate(components);

        BomLine line = Assert.Single(lines);
        Assert.Equal(new BomPartIdentity("MCP1700", "3.3V", "SOT-23"), line.Identity);
        Assert.Equal(["U1", "U2"], line.References);
        Assert.Equal(2, line.Quantity);
    }

    [Fact]
    public void Aggregate_OrdersLinesDeterministicallyByPartThenValueThenPackage()
    {
        BomComponent[] components =
        [
            new("U3", "REG-5V", "5V", "TO-220"),
            new("U1", "MCU", "ATMEGA328P", "TQFP-32"),
            new("U2", "REG-3V3", "3.3V", "SOT-223")
        ];

        BomLine[] lines = BomAggregator.Aggregate(components);

        Assert.Equal(
            [
                new BomPartIdentity("MCU", "ATMEGA328P", "TQFP-32"),
                new BomPartIdentity("REG-3V3", "3.3V", "SOT-223"),
                new BomPartIdentity("REG-5V", "5V", "TO-220")
            ],
            lines.Select(line => line.Identity).ToArray());
    }

    [Fact]
    public void Aggregate_AllowsMissingMetadataButKeepsReferences()
    {
        BomComponent[] components =
        [
            new("TP2", null, null, null),
            new("TP1", "", "", "")
        ];

        BomLine[] lines = BomAggregator.Aggregate(components);

        BomLine line = Assert.Single(lines);
        Assert.Equal(BomPartIdentity.Unspecified, line.Identity);
        Assert.Equal(["TP1", "TP2"], line.References);
        Assert.Equal(2, line.Quantity);
    }
}
