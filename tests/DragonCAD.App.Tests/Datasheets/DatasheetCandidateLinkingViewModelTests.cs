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
    public void AcceptAndRejectCreateAuditableDecisionRecordsWithoutLibraryMutation()
    {
        DatasheetCandidateLinkingViewModel linker = DatasheetCandidateLinkingViewModel.CreateSample();
        DatasheetCandidateLinkSuggestion accepted = Assert.Single(linker.Suggestions, row => row.TargetId == "dragon:lm7805");
        DatasheetCandidateLinkSuggestion rejected = Assert.Single(linker.Suggestions, row => row.TargetId == "vendor:digikey:296-1389-5-ND");

        accepted.Accept("Use canonical regulator already in core library.");
        rejected.Reject("Vendor row is useful for sourcing but not a canonical component.");

        Assert.Equal(DatasheetCandidateLinkReviewState.Accepted, accepted.ReviewState);
        Assert.Equal(DatasheetCandidateLinkReviewState.Rejected, rejected.ReviewState);
        Assert.Equal(2, linker.Decisions.Count);
        Assert.Contains(linker.Decisions, decision =>
            decision.TargetId == "dragon:lm7805" &&
            decision.Decision == "Accepted" &&
            decision.ReviewerNote == "Use canonical regulator already in core library." &&
            !decision.MutatedTrustedLibrary);
        Assert.Contains(linker.Decisions, decision =>
            decision.TargetId == "vendor:digikey:296-1389-5-ND" &&
            decision.Decision == "Rejected" &&
            decision.ReviewerNote == "Vendor row is useful for sourcing but not a canonical component." &&
            !decision.MutatedTrustedLibrary);
        Assert.Equal("Accepted link dragon:lm7805 for LM7805CT", accepted.StatusText);
        Assert.Equal("Rejected link vendor:digikey:296-1389-5-ND for LM7805CT", rejected.StatusText);
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
}
