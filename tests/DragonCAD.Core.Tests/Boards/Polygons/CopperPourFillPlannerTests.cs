using DragonCAD.Core.Boards.Polygons;
using DragonCAD.Core.Geometry;

namespace DragonCAD.Core.Tests.Boards.Polygons;

public sealed class CopperPourFillPlannerTests
{
    [Fact]
    public void CopperPourPolygonStoresPourMetadata()
    {
        CopperPourPolygon polygon = CreatePolygon(
            layer: "F.Cu",
            net: "GND",
            outline:
            [
                new CadPoint(0, 0),
                new CadPoint(10, 0),
                new CadPoint(10, 10),
                new CadPoint(0, 10),
            ],
            clearance: 6,
            thermalsEnabled: true,
            isolate: 12,
            rank: 4,
            state: CopperPourState.Filled);

        Assert.Equal("F.Cu", polygon.Layer);
        Assert.Equal("GND", polygon.Net);
        Assert.Equal(
            [new CadPoint(0, 0), new CadPoint(10, 0), new CadPoint(10, 10), new CadPoint(0, 10)],
            polygon.Outline);
        Assert.Equal(6, polygon.Clearance);
        Assert.True(polygon.ThermalsEnabled);
        Assert.Equal(12, polygon.Isolate);
        Assert.Equal(4, polygon.Rank);
        Assert.Equal(CopperPourState.Filled, polygon.State);
    }

    [Fact]
    public void ValidPolygonProducesFilledRegionWithClearanceMetadata()
    {
        CopperPourPolygon polygon = CreatePolygon(
            layer: "F.Cu",
            net: "GND",
            clearance: 8,
            thermalsEnabled: false,
            isolate: 14);

        CopperPourFillPlan plan = new CopperPourFillPlanner().Plan(
            [polygon],
            new CopperPourFillPlannerOptions(["F.Cu"]));

        CopperPourFilledRegion region = Assert.Single(plan.FilledRegions);
        Assert.Empty(plan.Blockers);
        Assert.Equal("F.Cu", region.Layer);
        Assert.Equal("GND", region.Net);
        Assert.Equal(polygon.Outline, region.Outline);
        Assert.Equal(8, region.Clearance);
        Assert.False(region.ThermalsEnabled);
        Assert.Equal(14, region.Isolate);
        Assert.Equal(polygon.Rank, region.Rank);
    }

    [Fact]
    public void InvalidOutlineReportsBlocker()
    {
        CopperPourPolygon polygon = CreatePolygon(
            outline:
            [
                new CadPoint(0, 0),
                new CadPoint(10, 0),
            ]);

        CopperPourFillPlan plan = new CopperPourFillPlanner().Plan(
            [polygon],
            new CopperPourFillPlannerOptions(["F.Cu"]));

        CopperPourBlocker blocker = Assert.Single(plan.Blockers);
        Assert.Empty(plan.FilledRegions);
        Assert.Equal(CopperPourBlockerCodes.InvalidPolygon, blocker.Code);
        Assert.Equal(0, blocker.PolygonIndex);
    }

    [Fact]
    public void MissingNetReportsBlocker()
    {
        CopperPourPolygon polygon = CreatePolygon(net: " ");

        CopperPourFillPlan plan = new CopperPourFillPlanner().Plan(
            [polygon],
            new CopperPourFillPlannerOptions(["F.Cu"]));

        CopperPourBlocker blocker = Assert.Single(plan.Blockers);
        Assert.Empty(plan.FilledRegions);
        Assert.Equal(CopperPourBlockerCodes.MissingNet, blocker.Code);
        Assert.Equal(0, blocker.PolygonIndex);
    }

    [Fact]
    public void HiddenLayerReportsBlocker()
    {
        CopperPourPolygon polygon = CreatePolygon(layer: "B.Cu");

        CopperPourFillPlan plan = new CopperPourFillPlanner().Plan(
            [polygon],
            new CopperPourFillPlannerOptions(["F.Cu"]));

        CopperPourBlocker blocker = Assert.Single(plan.Blockers);
        Assert.Empty(plan.FilledRegions);
        Assert.Equal(CopperPourBlockerCodes.HiddenLayer, blocker.Code);
        Assert.Equal(0, blocker.PolygonIndex);
    }

    [Fact]
    public void FilledRegionsUseDeterministicRankOrdering()
    {
        CopperPourPolygon lowerRank = CreatePolygon(layer: "F.Cu", net: "GND", rank: 1);
        CopperPourPolygon higherRank = CreatePolygon(layer: "B.Cu", net: "VBUS", rank: 3);
        CopperPourPolygon sameRankFirstByLayer = CreatePolygon(layer: "In1.Cu", net: "AGND", rank: 3);

        CopperPourFillPlan plan = new CopperPourFillPlanner().Plan(
            [lowerRank, higherRank, sameRankFirstByLayer],
            new CopperPourFillPlannerOptions(["B.Cu", "F.Cu", "In1.Cu"]));

        Assert.Equal(
            ["B.Cu:VBUS:3", "In1.Cu:AGND:3", "F.Cu:GND:1"],
            plan.FilledRegions.Select(region => $"{region.Layer}:{region.Net}:{region.Rank}"));
    }

    [Fact]
    public void PlanOutputIsDeterministicAcrossEquivalentInputOrder()
    {
        CopperPourPolygon first = CreatePolygon(layer: "F.Cu", net: "GND", rank: 2);
        CopperPourPolygon second = CreatePolygon(layer: "B.Cu", net: "VBUS", rank: 5);
        CopperPourPolygon invalid = CreatePolygon(layer: "In1.Cu", outline: [new CadPoint(0, 0)]);

        CopperPourFillPlanner planner = new();
        CopperPourFillPlannerOptions options = new(["F.Cu", "B.Cu", "In1.Cu"]);

        CopperPourFillPlan firstPlan = planner.Plan([first, second, invalid], options);
        CopperPourFillPlan secondPlan = planner.Plan([invalid, second, first], options);

        Assert.Equal(
            firstPlan.FilledRegions.Select(ToRegionSignature),
            secondPlan.FilledRegions.Select(ToRegionSignature));
        Assert.Equal(
            firstPlan.Blockers.Select(blocker => blocker.Code),
            secondPlan.Blockers.Select(blocker => blocker.Code));
    }

    private static CopperPourPolygon CreatePolygon(
        string layer = "F.Cu",
        string? net = "GND",
        IReadOnlyList<CadPoint>? outline = null,
        long clearance = 6,
        bool thermalsEnabled = true,
        long isolate = 10,
        int rank = 1,
        CopperPourState state = CopperPourState.Unfilled) =>
        new(
            layer,
            net,
            outline ??
            [
                new CadPoint(0, 0),
                new CadPoint(20, 0),
                new CadPoint(20, 20),
                new CadPoint(0, 20),
            ],
            clearance,
            thermalsEnabled,
            isolate,
            rank,
            state);

    private static string ToRegionSignature(CopperPourFilledRegion region) =>
        $"{region.Layer}:{region.Net}:{region.Rank}:{string.Join(";", region.Outline.Select(point => $"{point.X},{point.Y}"))}";
}
