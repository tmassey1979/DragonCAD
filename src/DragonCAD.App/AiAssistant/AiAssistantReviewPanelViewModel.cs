using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using DragonCAD.Core.Projects;

namespace DragonCAD.App.AiAssistant;

public sealed class AiAssistantReviewPanelViewModel : INotifyPropertyChanged
{
    private readonly ObservableCollection<AiAssistantReviewDecision> reviewDecisions = [];

    private AiAssistantReviewPanelViewModel(
        AiAssistantProviderStatus providerStatus,
        IReadOnlyList<AiAssistantActionPlanRow> rows)
    {
        ProviderStatus = providerStatus;
        ActionPlans = new ObservableCollection<AiAssistantActionPlanRow>(rows);
        ReviewDecisions = new ReadOnlyObservableCollection<AiAssistantReviewDecision>(reviewDecisions);
        ActionPlans.CollectionChanged += OnActionPlansChanged;

        foreach (AiAssistantActionPlanRow row in ActionPlans)
        {
            row.DecisionRecorded += OnDecisionRecorded;
            row.PropertyChanged += OnActionPlanPropertyChanged;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public AiAssistantProviderStatus ProviderStatus { get; }

    public ObservableCollection<AiAssistantActionPlanRow> ActionPlans { get; }

    public ReadOnlyObservableCollection<AiAssistantReviewDecision> ReviewDecisions { get; }

    public bool IsProviderDisabled => !ProviderStatus.IsEnabled;

    public string ProviderStatusTitle => IsProviderDisabled
        ? $"{ProviderStatus.ProviderName} disabled"
        : $"{ProviderStatus.ProviderName} ready";

    public string ProviderStatusMessage => ProviderStatus.Message;

    public int ActionPlanCount => ActionPlans.Count;

    public int PendingCount => ActionPlans.Count(row => row.ReviewState == AiAssistantReviewState.Pending);

    public int ApprovedCount => ActionPlans.Count(row => row.ReviewState == AiAssistantReviewState.Approved);

    public int RejectedCount => ActionPlans.Count(row => row.ReviewState == AiAssistantReviewState.Rejected);

    public string ActionPlanCountLabel => FormatCount(ActionPlanCount, "action plan", "action plans");

    public string ReviewSummary =>
        ActionPlanCount == 0
            ? IsProviderDisabled ? ProviderStatusMessage : "No AI action plans to review."
            : PendingCount == 0
                ? $"All {ActionPlanCountLabel} reviewed"
                : $"{FormatCount(PendingCount, "pending")} across {ActionPlanCountLabel}";

    public static AiAssistantReviewPanelViewModel FromProvider(
        IAiEngineeringAssistantProvider provider,
        AiAssistantReviewContext context)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(context);

        AiAssistantProviderStatus status = provider.Status;
        IReadOnlyList<AiEngineeringActionPlan> plans = status.IsEnabled
            ? provider.GetActionPlans(context)
            : [];

        return new AiAssistantReviewPanelViewModel(
            status,
            plans.Select(AiAssistantActionPlanRow.FromPlan).ToArray());
    }

    public static AiAssistantReviewPanelViewModel FromPlans(
        IReadOnlyList<AiEngineeringActionPlan> plans,
        DragonProject? project = null)
    {
        ArgumentNullException.ThrowIfNull(plans);

        _ = project;
        return new AiAssistantReviewPanelViewModel(
            AiAssistantProviderStatus.Enabled("Local review"),
            plans.Select(AiAssistantActionPlanRow.FromPlan).ToArray());
    }

    private void OnActionPlansChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        if (args.OldItems is not null)
        {
            foreach (AiAssistantActionPlanRow row in args.OldItems)
            {
                row.DecisionRecorded -= OnDecisionRecorded;
                row.PropertyChanged -= OnActionPlanPropertyChanged;
            }
        }

        if (args.NewItems is not null)
        {
            foreach (AiAssistantActionPlanRow row in args.NewItems)
            {
                row.DecisionRecorded += OnDecisionRecorded;
                row.PropertyChanged += OnActionPlanPropertyChanged;
            }
        }

        OnDisplayPropertiesChanged();
    }

    private void OnActionPlanPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(AiAssistantActionPlanRow.ReviewState))
        {
            OnDisplayPropertiesChanged();
        }
    }

    private void OnDecisionRecorded(object? sender, AiAssistantReviewDecision decision)
    {
        reviewDecisions.Add(decision);
        OnPropertyChanged(nameof(ReviewDecisions));
    }

    private void OnDisplayPropertiesChanged()
    {
        OnPropertyChanged(nameof(ActionPlanCount));
        OnPropertyChanged(nameof(PendingCount));
        OnPropertyChanged(nameof(ApprovedCount));
        OnPropertyChanged(nameof(RejectedCount));
        OnPropertyChanged(nameof(ActionPlanCountLabel));
        OnPropertyChanged(nameof(ReviewSummary));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private static string FormatCount(int count, string singularLabel, string? pluralLabel = null) =>
        count == 1 ? $"1 {singularLabel}" : $"{count} {pluralLabel ?? singularLabel}";
}

public sealed class AiAssistantActionPlanRow : INotifyPropertyChanged
{
    private AiAssistantReviewState reviewState = AiAssistantReviewState.Pending;

    private AiAssistantActionPlanRow(AiEngineeringActionPlan plan)
    {
        PlanId = RequireText(plan.Id, nameof(plan.Id));
        Title = RequireText(plan.Title, nameof(plan.Title));
        Confidence = Math.Clamp(plan.Confidence, 0, 1);
        Explanation = RequireText(plan.Explanation, nameof(plan.Explanation));
        AffectedObjects = plan.AffectedObjects.ToArray();
        Diagnostics = plan.Diagnostics.ToArray();

        ApproveCommand = new DelegateCommand(Approve, () => CanReview);
        RejectCommand = new DelegateCommand(Reject, () => CanReview);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler<AiAssistantReviewDecision>? DecisionRecorded;

    public string PlanId { get; }

    public string Title { get; }

    public double Confidence { get; }

    public string Explanation { get; }

    public IReadOnlyList<AiAssistantAffectedObject> AffectedObjects { get; }

    public IReadOnlyList<AiAssistantDiagnostic> Diagnostics { get; }

    public DelegateCommand ApproveCommand { get; }

    public DelegateCommand RejectCommand { get; }

    public AiAssistantReviewState ReviewState
    {
        get => reviewState;
        private set
        {
            if (reviewState == value)
            {
                return;
            }

            reviewState = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ReviewStateDisplay));
            OnPropertyChanged(nameof(ReviewNote));
            OnPropertyChanged(nameof(CanReview));
            ApproveCommand.RaiseCanExecuteChanged();
            RejectCommand.RaiseCanExecuteChanged();
        }
    }

    public string ConfidenceLabel =>
        string.Create(CultureInfo.InvariantCulture, $"{Confidence * 100:0}% confidence");

    public string DiagnosticsBadge => Diagnostics.Count == 0
        ? "No diagnostics"
        : Diagnostics.Count == 1 ? "1 diagnostic" : $"{Diagnostics.Count} diagnostics";

    public string ReviewStateDisplay =>
        ReviewState switch
        {
            AiAssistantReviewState.Pending => "Pending Review",
            AiAssistantReviewState.Approved => "Approved",
            AiAssistantReviewState.Rejected => "Rejected",
            _ => throw new InvalidOperationException($"Unsupported AI assistant review state {ReviewState}.")
        };

    public string ReviewNote =>
        ReviewState switch
        {
            AiAssistantReviewState.Pending => "Waiting for local engineering review.",
            AiAssistantReviewState.Approved => "Approved locally; no design changes were applied.",
            AiAssistantReviewState.Rejected => "Rejected locally; the action plan was not applied.",
            _ => throw new InvalidOperationException($"Unsupported AI assistant review state {ReviewState}.")
        };

    public bool CanReview => ReviewState == AiAssistantReviewState.Pending;

    public static AiAssistantActionPlanRow FromPlan(AiEngineeringActionPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return new AiAssistantActionPlanRow(plan);
    }

    private void Approve() => RecordDecision(AiAssistantReviewState.Approved);

    private void Reject() => RecordDecision(AiAssistantReviewState.Rejected);

    private void RecordDecision(AiAssistantReviewState state)
    {
        if (!CanReview)
        {
            return;
        }

        ReviewState = state;
        DecisionRecorded?.Invoke(
            this,
            new AiAssistantReviewDecision(PlanId, state, "local-review", DateTimeOffset.UtcNow));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private static string RequireText(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value is required.", parameterName) : value.Trim();
}

public interface IAiEngineeringAssistantProvider
{
    AiAssistantProviderStatus Status { get; }

    IReadOnlyList<AiEngineeringActionPlan> GetActionPlans(AiAssistantReviewContext context);
}

public sealed record AiAssistantProviderStatus(string ProviderName, bool IsEnabled, string Message)
{
    public static AiAssistantProviderStatus Enabled(string providerName) =>
        new(RequireText(providerName, nameof(providerName)), true, "AI engineering suggestions are available.");

    public static AiAssistantProviderStatus Disabled(string providerName, string message) =>
        new(RequireText(providerName, nameof(providerName)), false, RequireText(message, nameof(message)));

    private static string RequireText(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value is required.", parameterName) : value.Trim();
}

public sealed record AiAssistantReviewContext(DragonProject? Project)
{
    public static AiAssistantReviewContext ForProject(DragonProject project)
    {
        ArgumentNullException.ThrowIfNull(project);
        return new AiAssistantReviewContext(project);
    }
}

public sealed record AiEngineeringActionPlan(
    string Id,
    string Title,
    double Confidence,
    string Explanation,
    IReadOnlyList<AiAssistantAffectedObject> AffectedObjects,
    IReadOnlyList<AiAssistantDiagnostic> Diagnostics);

public sealed record AiAssistantAffectedObject(
    AiAssistantAffectedObjectKind Kind,
    string Label,
    string Target)
{
    public string DisplayText => $"{Kind} {Label} -> {Target}";
}

public sealed record AiAssistantDiagnostic(string Code, string Message)
{
    public string DisplayText => $"{Code}: {Message}";
}

public sealed record AiAssistantReviewDecision(
    string PlanId,
    AiAssistantReviewState State,
    string Source,
    DateTimeOffset CreatedAt);

public enum AiAssistantAffectedObjectKind
{
    Component,
    Net,
    File
}

public enum AiAssistantReviewState
{
    Pending,
    Approved,
    Rejected
}
