using DragonCAD.Core.Geometry;

namespace DragonCAD.Core.Boards.Polygons;

public sealed class CopperPourFillPlanner
{
    public CopperPourFillPlan Plan(
        IEnumerable<CopperPourPolygon> polygons,
        CopperPourFillPlannerOptions options)
    {
        ArgumentNullException.ThrowIfNull(polygons);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.VisibleLayers);

        IndexedPolygon[] indexedPolygons = polygons
            .Select((polygon, index) => new IndexedPolygon(index, polygon))
            .ToArray();

        CopperPourBlocker[] blockers = indexedPolygons
            .SelectMany(CreateBlockers)
            .OrderBy(blocker => blocker.Code, StringComparer.Ordinal)
            .ThenBy(blocker => blocker.Message, StringComparer.Ordinal)
            .ThenBy(blocker => blocker.PolygonIndex)
            .ToArray();

        HashSet<int> blockedPolygonIndexes = blockers
            .Select(blocker => blocker.PolygonIndex)
            .ToHashSet();

        CopperPourFilledRegion[] filledRegions = indexedPolygons
            .Where(indexed => !blockedPolygonIndexes.Contains(indexed.Index))
            .Where(indexed => options.VisibleLayers.Contains(indexed.Polygon.Layer))
            .Select(indexed => CreateFilledRegion(indexed.Polygon))
            .OrderByDescending(region => region.Rank)
            .ThenBy(region => region.Layer, StringComparer.Ordinal)
            .ThenBy(region => region.Net, StringComparer.Ordinal)
            .ThenBy(region => CreateOutlineSortKey(region.Outline), StringComparer.Ordinal)
            .ToArray();

        return new CopperPourFillPlan(filledRegions, blockers);

        IEnumerable<CopperPourBlocker> CreateBlockers(IndexedPolygon indexed)
        {
            CopperPourPolygon polygon = indexed.Polygon;

            if (!options.VisibleLayers.Contains(polygon.Layer))
            {
                yield return new CopperPourBlocker(
                    indexed.Index,
                    CopperPourBlockerCodes.HiddenLayer,
                    $"Copper pour polygon layer '{polygon.Layer}' is hidden.");
            }

            if (string.IsNullOrWhiteSpace(polygon.Net))
            {
                yield return new CopperPourBlocker(
                    indexed.Index,
                    CopperPourBlockerCodes.MissingNet,
                    "Copper pour polygon requires a net.");
            }

            if (!IsValidOutline(polygon.Outline))
            {
                yield return new CopperPourBlocker(
                    indexed.Index,
                    CopperPourBlockerCodes.InvalidPolygon,
                    "Copper pour polygon outline must contain at least three distinct vertices with non-zero area.");
            }
        }
    }

    private static CopperPourFilledRegion CreateFilledRegion(CopperPourPolygon polygon) =>
        new(
            polygon.Layer,
            polygon.Net?.Trim() ?? string.Empty,
            polygon.Outline.ToArray(),
            polygon.Clearance,
            polygon.ThermalsEnabled,
            polygon.Isolate,
            polygon.Rank);

    private static bool IsValidOutline(IReadOnlyList<CadPoint>? outline)
    {
        if (outline is null || outline.Distinct().Take(3).Count() < 3)
        {
            return false;
        }

        return CalculateTwiceSignedArea(outline) != 0;
    }

    private static long CalculateTwiceSignedArea(IReadOnlyList<CadPoint> outline)
    {
        long area = 0;

        for (int index = 0; index < outline.Count; index++)
        {
            CadPoint current = outline[index];
            CadPoint next = outline[(index + 1) % outline.Count];
            area = checked(area + (current.X * next.Y) - (next.X * current.Y));
        }

        return area;
    }

    private static string CreateOutlineSortKey(IReadOnlyList<CadPoint> outline) =>
        string.Join(";", outline.Select(point => $"{point.X},{point.Y}"));

    private sealed record IndexedPolygon(int Index, CopperPourPolygon Polygon);
}
