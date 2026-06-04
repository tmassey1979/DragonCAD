using DragonCAD.Core.Geometry;

namespace DragonCAD.Core.Boards.Polygons;

public enum CopperPourState
{
    Unfilled,
    Filled,
    Blocked
}

public sealed record CopperPourPolygon(
    string Layer,
    string? Net,
    IReadOnlyList<CadPoint> Outline,
    long Clearance,
    bool ThermalsEnabled,
    long Isolate,
    int Rank,
    CopperPourState State);

public sealed record CopperPourFillPlannerOptions(IReadOnlyCollection<string> VisibleLayers);

public static class CopperPourBlockerCodes
{
    public const string HiddenLayer = "HiddenLayer";
    public const string InvalidPolygon = "InvalidPolygon";
    public const string MissingNet = "MissingNet";
}

public sealed record CopperPourBlocker(int PolygonIndex, string Code, string Message);

public sealed record CopperPourFilledRegion(
    string Layer,
    string Net,
    IReadOnlyList<CadPoint> Outline,
    long Clearance,
    bool ThermalsEnabled,
    long Isolate,
    int Rank);

public sealed record CopperPourFillPlan(
    IReadOnlyList<CopperPourFilledRegion> FilledRegions,
    IReadOnlyList<CopperPourBlocker> Blockers);
