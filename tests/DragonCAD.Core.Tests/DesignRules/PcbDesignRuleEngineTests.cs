using DragonCAD.Core.DesignRules;

namespace DragonCAD.Core.Tests.DesignRules;

public sealed class PcbDesignRuleEngineTests
{
    [Fact]
    public void AnalyzeReportsTraceWidthViolationWithDiagnosticDetails()
    {
        PcbDrcDocument board = Board(
            traces:
            [
                Trace("trace:1", "net:a", layer: 1, start: Point(10, 10), end: Point(30, 10), width: 0.12)
            ]);

        PcbDrcResult result = PcbDesignRuleEngine.Analyze(board, DrcRuleProfile.TwoLayerPrototype());

        PcbDrcDiagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(PcbDrcRuleIds.MinimumTraceWidth, diagnostic.RuleId);
        Assert.Equal(PcbDrcSeverity.Error, diagnostic.Severity);
        Assert.Equal(["trace:1"], diagnostic.ObjectIds);
        Assert.Equal(0.12, diagnostic.MeasuredValue);
        Assert.Equal(0.20, diagnostic.RequiredValue);
        Assert.Equal(Point(20, 10), diagnostic.Location);
        Assert.True(result.HasErrors);
    }

    [Fact]
    public void AnalyzeReportsTraceAndPadClearanceViolations()
    {
        PcbDrcDocument board = Board(
            traces:
            [
                Trace("trace:a", "net:a", layer: 1, start: Point(10, 10), end: Point(40, 10), width: 0.20),
                Trace("trace:b", "net:b", layer: 1, start: Point(10, 10.30), end: Point(40, 10.30), width: 0.20)
            ],
            pads:
            [
                Pad("pad:a", "net:a", layer: 1, center: Point(60, 10), diameter: 1.00),
                Pad("pad:b", "net:b", layer: 1, center: Point(61.10, 10), diameter: 1.00)
            ]);

        PcbDrcResult result = PcbDesignRuleEngine.Analyze(board, DrcRuleProfile.TwoLayerPrototype());

        Assert.Equal(
            [
                "DRC004|pad:a,pad:b|0.100|0.200|60.550,10.000",
                "DRC005|trace:a,trace:b|0.100|0.200|25.000,10.150"
            ],
            result.Diagnostics.Select(FormatDiagnostic));
    }

    [Fact]
    public void AnalyzeReportsViaDrillAndDiameterViolations()
    {
        PcbDrcDocument board = Board(
            vias:
            [
                Via("via:1", "net:a", center: Point(10, 10), drill: 0.20, diameter: 0.45)
            ]);

        PcbDrcResult result = PcbDesignRuleEngine.Analyze(board, DrcRuleProfile.TwoLayerPrototype());

        Assert.Equal(
            [
                "DRC002|via:1|0.200|0.300|10.000,10.000",
                "DRC003|via:1|0.450|0.600|10.000,10.000"
            ],
            result.Diagnostics.Select(FormatDiagnostic));
    }

    [Fact]
    public void AnalyzeReportsBoardOutlineClearanceViolation()
    {
        PcbDrcDocument board = Board(
            traces:
            [
                Trace("trace:edge", "net:a", layer: 1, start: Point(0.30, 5), end: Point(8, 5), width: 0.20)
            ]);

        PcbDrcDiagnostic diagnostic = Assert.Single(
            PcbDesignRuleEngine.Analyze(board, DrcRuleProfile.TwoLayerPrototype()).Diagnostics);

        Assert.Equal(PcbDrcRuleIds.BoardOutlineClearance, diagnostic.RuleId);
        Assert.Equal(["trace:edge"], diagnostic.ObjectIds);
        Assert.Equal(0.20, diagnostic.MeasuredValue);
        Assert.Equal(0.25, diagnostic.RequiredValue);
        Assert.Equal(Point(0.30, 5), diagnostic.Location);
    }

    [Fact]
    public void AnalyzeReturnsNoDiagnosticsForCompliantBoard()
    {
        PcbDrcDocument board = Board(
            traces:
            [
                Trace("trace:a", "net:a", layer: 1, start: Point(10, 10), end: Point(30, 10), width: 0.25),
                Trace("trace:b", "net:b", layer: 1, start: Point(10, 12), end: Point(30, 12), width: 0.25)
            ],
            vias:
            [
                Via("via:a", "net:a", center: Point(35, 10), drill: 0.35, diameter: 0.70)
            ],
            pads:
            [
                Pad("pad:a", "net:a", layer: 1, center: Point(45, 10), diameter: 1.20),
                Pad("pad:b", "net:b", layer: 1, center: Point(48, 10), diameter: 1.20)
            ]);

        PcbDrcResult result = PcbDesignRuleEngine.Analyze(board, DrcRuleProfile.TwoLayerPrototype());

        Assert.Empty(result.Diagnostics);
        Assert.False(result.HasErrors);
    }

    [Fact]
    public void AnalyzeOrdersDiagnosticsDeterministically()
    {
        PcbDrcDocument board = Board(
            traces:
            [
                Trace("trace:z", "net:z", layer: 1, start: Point(10, 10), end: Point(30, 10), width: 0.10),
                Trace("trace:a", "net:a", layer: 1, start: Point(10, 10.25), end: Point(30, 10.25), width: 0.10)
            ],
            vias:
            [
                Via("via:b", "net:b", center: Point(40, 10), drill: 0.10, diameter: 0.40),
                Via("via:a", "net:a", center: Point(42, 10), drill: 0.10, diameter: 0.40)
            ]);

        PcbDrcResult first = PcbDesignRuleEngine.Analyze(board, DrcRuleProfile.TwoLayerPrototype());
        PcbDrcResult second = PcbDesignRuleEngine.Analyze(board, DrcRuleProfile.TwoLayerPrototype());

        string[] firstOrder = first.Diagnostics.Select(FormatDiagnostic).ToArray();
        Assert.Equal(firstOrder, second.Diagnostics.Select(FormatDiagnostic));
        Assert.Equal(
            [
                "DRC001|trace:a|0.100|0.200|20.000,10.250",
                "DRC001|trace:z|0.100|0.200|20.000,10.000",
                "DRC002|via:a|0.100|0.300|42.000,10.000",
                "DRC002|via:b|0.100|0.300|40.000,10.000",
                "DRC003|via:a|0.400|0.600|42.000,10.000",
                "DRC003|via:b|0.400|0.600|40.000,10.000",
                "DRC005|trace:a,trace:z|0.150|0.200|20.000,10.125"
            ],
            firstOrder);
    }

    [Fact]
    public void RuleProfilesExposeTwoLayerPrototypeAndFourLayerProductionDefaults()
    {
        DrcRuleProfile prototype = DrcRuleProfile.TwoLayerPrototype();
        DrcRuleProfile production = DrcRuleProfile.FourLayerProduction();

        Assert.Equal("two-layer-prototype", prototype.Id);
        Assert.Equal(2, prototype.LayerCount);
        Assert.Equal(0.20, prototype.MinimumTraceWidth);
        Assert.Equal(0.30, prototype.MinimumViaDrill);
        Assert.Equal(0.60, prototype.MinimumViaDiameter);
        Assert.Equal(0.20, prototype.MinimumCopperClearance);
        Assert.Equal(0.25, prototype.MinimumBoardOutlineClearance);

        Assert.Equal("four-layer-production", production.Id);
        Assert.Equal(4, production.LayerCount);
        Assert.Equal(0.15, production.MinimumTraceWidth);
        Assert.Equal(0.25, production.MinimumViaDrill);
        Assert.Equal(0.50, production.MinimumViaDiameter);
        Assert.Equal(0.15, production.MinimumCopperClearance);
        Assert.Equal(0.20, production.MinimumBoardOutlineClearance);
    }

    private static PcbDrcDocument Board(
        IReadOnlyList<PcbDrcTrace>? traces = null,
        IReadOnlyList<PcbDrcVia>? vias = null,
        IReadOnlyList<PcbDrcPad>? pads = null) =>
        new(
            traces ?? [],
            vias ?? [],
            pads ?? [],
            PcbDrcBoardOutline.Rectangle(minX: 0, minY: 0, maxX: 100, maxY: 80));

    private static PcbDrcTrace Trace(string id, string netId, int layer, DrcPoint start, DrcPoint end, double width) =>
        new(id, netId, layer, start, end, width);

    private static PcbDrcVia Via(string id, string netId, DrcPoint center, double drill, double diameter) =>
        new(id, netId, center, drill, diameter);

    private static PcbDrcPad Pad(string id, string netId, int layer, DrcPoint center, double diameter) =>
        new(id, netId, layer, center, diameter);

    private static DrcPoint Point(double x, double y) => new(x, y);

    private static string FormatDiagnostic(PcbDrcDiagnostic diagnostic) =>
        string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"{diagnostic.RuleId}|{string.Join(",", diagnostic.ObjectIds)}|{diagnostic.MeasuredValue:0.000}|{diagnostic.RequiredValue:0.000}|{diagnostic.Location.X:0.000},{diagnostic.Location.Y:0.000}");
}
