using DragonCAD.App.AiAssistant;
using DragonCAD.Core.Projects;

namespace DragonCAD.App.Tests.AiAssistant;

public sealed class AiAssistantReviewPanelViewModelTests
{
    [Fact]
    public void FromProviderExplainsDisabledCodexOllamaConfigurationWithoutRequestingPlans()
    {
        var provider = new RecordingAiAssistantProvider(
            AiAssistantProviderStatus.Disabled(
                "Local AI",
                "Configure Codex or Ollama to enable local engineering suggestions."),
            []);

        AiAssistantReviewPanelViewModel viewModel = AiAssistantReviewPanelViewModel.FromProvider(
            provider,
            AiAssistantReviewContext.ForProject(Project()));

        Assert.True(viewModel.IsProviderDisabled);
        Assert.Equal("Local AI disabled", viewModel.ProviderStatusTitle);
        Assert.Equal("Configure Codex or Ollama to enable local engineering suggestions.", viewModel.ProviderStatusMessage);
        Assert.Empty(viewModel.ActionPlans);
        Assert.Equal(0, provider.RequestCount);
    }

    [Fact]
    public void FromProviderDisplaysReviewableActionPlansWithConfidenceExplanationAffectedObjectsAndDiagnostics()
    {
        var provider = new RecordingAiAssistantProvider(
            AiAssistantProviderStatus.Enabled("Fake provider"),
            [
                Plan()
            ]);

        AiAssistantReviewPanelViewModel viewModel = AiAssistantReviewPanelViewModel.FromProvider(
            provider,
            AiAssistantReviewContext.ForProject(Project()));

        Assert.False(viewModel.IsProviderDisabled);
        Assert.Equal("1 action plan", viewModel.ActionPlanCountLabel);
        Assert.Equal(1, provider.RequestCount);

        AiAssistantActionPlanRow row = Assert.Single(viewModel.ActionPlans);
        Assert.Equal("plan-erc-decoupling", row.PlanId);
        Assert.Equal("Add local decoupling guidance", row.Title);
        Assert.Equal("82% confidence", row.ConfidenceLabel);
        Assert.Equal("U1 has power pins but no nearby bypass capacitor candidate.", row.Explanation);
        Assert.Equal("2 diagnostics", row.DiagnosticsBadge);
        Assert.Equal("Pending Review", row.ReviewStateDisplay);

        Assert.Equal(
            [
                "ERC001: U1 VCC should be checked for a local bypass capacitor.",
                "DRC014: Route VCC before approving the layout handoff."
            ],
            row.Diagnostics.Select(diagnostic => diagnostic.DisplayText).ToArray());
    }

    [Fact]
    public void AffectedObjectLinksPreserveComponentsNetsAndFiles()
    {
        AiAssistantActionPlanRow row = Assert.Single(AiAssistantReviewPanelViewModel.FromPlans([Plan()]).ActionPlans);

        Assert.Collection(
            row.AffectedObjects,
            affected =>
            {
                Assert.Equal(AiAssistantAffectedObjectKind.Component, affected.Kind);
                Assert.Equal("U1", affected.Label);
                Assert.Equal("component:U1", affected.Target);
                Assert.Equal("Component U1 -> component:U1", affected.DisplayText);
            },
            affected =>
            {
                Assert.Equal(AiAssistantAffectedObjectKind.Net, affected.Kind);
                Assert.Equal("VCC", affected.Label);
                Assert.Equal("net:VCC", affected.Target);
                Assert.Equal("Net VCC -> net:VCC", affected.DisplayText);
            },
            affected =>
            {
                Assert.Equal(AiAssistantAffectedObjectKind.File, affected.Kind);
                Assert.Equal("schematic/schematic.json", affected.Label);
                Assert.Equal("schematic/schematic.json", affected.Target);
                Assert.Equal("File schematic/schematic.json -> schematic/schematic.json", affected.DisplayText);
            });
    }

    [Fact]
    public void ApproveCreatesLocalReviewDecisionRecordOnly()
    {
        DragonProject project = Project();
        DragonProject before = Project();
        AiAssistantReviewPanelViewModel viewModel = AiAssistantReviewPanelViewModel.FromPlans([Plan()], project);

        AiAssistantActionPlanRow row = Assert.Single(viewModel.ActionPlans);
        row.ApproveCommand.Execute(null);

        Assert.Equal(AiAssistantReviewState.Approved, row.ReviewState);
        Assert.Equal("Approved", row.ReviewStateDisplay);
        Assert.Equal("Approved locally; no design changes were applied.", row.ReviewNote);
        Assert.False(row.ApproveCommand.CanExecute(null));
        Assert.False(row.RejectCommand.CanExecute(null));

        AiAssistantReviewDecision decision = Assert.Single(viewModel.ReviewDecisions);
        Assert.Equal("plan-erc-decoupling", decision.PlanId);
        Assert.Equal(AiAssistantReviewState.Approved, decision.State);
        Assert.Equal("local-review", decision.Source);
        Assert.Equal(before, project);
    }

    [Fact]
    public void RejectCreatesLocalReviewDecisionRecordOnly()
    {
        DragonProject project = Project();
        DragonProject before = Project();
        AiAssistantReviewPanelViewModel viewModel = AiAssistantReviewPanelViewModel.FromPlans([Plan()], project);

        AiAssistantActionPlanRow row = Assert.Single(viewModel.ActionPlans);
        row.RejectCommand.Execute(null);

        Assert.Equal(AiAssistantReviewState.Rejected, row.ReviewState);
        Assert.Equal("Rejected", row.ReviewStateDisplay);
        Assert.Equal("Rejected locally; the action plan was not applied.", row.ReviewNote);

        AiAssistantReviewDecision decision = Assert.Single(viewModel.ReviewDecisions);
        Assert.Equal("plan-erc-decoupling", decision.PlanId);
        Assert.Equal(AiAssistantReviewState.Rejected, decision.State);
        Assert.Equal(before, project);
    }

    [Fact]
    public void ReviewCommandsDoNotMutateSchematicOrBoardDocuments()
    {
        DragonProject project = Project();
        string schematicDocumentId = project.Schematic.DocumentId;
        string boardDocumentId = project.Board.DocumentId;
        DragonSchematicComponent[] componentsBefore = project.Schematic.Components.ToArray();
        DragonSchematicNet[] netsBefore = project.Schematic.Nets.ToArray();
        DragonBoardPlacement[] placementsBefore = project.Board.Placements.ToArray();
        DragonBoardTrace[] tracesBefore = project.Board.Traces.ToArray();

        AiAssistantReviewPanelViewModel viewModel = AiAssistantReviewPanelViewModel.FromPlans([Plan()], project);

        AiAssistantActionPlanRow row = Assert.Single(viewModel.ActionPlans);
        row.ApproveCommand.Execute(null);

        Assert.Equal(schematicDocumentId, project.Schematic.DocumentId);
        Assert.Equal(boardDocumentId, project.Board.DocumentId);
        Assert.Equal(componentsBefore, project.Schematic.Components);
        Assert.Equal(netsBefore, project.Schematic.Nets);
        Assert.Equal(placementsBefore, project.Board.Placements);
        Assert.Equal(tracesBefore, project.Board.Traces);
    }

    private static AiEngineeringActionPlan Plan() =>
        new(
            "plan-erc-decoupling",
            "Add local decoupling guidance",
            0.82,
            "U1 has power pins but no nearby bypass capacitor candidate.",
            [
                new AiAssistantAffectedObject(AiAssistantAffectedObjectKind.Component, "U1", "component:U1"),
                new AiAssistantAffectedObject(AiAssistantAffectedObjectKind.Net, "VCC", "net:VCC"),
                new AiAssistantAffectedObject(AiAssistantAffectedObjectKind.File, "schematic/schematic.json", "schematic/schematic.json")
            ],
            [
                new AiAssistantDiagnostic("ERC001", "U1 VCC should be checked for a local bypass capacitor."),
                new AiAssistantDiagnostic("DRC014", "Route VCC before approving the layout handoff.")
            ]);

    private static DragonProject Project() =>
        new(
            new DragonProjectManifest("AI Review Fixture", new Version(1, 0), "test"),
            new DragonSchematicDocument(
                "schematic-1",
                [new DragonSchematicComponent("cmp-u1", "U1", "mcu:atmega328p")],
                [new DragonSchematicNet("VCC", ["U1.7"])]),
            new DragonBoardDocument(
                "board-1",
                [new DragonBoardPlacement("cmp-u1", "U1", "TQFP-32", 10m, 20m, 0m)],
                [new DragonBoardTrace("VCC", "Top", 0.25m)]),
            new DragonLibraryReferences([]),
            new DragonDatasheetIntake([]),
            new DragonFabricationMetadata([], []),
            []);

    private sealed class RecordingAiAssistantProvider : IAiEngineeringAssistantProvider
    {
        private readonly IReadOnlyList<AiEngineeringActionPlan> plans;

        public RecordingAiAssistantProvider(
            AiAssistantProviderStatus status,
            IReadOnlyList<AiEngineeringActionPlan> plans)
        {
            Status = status;
            this.plans = plans;
        }

        public AiAssistantProviderStatus Status { get; }

        public int RequestCount { get; private set; }

        public IReadOnlyList<AiEngineeringActionPlan> GetActionPlans(AiAssistantReviewContext context)
        {
            RequestCount++;
            return plans;
        }
    }
}
