using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DragonCAD.App.Datasheets;

public sealed class DatasheetIntakeQueueViewModel : INotifyPropertyChanged
{
    private static readonly IReadOnlyList<string> ReviewStateFilterLabels =
    [
        "All",
        "Review Required",
        "Linked",
        "Rejected"
    ];

    private readonly IDatasheetIntakeClock clock;
    private readonly List<DatasheetIntakeItem> allItems = [];
    private DatasheetIntakeReviewStateFilter selectedReviewStateFilter;
    private DatasheetIntakeItem? selectedItem;

    public DatasheetIntakeQueueViewModel(IDatasheetIntakeClock? clock = null)
    {
        this.clock = clock ?? SystemDatasheetIntakeClock.Instance;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<DatasheetIntakeItem> Items { get; } = [];

    public IReadOnlyList<string> ReviewStateFilterOptions => ReviewStateFilterLabels;

    public DatasheetIntakeReviewStateFilter SelectedReviewStateFilter
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

    public DatasheetIntakeItem? SelectedItem
    {
        get => selectedItem;
        private set
        {
            if (selectedItem == value)
            {
                return;
            }

            selectedItem = value;
            OnPropertyChanged();
        }
    }

    public string Summary =>
        allItems.Count switch
        {
            0 => "No datasheet intake items pending review",
            1 => "1 datasheet intake item pending review",
            int count => $"{count} datasheet intake items pending review"
        };

    public DatasheetIntakeSubmissionResult Submit(DatasheetIntakeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        List<DatasheetIntakeDiagnostic> diagnostics = Validate(request);
        if (diagnostics.Count > 0)
        {
            return new DatasheetIntakeSubmissionResult(false, diagnostics);
        }

        DatasheetIntakeSourceType sourceType = ResolveSourceType(request.SourceIdentifier);
        var item = new DatasheetIntakeItem(
            SourceType: sourceType,
            SourceIdentifier: request.SourceIdentifier.Trim(),
            SubmittedActor: string.IsNullOrWhiteSpace(request.SubmittedActor) ? "Unknown" : request.SubmittedActor.Trim(),
            SubmittedAt: clock.UtcNow,
            ManufacturerPartNumber: request.ManufacturerPartNumber.Trim(),
            VendorProductId: request.VendorProductId.Trim(),
            PackageName: request.PackageName.Trim(),
            SourceNotes: request.SourceNotes.Trim(),
            ReviewState: DatasheetIntakeReviewState.ReviewRequired,
            WasFetched: false,
            MutatedTrustedLibrary: false);

        allItems.Add(item);
        ApplyFilters();
        OnPropertyChanged(nameof(Summary));
        return new DatasheetIntakeSubmissionResult(true, []);
    }

    private List<DatasheetIntakeDiagnostic> Validate(DatasheetIntakeRequest request)
    {
        List<DatasheetIntakeDiagnostic> diagnostics = [];
        string sourceIdentifier = request.SourceIdentifier.Trim();

        if (string.IsNullOrWhiteSpace(sourceIdentifier))
        {
            diagnostics.Add(new DatasheetIntakeDiagnostic("missing-source-identifier", "A datasheet file path or URL is required."));
            return diagnostics;
        }

        DatasheetIntakeSourceType sourceType = ResolveSourceType(sourceIdentifier);
        if (sourceType == DatasheetIntakeSourceType.Unsupported)
        {
            diagnostics.Add(new DatasheetIntakeDiagnostic("unsupported-datasheet-source", "Only local PDF files and HTTP/HTTPS datasheet URLs are supported for intake."));
            return diagnostics;
        }

        if (sourceType == DatasheetIntakeSourceType.LocalPdf && !File.Exists(sourceIdentifier))
        {
            diagnostics.Add(new DatasheetIntakeDiagnostic("local-file-not-found", "The local datasheet PDF does not exist."));
            return diagnostics;
        }

        if (allItems.Any(item => string.Equals(item.SourceIdentifier, sourceIdentifier, StringComparison.OrdinalIgnoreCase)))
        {
            diagnostics.Add(new DatasheetIntakeDiagnostic("duplicate-datasheet-intake", "This datasheet source is already in the intake queue."));
        }

        return diagnostics;
    }

    private void ApplyFilters()
    {
        IEnumerable<DatasheetIntakeItem> filteredItems = allItems;
        if (SelectedReviewStateFilter != DatasheetIntakeReviewStateFilter.All)
        {
            DatasheetIntakeReviewState targetState = SelectedReviewStateFilter switch
            {
                DatasheetIntakeReviewStateFilter.ReviewRequired => DatasheetIntakeReviewState.ReviewRequired,
                DatasheetIntakeReviewStateFilter.Linked => DatasheetIntakeReviewState.Linked,
                DatasheetIntakeReviewStateFilter.Rejected => DatasheetIntakeReviewState.Rejected,
                _ => throw new InvalidOperationException($"Unsupported datasheet intake filter {SelectedReviewStateFilter}.")
            };

            filteredItems = filteredItems.Where(item => item.ReviewState == targetState);
        }

        Items.Clear();
        foreach (DatasheetIntakeItem item in filteredItems)
        {
            Items.Add(item);
        }

        SelectedItem = Items.FirstOrDefault();
    }

    private static DatasheetIntakeSourceType ResolveSourceType(string sourceIdentifier)
    {
        if (Uri.TryCreate(sourceIdentifier, UriKind.Absolute, out Uri? uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return DatasheetIntakeSourceType.Url;
        }

        return string.Equals(Path.GetExtension(sourceIdentifier), ".pdf", StringComparison.OrdinalIgnoreCase)
            ? DatasheetIntakeSourceType.LocalPdf
            : DatasheetIntakeSourceType.Unsupported;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed record DatasheetIntakeRequest(
    string SourceIdentifier,
    string SubmittedActor,
    string ManufacturerPartNumber,
    string VendorProductId,
    string PackageName,
    string SourceNotes);

public sealed record DatasheetIntakeSubmissionResult(
    bool Accepted,
    IReadOnlyList<DatasheetIntakeDiagnostic> Diagnostics);

public sealed record DatasheetIntakeDiagnostic(string Code, string Message);

public sealed record DatasheetIntakeItem(
    DatasheetIntakeSourceType SourceType,
    string SourceIdentifier,
    string SubmittedActor,
    DateTimeOffset SubmittedAt,
    string ManufacturerPartNumber,
    string VendorProductId,
    string PackageName,
    string SourceNotes,
    DatasheetIntakeReviewState ReviewState,
    bool WasFetched,
    bool MutatedTrustedLibrary)
{
    public string SourceTypeDisplay =>
        SourceType switch
        {
            DatasheetIntakeSourceType.LocalPdf => "Local PDF",
            DatasheetIntakeSourceType.Url => "URL",
            _ => "Unsupported"
        };

    public string ReviewStateDisplay =>
        ReviewState switch
        {
            DatasheetIntakeReviewState.ReviewRequired => "Review required",
            DatasheetIntakeReviewState.Linked => "Linked",
            DatasheetIntakeReviewState.Rejected => "Rejected",
            _ => throw new InvalidOperationException($"Unsupported datasheet intake state {ReviewState}.")
        };

    public string SourceDisplay
    {
        get
        {
            if (Uri.TryCreate(SourceIdentifier, UriKind.Absolute, out Uri? sourceUri) && !string.IsNullOrWhiteSpace(sourceUri.Host))
            {
                return $"{sourceUri.Host}{sourceUri.AbsolutePath}";
            }

            return SourceIdentifier;
        }
    }
}

public interface IDatasheetIntakeClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class SystemDatasheetIntakeClock : IDatasheetIntakeClock
{
    public static SystemDatasheetIntakeClock Instance { get; } = new();

    private SystemDatasheetIntakeClock()
    {
    }

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

public enum DatasheetIntakeSourceType
{
    Unsupported,
    LocalPdf,
    Url,
}

public enum DatasheetIntakeReviewState
{
    ReviewRequired,
    Linked,
    Rejected,
}

public enum DatasheetIntakeReviewStateFilter
{
    All,
    ReviewRequired,
    Linked,
    Rejected,
}
