using DragonCAD.Core.Routing.Profiles;

namespace DragonCAD.Core.Tests.Routing.Profiles;

public sealed class RoutingProfileTests
{
    [Fact]
    public void DefaultsExposeLayerCountsAndSignalClasses()
    {
        RoutingProfile twoLayer = RoutingProfileDefaults.TwoLayerPrototype();
        RoutingProfile fourLayer = RoutingProfileDefaults.FourLayerProduction();
        RoutingProfile sixLayer = RoutingProfileDefaults.SixLayerDense();

        Assert.Equal(2, twoLayer.LayerCount);
        Assert.Equal(4, fourLayer.LayerCount);
        Assert.Equal(6, sixLayer.LayerCount);

        Assert.Equal(
            [RoutingSignalClass.Signal, RoutingSignalClass.Power, RoutingSignalClass.HighCurrent, RoutingSignalClass.Differential],
            twoLayer.ClassRules.Select(rule => rule.SignalClass));
    }

    [Fact]
    public void LookupReturnsClassRulesAndCustomOverrides()
    {
        RoutingProfile profile = RoutingProfileDefaults.TwoLayerPrototype()
            .WithClassRuleOverride(RoutingSignalClass.Power, rule => rule with { MinimumWidthMm = 0.80 });

        RoutingClassRule signal = profile.RequireRule(RoutingSignalClass.Signal);
        RoutingClassRule power = profile.RequireRule(RoutingSignalClass.Power);

        Assert.Equal(0.25, signal.MinimumWidthMm);
        Assert.Equal(0.80, power.MinimumWidthMm);
    }

    [Fact]
    public void ValidatorReportsWidthSpacingLengthViaAndLayerTransitionWarnings()
    {
        RoutingProfile profile = RoutingProfileDefaults.TwoLayerPrototype()
            .WithClassRuleOverride(RoutingSignalClass.Signal, rule => rule with { MaximumLengthMm = 15.00 });

        RoutingValidationBoard board = new(
            Traces:
            [
                Trace("trace:thin", "net:a", RoutingSignalClass.Signal, layer: 1, Point(0, 0), Point(20, 0), width: 0.10),
                Trace("trace:near", "net:b", RoutingSignalClass.Power, layer: 1, Point(0, 0.365), Point(20, 0.365), width: 0.50)
            ],
            Vias:
            [
                Via("via:small", "net:a", RoutingSignalClass.Signal, Point(10, 0), drill: 0.20, diameter: 0.40, fromLayer: 1, toLayer: 2),
                Via("via:jump", "net:b", RoutingSignalClass.Power, Point(12, 0.365), drill: 0.35, diameter: 0.70, fromLayer: 1, toLayer: 2)
            ]);

        RoutingValidationResult result = RoutingProfileValidator.Validate(board, profile);

        Assert.Equal(
            [
                "RP001|trace:thin|0.100|0.250",
                "RP002|trace:near,trace:thin|0.065|0.250",
                "RP003|net:a|20.000|15.000",
                "RP004|via:small|0.200|0.300",
                "RP005|via:jump|1.000|0.000"
            ],
            result.Diagnostics.Select(FormatDiagnostic));
    }

    [Fact]
    public void SerializationIsDeterministicAndRoundTrips()
    {
        RoutingProfile original = new(
            Id: "custom",
            LayerCount: 4,
            ClassRules:
            [
                new RoutingClassRule(RoutingSignalClass.HighCurrent, 1.00, 0.40, 50.00, 0.40, 0.80, [1, 4]),
                new RoutingClassRule(RoutingSignalClass.Signal, 0.15, 0.15, 120.00, 0.25, 0.50, [1, 2, 3, 4]),
                new RoutingClassRule(RoutingSignalClass.Differential, 0.15, 0.18, 80.00, 0.25, 0.50, [1, 4]),
                new RoutingClassRule(RoutingSignalClass.Power, 0.35, 0.25, 100.00, 0.30, 0.60, [1, 4])
            ],
            LayerTransitionRules:
            [
                new RoutingLayerTransitionRule(RoutingSignalClass.Power, Allowed: true, WarningMessage: "ok"),
                new RoutingLayerTransitionRule(RoutingSignalClass.HighCurrent, Allowed: false, WarningMessage: "avoid")
            ]);

        string first = RoutingProfileSerializer.Serialize(original);
        string second = RoutingProfileSerializer.Serialize(original);
        RoutingProfile roundTripped = RoutingProfileSerializer.Deserialize(first);

        Assert.Equal(first, second);
        Assert.Equal(
            "{\"id\":\"custom\",\"layerCount\":4,\"classRules\":[{\"signalClass\":\"Signal\",\"minimumWidthMm\":0.15,\"minimumSpacingMm\":0.15,\"maximumLengthMm\":120,\"preferredViaDrillMm\":0.25,\"preferredViaDiameterMm\":0.5,\"allowedLayers\":[1,2,3,4]},{\"signalClass\":\"Power\",\"minimumWidthMm\":0.35,\"minimumSpacingMm\":0.25,\"maximumLengthMm\":100,\"preferredViaDrillMm\":0.3,\"preferredViaDiameterMm\":0.6,\"allowedLayers\":[1,4]},{\"signalClass\":\"HighCurrent\",\"minimumWidthMm\":1,\"minimumSpacingMm\":0.4,\"maximumLengthMm\":50,\"preferredViaDrillMm\":0.4,\"preferredViaDiameterMm\":0.8,\"allowedLayers\":[1,4]},{\"signalClass\":\"Differential\",\"minimumWidthMm\":0.15,\"minimumSpacingMm\":0.18,\"maximumLengthMm\":80,\"preferredViaDrillMm\":0.25,\"preferredViaDiameterMm\":0.5,\"allowedLayers\":[1,4]}],\"layerTransitionRules\":[{\"signalClass\":\"Power\",\"allowed\":true,\"warningMessage\":\"ok\"},{\"signalClass\":\"HighCurrent\",\"allowed\":false,\"warningMessage\":\"avoid\"}]}",
            first);
        Assert.Equal(original.Id, roundTripped.Id);
        Assert.Equal(original.LayerCount, roundTripped.LayerCount);
        Assert.Equal(
            original.ClassRules.OrderBy(rule => rule.SignalClass).Select(rule => rule.SignalClass),
            roundTripped.ClassRules.Select(rule => rule.SignalClass));
    }

    private static RoutingValidationTrace Trace(
        string id,
        string netId,
        RoutingSignalClass signalClass,
        int layer,
        RoutingPoint start,
        RoutingPoint end,
        double width) =>
        new(id, netId, signalClass, layer, start, end, width);

    private static RoutingValidationVia Via(
        string id,
        string netId,
        RoutingSignalClass signalClass,
        RoutingPoint center,
        double drill,
        double diameter,
        int fromLayer,
        int toLayer) =>
        new(id, netId, signalClass, center, drill, diameter, fromLayer, toLayer);

    private static RoutingPoint Point(double x, double y) => new(x, y);

    private static string FormatDiagnostic(RoutingProfileDiagnostic diagnostic) =>
        string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"{diagnostic.RuleId}|{string.Join(",", diagnostic.ObjectIds)}|{diagnostic.MeasuredValue:0.000}|{diagnostic.RequiredValue:0.000}");
}
