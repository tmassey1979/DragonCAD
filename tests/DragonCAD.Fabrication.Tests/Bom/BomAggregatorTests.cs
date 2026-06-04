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
            new(" U2 ", " MCP1700 ", " 3.3V ", " SOT-23 ", " MCP1700T-3302E/TT ", "Place near MCU"),
            new("U1", "MCP1700", "3.3V", "SOT-23", "MCP1700T-3302E/TT", " Place near MCU ")
        ];

        BomLine[] lines = BomAggregator.Aggregate(components);

        BomLine line = Assert.Single(lines);
        Assert.Equal(new BomPartIdentity("MCP1700", "3.3V", "SOT-23", "MCP1700T-3302E/TT"), line.Identity);
        Assert.Equal(["U1", "U2"], line.References);
        Assert.Equal(2, line.Quantity);
        Assert.Equal("Place near MCU", line.Notes);
    }

    [Fact]
    public void Aggregate_OrdersLinesDeterministicallyByPartThenValueThenPackage()
    {
        BomComponent[] components =
        [
            new("U3", "REG-5V", "5V", "TO-220", "LM7805CT"),
            new("U1", "MCU", "ATMEGA328P", "TQFP-32", "ATMEGA328P-AU"),
            new("U4", "REG-5V", "5V", "TO-220", "UA7805CKCS"),
            new("U2", "REG-3V3", "3.3V", "SOT-223", "LD1117S33CTR")
        ];

        BomLine[] lines = BomAggregator.Aggregate(components);

        Assert.Equal(
            [
                new BomPartIdentity("MCU", "ATMEGA328P", "TQFP-32", "ATMEGA328P-AU"),
                new BomPartIdentity("REG-3V3", "3.3V", "SOT-223", "LD1117S33CTR"),
                new BomPartIdentity("REG-5V", "5V", "TO-220", "LM7805CT"),
                new BomPartIdentity("REG-5V", "5V", "TO-220", "UA7805CKCS")
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
        Assert.Equal(string.Empty, line.Notes);
    }

    [Fact]
    public void Aggregate_CombinesDistinctNotesDeterministically()
    {
        BomComponent[] components =
        [
            new("R2", "RES-10K", "10k", "0603", "RC0603FR-0710KL", "DNP"),
            new("R1", "RES-10K", "10k", "0603", "RC0603FR-0710KL", "Alternate value allowed"),
            new("R3", "RES-10K", "10k", "0603", "RC0603FR-0710KL", "DNP")
        ];

        BomLine line = Assert.Single(BomAggregator.Aggregate(components));

        Assert.Equal(["R1", "R2", "R3"], line.References);
        Assert.Equal("Alternate value allowed; DNP", line.Notes);
    }
}
