using DragonCAD.Core.Routing.Autoroute;

namespace DragonCAD.Core.Tests.Routing.Autoroute;

public sealed class AutorouteJobModelTests
{
    [Fact]
    public void CreateJobCapturesBoardScopeProfileCostsKeepoutsLimitsAndReviewMode()
    {
        AutorouteProfile profile = ValidProfile();

        AutorouteJob job = AutorouteJob.Create(
            boardId: "board:main",
            availableNets: ["GND", "VCC", "D0"],
            requestedNetIds: ["D0", "GND"],
            layerProfile: profile,
            costWeights: new AutorouteCostWeights(SegmentLength: 1.25, Via: 4.00, LayerChange: 2.50, KeepoutProximity: 8.00),
            keepoutRules: [new AutorouteKeepoutRule("mounting-hole", "Mechanical clearance", ["GND"])],
            maxViaCount: 6,
            reviewMode: AutorouteReviewMode.ProposeOnly);

        Assert.Equal("board:main", job.BoardId);
        Assert.Equal(["D0", "GND"], job.NetScope.NetIds);
        Assert.Equal(profile, job.LayerProfile);
        Assert.Equal(1.25, job.CostWeights.SegmentLength);
        Assert.Equal("mounting-hole", Assert.Single(job.KeepoutRules).Id);
        Assert.Equal(6, job.MaxViaCount);
        Assert.Equal(AutorouteReviewMode.ProposeOnly, job.ReviewMode);
    }

    [Fact]
    public void CreateJobFiltersRequestedNetsToAvailableBoardNets()
    {
        AutorouteJob job = AutorouteJob.Create(
            boardId: "board:main",
            availableNets: ["GND", "VCC", "D0"],
            requestedNetIds: ["D1", "GND", "D0", "D0"],
            layerProfile: ValidProfile(),
            costWeights: AutorouteCostWeights.Default,
            keepoutRules: [],
            maxViaCount: 3,
            reviewMode: AutorouteReviewMode.ProposeOnly);

        Assert.Equal(["GND", "D0"], job.NetScope.NetIds);
        Assert.Equal(["D1"], job.NetScope.ExcludedNetIds);
    }

    [Fact]
    public void ValidateProfileReportsInvalidProfileDiagnostics()
    {
        AutorouteProfile profile = new(
            Id: "",
            LayerCount: 1,
            AllowedLayers: [0, 2],
            MinimumTraceWidthMm: 0,
            MinimumTraceSpacingMm: -0.1,
            PreferredViaDrillMm: 0.3,
            PreferredViaDiameterMm: 0.2);

        AutorouteProfileValidationResult result = AutorouteProfileValidator.Validate(profile);

        Assert.True(result.HasErrors);
        Assert.Equal(
            [
                "AR001|Profile id is required.",
                "AR002|Layer count must be at least 2.",
                "AR003|Allowed layers must be within the profile layer count.",
                "AR004|Minimum trace width must be greater than zero.",
                "AR005|Minimum trace spacing must be greater than zero.",
                "AR006|Preferred via diameter must be greater than or equal to preferred via drill."
            ],
            result.Diagnostics.Select(diagnostic => $"{diagnostic.RuleId}|{diagnostic.Message}"));
    }

    [Fact]
    public void ResultSummaryCountsProposedRoutesViasUnroutedNetsWarningsAndConfidence()
    {
        AutorouteResult result = new(
            JobId: "job:1",
            ProposedTraces:
            [
                new AutorouteProposedTrace("trace:1", "GND", Layer: 1, Start: Point(0, 0), End: Point(10, 0), WidthMm: 0.25),
                new AutorouteProposedTrace("trace:2", "VCC", Layer: 2, Start: Point(0, 1), End: Point(8, 1), WidthMm: 0.50)
            ],
            ProposedVias:
            [
                new AutorouteProposedVia("via:1", "GND", Center: Point(5, 0), FromLayer: 1, ToLayer: 2, DrillMm: 0.30, DiameterMm: 0.60)
            ],
            UnroutedNetIds: ["D0"],
            Warnings:
            [
                new AutorouteWarning("ARW001", "D0", "No legal route found.")
            ],
            Confidence: AutorouteConfidence.Medium);

        AutorouteResultSummary summary = result.Summarize();

        Assert.Equal(2, summary.ProposedTraceCount);
        Assert.Equal(1, summary.ProposedViaCount);
        Assert.Equal(1, summary.UnroutedNetCount);
        Assert.Equal(1, summary.WarningCount);
        Assert.Equal(AutorouteConfidence.Medium, summary.Confidence);
    }

    [Fact]
    public void ApplyingResultIsBlockedByDefaultAndDoesNotExposeBoardMutation()
    {
        AutorouteResult result = new(
            JobId: "job:1",
            ProposedTraces: [],
            ProposedVias: [],
            UnroutedNetIds: [],
            Warnings: [],
            Confidence: AutorouteConfidence.Low);

        AutorouteApplyState applyState = result.CreateApplyState();

        Assert.False(applyState.CanApply);
        Assert.Equal("Autoroute results are review-only; applying changes is out of scope.", applyState.BlockedReason);
    }

    private static AutorouteProfile ValidProfile() =>
        new(
            Id: "two-layer-review",
            LayerCount: 2,
            AllowedLayers: [1, 2],
            MinimumTraceWidthMm: 0.20,
            MinimumTraceSpacingMm: 0.20,
            PreferredViaDrillMm: 0.30,
            PreferredViaDiameterMm: 0.60);

    private static AutoroutePoint Point(double x, double y) => new(x, y);
}
