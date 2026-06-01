using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using DragonCAD.Core.Components.Marketplace.Provenance;

namespace DragonCAD.App.Marketplace.Audit;

public sealed class MarketplaceAuditTimelineViewModel : INotifyPropertyChanged
{
    private readonly IReadOnlyList<MarketplaceAuditTimelineRow> allRows;
    private readonly IReadOnlyList<string> sourceFilterOptions;
    private readonly IReadOnlyList<string> reviewStateFilterOptions;
    private string selectedSourceFilter = "All";
    private string selectedReviewStateFilter = "All";

    private MarketplaceAuditTimelineViewModel(IReadOnlyList<MarketplaceAuditTimelineRow> rows)
    {
        allRows = rows;
        sourceFilterOptions = BuildFilterOptions(rows.Select(row => row.SourceType));
        reviewStateFilterOptions = BuildFilterOptions(rows.Select(row => row.ReviewState));
        Rows = new ObservableCollection<MarketplaceAuditTimelineRow>(rows);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<MarketplaceAuditTimelineRow> Rows { get; }

    public IReadOnlyList<string> SourceFilterOptions => sourceFilterOptions;

    public IReadOnlyList<string> ReviewStateFilterOptions => reviewStateFilterOptions;

    public string SelectedSourceFilter
    {
        get => selectedSourceFilter;
        set
        {
            string nextValue = string.IsNullOrWhiteSpace(value) ? "All" : value;
            if (selectedSourceFilter == nextValue)
            {
                return;
            }

            selectedSourceFilter = nextValue;
            ApplyFilters();
            OnPropertyChanged();
        }
    }

    public string SelectedReviewStateFilter
    {
        get => selectedReviewStateFilter;
        set
        {
            string nextValue = string.IsNullOrWhiteSpace(value) ? "All" : value;
            if (selectedReviewStateFilter == nextValue)
            {
                return;
            }

            selectedReviewStateFilter = nextValue;
            ApplyFilters();
            OnPropertyChanged();
        }
    }

    public static MarketplaceAuditTimelineViewModel FromRecords(IEnumerable<MarketplaceComponentProvenance> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        MarketplaceAuditTimelineRow[] rows = records
            .Select(MarketplaceAuditTimelineRow.FromRecord)
            .OrderByDescending(row => row.Timestamp)
            .ThenBy(row => row.ComponentKey, StringComparer.Ordinal)
            .ThenBy(row => row.SourceType, StringComparer.Ordinal)
            .ThenBy(row => row.Vendor, StringComparer.Ordinal)
            .ToArray();

        return new MarketplaceAuditTimelineViewModel(rows);
    }

    private void ApplyFilters()
    {
        IEnumerable<MarketplaceAuditTimelineRow> rows = allRows;

        if (selectedSourceFilter != "All")
        {
            rows = rows.Where(row => string.Equals(row.SourceType, selectedSourceFilter, StringComparison.Ordinal));
        }

        if (selectedReviewStateFilter != "All")
        {
            rows = rows.Where(row => string.Equals(row.ReviewState, selectedReviewStateFilter, StringComparison.Ordinal));
        }

        Rows.Clear();
        foreach (MarketplaceAuditTimelineRow row in rows)
        {
            Rows.Add(row);
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private static IReadOnlyList<string> BuildFilterOptions(IEnumerable<string> values) =>
        new[] { "All" }
            .Concat(values.Where(value => value.Length > 0).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal))
            .ToArray();
}

public sealed record MarketplaceAuditTimelineRow(
    string ComponentKey,
    string SourceType,
    string Vendor,
    string DatasheetUrl,
    string Generator,
    string ReviewState,
    string Note,
    DateTimeOffset Timestamp)
{
    public string TimestampDisplay => Timestamp
        .ToUniversalTime()
        .ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture);

    public static MarketplaceAuditTimelineRow FromRecord(MarketplaceComponentProvenance record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new MarketplaceAuditTimelineRow(
            record.ComponentKey.Value,
            FormatSourceType(record.Kind),
            record.SourceVendor,
            record.DatasheetUrl,
            record.GeneratorName,
            FormatReviewState(record.ReviewState),
            string.IsNullOrWhiteSpace(record.ReviewerNote) ? "No reviewer note" : record.ReviewerNote,
            record.Timestamp);
    }

    private static string FormatSourceType(MarketplaceProvenanceKind kind) =>
        kind switch
        {
            MarketplaceProvenanceKind.VendorImport => "Vendor Import",
            MarketplaceProvenanceKind.DatasheetGenerated => "Datasheet Generated",
            MarketplaceProvenanceKind.ManualOverride => "Manual Override",
            _ => kind.ToString(),
        };

    private static string FormatReviewState(MarketplaceReviewState reviewState) =>
        reviewState switch
        {
            MarketplaceReviewState.PendingReview => "Pending Review",
            _ => reviewState.ToString(),
        };
}
