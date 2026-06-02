namespace DragonCAD.ComponentIntelligence.AiActions;

public interface IAiActionProvider
{
    AiActionProviderResult CreatePlan(AiActionRequest request);
}

public sealed record AiActionProviderResult(AiActionPlan? Plan, IReadOnlyList<AiActionDiagnostic> Diagnostics)
{
    public static AiActionProviderResult FromPlan(AiActionPlan plan)
    {
        return new AiActionProviderResult(plan, []);
    }

    public static AiActionProviderResult FromDiagnostics(params AiActionDiagnostic[] diagnostics)
    {
        return new AiActionProviderResult(null, diagnostics);
    }
}

public sealed record AiActionProviderConfiguration(
    AiActionProviderKind Kind,
    Uri? Endpoint,
    string ModelName)
{
    public static AiActionProviderConfiguration Disabled()
    {
        return new AiActionProviderConfiguration(AiActionProviderKind.Disabled, Endpoint: null, ModelName: "disabled");
    }

    public static AiActionProviderConfiguration Ollama(Uri endpoint, string modelName)
    {
        return new AiActionProviderConfiguration(AiActionProviderKind.Ollama, endpoint, RequireText(modelName, nameof(modelName)));
    }

    public static AiActionProviderConfiguration Codex(string modelName)
    {
        return new AiActionProviderConfiguration(AiActionProviderKind.Codex, Endpoint: null, RequireText(modelName, nameof(modelName)));
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

public static class AiActionProviderFactory
{
    public static IAiActionProvider Create(AiActionProviderConfiguration configuration)
    {
        return configuration.Kind switch
        {
            AiActionProviderKind.Disabled => new DisabledAiActionProvider(),
            AiActionProviderKind.Fake => new FakeAiActionProvider(),
            AiActionProviderKind.Ollama or AiActionProviderKind.Codex => new DisabledAiActionProvider(
                $"{configuration.Kind} provider is configured but live AI calls are outside this review-only boundary."),
            _ => throw new ArgumentOutOfRangeException(nameof(configuration)),
        };
    }
}

public sealed class DisabledAiActionProvider : IAiActionProvider
{
    private readonly string _message;

    public DisabledAiActionProvider(string? message = null)
    {
        _message = message ?? "AI action planning is disabled; no provider call was made.";
    }

    public AiActionProviderResult CreatePlan(AiActionRequest request)
    {
        return AiActionProviderResult.FromDiagnostics(
            new AiActionDiagnostic(AiActionDiagnosticCode.ProviderDisabled, _message));
    }
}

public sealed class FakeAiActionProvider : IAiActionProvider
{
    private readonly string _planId;

    public FakeAiActionProvider(string planId = "fake-ai-action-plan")
    {
        _planId = planId;
    }

    public AiActionProviderResult CreatePlan(AiActionRequest request)
    {
        var plan = new AiActionPlan(
            id: _planId,
            request: request,
            provider: new AiActionProviderDescriptor(AiActionProviderKind.Fake, "deterministic-fake"),
            suggestedComponents:
            [
                new AiActionSuggestedComponent(
                    ReferenceDesignator: "LED1",
                    Description: "status LED",
                    ManufacturerPartNumber: null),
            ],
            suggestedNets:
            [
                new AiActionSuggestedNet(
                    Name: "STATUS_LED",
                    Participants: ["MCU GPIO", "LED1", "R1"],
                    Explanation: "Connects firmware-controlled GPIO to the proposed indicator LED."),
            ],
            suggestedConstraints:
            [
                new AiActionSuggestedConstraint(
                    AiActionConstraintKind.Electrical,
                    "Limit LED current with a series resistor sized for the 3.3 V rail."),
            ],
            firmwareNotes:
            [
                new AiActionFirmwareNote("Reserve one GPIO output for the status LED and default it low at boot."),
            ],
            diagnostics:
            [
                new AiActionDiagnostic(
                    AiActionDiagnosticCode.HumanReviewRequired,
                    "AI action plans are suggestions and require human review before design changes."),
            ],
            confidence: AiActionConfidence.Medium,
            explanation: "Deterministic fake suggestion for review-only AI action planning.");

        return AiActionProviderResult.FromPlan(plan);
    }
}
