using DragonCAD.Core.Routing.PushShove;

namespace DragonCAD.Core.Tests.Routing.PushShove;

public sealed class PushShoveRoutingTests
{
    [Fact]
    public void ObstacleIndexReturnsFixedAndMovableObstaclesByLayerAndClearanceEnvelope()
    {
        PushShoveObstacleIndex index = new(
        [
            Obstacle("pad:1", PushShoveObstacleKind.Fixed, layer: 1, left: 0, top: 0, right: 10, bottom: 10),
            Obstacle("trace:2", PushShoveObstacleKind.Movable, layer: 1, left: 20, top: 0, right: 30, bottom: 10),
            Obstacle("pad:bottom", PushShoveObstacleKind.Fixed, layer: 2, left: 0, top: 0, right: 10, bottom: 10)
        ]);

        IReadOnlyList<PushShoveObstacle> hits = index.FindIntersections(new PushShoveEnvelope(11, 0, 19, 10), layer: 1, clearance: 1);

        Assert.Equal(["pad:1", "trace:2"], hits.Select(hit => hit.Id));
        Assert.Equal([PushShoveObstacleKind.Fixed, PushShoveObstacleKind.Movable], hits.Select(hit => hit.Kind));
    }

    [Fact]
    public void ProposalModelsRouteShapeClearancesAndMovableObstacleActions()
    {
        PushShoveRouteProposal proposal = new(
            Id: "proposal:a",
            Net: Net("net:a"),
            Segments:
            [
                new PushShoveRouteSegment(new PushShovePoint(0, 0), new PushShovePoint(10, 0), Layer: 1, Width: 2),
                new PushShoveRouteSegment(new PushShovePoint(10, 0), new PushShovePoint(10, 10), Layer: 2, Width: 2)
            ],
            Vias: [new PushShoveVia(new PushShovePoint(10, 0), FromLayer: 1, ToLayer: 2, Drill: 1, Diameter: 3)],
            RequiredClearances: [new PushShoveClearance("net:a", "obstacle:b", 4)],
            MovedObstacles: [new PushShoveObstacleMove("trace:2", new PushShoveVector(0, 5))]);

        Assert.Equal("proposal:a", proposal.Id);
        Assert.Equal("net:a", proposal.Net.Id);
        Assert.Equal([1, 2], proposal.Segments.Select(segment => segment.Layer));
        Assert.Equal("obstacle:b", proposal.RequiredClearances.Single().OtherId);
        Assert.Equal(new PushShoveVector(0, 5), proposal.MovedObstacles.Single().Offset);
    }

    [Theory]
    [InlineData(PushShoveRejectionReason.Blocked, "PS001")]
    [InlineData(PushShoveRejectionReason.NeedsVia, "PS002")]
    [InlineData(PushShoveRejectionReason.ClearanceConflict, "PS003")]
    [InlineData(PushShoveRejectionReason.NoPath, "PS004")]
    public void RejectedProposalDiagnosticsExposeDistinctReasonCodes(PushShoveRejectionReason reason, string expectedCode)
    {
        PushShoveRejectedProposal rejection = PushShoveRejectedProposal.Create(
            Net("net:a"),
            reason,
            objectIds: ["pad:1"],
            message: "Cannot route.");

        Assert.Equal(expectedCode, rejection.Diagnostic.Code);
        Assert.Equal(reason, rejection.Diagnostic.Reason);
        Assert.Equal(["pad:1"], rejection.Diagnostic.ObjectIds);
    }

    [Fact]
    public void FakePlannerReturnsDeterministicQueuedResults()
    {
        PushShoveRouteRequest request = new(
            Net("net:a"),
            Start: new PushShovePoint(0, 0),
            End: new PushShovePoint(10, 0),
            Layer: 1,
            TraceWidth: 2,
            Obstacles: new PushShoveObstacleIndex([]),
            MinimumClearance: 1);
        PushShoveRouteProposal proposal = PushShoveRouteProposal.Direct("proposal:1", request);
        PushShoveRejectedProposal rejection = PushShoveRejectedProposal.Create(
            request.Net,
            PushShoveRejectionReason.NoPath,
            objectIds: [],
            message: "No path.");
        FakePushShovePlanner planner = new([proposal, rejection]);

        Assert.Same(proposal, planner.Plan(request));
        Assert.Same(rejection, planner.Plan(request));
        Assert.Same(rejection, planner.Plan(request));
        Assert.Equal([request, request, request], planner.Requests);
    }

    private static PushShoveNet Net(string id) =>
        new(id, ClearanceClass: "default", AllowedLayers: [1, 2]);

    private static PushShoveObstacle Obstacle(
        string id,
        PushShoveObstacleKind kind,
        int layer,
        long left,
        long top,
        long right,
        long bottom) =>
        new(id, kind, new PushShoveEnvelope(left, top, right, bottom), layer);
}
