using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DragonCAD.App.Datasheets;

public sealed class DatasheetReviewQueueViewModel : INotifyPropertyChanged
{
    private static readonly IReadOnlyList<string> ReviewStateFilterLabels =
    [
        "All",
        "Pending",
        "Promoted",
        "Rejected"
    ];

    private static readonly IReadOnlyList<string> ReviewCategoryFilterLabels =
    [
        "Ready",
        "Blocked",
        "Duplicate",
        "Needs Data",
        "Rejected"
    ];

    private readonly IReadOnlyList<DatasheetReviewRow> allRows;
    private DatasheetReviewCategoryFilter selectedReviewCategoryFilter;
    private DatasheetReviewStateFilter selectedReviewStateFilter;
    private DatasheetReviewRow? selectedRow;

    private DatasheetReviewQueueViewModel(IReadOnlyList<DatasheetReviewRow> rows)
    {
        allRows = SortRows(rows).ToArray();
        Rows = new ObservableCollection<DatasheetReviewRow>(allRows);
        selectedRow = Rows.FirstOrDefault();

        foreach (DatasheetReviewRow row in allRows)
        {
            row.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName is nameof(DatasheetReviewRow.ReviewState))
                {
                    ApplyFilters();
                }
            };
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<DatasheetReviewRow> Rows { get; }

    public IReadOnlyList<string> ReviewStateFilterOptions => ReviewStateFilterLabels;

    public IReadOnlyList<string> ReviewCategoryFilterOptions => ReviewCategoryFilterLabels;

    public DatasheetReviewCategoryFilter SelectedReviewCategoryFilter
    {
        get => selectedReviewCategoryFilter;
        set
        {
            if (selectedReviewCategoryFilter == value)
            {
                return;
            }

            selectedReviewCategoryFilter = value;
            ApplyFilters();
            OnPropertyChanged();
        }
    }

    public DatasheetReviewStateFilter SelectedReviewStateFilter
    {
        get => selectedReviewStateFilter;
        set
        {
            if (selectedReviewStateFilter == value)
            {
                return;
            }

            selectedReviewStateFilter = value;
            ApplyFilters();
            OnPropertyChanged();
        }
    }

    public DatasheetReviewRow? SelectedRow
    {
        get => selectedRow;
        set
        {
            if (selectedRow == value)
            {
                return;
            }

            selectedRow = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedRowDetails));
        }
    }

    public DatasheetReviewRowDetails? SelectedRowDetails =>
        SelectedRow is null
            ? null
            : new DatasheetReviewRowDetails(
                SelectedRow.DraftId,
                SelectedRow.ReviewNotes,
                SelectedRow.Provenance.Select(item => item.ToDisplayLine()).ToArray());

    public static DatasheetReviewQueueViewModel FromRows(IEnumerable<DatasheetReviewRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        return new DatasheetReviewQueueViewModel(rows.ToArray());
    }

    private void ApplyFilters()
    {
        IEnumerable<DatasheetReviewRow> filteredRows = allRows;
        if (selectedReviewStateFilter != DatasheetReviewStateFilter.All)
        {
            DatasheetReviewState targetState = selectedReviewStateFilter switch
            {
                DatasheetReviewStateFilter.Pending => DatasheetReviewState.Pending,
                DatasheetReviewStateFilter.Promoted => DatasheetReviewState.Promoted,
                DatasheetReviewStateFilter.Rejected => DatasheetReviewState.Rejected,
                _ => throw new InvalidOperationException($"Unsupported review state filter {selectedReviewStateFilter}.")
            };

            filteredRows = filteredRows.Where(row => row.ReviewState == targetState);
        }

        if (selectedReviewCategoryFilter != DatasheetReviewCategoryFilter.All)
        {
            filteredRows = filteredRows.Where(row => row.FilterCategory == selectedReviewCategoryFilter);
        }

        Rows.Clear();
        foreach (DatasheetReviewRow row in SortRows(filteredRows))
        {
            Rows.Add(row);
        }

        SelectedRow = Rows.FirstOrDefault();
    }

    private static IEnumerable<DatasheetReviewRow> SortRows(IEnumerable<DatasheetReviewRow> rows) =>
        rows
            .OrderBy(row => GetCategorySortOrder(row.FilterCategory))
            .ThenBy(row => row.DraftId, StringComparer.Ordinal);

    private static int GetCategorySortOrder(DatasheetReviewCategoryFilter category) =>
        category switch
        {
            DatasheetReviewCategoryFilter.Ready => 0,
            DatasheetReviewCategoryFilter.Blocked => 1,
            DatasheetReviewCategoryFilter.Duplicate => 2,
            DatasheetReviewCategoryFilter.NeedsData => 3,
            DatasheetReviewCategoryFilter.Rejected => 4,
            DatasheetReviewCategoryFilter.All => 5,
            _ => throw new InvalidOperationException($"Unsupported review category filter {category}.")
        };

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class DatasheetReviewRow : INotifyPropertyChanged
{
    private readonly IReadOnlyList<DatasheetReviewDiagnostic> diagnostics;
    private readonly IReadOnlyList<DatasheetReviewProvenance> provenance;
    private readonly IReadOnlyList<DatasheetReviewWarning> warnings;
    private DatasheetReviewState reviewState = DatasheetReviewState.Pending;
    private string rejectReason = "";

    public DatasheetReviewRow(
        string componentName,
        string datasheetSource,
        int extractedPinCount,
        DatasheetProposalStatus symbolStatus,
        DatasheetProposalStatus footprintStatus,
        DatasheetProposalStatus threeDimensionalModelStatus,
        DatasheetReviewConfidence confidence,
        IReadOnlyList<DatasheetReviewWarning> warnings,
        string draftId = "",
        string manufacturerPartNumber = "",
        DatasheetReviewCategory category = DatasheetReviewCategory.Ready,
        IReadOnlyList<DatasheetReviewDiagnostic>? diagnostics = null,
        IReadOnlyList<DatasheetReviewProvenance>? provenance = null,
        string reviewNotes = "")
    {
        if (string.IsNullOrWhiteSpace(componentName))
        {
            throw new ArgumentException("Component name is required.", nameof(componentName));
        }

        if (string.IsNullOrWhiteSpace(datasheetSource))
        {
            throw new ArgumentException("Datasheet source is required.", nameof(datasheetSource));
        }

        if (extractedPinCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(extractedPinCount), extractedPinCount, "Extracted pin count cannot be negative.");
        }

        DraftId = string.IsNullOrWhiteSpace(draftId) ? componentName : draftId.Trim();
        ComponentName = componentName;
        ManufacturerPartNumber = manufacturerPartNumber.Trim();
        DatasheetSource = datasheetSource;
        ExtractedPinCount = extractedPinCount;
        SymbolStatus = symbolStatus;
        FootprintStatus = footprintStatus;
        ThreeDimensionalModelStatus = threeDimensionalModelStatus;
        Confidence = confidence;
        Category = category;
        this.diagnostics = (diagnostics ?? []).ToArray();
        this.provenance = (provenance ?? []).ToArray();
        ReviewNotes = string.IsNullOrWhiteSpace(reviewNotes) ? "No review notes." : reviewNotes.Trim();
        this.warnings = warnings.ToArray();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string DraftId { get; }

    public string ComponentName { get; }

    public string ManufacturerPartNumber { get; }

    public string DatasheetSource { get; }

    public int ExtractedPinCount { get; }

    public DatasheetProposalStatus SymbolStatus { get; }

    public DatasheetProposalStatus FootprintStatus { get; }

    public DatasheetProposalStatus ThreeDimensionalModelStatus { get; }

    public DatasheetReviewConfidence Confidence { get; }

    public DatasheetReviewCategory Category { get; }

    public IReadOnlyList<DatasheetReviewDiagnostic> Diagnostics => diagnostics;

    public IReadOnlyList<DatasheetReviewProvenance> Provenance => provenance;

    public string ReviewNotes { get; }

    public ObservableCollection<DatasheetReviewDecisionRecord> DecisionRecords { get; } = [];

    public IReadOnlyList<DatasheetReviewWarning> Warnings => warnings;

    public bool MutatedTrustedLibrary => false;

    public DatasheetReviewState ReviewState
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
            OnPropertyChanged(nameof(IsApproved));
            OnPropertyChanged(nameof(CanApprove));
            OnPropertyChanged(nameof(FilterCategory));
            OnPropertyChanged(nameof(RecommendedAction));
        }
    }

    public string RejectReason
    {
        get => rejectReason;
        private set
        {
            if (rejectReason == value)
            {
                return;
            }

            rejectReason = value;
            OnPropertyChanged();
        }
    }

    public string SourceDisplay
    {
        get
        {
            if (Uri.TryCreate(DatasheetSource, UriKind.Absolute, out Uri? sourceUri) && !string.IsNullOrWhiteSpace(sourceUri.Host))
            {
                return $"{sourceUri.Host}{sourceUri.AbsolutePath}";
            }

            return DatasheetSource;
        }
    }

    public string ExtractedPinsSummary => ExtractedPinCount == 1 ? "1 pin extracted" : $"{ExtractedPinCount} pins extracted";

    public string SymbolStatusDisplay => $"Symbol {FormatProposalStatus(SymbolStatus)}";

    public string FootprintStatusDisplay => $"Footprint {FormatProposalStatus(FootprintStatus)}";

    public string ThreeDimensionalModelStatusDisplay => $"3D model {FormatProposalStatus(ThreeDimensionalModelStatus)}";

    public string ConfidenceDisplay => $"{Confidence} confidence";

    public string WarningDisplay => warnings.Count == 0 ? "No warnings" : string.Join("; ", warnings.Select(warning => warning.Message));

    public string BlockerDisplay
    {
        get
        {
            string[] blockers = diagnostics
                .Where(diagnostic => diagnostic.Severity == DatasheetReviewDiagnosticSeverity.Blocker)
                .Select(diagnostic => diagnostic.Message)
                .Concat(warnings
                    .Where(warning => warning.Severity == DatasheetReviewWarningSeverity.Critical)
                    .Select(warning => warning.Message))
                .ToArray();

            return blockers.Length == 0 ? "No blockers" : string.Join("; ", blockers);
        }
    }

    public bool HasCriticalWarnings => warnings.Any(warning => warning.Severity == DatasheetReviewWarningSeverity.Critical) ||
        diagnostics.Any(diagnostic => diagnostic.Severity == DatasheetReviewDiagnosticSeverity.Blocker);

    public bool CanApprove => ReviewState == DatasheetReviewState.Pending &&
        Category == DatasheetReviewCategory.Ready &&
        Confidence == DatasheetReviewConfidence.High &&
        !HasCriticalWarnings;

    public bool IsApproved => ReviewState == DatasheetReviewState.Promoted;

    public DatasheetReviewCategoryFilter FilterCategory =>
        ReviewState == DatasheetReviewState.Rejected
            ? DatasheetReviewCategoryFilter.Rejected
            : Category switch
            {
                DatasheetReviewCategory.Ready => DatasheetReviewCategoryFilter.Ready,
                DatasheetReviewCategory.Blocked => DatasheetReviewCategoryFilter.Blocked,
                DatasheetReviewCategory.Duplicate => DatasheetReviewCategoryFilter.Duplicate,
                DatasheetReviewCategory.NeedsData => DatasheetReviewCategoryFilter.NeedsData,
                _ => throw new InvalidOperationException($"Unsupported review category {Category}.")
            };

    public string RecommendedAction =>
        ReviewState == DatasheetReviewState.Rejected
            ? "Rejected"
            : Category switch
            {
                DatasheetReviewCategory.Ready when CanApprove => "Approve",
                DatasheetReviewCategory.Ready => "Review confidence",
                DatasheetReviewCategory.Blocked => "Resolve blockers",
                DatasheetReviewCategory.Duplicate => "Compare duplicate",
                DatasheetReviewCategory.NeedsData => "Request more data",
                _ => throw new InvalidOperationException($"Unsupported review category {Category}.")
            };

    public string ApproveLabel => "Approve";

    public string RejectLabel => "Reject";

    public bool Approve() => Approve("Approved.");

    public bool Approve(string note)
    {
        if (!CanApprove)
        {
            return false;
        }

        ReviewState = DatasheetReviewState.Promoted;
        RecordDecision("Approved for promotion", note);
        return true;
    }

    public void Reject(string reason)
    {
        RejectReason = string.IsNullOrWhiteSpace(reason) ? "Rejected without note." : reason.Trim();
        ReviewState = DatasheetReviewState.Rejected;
        RecordDecision("Rejected", RejectReason);
    }

    public void Defer(string note)
    {
        ReviewState = DatasheetReviewState.Pending;
        RecordDecision("Deferred", note);
    }

    private void RecordDecision(string decision, string note) =>
        DecisionRecords.Add(new DatasheetReviewDecisionRecord(
            DraftId,
            decision,
            string.IsNullOrWhiteSpace(note) ? "No reviewer note." : note.Trim(),
            MutatedTrustedLibrary));

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private static string FormatProposalStatus(DatasheetProposalStatus status) =>
        status switch
        {
            DatasheetProposalStatus.Ready => "ready",
            DatasheetProposalStatus.NeedsReview => "needs review",
            DatasheetProposalStatus.Placeholder => "placeholder",
            DatasheetProposalStatus.Missing => "missing",
            _ => throw new InvalidOperationException($"Unsupported proposal status {status}.")
        };
}

public sealed record DatasheetReviewRowDetails(
    string DraftId,
    string ReviewNotes,
    IReadOnlyList<string> ProvenanceDisplayLines)
{
    public string ProvenanceDisplay => string.Join(Environment.NewLine, ProvenanceDisplayLines);
}

public sealed record DatasheetReviewDiagnostic(DatasheetReviewDiagnosticSeverity Severity, string Message);

public sealed record DatasheetReviewProvenance(string Label, string Value, bool IsSecret)
{
    public string ToDisplayLine() => $"{Label}: {(IsSecret ? "[redacted]" : Value)}";
}

public sealed record DatasheetReviewDecisionRecord(
    string DraftId,
    string Decision,
    string ReviewerNote,
    bool MutatedTrustedLibrary);

public sealed record DatasheetReviewWarning(DatasheetReviewWarningSeverity Severity, string Message);

public enum DatasheetReviewDiagnosticSeverity
{
    Info,
    Warning,
    Blocker,
}

public enum DatasheetReviewWarningSeverity
{
    Info,
    Warning,
    Critical,
}

public enum DatasheetProposalStatus
{
    Missing,
    Placeholder,
    NeedsReview,
    Ready,
}

public enum DatasheetReviewConfidence
{
    Low,
    Medium,
    High,
}

public enum DatasheetReviewCategory
{
    Ready,
    Blocked,
    Duplicate,
    NeedsData,
}

public enum DatasheetReviewState
{
    Pending,
    Promoted,
    Rejected,
}

public enum DatasheetReviewStateFilter
{
    All,
    Pending,
    Promoted,
    Rejected,
}

public enum DatasheetReviewCategoryFilter
{
    All,
    Ready,
    Blocked,
    Duplicate,
    NeedsData,
    Rejected,
}
