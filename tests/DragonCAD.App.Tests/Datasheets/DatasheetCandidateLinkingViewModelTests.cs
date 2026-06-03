using DragonCAD.App.Datasheets;

namespace DragonCAD.App.Tests.Datasheets;

public sealed class DatasheetCandidateLinkingViewModelTests
{
    [Fact]
    public void ExactManufacturerPartNumberMatchCreatesHighConfidenceCanonicalSuggestion()
    {
        DatasheetCandidateLinkingViewModel linker = DatasheetCandidateLinkingViewModel.CreateSample();

        DatasheetCandidateLinkSuggestion suggestion = Assert.Single(
            linker.Suggestions,
            row => row.TargetId == "dragon:lm7805");

        Assert.Equal(DatasheetCandidateLinkTargetType.CanonicalComponent, suggestion.TargetType);
        Assert.Equal(DatasheetCandidateLinkConfidence.High, suggestion.Confidence);
        Assert.Equal("Exact MPN and package match", suggestion.MatchBasis);
        Assert.Equal("No conflicts", suggestion.ConflictDisplay);
        Assert.Equal("Review pending", suggestion.ReviewStateDisplay);
        Assert.False(suggestion.MutatedTrustedLibrary);
    }

    [Fact]
    public void SampleSuggestionsCoverEverySupportedTargetTypeForOneIntakeRecord()
    {
        DatasheetCandidateLinkingViewModel linker = DatasheetCandidateLinkingViewModel.CreateSample();

        Assert.All(linker.Suggestions, suggestion => Assert.Equal("intake:lm7805ct", suggestion.IntakeRecordId));
        Assert.Equal(
            [
                DatasheetCandidateLinkTargetType.CanonicalComponent,
                DatasheetCandidateLinkTargetType.VendorCatalogRow,
                DatasheetCandidateLinkTargetType.ImportedCandidate,
                DatasheetCandidateLinkTargetType.NewCandidatePlaceholder
            ],
            linker.Suggestions.Select(suggestion => suggestion.TargetType).ToArray());
    }

    [Fact]
    public void PackageConflictSuggestionPreservesSourceAndTargetValues()
    {
        DatasheetCandidateLinkingViewModel linker = DatasheetCandidateLinkingViewModel.CreateSample();

        DatasheetCandidateLinkSuggestion suggestion = Assert.Single(
            linker.Suggestions,
            row => row.TargetId == "dragon:lm7805-smd");

        DatasheetCandidateLinkConflict conflict = Assert.Single(suggestion.Conflicts);
        Assert.Equal("Package", conflict.FieldName);
        Assert.Equal("TO-220-3", conflict.SourceValue);
        Assert.Equal("SOT-223", conflict.TargetValue);
        Assert.Equal("Package: source TO-220-3 vs target SOT-223", suggestion.ConflictDisplay);
    }

    [Fact]
    public void AcceptCreatesAuditableDecisionRecordWithoutLibraryMutation()
    {
        DatasheetCandidateLinkingViewModel linker = DatasheetCandidateLinkingViewModel.CreateSample();
        DatasheetCandidateLinkSuggestion accepted = Assert.Single(linker.Suggestions, row => row.TargetId == "dragon:lm7805");

        accepted.Accept("Use canonical regulator already in core library.", new DateTimeOffset(2026, 6, 3, 9, 15, 0, TimeSpan.Zero));

        Assert.Equal(DatasheetCandidateLinkReviewState.Accepted, accepted.ReviewState);
        DatasheetCandidateLinkDecisionRecord decision = Assert.Single(linker.Decisions);
        Assert.Equal("intake:lm7805ct", decision.IntakeRecordId);
        Assert.Equal("dragon:lm7805", decision.TargetId);
        Assert.Equal(DatasheetCandidateLinkTargetType.CanonicalComponent, decision.TargetType);
        Assert.Equal(DatasheetCandidateLinkReviewState.Accepted, decision.ReviewState);
        Assert.Equal(DatasheetCandidateLinkConfidence.High, decision.Confidence);
        Assert.Equal("Exact MPN and package match", decision.MatchBasis);
        Assert.Empty(decision.Conflicts);
        Assert.Equal("Use canonical regulator already in core library.", decision.ReviewerNote);
        Assert.Equal(new DateTimeOffset(2026, 6, 3, 9, 15, 0, TimeSpan.Zero), decision.ReviewedAt);
        Assert.False(decision.MutatedTrustedLibrary);
        Assert.Equal("Accepted link dragon:lm7805 for LM7805CT", accepted.StatusText);
    }

    [Fact]
    public void RejectCreatesAuditableDecisionRecordWithConflictsAndDefaultReviewerNote()
    {
        DatasheetCandidateLinkingViewModel linker = DatasheetCandidateLinkingViewModel.CreateSample();
        DatasheetCandidateLinkSuggestion rejected = Assert.Single(linker.Suggestions, row => row.TargetId == "dragon:lm7805-smd");

        rejected.Reject("  ", new DateTimeOffset(2026, 6, 3, 9, 20, 0, TimeSpan.Zero));

        Assert.Equal(DatasheetCandidateLinkReviewState.Rejected, rejected.ReviewState);
        DatasheetCandidateLinkDecisionRecord decision = Assert.Single(linker.Decisions);
        DatasheetCandidateLinkConflict conflict = Assert.Single(decision.Conflicts);
        Assert.Equal("Package", conflict.FieldName);
        Assert.Equal(DatasheetCandidateLinkReviewState.Rejected, decision.ReviewState);
        Assert.Equal("No reviewer note.", decision.ReviewerNote);
        Assert.Equal(new DateTimeOffset(2026, 6, 3, 9, 20, 0, TimeSpan.Zero), decision.ReviewedAt);
        Assert.Equal("Rejected link dragon:lm7805-smd for LM7805CT", rejected.StatusText);
    }

    [Fact]
    public void FilterShowsOnlyAcceptedRejectedOrPendingSuggestions()
    {
        DatasheetCandidateLinkingViewModel linker = DatasheetCandidateLinkingViewModel.CreateSample();
        linker.Suggestions.Single(row => row.TargetId == "dragon:lm7805").Accept("Approved");
        linker.Suggestions.Single(row => row.TargetId == "vendor:digikey:296-1389-5-ND").Reject("Not canonical");

        linker.SelectedReviewStateFilter = DatasheetCandidateLinkReviewStateFilter.Accepted;

        DatasheetCandidateLinkSuggestion suggestion = Assert.Single(linker.Suggestions);
        Assert.Equal("dragon:lm7805", suggestion.TargetId);
        Assert.Equal(["All", "Pending", "Accepted", "Rejected"], linker.ReviewStateFilterOptions);
    }

    [Fact]
    public void AcceptedLinksForSameIntakeRecordProduceConflictDiagnostics()
    {
        DatasheetCandidateLinkingViewModel linker = DatasheetCandidateLinkingViewModel.CreateSample();

        linker.Suggestions.Single(row => row.TargetId == "dragon:lm7805").Accept(
            "Canonical component is preferred.",
            new DateTimeOffset(2026, 6, 3, 9, 15, 0, TimeSpan.Zero));
        linker.Suggestions.Single(row => row.TargetId == "vendor:digikey:296-1389-5-ND").Accept(
            "Vendor evidence should stay linked too.",
            new DateTimeOffset(2026, 6, 3, 9, 16, 0, TimeSpan.Zero));

        DatasheetCandidateLinkDiagnostic diagnostic = Assert.Single(linker.Diagnostics);
        Assert.Equal("intake:lm7805ct", diagnostic.IntakeRecordId);
        Assert.Equal(DatasheetCandidateLinkDiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal(
            "Accepted link conflict for intake:lm7805ct: dragon:lm7805, vendor:digikey:296-1389-5-ND",
            diagnostic.Message);
    }

    [Fact]
    public void SuggestionsAreOrderedByConfidenceThenTargetTypeThenTargetId()
    {
        DatasheetCandidateLinkingViewModel linker = DatasheetCandidateLinkingViewModel.CreateSample();

        Assert.Equal(
            [
                "dragon:lm7805",
                "vendor:digikey:296-1389-5-ND",
                "dragon:lm7805-smd",
                "new:lm7805ct"
            ],
            linker.Suggestions.Select(suggestion => suggestion.TargetId).ToArray());
    }

    [Fact]
    public void ReviewSummaryOutputIsDeterministic()
    {
        DatasheetCandidateLinkingViewModel linker = DatasheetCandidateLinkingViewModel.CreateSample();
        linker.Suggestions.Single(row => row.TargetId == "dragon:lm7805").Accept(
            "Canonical component is preferred.",
            new DateTimeOffset(2026, 6, 3, 9, 15, 0, TimeSpan.Zero));
        linker.Suggestions.Single(row => row.TargetId == "dragon:lm7805-smd").Reject(
            "Package conflict.",
            new DateTimeOffset(2026, 6, 3, 9, 16, 0, TimeSpan.Zero));

        Assert.Equal(
            """
            Datasheet candidate link review
            Intake records: 1
            Suggestions: 4
            Accepted: 1
            Rejected: 1
            Pending: 2
            Diagnostics: 0
            Decisions:
            - 2026-06-03T09:15:00.0000000+00:00 | intake:lm7805ct | Accepted | CanonicalComponent | dragon:lm7805 | High | Exact MPN and package match | Canonical component is preferred.
            - 2026-06-03T09:16:00.0000000+00:00 | intake:lm7805ct | Rejected | ImportedCandidate | dragon:lm7805-smd | Medium | MPN match with package conflict | Package conflict.
            """,
            linker.ReviewSummary);
    }
}
