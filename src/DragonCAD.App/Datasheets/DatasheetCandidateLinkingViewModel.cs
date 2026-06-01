using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DragonCAD.App.Datasheets;

public sealed class DatasheetCandidateLinkingViewModel : INotifyPropertyChanged
{
    private static readonly IReadOnlyList<string> ReviewStateFilterLabels =
    [
        "All",
        "Pending",
        "Accepted",
        "Rejected"
    ];

    private readonly IReadOnlyList<DatasheetCandidateLinkSuggestion> allSuggestions;
    private DatasheetCandidateLinkReviewStateFilter selectedReviewStateFilter;
    private DatasheetCandidateLinkSuggestion? selectedSuggestion;

    private DatasheetCandidateLinkingViewModel(IReadOnlyList<DatasheetCandidateLinkSuggestion> suggestions)
    {
        allSuggestions = suggestions;
        Suggestions = new ObservableCollection<DatasheetCandidateLinkSuggestion>(suggestions);
        selectedSuggestion = Suggestions.FirstOrDefault();

        foreach (DatasheetCandidateLinkSuggestion suggestion in allSuggestions)
        {
            suggestion.PropertyChanged += SuggestionPropertyChanged;
            suggestion.DecisionRecorded += (_, decision) =>
            {
                Decisions.Add(decision);
                OnPropertyChanged(nameof(Summary));
            };
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<DatasheetCandidateLinkSuggestion> Suggestions { get; }

    public ObservableCollection<DatasheetCandidateLinkDecisionRecord> Decisions { get; } = [];

    public IReadOnlyList<string> ReviewStateFilterOptions => ReviewStateFilterLabels;

    public DatasheetCandidateLinkReviewStateFilter SelectedReviewStateFilter
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

    public DatasheetCandidateLinkSuggestion? SelectedSuggestion
    {
        get => selectedSuggestion;
        private set
        {
            if (selectedSuggestion == value)
            {
                return;
            }

            selectedSuggestion = value;
            OnPropertyChanged();
        }
    }

    public string Summary =>
        $"{allSuggestions.Count} datasheet candidate link suggestions, {Decisions.Count} decisions recorded";

    public static DatasheetCandidateLinkingViewModel CreateSample() =>
        new(
        [
            new DatasheetCandidateLinkSuggestion(
                candidateName: "LM7805CT",
                sourceManufacturerPartNumber: "LM7805CT",
                sourcePackageName: "TO-220-3",
                targetId: "dragon:lm7805",
                targetName: "LM7805 5V Linear Regulator",
                targetType: DatasheetCandidateLinkTargetType.CanonicalComponent,
                targetManufacturerPartNumber: "LM7805CT",
                targetPackageName: "TO-220-3",
                confidence: DatasheetCandidateLinkConfidence.High,
                matchBasis: "Exact MPN and package match",
                conflicts: []),
            new DatasheetCandidateLinkSuggestion(
                candidateName: "LM7805CT",
                sourceManufacturerPartNumber: "LM7805CT",
                sourcePackageName: "TO-220-3",
                targetId: "dragon:lm7805-smd",
                targetName: "LM7805 SMD Package Variant",
                targetType: DatasheetCandidateLinkTargetType.ImportedCandidate,
                targetManufacturerPartNumber: "LM7805CT",
                targetPackageName: "SOT-223",
                confidence: DatasheetCandidateLinkConfidence.Medium,
                matchBasis: "MPN match with package conflict",
                conflicts:
                [
                    new DatasheetCandidateLinkConflict("Package", "TO-220-3", "SOT-223")
                ]),
            new DatasheetCandidateLinkSuggestion(
                candidateName: "LM7805CT",
                sourceManufacturerPartNumber: "LM7805CT",
                sourcePackageName: "TO-220-3",
                targetId: "vendor:digikey:296-1389-5-ND",
                targetName: "Digi-Key LM7805CT Offer",
                targetType: DatasheetCandidateLinkTargetType.VendorCatalogRow,
                targetManufacturerPartNumber: "LM7805CT",
                targetPackageName: "TO-220-3",
                confidence: DatasheetCandidateLinkConfidence.High,
                matchBasis: "Vendor SKU and MPN match",
                conflicts: []),
            new DatasheetCandidateLinkSuggestion(
                candidateName: "LM7805CT",
                sourceManufacturerPartNumber: "LM7805CT",
                sourcePackageName: "TO-220-3",
                targetId: "new:lm7805ct",
                targetName: "Create New LM7805CT Candidate",
                targetType: DatasheetCandidateLinkTargetType.NewCandidatePlaceholder,
                targetManufacturerPartNumber: "",
                targetPackageName: "TO-220-3",
                confidence: DatasheetCandidateLinkConfidence.Low,
                matchBasis: "No trusted component selected",
                conflicts: [])
        ]);

    private void SuggestionPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName is nameof(DatasheetCandidateLinkSuggestion.ReviewState))
        {
            ApplyFilters();
        }
    }

    private void ApplyFilters()
    {
        IEnumerable<DatasheetCandidateLinkSuggestion> filteredSuggestions = allSuggestions;
        if (SelectedReviewStateFilter != DatasheetCandidateLinkReviewStateFilter.All)
        {
            DatasheetCandidateLinkReviewState targetState = SelectedReviewStateFilter switch
            {
                DatasheetCandidateLinkReviewStateFilter.Pending => DatasheetCandidateLinkReviewState.Pending,
                DatasheetCandidateLinkReviewStateFilter.Accepted => DatasheetCandidateLinkReviewState.Accepted,
                DatasheetCandidateLinkReviewStateFilter.Rejected => DatasheetCandidateLinkReviewState.Rejected,
                _ => throw new InvalidOperationException($"Unsupported candidate link filter {SelectedReviewStateFilter}.")
            };

            filteredSuggestions = filteredSuggestions.Where(suggestion => suggestion.ReviewState == targetState);
        }

        Suggestions.Clear();
        foreach (DatasheetCandidateLinkSuggestion suggestion in filteredSuggestions)
        {
            Suggestions.Add(suggestion);
        }

        SelectedSuggestion = Suggestions.FirstOrDefault();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class DatasheetCandidateLinkSuggestion : INotifyPropertyChanged
{
    private DatasheetCandidateLinkReviewState reviewState = DatasheetCandidateLinkReviewState.Pending;
    private string statusText = "Review pending";

    public DatasheetCandidateLinkSuggestion(
        string candidateName,
        string sourceManufacturerPartNumber,
        string sourcePackageName,
        string targetId,
        string targetName,
        DatasheetCandidateLinkTargetType targetType,
        string targetManufacturerPartNumber,
        string targetPackageName,
        DatasheetCandidateLinkConfidence confidence,
        string matchBasis,
        IReadOnlyList<DatasheetCandidateLinkConflict> conflicts)
    {
        CandidateName = candidateName;
        SourceManufacturerPartNumber = sourceManufacturerPartNumber;
        SourcePackageName = sourcePackageName;
        TargetId = targetId;
        TargetName = targetName;
        TargetType = targetType;
        TargetManufacturerPartNumber = targetManufacturerPartNumber;
        TargetPackageName = targetPackageName;
        Confidence = confidence;
        MatchBasis = matchBasis;
        Conflicts = conflicts.ToArray();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler<DatasheetCandidateLinkDecisionRecord>? DecisionRecorded;

    public string CandidateName { get; }

    public string SourceManufacturerPartNumber { get; }

    public string SourcePackageName { get; }

    public string TargetId { get; }

    public string TargetName { get; }

    public DatasheetCandidateLinkTargetType TargetType { get; }

    public string TargetManufacturerPartNumber { get; }

    public string TargetPackageName { get; }

    public DatasheetCandidateLinkConfidence Confidence { get; }

    public string MatchBasis { get; }

    public IReadOnlyList<DatasheetCandidateLinkConflict> Conflicts { get; }

    public bool MutatedTrustedLibrary => false;

    public DatasheetCandidateLinkReviewState ReviewState
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
        }
    }

    public string StatusText
    {
        get => statusText;
        private set
        {
            if (statusText == value)
            {
                return;
            }

            statusText = value;
            OnPropertyChanged();
        }
    }

    public string TargetTypeDisplay =>
        TargetType switch
        {
            DatasheetCandidateLinkTargetType.CanonicalComponent => "Canonical Component",
            DatasheetCandidateLinkTargetType.VendorCatalogRow => "Vendor Catalog Row",
            DatasheetCandidateLinkTargetType.ImportedCandidate => "Imported Candidate",
            DatasheetCandidateLinkTargetType.NewCandidatePlaceholder => "New Candidate Placeholder",
            _ => throw new InvalidOperationException($"Unsupported candidate link target type {TargetType}.")
        };

    public string ReviewStateDisplay =>
        ReviewState switch
        {
            DatasheetCandidateLinkReviewState.Pending => "Review pending",
            DatasheetCandidateLinkReviewState.Accepted => "Accepted",
            DatasheetCandidateLinkReviewState.Rejected => "Rejected",
            _ => throw new InvalidOperationException($"Unsupported candidate link review state {ReviewState}.")
        };

    public string ConflictDisplay =>
        Conflicts.Count == 0
            ? "No conflicts"
            : string.Join("; ", Conflicts.Select(conflict => $"{conflict.FieldName}: source {conflict.SourceValue} vs target {conflict.TargetValue}"));

    public void Accept(string reviewerNote)
    {
        ReviewState = DatasheetCandidateLinkReviewState.Accepted;
        StatusText = $"Accepted link {TargetId} for {CandidateName}";
        DecisionRecorded?.Invoke(this, CreateDecisionRecord("Accepted", reviewerNote));
    }

    public void Reject(string reviewerNote)
    {
        ReviewState = DatasheetCandidateLinkReviewState.Rejected;
        StatusText = $"Rejected link {TargetId} for {CandidateName}";
        DecisionRecorded?.Invoke(this, CreateDecisionRecord("Rejected", reviewerNote));
    }

    private DatasheetCandidateLinkDecisionRecord CreateDecisionRecord(string decision, string reviewerNote) =>
        new(
            CandidateName,
            SourceManufacturerPartNumber,
            TargetId,
            TargetType,
            decision,
            string.IsNullOrWhiteSpace(reviewerNote) ? "No reviewer note." : reviewerNote.Trim(),
            MutatedTrustedLibrary);

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed record DatasheetCandidateLinkConflict(
    string FieldName,
    string SourceValue,
    string TargetValue);

public sealed record DatasheetCandidateLinkDecisionRecord(
    string CandidateName,
    string SourceManufacturerPartNumber,
    string TargetId,
    DatasheetCandidateLinkTargetType TargetType,
    string Decision,
    string ReviewerNote,
    bool MutatedTrustedLibrary);

public enum DatasheetCandidateLinkTargetType
{
    CanonicalComponent,
    VendorCatalogRow,
    ImportedCandidate,
    NewCandidatePlaceholder,
}

public enum DatasheetCandidateLinkConfidence
{
    Low,
    Medium,
    High,
}

public enum DatasheetCandidateLinkReviewState
{
    Pending,
    Accepted,
    Rejected,
}

public enum DatasheetCandidateLinkReviewStateFilter
{
    All,
    Pending,
    Accepted,
    Rejected,
}
