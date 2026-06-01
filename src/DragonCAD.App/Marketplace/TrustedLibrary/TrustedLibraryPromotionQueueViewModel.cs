using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DragonCAD.Core.Components.Identity;
using DragonCAD.Sourcing.TrustedLibrary;

namespace DragonCAD.App.Marketplace.TrustedLibrary;

public sealed class TrustedLibraryPromotionQueueViewModel : INotifyPropertyChanged
{
    private TrustedLibraryPromotionQueueViewModel(IReadOnlyList<TrustedLibraryPromotionRow> rows, bool mutatesCoreLibrary)
    {
        MutatesCoreLibrary = mutatesCoreLibrary;
        Rows = new ObservableCollection<TrustedLibraryPromotionRow>(rows);

        foreach (TrustedLibraryPromotionRow row in Rows)
        {
            row.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName is nameof(TrustedLibraryPromotionRow.ReviewState))
                {
                    NotifyQueueStatusChanged();
                }
            };
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<TrustedLibraryPromotionRow> Rows { get; }

    public bool MutatesCoreLibrary { get; }

    public int ReadyCount => CountRows(TrustedLibraryMatchReviewState.Approved);

    public int PendingCount => CountRows(TrustedLibraryMatchReviewState.PendingReview);

    public int BlockedCount => CountRows(TrustedLibraryMatchReviewState.Rejected);

    public string ReadyStatusLabel => FormatStatusLabel(ReadyCount, "ready");

    public string PendingStatusLabel => FormatStatusLabel(PendingCount, "pending");

    public string BlockedStatusLabel => FormatStatusLabel(BlockedCount, "blocked");

    public string QueueStatusSummary => $"{ReadyStatusLabel} / {PendingStatusLabel} / {BlockedStatusLabel}";

    public string QueueSummary
    {
        get
        {
            string candidateLabel = Rows.Count == 1 ? "promotion candidate" : "promotion candidates";
            return $"{Rows.Count} {candidateLabel}, {ReadyCount} ready to stage";
        }
    }

    public static TrustedLibraryPromotionQueueViewModel FromPlan(TrustedLibraryVendorMatchPromotionPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        TrustedLibraryPromotionRow[] rows = plan.Records
            .Select(TrustedLibraryPromotionRow.FromRecord)
            .ToArray();

        return new TrustedLibraryPromotionQueueViewModel(rows, plan.MutatesCoreLibrary);
    }

    public static TrustedLibraryPromotionQueueViewModel FromReviewedCandidates(IEnumerable<TrustedLibraryReviewedCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        TrustedLibraryVendorMatchPromotionPlan plan = TrustedLibraryVendorMatchPromotionPlanner.Plan(
            candidates.Select(candidate => candidate.ToReviewedVendorCatalogMatch()));

        return FromPlan(plan);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private int CountRows(TrustedLibraryMatchReviewState reviewState) =>
        Rows.Count(row => row.ReviewState == reviewState);

    private void NotifyQueueStatusChanged()
    {
        OnPropertyChanged(nameof(ReadyCount));
        OnPropertyChanged(nameof(PendingCount));
        OnPropertyChanged(nameof(BlockedCount));
        OnPropertyChanged(nameof(ReadyStatusLabel));
        OnPropertyChanged(nameof(PendingStatusLabel));
        OnPropertyChanged(nameof(BlockedStatusLabel));
        OnPropertyChanged(nameof(QueueStatusSummary));
        OnPropertyChanged(nameof(QueueSummary));
    }

    private static string FormatStatusLabel(int count, string status) => $"{count} {status}";
}

public sealed record TrustedLibraryReviewedCandidate
{
    public TrustedLibraryReviewedCandidate(
        string componentId,
        string provider,
        string sku,
        string manufacturerPartNumber,
        TrustedLibraryMatchReviewState reviewState,
        IReadOnlyList<TrustedLibraryReviewedArtifactCandidate> artifactPaths,
        IReadOnlyList<string> warnings)
    {
        ComponentId = componentId;
        Provider = provider;
        Sku = sku;
        ManufacturerPartNumber = manufacturerPartNumber;
        ReviewState = reviewState;
        ArtifactPaths = artifactPaths.ToArray();
        Warnings = warnings.ToArray();
    }

    public string ComponentId { get; }

    public string Provider { get; }

    public string Sku { get; }

    public string ManufacturerPartNumber { get; }

    public TrustedLibraryMatchReviewState ReviewState { get; }

    public IReadOnlyList<TrustedLibraryReviewedArtifactCandidate> ArtifactPaths { get; }

    public IReadOnlyList<string> Warnings { get; }

    internal ReviewedVendorCatalogMatch ToReviewedVendorCatalogMatch() =>
        new(
            ReviewState,
            Provider,
            Sku,
            ManufacturerPartNumber,
            new ComponentId(ComponentId),
            ArtifactPaths.Select(artifact => artifact.ToTrustedLibraryArtifactPath()).ToArray(),
            Warnings);
}

public sealed record TrustedLibraryReviewedArtifactCandidate(string Kind, string Path, string? Checksum)
{
    internal TrustedLibraryArtifactPath ToTrustedLibraryArtifactPath() => new(Kind, Path, Checksum);
}

public sealed class TrustedLibraryPromotionRow : INotifyPropertyChanged
{
    private readonly IReadOnlyList<TrustedLibraryArtifactPath> artifactPaths;
    private readonly IReadOnlyList<string> warnings;
    private TrustedLibraryMatchReviewState reviewState;

    private TrustedLibraryPromotionRow(
        TrustedLibraryMatchReviewState reviewState,
        string targetComponentId,
        string provider,
        string vendorSku,
        string manufacturerPartNumber,
        IReadOnlyList<TrustedLibraryArtifactPath> artifactPaths,
        IReadOnlyList<string> warnings)
    {
        this.reviewState = reviewState;
        TargetComponentId = targetComponentId;
        Provider = provider;
        VendorSku = vendorSku;
        ManufacturerPartNumber = manufacturerPartNumber;
        this.artifactPaths = artifactPaths.ToArray();
        this.warnings = warnings.ToArray();
        MarkApprovedCommand = new DelegateCommand(MarkApproved);
        MarkRejectedCommand = new DelegateCommand(MarkRejected);
        MarkPendingCommand = new DelegateCommand(MarkPending);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string TargetComponentId { get; }

    public string Provider { get; }

    public string VendorSku { get; }

    public string ManufacturerPartNumber { get; }

    public IReadOnlyList<TrustedLibraryArtifactPath> ArtifactPaths => artifactPaths;

    public IReadOnlyList<string> Warnings => warnings;

    public DelegateCommand MarkApprovedCommand { get; }

    public DelegateCommand MarkRejectedCommand { get; }

    public DelegateCommand MarkPendingCommand { get; }

    public TrustedLibraryMatchReviewState ReviewState
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
            OnPropertyChanged(nameof(ReviewStateLabel));
            OnPropertyChanged(nameof(StageReadiness));
            OnPropertyChanged(nameof(CanStage));
        }
    }

    public string ReviewStateLabel => ReviewState switch
    {
        TrustedLibraryMatchReviewState.PendingReview => "Pending review",
        TrustedLibraryMatchReviewState.Approved => "Approved",
        TrustedLibraryMatchReviewState.Rejected => "Rejected",
        _ => throw new InvalidOperationException($"Unsupported trusted-library review state {ReviewState}.")
    };

    public string StageReadiness => ReviewState switch
    {
        TrustedLibraryMatchReviewState.Approved => "Ready to stage",
        TrustedLibraryMatchReviewState.Rejected => "Rejected",
        TrustedLibraryMatchReviewState.PendingReview => "Blocked until approved",
        _ => throw new InvalidOperationException($"Unsupported trusted-library review state {ReviewState}.")
    };

    public bool CanStage => ReviewState == TrustedLibraryMatchReviewState.Approved;

    public string WarningSummary => warnings.Count == 0 ? "No warnings" : string.Join("; ", warnings);

    public string ArtifactPathSummary => artifactPaths.Count == 0 ? "No artifacts" : string.Join("; ", artifactPaths.Select(artifact => artifact.Summary));

    public static TrustedLibraryPromotionRow FromRecord(TrustedLibraryVendorMatchPromotionRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new TrustedLibraryPromotionRow(
            record.ReviewState,
            record.TargetComponentId.Value,
            record.SourceProvider,
            record.VendorSku,
            record.ManufacturerPartNumber,
            record.ArtifactPaths,
            record.Warnings);
    }

    public void MarkApproved() => ReviewState = TrustedLibraryMatchReviewState.Approved;

    public void MarkRejected() => ReviewState = TrustedLibraryMatchReviewState.Rejected;

    public void MarkPending() => ReviewState = TrustedLibraryMatchReviewState.PendingReview;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
