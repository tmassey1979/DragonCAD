namespace DragonCAD.Core.Routing.PushShove;

public sealed record PushShoveNet(
    string Id,
    string ClearanceClass,
    IReadOnlyList<int> AllowedLayers);

public readonly record struct PushShovePoint(long X, long Y);

public readonly record struct PushShoveVector(long X, long Y);

public readonly record struct PushShoveEnvelope
{
    public PushShoveEnvelope(long left, long top, long right, long bottom)
    {
        if (right < left)
        {
            throw new ArgumentException("Right must be greater than or equal to left.", nameof(right));
        }

        if (bottom < top)
        {
            throw new ArgumentException("Bottom must be greater than or equal to top.", nameof(bottom));
        }

        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    public long Left { get; }

    public long Top { get; }

    public long Right { get; }

    public long Bottom { get; }

    public PushShoveEnvelope Inflate(long clearance) =>
        new(Left - clearance, Top - clearance, Right + clearance, Bottom + clearance);

    public bool Intersects(PushShoveEnvelope other) =>
        Left <= other.Right &&
        Right >= other.Left &&
        Top <= other.Bottom &&
        Bottom >= other.Top;
}

public enum PushShoveObstacleKind
{
    Fixed,
    Movable
}

public sealed record PushShoveObstacle(
    string Id,
    PushShoveObstacleKind Kind,
    PushShoveEnvelope Envelope,
    int Layer);

public sealed class PushShoveObstacleIndex
{
    private readonly IReadOnlyList<PushShoveObstacle> obstacles;

    public PushShoveObstacleIndex(IReadOnlyList<PushShoveObstacle> obstacles)
    {
        ArgumentNullException.ThrowIfNull(obstacles);
        this.obstacles = obstacles.ToArray();
    }

    public IReadOnlyList<PushShoveObstacle> Obstacles => obstacles;

    public IReadOnlyList<PushShoveObstacle> FindIntersections(PushShoveEnvelope envelope, int layer, long clearance) =>
        obstacles
            .Where(obstacle => obstacle.Layer == layer)
            .Where(obstacle => obstacle.Envelope.Inflate(clearance).Intersects(envelope))
            .ToArray();
}

public sealed record PushShoveClearance(
    string NetId,
    string OtherId,
    long MinimumDistance);

public sealed record PushShoveRouteSegment(
    PushShovePoint Start,
    PushShovePoint End,
    int Layer,
    long Width);

public sealed record PushShoveVia(
    PushShovePoint Center,
    int FromLayer,
    int ToLayer,
    long Drill,
    long Diameter);

public sealed record PushShoveObstacleMove(
    string ObstacleId,
    PushShoveVector Offset);

public interface IPushShovePlanResult
{
    PushShoveNet Net { get; }
}

public sealed record PushShoveRouteProposal(
    string Id,
    PushShoveNet Net,
    IReadOnlyList<PushShoveRouteSegment> Segments,
    IReadOnlyList<PushShoveVia> Vias,
    IReadOnlyList<PushShoveClearance> RequiredClearances,
    IReadOnlyList<PushShoveObstacleMove> MovedObstacles) : IPushShovePlanResult
{
    public static PushShoveRouteProposal Direct(string id, PushShoveRouteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new PushShoveRouteProposal(
            id,
            request.Net,
            Segments: [new PushShoveRouteSegment(request.Start, request.End, request.Layer, request.TraceWidth)],
            Vias: [],
            RequiredClearances: [],
            MovedObstacles: []);
    }
}

public enum PushShoveRejectionReason
{
    Blocked,
    NeedsVia,
    ClearanceConflict,
    NoPath
}

public sealed record PushShoveDiagnostic(
    string Code,
    PushShoveRejectionReason Reason,
    IReadOnlyList<string> ObjectIds,
    string Message);

public sealed record PushShoveRejectedProposal(
    PushShoveNet Net,
    PushShoveDiagnostic Diagnostic) : IPushShovePlanResult
{
    public static PushShoveRejectedProposal Create(
        PushShoveNet net,
        PushShoveRejectionReason reason,
        IReadOnlyList<string> objectIds,
        string message) =>
        new(net, new PushShoveDiagnostic(CodeFor(reason), reason, objectIds.ToArray(), message));

    private static string CodeFor(PushShoveRejectionReason reason) =>
        reason switch
        {
            PushShoveRejectionReason.Blocked => "PS001",
            PushShoveRejectionReason.NeedsVia => "PS002",
            PushShoveRejectionReason.ClearanceConflict => "PS003",
            PushShoveRejectionReason.NoPath => "PS004",
            _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unknown push-shove rejection reason.")
        };
}

public sealed record PushShoveRouteRequest(
    PushShoveNet Net,
    PushShovePoint Start,
    PushShovePoint End,
    int Layer,
    long TraceWidth,
    PushShoveObstacleIndex Obstacles,
    long MinimumClearance);

public interface IPushShovePlanner
{
    IPushShovePlanResult Plan(PushShoveRouteRequest request);
}

public sealed class FakePushShovePlanner : IPushShovePlanner
{
    private readonly IReadOnlyList<IPushShovePlanResult> results;
    private readonly List<PushShoveRouteRequest> requests = [];
    private int nextResultIndex;

    public FakePushShovePlanner(IReadOnlyList<IPushShovePlanResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        if (results.Count == 0)
        {
            throw new ArgumentException("At least one fake planner result is required.", nameof(results));
        }

        this.results = results.ToArray();
    }

    public IReadOnlyList<PushShoveRouteRequest> Requests => requests;

    public IPushShovePlanResult Plan(PushShoveRouteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        requests.Add(request);
        int resultIndex = Math.Min(nextResultIndex, results.Count - 1);
        nextResultIndex++;

        return results[resultIndex];
    }
}
