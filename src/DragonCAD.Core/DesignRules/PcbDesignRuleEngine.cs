namespace DragonCAD.Core.DesignRules;

public static class PcbDesignRuleEngine
{
    private const double Tolerance = 0.0000001;

    public static PcbDrcResult Analyze(PcbDrcDocument document, DrcRuleProfile profile)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(profile);

        List<PcbDrcDiagnostic> diagnostics = [];

        diagnostics.AddRange(FindTraceWidthViolations(document.Traces, profile));
        diagnostics.AddRange(FindViaDrillViolations(document.Vias, profile));
        diagnostics.AddRange(FindViaDiameterViolations(document.Vias, profile));
        diagnostics.AddRange(FindCopperClearanceViolations(document, profile));
        diagnostics.AddRange(FindBoardOutlineClearanceViolations(document, profile));

        return new PcbDrcResult(
            diagnostics
                .OrderBy(diagnostic => diagnostic.RuleId, StringComparer.Ordinal)
                .ThenBy(diagnostic => diagnostic.PrimaryObjectId, StringComparer.Ordinal)
                .ThenBy(diagnostic => diagnostic.SecondaryObjectId, StringComparer.Ordinal)
                .ThenBy(diagnostic => diagnostic.Location.X)
                .ThenBy(diagnostic => diagnostic.Location.Y)
                .ToArray());
    }

    private static IEnumerable<PcbDrcDiagnostic> FindTraceWidthViolations(
        IEnumerable<PcbDrcTrace> traces,
        DrcRuleProfile profile)
    {
        return traces
            .Where(trace => trace.Width + Tolerance < profile.MinimumTraceWidth)
            .Select(trace => new PcbDrcDiagnostic(
                PcbDrcRuleIds.MinimumTraceWidth,
                PcbDrcSeverity.Error,
                [trace.Id],
                Normalize(trace.Width),
                profile.MinimumTraceWidth,
                trace.Midpoint()));
    }

    private static IEnumerable<PcbDrcDiagnostic> FindViaDrillViolations(
        IEnumerable<PcbDrcVia> vias,
        DrcRuleProfile profile)
    {
        return vias
            .Where(via => via.DrillDiameter + Tolerance < profile.MinimumViaDrill)
            .Select(via => new PcbDrcDiagnostic(
                PcbDrcRuleIds.MinimumViaDrill,
                PcbDrcSeverity.Error,
                [via.Id],
                Normalize(via.DrillDiameter),
                profile.MinimumViaDrill,
                via.Center));
    }

    private static IEnumerable<PcbDrcDiagnostic> FindViaDiameterViolations(
        IEnumerable<PcbDrcVia> vias,
        DrcRuleProfile profile)
    {
        return vias
            .Where(via => via.Diameter + Tolerance < profile.MinimumViaDiameter)
            .Select(via => new PcbDrcDiagnostic(
                PcbDrcRuleIds.MinimumViaDiameter,
                PcbDrcSeverity.Error,
                [via.Id],
                Normalize(via.Diameter),
                profile.MinimumViaDiameter,
                via.Center));
    }

    private static IEnumerable<PcbDrcDiagnostic> FindCopperClearanceViolations(
        PcbDrcDocument document,
        DrcRuleProfile profile)
    {
        CopperPrimitive[] primitives = CopperPrimitive.From(document).ToArray();

        for (int i = 0; i < primitives.Length; i++)
        {
            CopperPrimitive first = primitives[i];

            for (int j = i + 1; j < primitives.Length; j++)
            {
                CopperPrimitive second = primitives[j];
                if (!ShouldCheckClearance(first, second))
                {
                    continue;
                }

                ClearanceMeasurement measurement = first.MeasureClearanceTo(second);
                if (measurement.Clearance + Tolerance >= profile.MinimumCopperClearance)
                {
                    continue;
                }

                string ruleId = first.Kind == CopperPrimitiveKind.Pad || second.Kind == CopperPrimitiveKind.Pad
                    ? PcbDrcRuleIds.PadClearance
                    : PcbDrcRuleIds.TraceClearance;

                string[] objectIds = [first.Id, second.Id];
                Array.Sort(objectIds, StringComparer.Ordinal);

                yield return new PcbDrcDiagnostic(
                    ruleId,
                    PcbDrcSeverity.Error,
                    objectIds,
                    Normalize(Math.Max(0, measurement.Clearance)),
                    profile.MinimumCopperClearance,
                    measurement.Location);
            }
        }
    }

    private static IEnumerable<PcbDrcDiagnostic> FindBoardOutlineClearanceViolations(
        PcbDrcDocument document,
        DrcRuleProfile profile)
    {
        foreach (CopperPrimitive primitive in CopperPrimitive.From(document))
        {
            OutlineMeasurement measurement = document.BoardOutline.MeasureClearance(primitive);
            if (measurement.Clearance + Tolerance >= profile.MinimumBoardOutlineClearance)
            {
                continue;
            }

            yield return new PcbDrcDiagnostic(
                PcbDrcRuleIds.BoardOutlineClearance,
                PcbDrcSeverity.Error,
                [primitive.Id],
                Normalize(Math.Max(0, measurement.Clearance)),
                profile.MinimumBoardOutlineClearance,
                measurement.Location);
        }
    }

    private static bool ShouldCheckClearance(CopperPrimitive first, CopperPrimitive second)
    {
        if (string.Equals(first.NetId, second.NetId, StringComparison.Ordinal))
        {
            return false;
        }

        return first.Layer == second.Layer || first.IsThroughBoard || second.IsThroughBoard;
    }

    private static double Normalize(double value) => Math.Round(value, 6, MidpointRounding.AwayFromZero);

    private sealed record CopperPrimitive(
        string Id,
        string NetId,
        int? Layer,
        CopperPrimitiveKind Kind,
        DrcPoint Start,
        DrcPoint End,
        double Radius)
    {
        public bool IsThroughBoard => Layer is null;

        public static IEnumerable<CopperPrimitive> From(PcbDrcDocument document)
        {
            foreach (PcbDrcTrace trace in document.Traces)
            {
                yield return new CopperPrimitive(
                    trace.Id,
                    trace.NetId,
                    trace.Layer,
                    CopperPrimitiveKind.Trace,
                    trace.Start,
                    trace.End,
                    trace.Width / 2);
            }

            foreach (PcbDrcVia via in document.Vias)
            {
                yield return new CopperPrimitive(
                    via.Id,
                    via.NetId,
                    Layer: null,
                    CopperPrimitiveKind.Via,
                    via.Center,
                    via.Center,
                    via.Diameter / 2);
            }

            foreach (PcbDrcPad pad in document.Pads)
            {
                yield return new CopperPrimitive(
                    pad.Id,
                    pad.NetId,
                    pad.Layer,
                    CopperPrimitiveKind.Pad,
                    pad.Center,
                    pad.Center,
                    pad.Diameter / 2);
            }
        }

        public ClearanceMeasurement MeasureClearanceTo(CopperPrimitive other)
        {
            ClosestPoints closestPoints = Geometry.ClosestPoints(Start, End, other.Start, other.End);
            double clearance = DrcPoint.Distance(closestPoints.First, closestPoints.Second) - Radius - other.Radius;
            DrcPoint location = DrcPoint.Midpoint(closestPoints.First, closestPoints.Second);

            return new ClearanceMeasurement(clearance, location);
        }

        public IEnumerable<DrcPoint> RepresentativePoints()
        {
            yield return Start;

            if (Start != End)
            {
                yield return End;
                yield return DrcPoint.Midpoint(Start, End);
            }
        }
    }

    private enum CopperPrimitiveKind
    {
        Trace,
        Via,
        Pad
    }

    private sealed record ClearanceMeasurement(double Clearance, DrcPoint Location);

    private sealed record OutlineMeasurement(double Clearance, DrcPoint Location);

    private sealed record ClosestPoints(DrcPoint First, DrcPoint Second);

    private static class Geometry
    {
        public static ClosestPoints ClosestPoints(DrcPoint firstStart, DrcPoint firstEnd, DrcPoint secondStart, DrcPoint secondEnd)
        {
            if (firstStart == firstEnd && secondStart == secondEnd)
            {
                return new ClosestPoints(firstStart, secondStart);
            }

            if (firstStart == firstEnd)
            {
                DrcPoint secondPoint = ClosestPointOnSegment(firstStart, secondStart, secondEnd);
                return new ClosestPoints(firstStart, secondPoint);
            }

            if (secondStart == secondEnd)
            {
                DrcPoint firstPoint = ClosestPointOnSegment(secondStart, firstStart, firstEnd);
                return new ClosestPoints(firstPoint, secondStart);
            }

            DrcPoint[] firstCandidates =
            [
                ClosestPointOnSegment(secondStart, firstStart, firstEnd),
                ClosestPointOnSegment(secondEnd, firstStart, firstEnd)
            ];
            DrcPoint[] secondCandidates =
            [
                ClosestPointOnSegment(firstStart, secondStart, secondEnd),
                ClosestPointOnSegment(firstEnd, secondStart, secondEnd)
            ];

            (DrcPoint First, DrcPoint Second)[] pairs =
            [
                (firstCandidates[0], secondStart),
                (firstCandidates[1], secondEnd),
                (firstStart, secondCandidates[0]),
                (firstEnd, secondCandidates[1])
            ];

            if (TryFindParallelClosestPoints(firstStart, firstEnd, secondStart, secondEnd, out DrcPoint parallelFirstPoint))
            {
                DrcPoint parallelSecondPoint = ClosestPointOnSegment(parallelFirstPoint, secondStart, secondEnd);
                return new ClosestPoints(parallelFirstPoint, parallelSecondPoint);
            }

            if (TryFindSegmentIntersection(firstStart, firstEnd, secondStart, secondEnd, out DrcPoint intersection))
            {
                return new ClosestPoints(intersection, intersection);
            }

            return pairs
                .OrderBy(pair => DrcPoint.Distance(pair.First, pair.Second))
                .Select(pair => new ClosestPoints(pair.First, pair.Second))
                .First();
        }

        private static DrcPoint ClosestPointOnSegment(DrcPoint point, DrcPoint segmentStart, DrcPoint segmentEnd)
        {
            double dx = segmentEnd.X - segmentStart.X;
            double dy = segmentEnd.Y - segmentStart.Y;
            double lengthSquared = (dx * dx) + (dy * dy);

            if (lengthSquared <= Tolerance)
            {
                return segmentStart;
            }

            double t = (((point.X - segmentStart.X) * dx) + ((point.Y - segmentStart.Y) * dy)) / lengthSquared;
            t = Math.Clamp(t, 0, 1);

            return new DrcPoint(segmentStart.X + (t * dx), segmentStart.Y + (t * dy));
        }

        private static bool TryFindSegmentIntersection(
            DrcPoint firstStart,
            DrcPoint firstEnd,
            DrcPoint secondStart,
            DrcPoint secondEnd,
            out DrcPoint intersection)
        {
            double firstDx = firstEnd.X - firstStart.X;
            double firstDy = firstEnd.Y - firstStart.Y;
            double secondDx = secondEnd.X - secondStart.X;
            double secondDy = secondEnd.Y - secondStart.Y;
            double denominator = Cross(firstDx, firstDy, secondDx, secondDy);

            if (Math.Abs(denominator) <= Tolerance)
            {
                intersection = default;
                return false;
            }

            double relativeX = secondStart.X - firstStart.X;
            double relativeY = secondStart.Y - firstStart.Y;
            double t = Cross(relativeX, relativeY, secondDx, secondDy) / denominator;
            double u = Cross(relativeX, relativeY, firstDx, firstDy) / denominator;

            if (t < -Tolerance || t > 1 + Tolerance || u < -Tolerance || u > 1 + Tolerance)
            {
                intersection = default;
                return false;
            }

            intersection = new DrcPoint(firstStart.X + (t * firstDx), firstStart.Y + (t * firstDy));
            return true;
        }

        private static bool TryFindParallelClosestPoints(
            DrcPoint firstStart,
            DrcPoint firstEnd,
            DrcPoint secondStart,
            DrcPoint secondEnd,
            out DrcPoint firstPoint)
        {
            double firstDx = firstEnd.X - firstStart.X;
            double firstDy = firstEnd.Y - firstStart.Y;
            double secondDx = secondEnd.X - secondStart.X;
            double secondDy = secondEnd.Y - secondStart.Y;
            if (Math.Abs(Cross(firstDx, firstDy, secondDx, secondDy)) > Tolerance)
            {
                firstPoint = default;
                return false;
            }

            bool useX = Math.Abs(firstDx) >= Math.Abs(firstDy);

            double firstMin = Math.Min(useX ? firstStart.X : firstStart.Y, useX ? firstEnd.X : firstEnd.Y);
            double firstMax = Math.Max(useX ? firstStart.X : firstStart.Y, useX ? firstEnd.X : firstEnd.Y);
            double secondMin = Math.Min(useX ? secondStart.X : secondStart.Y, useX ? secondEnd.X : secondEnd.Y);
            double secondMax = Math.Max(useX ? secondStart.X : secondStart.Y, useX ? secondEnd.X : secondEnd.Y);
            double overlapMin = Math.Max(firstMin, secondMin);
            double overlapMax = Math.Min(firstMax, secondMax);

            if (overlapMax + Tolerance < overlapMin)
            {
                firstPoint = default;
                return false;
            }

            double overlapMid = (overlapMin + overlapMax) / 2;
            double t = useX
                ? (overlapMid - firstStart.X) / firstDx
                : (overlapMid - firstStart.Y) / firstDy;

            firstPoint = new DrcPoint(firstStart.X + (t * firstDx), firstStart.Y + (t * firstDy));
            return true;
        }

        private static double Cross(double firstX, double firstY, double secondX, double secondY) =>
            (firstX * secondY) - (firstY * secondX);
    }

    private static OutlineMeasurement MeasureClearance(this PcbDrcBoardOutline outline, CopperPrimitive primitive)
    {
        return primitive.RepresentativePoints()
            .Select(point =>
            {
                double pointClearance = outline.DistanceToNearestEdge(point) - primitive.Radius;
                return new OutlineMeasurement(pointClearance, point);
            })
            .OrderBy(measurement => measurement.Clearance)
            .ThenBy(measurement => measurement.Location.X)
            .ThenBy(measurement => measurement.Location.Y)
            .First();
    }
}

public static class PcbDrcRuleIds
{
    public const string MinimumTraceWidth = "DRC001";
    public const string MinimumViaDrill = "DRC002";
    public const string MinimumViaDiameter = "DRC003";
    public const string PadClearance = "DRC004";
    public const string TraceClearance = "DRC005";
    public const string BoardOutlineClearance = "DRC006";
}

public sealed record PcbDrcResult(IReadOnlyList<PcbDrcDiagnostic> Diagnostics)
{
    public bool HasErrors => Diagnostics.Any(diagnostic => diagnostic.Severity == PcbDrcSeverity.Error);
}

public sealed record PcbDrcDiagnostic(
    string RuleId,
    PcbDrcSeverity Severity,
    IReadOnlyList<string> ObjectIds,
    double MeasuredValue,
    double RequiredValue,
    DrcPoint Location)
{
    public string PrimaryObjectId => ObjectIds.Count > 0 ? ObjectIds[0] : string.Empty;

    public string SecondaryObjectId => ObjectIds.Count > 1 ? ObjectIds[1] : string.Empty;
}

public enum PcbDrcSeverity
{
    Info,
    Warning,
    Error
}

public sealed record DrcRuleProfile(
    string Id,
    int LayerCount,
    double MinimumTraceWidth,
    double MinimumViaDrill,
    double MinimumViaDiameter,
    double MinimumCopperClearance,
    double MinimumBoardOutlineClearance)
{
    public static DrcRuleProfile TwoLayerPrototype() =>
        new(
            "two-layer-prototype",
            LayerCount: 2,
            MinimumTraceWidth: 0.20,
            MinimumViaDrill: 0.30,
            MinimumViaDiameter: 0.60,
            MinimumCopperClearance: 0.20,
            MinimumBoardOutlineClearance: 0.25);

    public static DrcRuleProfile FourLayerProduction() =>
        new(
            "four-layer-production",
            LayerCount: 4,
            MinimumTraceWidth: 0.15,
            MinimumViaDrill: 0.25,
            MinimumViaDiameter: 0.50,
            MinimumCopperClearance: 0.15,
            MinimumBoardOutlineClearance: 0.20);
}

public sealed record PcbDrcDocument(
    IReadOnlyList<PcbDrcTrace> Traces,
    IReadOnlyList<PcbDrcVia> Vias,
    IReadOnlyList<PcbDrcPad> Pads,
    PcbDrcBoardOutline BoardOutline);

public sealed record PcbDrcTrace(
    string Id,
    string NetId,
    int Layer,
    DrcPoint Start,
    DrcPoint End,
    double Width)
{
    public DrcPoint Midpoint() => DrcPoint.Midpoint(Start, End);
}

public sealed record PcbDrcVia(
    string Id,
    string NetId,
    DrcPoint Center,
    double DrillDiameter,
    double Diameter);

public sealed record PcbDrcPad(
    string Id,
    string NetId,
    int Layer,
    DrcPoint Center,
    double Diameter);

public sealed record PcbDrcBoardOutline(double MinX, double MinY, double MaxX, double MaxY)
{
    public static PcbDrcBoardOutline Rectangle(double minX, double minY, double maxX, double maxY)
    {
        if (maxX <= minX)
        {
            throw new ArgumentOutOfRangeException(nameof(maxX), "Maximum X must be greater than minimum X.");
        }

        if (maxY <= minY)
        {
            throw new ArgumentOutOfRangeException(nameof(maxY), "Maximum Y must be greater than minimum Y.");
        }

        return new PcbDrcBoardOutline(minX, minY, maxX, maxY);
    }

    public double DistanceToNearestEdge(DrcPoint point)
    {
        double left = point.X - MinX;
        double right = MaxX - point.X;
        double top = point.Y - MinY;
        double bottom = MaxY - point.Y;

        return Math.Min(Math.Min(left, right), Math.Min(top, bottom));
    }
}

public readonly record struct DrcPoint(double X, double Y)
{
    public static DrcPoint Midpoint(DrcPoint first, DrcPoint second) =>
        new((first.X + second.X) / 2, (first.Y + second.Y) / 2);

    public static double Distance(DrcPoint first, DrcPoint second)
    {
        double dx = second.X - first.X;
        double dy = second.Y - first.Y;

        return Math.Sqrt((dx * dx) + (dy * dy));
    }
}
