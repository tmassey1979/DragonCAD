using System.Text.Json;
using System.Text.Json.Serialization;

namespace DragonCAD.Core.Routing.Profiles;

public enum RoutingSignalClass
{
    Signal,
    Power,
    HighCurrent,
    Differential
}

public sealed record RoutingProfile(
    string Id,
    int LayerCount,
    IReadOnlyList<RoutingClassRule> ClassRules,
    IReadOnlyList<RoutingLayerTransitionRule> LayerTransitionRules)
{
    public RoutingClassRule RequireRule(RoutingSignalClass signalClass) =>
        ClassRules.FirstOrDefault(rule => rule.SignalClass == signalClass)
        ?? throw new InvalidOperationException($"Routing profile '{Id}' does not define '{signalClass}' rules.");

    public RoutingProfile WithClassRuleOverride(
        RoutingSignalClass signalClass,
        Func<RoutingClassRule, RoutingClassRule> update)
    {
        ArgumentNullException.ThrowIfNull(update);

        RoutingClassRule[] rules = ClassRules
            .Select(rule => rule.SignalClass == signalClass ? update(rule) : rule)
            .OrderBy(rule => rule.SignalClass)
            .ToArray();

        if (!rules.Any(rule => rule.SignalClass == signalClass))
        {
            throw new InvalidOperationException($"Routing profile '{Id}' does not define '{signalClass}' rules.");
        }

        return this with { ClassRules = rules };
    }

    public bool AllowsLayerTransition(RoutingSignalClass signalClass) =>
        LayerTransitionRules.FirstOrDefault(rule => rule.SignalClass == signalClass)?.Allowed ?? true;
}

public sealed record RoutingClassRule(
    RoutingSignalClass SignalClass,
    double MinimumWidthMm,
    double MinimumSpacingMm,
    double? MaximumLengthMm,
    double PreferredViaDrillMm,
    double PreferredViaDiameterMm,
    IReadOnlyList<int> AllowedLayers);

public sealed record RoutingLayerTransitionRule(
    RoutingSignalClass SignalClass,
    bool Allowed,
    string WarningMessage);

public static class RoutingProfileDefaults
{
    public static RoutingProfile TwoLayerPrototype() =>
        new(
            Id: "two-layer-prototype",
            LayerCount: 2,
            ClassRules:
            [
                Rule(RoutingSignalClass.Signal, width: 0.25, spacing: 0.20, length: 150.00, drill: 0.30, diameter: 0.40, layers: [1, 2]),
                Rule(RoutingSignalClass.Power, width: 0.50, spacing: 0.25, length: 100.00, drill: 0.35, diameter: 0.70, layers: [1, 2]),
                Rule(RoutingSignalClass.HighCurrent, width: 1.00, spacing: 0.40, length: 75.00, drill: 0.45, diameter: 0.90, layers: [1, 2]),
                Rule(RoutingSignalClass.Differential, width: 0.20, spacing: 0.18, length: 120.00, drill: 0.30, diameter: 0.60, layers: [1, 2])
            ],
            LayerTransitionRules:
            [
                new RoutingLayerTransitionRule(RoutingSignalClass.Power, Allowed: false, "Keep power routes on one side when possible on two-layer boards."),
                new RoutingLayerTransitionRule(RoutingSignalClass.HighCurrent, Allowed: false, "Avoid high-current layer transitions on two-layer boards.")
            ]);

    public static RoutingProfile FourLayerProduction() =>
        new(
            Id: "four-layer-production",
            LayerCount: 4,
            ClassRules:
            [
                Rule(RoutingSignalClass.Signal, width: 0.15, spacing: 0.15, length: 150.00, drill: 0.25, diameter: 0.50, layers: [1, 2, 3, 4]),
                Rule(RoutingSignalClass.Power, width: 0.35, spacing: 0.20, length: 125.00, drill: 0.30, diameter: 0.60, layers: [1, 2, 3, 4]),
                Rule(RoutingSignalClass.HighCurrent, width: 0.75, spacing: 0.35, length: 90.00, drill: 0.40, diameter: 0.80, layers: [1, 4]),
                Rule(RoutingSignalClass.Differential, width: 0.15, spacing: 0.18, length: 100.00, drill: 0.25, diameter: 0.50, layers: [1, 4])
            ],
            LayerTransitionRules: []);

    public static RoutingProfile SixLayerDense() =>
        new(
            Id: "six-layer-dense",
            LayerCount: 6,
            ClassRules:
            [
                Rule(RoutingSignalClass.Signal, width: 0.10, spacing: 0.10, length: 125.00, drill: 0.20, diameter: 0.45, layers: [1, 2, 3, 4, 5, 6]),
                Rule(RoutingSignalClass.Power, width: 0.25, spacing: 0.15, length: 125.00, drill: 0.25, diameter: 0.50, layers: [1, 2, 3, 4, 5, 6]),
                Rule(RoutingSignalClass.HighCurrent, width: 0.60, spacing: 0.30, length: 90.00, drill: 0.35, diameter: 0.70, layers: [1, 6]),
                Rule(RoutingSignalClass.Differential, width: 0.10, spacing: 0.12, length: 80.00, drill: 0.20, diameter: 0.45, layers: [1, 3, 4, 6])
            ],
            LayerTransitionRules: []);

    private static RoutingClassRule Rule(
        RoutingSignalClass signalClass,
        double width,
        double spacing,
        double? length,
        double drill,
        double diameter,
        IReadOnlyList<int> layers) =>
        new(signalClass, width, spacing, length, drill, diameter, layers);
}

public static class RoutingProfileSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    static RoutingProfileSerializer()
    {
        Options.Converters.Add(new JsonStringEnumConverter());
    }

    public static string Serialize(RoutingProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        RoutingProfile ordered = profile with
        {
            ClassRules = profile.ClassRules
                .OrderBy(rule => rule.SignalClass)
                .Select(rule => rule with { AllowedLayers = rule.AllowedLayers.Order().ToArray() })
                .ToArray(),
            LayerTransitionRules = profile.LayerTransitionRules
                .OrderBy(rule => rule.SignalClass)
                .ToArray()
        };

        return JsonSerializer.Serialize(ordered, Options);
    }

    public static RoutingProfile Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        RoutingProfile profile = JsonSerializer.Deserialize<RoutingProfile>(json, Options)
            ?? throw new InvalidOperationException("Routing profile JSON did not contain a profile.");

        return profile with
        {
            ClassRules = profile.ClassRules.OrderBy(rule => rule.SignalClass).ToArray(),
            LayerTransitionRules = profile.LayerTransitionRules.OrderBy(rule => rule.SignalClass).ToArray()
        };
    }
}

public sealed record RoutingValidationBoard(
    IReadOnlyList<RoutingValidationTrace> Traces,
    IReadOnlyList<RoutingValidationVia> Vias);

public sealed record RoutingValidationTrace(
    string Id,
    string NetId,
    RoutingSignalClass SignalClass,
    int Layer,
    RoutingPoint Start,
    RoutingPoint End,
    double WidthMm)
{
    public double LengthMm => RoutingPoint.Distance(Start, End);
}

public sealed record RoutingValidationVia(
    string Id,
    string NetId,
    RoutingSignalClass SignalClass,
    RoutingPoint Center,
    double DrillMm,
    double DiameterMm,
    int FromLayer,
    int ToLayer);

public readonly record struct RoutingPoint(double X, double Y)
{
    public static double Distance(RoutingPoint first, RoutingPoint second)
    {
        double dx = second.X - first.X;
        double dy = second.Y - first.Y;

        return Math.Sqrt((dx * dx) + (dy * dy));
    }
}

public static class RoutingProfileValidator
{
    private const double Tolerance = 0.0000001;

    public static RoutingValidationResult Validate(RoutingValidationBoard board, RoutingProfile profile)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(profile);

        List<RoutingProfileDiagnostic> diagnostics = [];

        diagnostics.AddRange(FindWidthWarnings(board.Traces, profile));
        diagnostics.AddRange(FindSpacingWarnings(board.Traces, profile));
        diagnostics.AddRange(FindLengthWarnings(board.Traces, profile));
        diagnostics.AddRange(FindViaWarnings(board.Vias, profile));
        diagnostics.AddRange(FindLayerTransitionWarnings(board.Vias, profile));

        return new RoutingValidationResult(
            diagnostics
                .OrderBy(diagnostic => diagnostic.RuleId, StringComparer.Ordinal)
                .ThenBy(diagnostic => diagnostic.PrimaryObjectId, StringComparer.Ordinal)
                .ThenBy(diagnostic => diagnostic.SecondaryObjectId, StringComparer.Ordinal)
                .ToArray());
    }

    private static IEnumerable<RoutingProfileDiagnostic> FindWidthWarnings(
        IEnumerable<RoutingValidationTrace> traces,
        RoutingProfile profile)
    {
        foreach (RoutingValidationTrace trace in traces)
        {
            RoutingClassRule rule = profile.RequireRule(trace.SignalClass);
            if (trace.WidthMm + Tolerance >= rule.MinimumWidthMm)
            {
                continue;
            }

            yield return RoutingProfileDiagnostic.Warning(
                RoutingProfileRuleIds.MinimumWidth,
                [trace.Id],
                trace.WidthMm,
                rule.MinimumWidthMm,
                $"Trace '{trace.Id}' is narrower than the {trace.SignalClass} profile.");
        }
    }

    private static IEnumerable<RoutingProfileDiagnostic> FindSpacingWarnings(
        IReadOnlyList<RoutingValidationTrace> traces,
        RoutingProfile profile)
    {
        for (int i = 0; i < traces.Count; i++)
        {
            RoutingValidationTrace first = traces[i];
            for (int j = i + 1; j < traces.Count; j++)
            {
                RoutingValidationTrace second = traces[j];
                if (first.Layer != second.Layer || string.Equals(first.NetId, second.NetId, StringComparison.Ordinal))
                {
                    continue;
                }

                double required = Math.Max(
                    profile.RequireRule(first.SignalClass).MinimumSpacingMm,
                    profile.RequireRule(second.SignalClass).MinimumSpacingMm);
                double measured = MeasureClearance(first, second);

                if (measured + Tolerance >= required)
                {
                    continue;
                }

                string[] ids = [first.Id, second.Id];
                Array.Sort(ids, StringComparer.Ordinal);
                yield return RoutingProfileDiagnostic.Warning(
                    RoutingProfileRuleIds.MinimumSpacing,
                    ids,
                    Math.Max(0, measured),
                    required,
                    "Routes are closer than the active routing profile allows.");
            }
        }
    }

    private static IEnumerable<RoutingProfileDiagnostic> FindLengthWarnings(
        IEnumerable<RoutingValidationTrace> traces,
        RoutingProfile profile)
    {
        foreach (IGrouping<string, RoutingValidationTrace> netGroup in traces.GroupBy(trace => trace.NetId, StringComparer.Ordinal))
        {
            RoutingValidationTrace firstTrace = netGroup.OrderBy(trace => trace.Id, StringComparer.Ordinal).First();
            RoutingClassRule rule = profile.RequireRule(firstTrace.SignalClass);
            if (rule.MaximumLengthMm is null)
            {
                continue;
            }

            double length = netGroup.Sum(trace => trace.LengthMm);
            if (length <= rule.MaximumLengthMm.Value + Tolerance)
            {
                continue;
            }

            yield return RoutingProfileDiagnostic.Warning(
                RoutingProfileRuleIds.MaximumLength,
                [netGroup.Key],
                length,
                rule.MaximumLengthMm.Value,
                $"Net '{netGroup.Key}' exceeds the {firstTrace.SignalClass} profile length target.");
        }
    }

    private static IEnumerable<RoutingProfileDiagnostic> FindViaWarnings(
        IEnumerable<RoutingValidationVia> vias,
        RoutingProfile profile)
    {
        foreach (RoutingValidationVia via in vias)
        {
            RoutingClassRule rule = profile.RequireRule(via.SignalClass);
            if (via.DrillMm + Tolerance >= rule.PreferredViaDrillMm)
            {
                continue;
            }

            yield return RoutingProfileDiagnostic.Warning(
                RoutingProfileRuleIds.MinimumViaDrill,
                [via.Id],
                via.DrillMm,
                rule.PreferredViaDrillMm,
                $"Via '{via.Id}' is below the preferred {via.SignalClass} drill size.");
        }
    }

    private static IEnumerable<RoutingProfileDiagnostic> FindLayerTransitionWarnings(
        IEnumerable<RoutingValidationVia> vias,
        RoutingProfile profile)
    {
        foreach (RoutingValidationVia via in vias)
        {
            if (via.FromLayer == via.ToLayer || profile.AllowsLayerTransition(via.SignalClass))
            {
                continue;
            }

            yield return RoutingProfileDiagnostic.Warning(
                RoutingProfileRuleIds.LayerTransition,
                [via.Id],
                Math.Abs(via.ToLayer - via.FromLayer),
                0,
                $"Via '{via.Id}' changes layers against the {via.SignalClass} routing profile.");
        }
    }

    private static double MeasureClearance(RoutingValidationTrace first, RoutingValidationTrace second)
    {
        ClosestPoints closestPoints = Geometry.ClosestPoints(first.Start, first.End, second.Start, second.End);
        return RoutingPoint.Distance(closestPoints.First, closestPoints.Second) - (first.WidthMm / 2) - (second.WidthMm / 2);
    }

    private sealed record ClosestPoints(RoutingPoint First, RoutingPoint Second);

    private static class Geometry
    {
        public static ClosestPoints ClosestPoints(
            RoutingPoint firstStart,
            RoutingPoint firstEnd,
            RoutingPoint secondStart,
            RoutingPoint secondEnd)
        {
            if (firstStart == firstEnd && secondStart == secondEnd)
            {
                return new ClosestPoints(firstStart, secondStart);
            }

            if (firstStart == firstEnd)
            {
                return new ClosestPoints(firstStart, ClosestPointOnSegment(firstStart, secondStart, secondEnd));
            }

            if (secondStart == secondEnd)
            {
                return new ClosestPoints(ClosestPointOnSegment(secondStart, firstStart, firstEnd), secondStart);
            }

            (RoutingPoint First, RoutingPoint Second)[] candidates =
            [
                (ClosestPointOnSegment(secondStart, firstStart, firstEnd), secondStart),
                (ClosestPointOnSegment(secondEnd, firstStart, firstEnd), secondEnd),
                (firstStart, ClosestPointOnSegment(firstStart, secondStart, secondEnd)),
                (firstEnd, ClosestPointOnSegment(firstEnd, secondStart, secondEnd))
            ];

            if (TryFindSegmentIntersection(firstStart, firstEnd, secondStart, secondEnd, out RoutingPoint intersection))
            {
                return new ClosestPoints(intersection, intersection);
            }

            return candidates
                .OrderBy(candidate => RoutingPoint.Distance(candidate.First, candidate.Second))
                .Select(candidate => new ClosestPoints(candidate.First, candidate.Second))
                .First();
        }

        private static RoutingPoint ClosestPointOnSegment(RoutingPoint point, RoutingPoint start, RoutingPoint end)
        {
            double dx = end.X - start.X;
            double dy = end.Y - start.Y;
            double lengthSquared = (dx * dx) + (dy * dy);

            if (lengthSquared <= Tolerance)
            {
                return start;
            }

            double t = (((point.X - start.X) * dx) + ((point.Y - start.Y) * dy)) / lengthSquared;
            t = Math.Clamp(t, 0, 1);

            return new RoutingPoint(start.X + (t * dx), start.Y + (t * dy));
        }

        private static bool TryFindSegmentIntersection(
            RoutingPoint firstStart,
            RoutingPoint firstEnd,
            RoutingPoint secondStart,
            RoutingPoint secondEnd,
            out RoutingPoint intersection)
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

            intersection = new RoutingPoint(firstStart.X + (t * firstDx), firstStart.Y + (t * firstDy));
            return true;
        }

        private static double Cross(double firstX, double firstY, double secondX, double secondY) =>
            (firstX * secondY) - (firstY * secondX);
    }
}

public static class RoutingProfileRuleIds
{
    public const string MinimumWidth = "RP001";
    public const string MinimumSpacing = "RP002";
    public const string MaximumLength = "RP003";
    public const string MinimumViaDrill = "RP004";
    public const string LayerTransition = "RP005";
}

public sealed record RoutingValidationResult(IReadOnlyList<RoutingProfileDiagnostic> Diagnostics)
{
    public bool HasWarnings => Diagnostics.Any(diagnostic => diagnostic.Severity == RoutingProfileDiagnosticSeverity.Warning);
}

public sealed record RoutingProfileDiagnostic(
    string RuleId,
    RoutingProfileDiagnosticSeverity Severity,
    IReadOnlyList<string> ObjectIds,
    double MeasuredValue,
    double RequiredValue,
    string Message)
{
    public string PrimaryObjectId => ObjectIds.Count > 0 ? ObjectIds[0] : string.Empty;

    public string SecondaryObjectId => ObjectIds.Count > 1 ? ObjectIds[1] : string.Empty;

    public static RoutingProfileDiagnostic Warning(
        string ruleId,
        IReadOnlyList<string> objectIds,
        double measuredValue,
        double requiredValue,
        string message) =>
        new(
            ruleId,
            RoutingProfileDiagnosticSeverity.Warning,
            objectIds,
            Normalize(measuredValue),
            Normalize(requiredValue),
            message);

    private static double Normalize(double value) => Math.Round(value, 6, MidpointRounding.AwayFromZero);
}

public enum RoutingProfileDiagnosticSeverity
{
    Info,
    Warning,
    Error
}
