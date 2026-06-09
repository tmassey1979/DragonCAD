using DragonCAD.App.Timeline;
using DragonCAD.Core.Timeline;

namespace DragonCAD.App.Tests.Timeline;

public sealed class RevisionTimelineWorkspaceViewModelTests
{
    [Fact]
    public void GroupsEventsByDateAndArea()
    {
        RevisionTimelineWorkspaceViewModel viewModel = RevisionTimelineWorkspaceViewModel.FromEvents(
        [
            Save("evt-save-001", new DateTimeOffset(2026, 6, 2, 15, 0, 0, TimeSpan.Zero), "maintainer", "Saved schematic", "project-alpha", "dragoncad.json", "abc123"),
            Import("evt-import-001", new DateTimeOffset(2026, 6, 2, 14, 0, 0, TimeSpan.Zero), "vendor-sync", "Imported regulator", "LM7805CT", "digikey:296-13996-5-ND"),
            Promotion("evt-promotion-001", new DateTimeOffset(2026, 6, 1, 9, 0, 0, TimeSpan.Zero), "reviewer", "Promoted regulator", "LM7805CT", "promotion:LM7805CT"),
        ]);

        Assert.Equal(["2026-06-02", "2026-06-01"], viewModel.DateGroups.Select(group => group.DateDisplay));
        Assert.Equal(["Save", "Import"], viewModel.DateGroups[0].AreaGroups.Select(group => group.AreaDisplay));
        Assert.Equal(["evt-save-001"], viewModel.DateGroups[0].AreaGroups[0].Events.Select(row => row.Id));
        Assert.Equal(["evt-import-001"], viewModel.DateGroups[0].AreaGroups[1].Events.Select(row => row.Id));
    }

    [Fact]
    public void FiltersByAreaActorAffectedObjectArtifactAndGitCommit()
    {
        RevisionTimelineWorkspaceViewModel viewModel = RevisionTimelineWorkspaceViewModel.FromEvents(
        [
            Save("evt-save-001", new DateTimeOffset(2026, 6, 2, 15, 0, 0, TimeSpan.Zero), "maintainer", "Saved schematic", "project-alpha", "dragoncad.json", "abc123"),
            Save("evt-save-002", new DateTimeOffset(2026, 6, 2, 16, 0, 0, TimeSpan.Zero), "backup", "Saved board", "main-board", "board.json", "def456"),
            Import("evt-import-001", new DateTimeOffset(2026, 6, 2, 14, 0, 0, TimeSpan.Zero), "vendor-sync", "Imported regulator", "LM7805CT", "digikey:296-13996-5-ND"),
        ]);

        viewModel.SelectedAreaFilter = "Save";
        viewModel.SelectedActorFilter = "maintainer";
        viewModel.AffectedObjectFilter = "project-alpha";
        viewModel.ArtifactRefFilter = "dragoncad.json";
        viewModel.GitCommitIdFilter = "abc123";

        RevisionTimelineEventRow row = Assert.Single(viewModel.DateGroups.Single().AreaGroups.Single().Events);
        Assert.Equal("evt-save-001", row.Id);
        Assert.Equal(["All", "Import", "Save"], viewModel.AreaFilterOptions);
        Assert.Equal(["All", "backup", "maintainer", "vendor-sync"], viewModel.ActorFilterOptions);
    }

    [Fact]
    public void SelectingEventShowsSummaryReferencesArtifactsAndDiagnostics()
    {
        RevisionTimelineWorkspaceViewModel viewModel = RevisionTimelineWorkspaceViewModel.FromEvents(
        [
            Save("evt-save-001", new DateTimeOffset(2026, 6, 2, 15, 0, 0, TimeSpan.Zero), "maintainer", "Saved schematic cleanup", "project-alpha", "dragoncad.json", "abc123"),
        ]);

        viewModel.SelectEvent("evt-save-001");

        RevisionTimelineEventDetails? details = viewModel.SelectedEventDetails;
        Assert.NotNull(details);
        Assert.Equal("Saved schematic cleanup", details.Summary);
        Assert.Equal(["Project:project-alpha"], details.ChangedObjectRefs);
        Assert.Equal(["Document:dragoncad.json"], details.ArtifactRefs);
        Assert.Equal(["Git commit abc123 is linked to this timeline event."], details.Diagnostics);
    }

    [Fact]
    public void EmptyTimelineExposesEmptyStateAndNoSelection()
    {
        RevisionTimelineWorkspaceViewModel viewModel = RevisionTimelineWorkspaceViewModel.FromEvents([]);

        Assert.True(viewModel.IsEmpty);
        Assert.Equal("No revision timeline events match the current filters.", viewModel.EmptyStateMessage);
        Assert.Empty(viewModel.DateGroups);
        Assert.Null(viewModel.SelectedEventDetails);
    }

    [Fact]
    public void EventsAreOrderedDeterministicallyWithinAreaGroups()
    {
        RevisionTimelineWorkspaceViewModel viewModel = RevisionTimelineWorkspaceViewModel.FromEvents(
        [
            Save("evt-save-b", new DateTimeOffset(2026, 6, 2, 15, 0, 0, TimeSpan.Zero), "maintainer", "Saved B", "project-beta", "b.json", "bbb"),
            Save("evt-save-a", new DateTimeOffset(2026, 6, 2, 15, 0, 0, TimeSpan.Zero), "maintainer", "Saved A", "project-alpha", "a.json", "aaa"),
            Save("evt-save-new", new DateTimeOffset(2026, 6, 2, 16, 0, 0, TimeSpan.Zero), "maintainer", "Saved new", "project-new", "new.json", "new"),
        ]);

        Assert.Equal(
            ["evt-save-new", "evt-save-a", "evt-save-b"],
            viewModel.DateGroups.Single().AreaGroups.Single().Events.Select(row => row.Id));
    }

    private static RevisionTimelineEvent Save(
        string id,
        DateTimeOffset occurredAt,
        string actor,
        string summary,
        string objectRef,
        string artifactRef,
        string commitId) =>
        RevisionTimelineEvent.Save(
            RevisionTimelineEventId.From(id),
            occurredAt,
            actor,
            summary,
            [RevisionObjectRef.Project(objectRef)],
            [RevisionArtifactRef.Document(artifactRef)],
            GitCommitId.From(commitId));

    private static RevisionTimelineEvent Import(
        string id,
        DateTimeOffset occurredAt,
        string actor,
        string summary,
        string objectRef,
        string artifactRef) =>
        RevisionTimelineEvent.Import(
            RevisionTimelineEventId.From(id),
            occurredAt,
            actor,
            summary,
            [RevisionObjectRef.LibraryComponent(objectRef)],
            [RevisionArtifactRef.ImportSource(artifactRef)]);

    private static RevisionTimelineEvent Promotion(
        string id,
        DateTimeOffset occurredAt,
        string actor,
        string summary,
        string objectRef,
        string artifactRef) =>
        RevisionTimelineEvent.Promotion(
            RevisionTimelineEventId.From(id),
            occurredAt,
            actor,
            summary,
            [RevisionObjectRef.LibraryComponent(objectRef)],
            [RevisionArtifactRef.PromotionRecord(artifactRef)]);
}
