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
        allSuggestions = SortSuggestions(suggestions);
        Suggestions = new ObservableCollection<DatasheetCandidateLinkSuggestion>(allSuggestions);
        selectedSuggestion = Suggestions.FirstOrDefault();

        foreach (DatasheetCandidateLinkSuggestion suggestion in allSuggestions)
        {
            suggestion.PropertyChanged += SuggestionPropertyChanged;
            suggestion.DecisionRecorded += (_, decision) =>
            {
                Decisions.Add(decision);
                RebuildDiagnostics();
                OnPropertyChanged(nameof(Summary));
                OnPropertyChanged(nameof(ReviewSummary));
            };
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<DatasheetCandidateLinkSuggestion> Suggestions { get; }

    public ObservableCollection<DatasheetCandidateLinkDecisionRecord> Decisions { get; } = [];

    public ObservableCollection<DatasheetCandidateLinkDiagnostic> Diagnostics { get; } = [];

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

    public string ReviewSummary =>
        string.Join(
            "\n",
            [
                "Datasheet candidate link review",
                $"Intake records: {allSuggestions.Select(suggestion => suggestion.IntakeRecordId).Distinct(StringComparer.Ordinal).Count()}",
                $"Suggestions: {allSuggestions.Count}",
                $"Accepted: {allSuggestions.Count(suggestion => suggestion.ReviewState == DatasheetCandidateLinkReviewState.Accepted)}",
                $"Rejected: {allSuggestions.Count(suggestion => suggestion.ReviewState == DatasheetCandidateLinkReviewState.Rejected)}",
                $"Pending: {allSuggestions.Count(suggestion => suggestion.ReviewState == DatasheetCandidateLinkReviewState.Pending)}",
                $"Diagnostics: {Diagnostics.Count}",
                "Decisions:",
                .. Decisions
                    .OrderBy(decision => decision.ReviewedAt)
                    .ThenBy(decision => decision.IntakeRecordId, StringComparer.Ordinal)
                    .ThenBy(decision => decision.TargetId, StringComparer.Ordinal)
                    .Select(FormatDecisionSummary)
            ]);

    public static DatasheetCandidateLinkingViewModel CreateSample() =>
        new(
        [
            new DatasheetCandidateLinkSuggestion(
                intakeRecordId: "intake:lm7805ct",
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
                intakeRecordId: "intake:lm7805ct",
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
                intakeRecordId: "intake:lm7805ct",
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
                intakeRecordId: "intake:lm7805ct",
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
        foreach (DatasheetCandidateLinkSuggestion suggestion in SortSuggestions(filteredSuggestions))
        {
            Suggestions.Add(suggestion);
        }

        SelectedSuggestion = Suggestions.FirstOrDefault();
    }

    private void RebuildDiagnostics()
    {
        Diagnostics.Clear();

        foreach (IGrouping<string, DatasheetCandidateLinkDecisionRecord> acceptedGroup in Decisions
            .Where(decision => decision.ReviewState == DatasheetCandidateLinkReviewState.Accepted)
            .GroupBy(decision => decision.IntakeRecordId)
            .Where(group => group.Select(decision => decision.TargetId).Distinct(StringComparer.Ordinal).Count() > 1)
            .OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            string targetIds = string.Join(
                ", ",
                acceptedGroup
                    .Select(decision => decision.TargetId)
                    .Distinct(StringComparer.Ordinal)
                    .Order(StringComparer.Ordinal));

            Diagnostics.Add(
                new DatasheetCandidateLinkDiagnostic(
                    acceptedGroup.Key,
                    DatasheetCandidateLinkDiagnosticSeverity.Warning,
                    $"Accepted link conflict for {acceptedGroup.Key}: {targetIds}"));
        }

        OnPropertyChanged(nameof(ReviewSummary));
    }

    private static IReadOnlyList<DatasheetCandidateLinkSuggestion> SortSuggestions(IEnumerable<DatasheetCandidateLinkSuggestion> suggestions) =>
        suggestions
            .OrderByDescending(suggestion => suggestion.Confidence)
            .ThenBy(suggestion => GetTargetTypeSortOrder(suggestion.TargetType))
            .ThenBy(suggestion => suggestion.TargetId, StringComparer.Ordinal)
            .ToArray();

    private static int GetTargetTypeSortOrder(DatasheetCandidateLinkTargetType targetType) =>
        targetType switch
        {
            DatasheetCandidateLinkTargetType.CanonicalComponent => 0,
            DatasheetCandidateLinkTargetType.VendorCatalogRow => 1,
            DatasheetCandidateLinkTargetType.ImportedCandidate => 2,
            DatasheetCandidateLinkTargetType.NewCandidatePlaceholder => 3,
            _ => throw new InvalidOperationException($"Unsupported candidate link target type {targetType}.")
        };

    private static string FormatDecisionSummary(DatasheetCandidateLinkDecisionRecord decision) =>
        $"- {decision.ReviewedAt:O} | {decision.IntakeRecordId} | {decision.Decision} | {decision.TargetType} | {decision.TargetId} | {decision.Confidence} | {decision.MatchBasis} | {decision.ReviewerNote}";

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class DatasheetCandidateLinkSuggestion : INotifyPropertyChanged
{
    private DatasheetCandidateLinkReviewState reviewState = DatasheetCandidateLinkReviewState.Pending;
    private string statusText = "Review pending";

    public DatasheetCandidateLinkSuggestion(
        string intakeRecordId,
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
        IntakeRecordId = intakeRecordId;
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

    public string IntakeRecordId { get; }

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
        => Accept(reviewerNote, DateTimeOffset.UtcNow);

    public void Accept(string reviewerNote, DateTimeOffset reviewedAt)
    {
        ReviewState = DatasheetCandidateLinkReviewState.Accepted;
        StatusText = $"Accepted link {TargetId} for {CandidateName}";
        DecisionRecorded?.Invoke(this, CreateDecisionRecord(DatasheetCandidateLinkReviewState.Accepted, reviewerNote, reviewedAt));
    }

    public void Reject(string reviewerNote)
        => Reject(reviewerNote, DateTimeOffset.UtcNow);

    public void Reject(string reviewerNote, DateTimeOffset reviewedAt)
    {
        ReviewState = DatasheetCandidateLinkReviewState.Rejected;
        StatusText = $"Rejected link {TargetId} for {CandidateName}";
        DecisionRecorded?.Invoke(this, CreateDecisionRecord(DatasheetCandidateLinkReviewState.Rejected, reviewerNote, reviewedAt));
    }

    private DatasheetCandidateLinkDecisionRecord CreateDecisionRecord(
        DatasheetCandidateLinkReviewState decision,
        string reviewerNote,
        DateTimeOffset reviewedAt) =>
        new(
            IntakeRecordId,
            CandidateName,
            SourceManufacturerPartNumber,
            TargetId,
            TargetType,
            decision == DatasheetCandidateLinkReviewState.Accepted ? "Accepted" : "Rejected",
            decision,
            Confidence,
            MatchBasis,
            Conflicts,
            string.IsNullOrWhiteSpace(reviewerNote) ? "No reviewer note." : reviewerNote.Trim(),
            reviewedAt,
            MutatedTrustedLibrary);

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed record DatasheetCandidateLinkConflict(
    string FieldName,
    string SourceValue,
    string TargetValue);

public sealed record DatasheetCandidateLinkDecisionRecord(
    string IntakeRecordId,
    string CandidateName,
    string SourceManufacturerPartNumber,
    string TargetId,
    DatasheetCandidateLinkTargetType TargetType,
    string Decision,
    DatasheetCandidateLinkReviewState ReviewState,
    DatasheetCandidateLinkConfidence Confidence,
    string MatchBasis,
    IReadOnlyList<DatasheetCandidateLinkConflict> Conflicts,
    string ReviewerNote,
    DateTimeOffset ReviewedAt,
    bool MutatedTrustedLibrary);

public sealed record DatasheetCandidateLinkDiagnostic(
    string IntakeRecordId,
    DatasheetCandidateLinkDiagnosticSeverity Severity,
    string Message);

public enum DatasheetCandidateLinkDiagnosticSeverity
{
    Warning,
}

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
