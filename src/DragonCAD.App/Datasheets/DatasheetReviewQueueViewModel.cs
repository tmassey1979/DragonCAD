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
        "Approved",
        "Rejected"
    ];

    private readonly IReadOnlyList<DatasheetReviewRow> allRows;
    private DatasheetReviewStateFilter selectedReviewStateFilter;
    private DatasheetReviewRow? selectedRow;

    private DatasheetReviewQueueViewModel(IReadOnlyList<DatasheetReviewRow> rows)
    {
        allRows = rows;
        Rows = new ObservableCollection<DatasheetReviewRow>(rows);
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
        }
    }

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
                DatasheetReviewStateFilter.Approved => DatasheetReviewState.Approved,
                DatasheetReviewStateFilter.Rejected => DatasheetReviewState.Rejected,
                _ => throw new InvalidOperationException($"Unsupported review state filter {selectedReviewStateFilter}.")
            };

            filteredRows = filteredRows.Where(row => row.ReviewState == targetState);
        }

        Rows.Clear();
        foreach (DatasheetReviewRow row in filteredRows)
        {
            Rows.Add(row);
        }

        SelectedRow = Rows.FirstOrDefault();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class DatasheetReviewRow : INotifyPropertyChanged
{
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
        IReadOnlyList<DatasheetReviewWarning> warnings)
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

        ComponentName = componentName;
        DatasheetSource = datasheetSource;
        ExtractedPinCount = extractedPinCount;
        SymbolStatus = symbolStatus;
        FootprintStatus = footprintStatus;
        ThreeDimensionalModelStatus = threeDimensionalModelStatus;
        Confidence = confidence;
        this.warnings = warnings.ToArray();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string ComponentName { get; }

    public string DatasheetSource { get; }

    public int ExtractedPinCount { get; }

    public DatasheetProposalStatus SymbolStatus { get; }

    public DatasheetProposalStatus FootprintStatus { get; }

    public DatasheetProposalStatus ThreeDimensionalModelStatus { get; }

    public DatasheetReviewConfidence Confidence { get; }

    public IReadOnlyList<DatasheetReviewWarning> Warnings => warnings;

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

    public bool HasCriticalWarnings => warnings.Any(warning => warning.Severity == DatasheetReviewWarningSeverity.Critical);

    public bool CanApprove => ReviewState == DatasheetReviewState.Pending && Confidence == DatasheetReviewConfidence.High && !HasCriticalWarnings;

    public bool IsApproved => ReviewState == DatasheetReviewState.Approved;

    public string ApproveLabel => "Approve";

    public string RejectLabel => "Reject";

    public bool Approve()
    {
        if (!CanApprove)
        {
            return false;
        }

        ReviewState = DatasheetReviewState.Approved;
        return true;
    }

    public void Reject(string reason)
    {
        RejectReason = string.IsNullOrWhiteSpace(reason) ? "Rejected without note." : reason.Trim();
        ReviewState = DatasheetReviewState.Rejected;
    }

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

public sealed record DatasheetReviewWarning(DatasheetReviewWarningSeverity Severity, string Message);

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

public enum DatasheetReviewState
{
    Pending,
    Approved,
    Rejected,
}

public enum DatasheetReviewStateFilter
{
    All,
    Pending,
    Approved,
    Rejected,
}
