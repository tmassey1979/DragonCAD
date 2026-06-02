namespace DragonCAD.ComponentIntelligence.AiActions;

public sealed record AiActionRequest(string Instruction, string DesignContext);

public enum AiActionProviderKind
{
    Disabled,
    Ollama,
    Codex,
    Fake,
}

public sealed record AiActionProviderDescriptor(AiActionProviderKind Kind, string ModelName);

public enum AiActionConfidence
{
    Low,
    Medium,
    High,
}

public enum AiActionConstraintKind
{
    Electrical,
    Physical,
    Firmware,
}

public sealed record AiActionSuggestedComponent(
    string ReferenceDesignator,
    string Description,
    string? ManufacturerPartNumber);

public sealed record AiActionSuggestedNet(string Name, IReadOnlyList<string> Participants, string Explanation);

public sealed record AiActionSuggestedConstraint(AiActionConstraintKind Kind, string Description);

public sealed record AiActionFirmwareNote(string Text);

public enum AiActionDiagnosticCode
{
    ProviderDisabled,
    HumanReviewRequired,
}

public sealed record AiActionDiagnostic(AiActionDiagnosticCode Code, string Message);

public enum AiActionDecisionKind
{
    Approved,
    Rejected,
}

public sealed record AiActionDecision(
    AiActionDecisionKind Kind,
    string Reviewer,
    string Rationale,
    DateTimeOffset DecidedAt);

public enum AiActionReviewStatus
{
    PendingReview,
    Approved,
    Rejected,
}

public sealed record AiActionPlan
{
    public AiActionPlan(
        string id,
        AiActionRequest request,
        AiActionProviderDescriptor provider,
        IReadOnlyList<AiActionSuggestedComponent> suggestedComponents,
        IReadOnlyList<AiActionSuggestedNet> suggestedNets,
        IReadOnlyList<AiActionSuggestedConstraint> suggestedConstraints,
        IReadOnlyList<AiActionFirmwareNote> firmwareNotes,
        IReadOnlyList<AiActionDiagnostic> diagnostics,
        AiActionConfidence confidence,
        string explanation,
        AiActionReviewStatus reviewStatus = AiActionReviewStatus.PendingReview,
        IReadOnlyList<AiActionDecision>? decisions = null)
    {
        Id = RequireText(id, nameof(id));
        Request = request;
        Provider = provider;
        SuggestedComponents = suggestedComponents;
        SuggestedNets = suggestedNets;
        SuggestedConstraints = suggestedConstraints;
        FirmwareNotes = firmwareNotes;
        Diagnostics = diagnostics;
        Confidence = confidence;
        Explanation = RequireText(explanation, nameof(explanation));
        ReviewStatus = reviewStatus;
        Decisions = decisions ?? [];
    }

    public string Id { get; init; }

    public AiActionRequest Request { get; init; }

    public AiActionProviderDescriptor Provider { get; init; }

    public IReadOnlyList<AiActionSuggestedComponent> SuggestedComponents { get; init; }

    public IReadOnlyList<AiActionSuggestedNet> SuggestedNets { get; init; }

    public IReadOnlyList<AiActionSuggestedConstraint> SuggestedConstraints { get; init; }

    public IReadOnlyList<AiActionFirmwareNote> FirmwareNotes { get; init; }

    public IReadOnlyList<AiActionDiagnostic> Diagnostics { get; init; }

    public AiActionConfidence Confidence { get; init; }

    public string Explanation { get; init; }

    public AiActionReviewStatus ReviewStatus { get; init; }

    public IReadOnlyList<AiActionDecision> Decisions { get; init; }

    public bool IsApproved => ReviewStatus == AiActionReviewStatus.Approved;

    public bool CanMutateDesign => false;

    public AiActionPlan Approve(string reviewer, string rationale)
    {
        return WithDecision(AiActionDecisionKind.Approved, reviewer, rationale, AiActionReviewStatus.Approved);
    }

    public AiActionPlan Reject(string reviewer, string rationale)
    {
        return WithDecision(AiActionDecisionKind.Rejected, reviewer, rationale, AiActionReviewStatus.Rejected);
    }

    private AiActionPlan WithDecision(
        AiActionDecisionKind kind,
        string reviewer,
        string rationale,
        AiActionReviewStatus reviewStatus)
    {
        var decisions = Decisions
            .Append(new AiActionDecision(
                kind,
                RequireText(reviewer, nameof(reviewer)),
                RequireText(rationale, nameof(rationale)),
                DateTimeOffset.UtcNow))
            .ToArray();

        return this with
        {
            ReviewStatus = reviewStatus,
            Decisions = decisions,
        };
    }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return value;
    }
}
