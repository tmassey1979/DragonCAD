namespace DragonCAD.Core.Routing.Autoroute;

public sealed record AutorouteJob(
    string Id,
    string BoardId,
    AutorouteNetScope NetScope,
    AutorouteProfile LayerProfile,
    AutorouteCostWeights CostWeights,
    IReadOnlyList<AutorouteKeepoutRule> KeepoutRules,
    int MaxViaCount,
    AutorouteReviewMode ReviewMode)
{
    public static AutorouteJob Create(
        string boardId,
        IReadOnlyList<string> availableNets,
        IReadOnlyList<string> requestedNetIds,
        AutorouteProfile layerProfile,
        AutorouteCostWeights costWeights,
        IReadOnlyList<AutorouteKeepoutRule> keepoutRules,
        int maxViaCount,
        AutorouteReviewMode reviewMode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(boardId);
        ArgumentNullException.ThrowIfNull(availableNets);
        ArgumentNullException.ThrowIfNull(requestedNetIds);
        ArgumentNullException.ThrowIfNull(layerProfile);
        ArgumentNullException.ThrowIfNull(costWeights);
        ArgumentNullException.ThrowIfNull(keepoutRules);

        if (maxViaCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxViaCount), maxViaCount, "Max via count must not be negative.");
        }

        AutorouteNetScope scope = AutorouteNetScope.Filter(availableNets, requestedNetIds);

        return new AutorouteJob(
            Id: $"autoroute:{boardId}:{string.Join(",", scope.NetIds)}",
            BoardId: boardId,
            NetScope: scope,
            LayerProfile: layerProfile,
            CostWeights: costWeights,
            KeepoutRules: keepoutRules.ToArray(),
            MaxViaCount: maxViaCount,
            ReviewMode: reviewMode);
    }
}

public sealed record AutorouteNetScope(
    IReadOnlyList<string> NetIds,
    IReadOnlyList<string> ExcludedNetIds)
{
    public static AutorouteNetScope Filter(
        IReadOnlyList<string> availableNets,
        IReadOnlyList<string> requestedNetIds)
    {
        HashSet<string> available = new(availableNets.Where(netId => !string.IsNullOrWhiteSpace(netId)), StringComparer.Ordinal);
        HashSet<string> included = new(StringComparer.Ordinal);
        HashSet<string> excluded = new(StringComparer.Ordinal);
        List<string> netIds = [];
        List<string> excludedNetIds = [];

        foreach (string requestedNetId in requestedNetIds.Where(netId => !string.IsNullOrWhiteSpace(netId)))
        {
            if (available.Contains(requestedNetId))
            {
                if (included.Add(requestedNetId))
                {
                    netIds.Add(requestedNetId);
                }

                continue;
            }

            if (excluded.Add(requestedNetId))
            {
                excludedNetIds.Add(requestedNetId);
            }
        }

        return new AutorouteNetScope(netIds, excludedNetIds);
    }
}

public sealed record AutorouteProfile(
    string Id,
    int LayerCount,
    IReadOnlyList<int> AllowedLayers,
    double MinimumTraceWidthMm,
    double MinimumTraceSpacingMm,
    double PreferredViaDrillMm,
    double PreferredViaDiameterMm);

public sealed record AutorouteCostWeights(
    double SegmentLength,
    double Via,
    double LayerChange,
    double KeepoutProximity)
{
    public static AutorouteCostWeights Default { get; } = new(
        SegmentLength: 1.00,
        Via: 3.00,
        LayerChange: 2.00,
        KeepoutProximity: 5.00);
}

public sealed record AutorouteKeepoutRule(
    string Id,
    string Description,
    IReadOnlyList<string> AppliesToNetIds);

public enum AutorouteReviewMode
{
    ProposeOnly,
    ReviewRequired
}

public static class AutorouteProfileValidator
{
    public static AutorouteProfileValidationResult Validate(AutorouteProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        List<AutorouteProfileDiagnostic> diagnostics = [];

        if (string.IsNullOrWhiteSpace(profile.Id))
        {
            diagnostics.Add(AutorouteProfileDiagnostic.Error("AR001", "Profile id is required."));
        }

        if (profile.LayerCount < 2)
        {
            diagnostics.Add(AutorouteProfileDiagnostic.Error("AR002", "Layer count must be at least 2."));
        }

        if (profile.AllowedLayers.Count == 0 || profile.AllowedLayers.Any(layer => layer < 1 || layer > profile.LayerCount))
        {
            diagnostics.Add(AutorouteProfileDiagnostic.Error("AR003", "Allowed layers must be within the profile layer count."));
        }

        if (profile.MinimumTraceWidthMm <= 0)
        {
            diagnostics.Add(AutorouteProfileDiagnostic.Error("AR004", "Minimum trace width must be greater than zero."));
        }

        if (profile.MinimumTraceSpacingMm <= 0)
        {
            diagnostics.Add(AutorouteProfileDiagnostic.Error("AR005", "Minimum trace spacing must be greater than zero."));
        }

        if (profile.PreferredViaDiameterMm < profile.PreferredViaDrillMm)
        {
            diagnostics.Add(AutorouteProfileDiagnostic.Error("AR006", "Preferred via diameter must be greater than or equal to preferred via drill."));
        }

        return new AutorouteProfileValidationResult(diagnostics);
    }
}

public sealed record AutorouteProfileValidationResult(IReadOnlyList<AutorouteProfileDiagnostic> Diagnostics)
{
    public bool HasErrors => Diagnostics.Any(diagnostic => diagnostic.Severity == AutorouteDiagnosticSeverity.Error);
}

public sealed record AutorouteProfileDiagnostic(
    string RuleId,
    AutorouteDiagnosticSeverity Severity,
    string Message)
{
    public static AutorouteProfileDiagnostic Error(string ruleId, string message) =>
        new(ruleId, AutorouteDiagnosticSeverity.Error, message);
}

public enum AutorouteDiagnosticSeverity
{
    Info,
    Warning,
    Error
}

public sealed record AutorouteResult(
    string JobId,
    IReadOnlyList<AutorouteProposedTrace> ProposedTraces,
    IReadOnlyList<AutorouteProposedVia> ProposedVias,
    IReadOnlyList<string> UnroutedNetIds,
    IReadOnlyList<AutorouteWarning> Warnings,
    AutorouteConfidence Confidence)
{
    private const string ApplyBlockedReason = "Autoroute results are review-only; applying changes is out of scope.";

    public AutorouteResultSummary Summarize() =>
        new(
            ProposedTraceCount: ProposedTraces.Count,
            ProposedViaCount: ProposedVias.Count,
            UnroutedNetCount: UnroutedNetIds.Count,
            WarningCount: Warnings.Count,
            Confidence: Confidence);

    public AutorouteApplyState CreateApplyState() =>
        new(CanApply: false, BlockedReason: ApplyBlockedReason);
}

public sealed record AutorouteResultSummary(
    int ProposedTraceCount,
    int ProposedViaCount,
    int UnroutedNetCount,
    int WarningCount,
    AutorouteConfidence Confidence);

public sealed record AutorouteProposedTrace(
    string Id,
    string NetId,
    int Layer,
    AutoroutePoint Start,
    AutoroutePoint End,
    double WidthMm);

public sealed record AutorouteProposedVia(
    string Id,
    string NetId,
    AutoroutePoint Center,
    int FromLayer,
    int ToLayer,
    double DrillMm,
    double DiameterMm);

public sealed record AutorouteWarning(
    string RuleId,
    string ObjectId,
    string Message);

public readonly record struct AutoroutePoint(double X, double Y);

public enum AutorouteConfidence
{
    Low,
    Medium,
    High
}

public sealed record AutorouteApplyState(
    bool CanApply,
    string BlockedReason);
