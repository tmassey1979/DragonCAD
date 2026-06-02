using DragonCAD.ComponentIntelligence.AiActions;

namespace DragonCAD.ComponentIntelligence.Tests.AiActions;

public sealed class AiActionPlanTests
{
    [Fact]
    public void DisabledProviderReturnsDiagnosticWithoutPlan()
    {
        var provider = AiActionProviderFactory.Create(AiActionProviderConfiguration.Disabled());
        var request = CreateRequest("Add a status LED to the board.");

        AiActionProviderResult result = provider.CreatePlan(request);

        Assert.Null(result.Plan);
        AiActionDiagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(AiActionDiagnosticCode.ProviderDisabled, diagnostic.Code);
        Assert.Contains("disabled", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FakeProviderCreatesDeterministicReviewOnlyPlan()
    {
        var provider = new FakeAiActionProvider(planId: "plan-led-001");
        var request = CreateRequest("Add a status LED to the board.");

        AiActionProviderResult result = provider.CreatePlan(request);

        Assert.Empty(result.Diagnostics);
        Assert.NotNull(result.Plan);
        AiActionPlan plan = result.Plan;
        Assert.Equal("plan-led-001", plan.Id);
        Assert.Equal(AiActionReviewStatus.PendingReview, plan.ReviewStatus);
        Assert.False(plan.IsApproved);
        Assert.False(plan.CanMutateDesign);
        Assert.Equal(AiActionProviderKind.Fake, plan.Provider.Kind);
        Assert.Equal(AiActionConfidence.Medium, plan.Confidence);
        Assert.Equal("Add a status LED to the board.", plan.Request.Instruction);
        Assert.Contains("review", plan.Explanation, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(plan.Decisions);

        Assert.Collection(
            plan.SuggestedComponents,
            component =>
            {
                Assert.Equal("LED1", component.ReferenceDesignator);
                Assert.Equal("status LED", component.Description);
            });
        Assert.Collection(
            plan.SuggestedNets,
            net =>
            {
                Assert.Equal("STATUS_LED", net.Name);
                Assert.Contains("LED1", net.Participants);
            });
        Assert.Collection(
            plan.SuggestedConstraints,
            constraint =>
            {
                Assert.Equal(AiActionConstraintKind.Electrical, constraint.Kind);
                Assert.Contains("current", constraint.Description, StringComparison.OrdinalIgnoreCase);
            });
        Assert.Collection(
            plan.FirmwareNotes,
            note => Assert.Contains("GPIO", note.Text, StringComparison.OrdinalIgnoreCase));
        Assert.Collection(
            plan.Diagnostics,
            diagnostic => Assert.Equal(AiActionDiagnosticCode.HumanReviewRequired, diagnostic.Code));
    }

    [Fact]
    public void ApproveDecisionRecordsReviewerAndKeepsPlanReviewOnly()
    {
        AiActionPlan plan = CreatePlan();

        AiActionPlan decided = plan.Approve("casey", "Matches the design intent.");

        AiActionDecision decision = Assert.Single(decided.Decisions);
        Assert.Equal(AiActionDecisionKind.Approved, decision.Kind);
        Assert.Equal("casey", decision.Reviewer);
        Assert.Equal("Matches the design intent.", decision.Rationale);
        Assert.Equal(AiActionReviewStatus.Approved, decided.ReviewStatus);
        Assert.True(decided.IsApproved);
        Assert.False(decided.CanMutateDesign);
    }

    [Fact]
    public void RejectDecisionRecordsReviewerAndRationale()
    {
        AiActionPlan plan = CreatePlan();

        AiActionPlan decided = plan.Reject("casey", "Conflicts with the power budget.");

        AiActionDecision decision = Assert.Single(decided.Decisions);
        Assert.Equal(AiActionDecisionKind.Rejected, decision.Kind);
        Assert.Equal("casey", decision.Reviewer);
        Assert.Equal("Conflicts with the power budget.", decision.Rationale);
        Assert.Equal(AiActionReviewStatus.Rejected, decided.ReviewStatus);
        Assert.False(decided.IsApproved);
        Assert.False(decided.CanMutateDesign);
    }

    [Fact]
    public void ProviderConfigurationCapturesOllamaAndCodexWithoutCreatingLiveProvider()
    {
        var ollama = AiActionProviderConfiguration.Ollama(new Uri("http://localhost:11434"), "llama3.1");
        var codex = AiActionProviderConfiguration.Codex("gpt-5");

        Assert.Equal(AiActionProviderKind.Ollama, ollama.Kind);
        Assert.Equal("http://localhost:11434/", ollama.Endpoint?.ToString());
        Assert.Equal("llama3.1", ollama.ModelName);
        Assert.Equal(AiActionProviderKind.Codex, codex.Kind);
        Assert.Null(codex.Endpoint);
        Assert.Equal("gpt-5", codex.ModelName);
    }

    private static AiActionRequest CreateRequest(string instruction)
    {
        return new AiActionRequest(
            Instruction: instruction,
            DesignContext: "MCU board with available GPIO and 3.3 V rail.");
    }

    private static AiActionPlan CreatePlan()
    {
        var provider = new FakeAiActionProvider(planId: "plan-led-001");
        AiActionPlan? plan = provider.CreatePlan(CreateRequest("Add a status LED to the board.")).Plan;
        Assert.NotNull(plan);
        return plan;
    }
}
