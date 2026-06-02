using DragonCAD.Core.Timeline;

namespace DragonCAD.Core.Tests.Timeline;

public sealed class RevisionTimelineTests
{
    [Fact]
    public void SaveEventRecordsProjectRevisionDetails()
    {
        DateTimeOffset occurredAt = new(2026, 6, 2, 9, 15, 0, TimeSpan.Zero);

        RevisionTimelineEvent timelineEvent = RevisionTimelineEvent.Save(
            RevisionTimelineEventId.From("evt-save-001"),
            occurredAt,
            actor: " maintainer ",
            summary: " Saved schematic cleanup ",
            changedObjectRefs: [RevisionObjectRef.Project(" project-alpha ")],
            artifactRefs: [RevisionArtifactRef.Document(" file:///projects/alpha/dragoncad.json ")],
            gitCommitId: GitCommitId.From(" abc123def456 "));

        Assert.Equal(RevisionTimelineEventId.From("evt-save-001"), timelineEvent.Id);
        Assert.Equal(occurredAt, timelineEvent.OccurredAt);
        Assert.Equal("maintainer", timelineEvent.Actor);
        Assert.Equal(RevisionTimelineArea.Save, timelineEvent.Area);
        Assert.Equal("Saved schematic cleanup", timelineEvent.Summary);
        Assert.Equal([RevisionObjectRef.Project("project-alpha")], timelineEvent.ChangedObjectRefs);
        Assert.Equal([RevisionArtifactRef.Document("file:///projects/alpha/dragoncad.json")], timelineEvent.ArtifactRefs);
        Assert.Equal(GitCommitId.From("abc123def456"), timelineEvent.GitCommitId);
    }

    [Fact]
    public void ImportEventRecordsChangedLibraryObjectAndSourceArtifact()
    {
        RevisionTimelineEvent timelineEvent = RevisionTimelineEvent.Import(
            RevisionTimelineEventId.From("evt-import-001"),
            occurredAt: new DateTimeOffset(2026, 6, 2, 9, 20, 0, TimeSpan.Zero),
            actor: "vendor-sync",
            summary: "Imported regulator library",
            changedObjectRefs: [RevisionObjectRef.LibraryComponent("LM7805CT")],
            artifactRefs: [RevisionArtifactRef.ImportSource("digikey:296-13996-5-ND")]);

        Assert.Equal(RevisionTimelineArea.Import, timelineEvent.Area);
        Assert.Equal([RevisionObjectRef.LibraryComponent("LM7805CT")], timelineEvent.ChangedObjectRefs);
        Assert.Equal([RevisionArtifactRef.ImportSource("digikey:296-13996-5-ND")], timelineEvent.ArtifactRefs);
        Assert.Null(timelineEvent.GitCommitId);
    }

    [Fact]
    public void PromotionEventRecordsPromotedObjectAndArtifact()
    {
        RevisionTimelineEvent timelineEvent = RevisionTimelineEvent.Promotion(
            RevisionTimelineEventId.From("evt-promotion-001"),
            occurredAt: new DateTimeOffset(2026, 6, 2, 9, 25, 0, TimeSpan.Zero),
            actor: "reviewer",
            summary: "Promoted reviewed component to canonical library",
            changedObjectRefs: [RevisionObjectRef.LibraryComponent("ESP32-DEVKITC")],
            artifactRefs: [RevisionArtifactRef.PromotionRecord("promotion:ESP32-DEVKITC:2026-06-02")]);

        Assert.Equal(RevisionTimelineArea.Promotion, timelineEvent.Area);
        Assert.Equal("Promoted reviewed component to canonical library", timelineEvent.Summary);
        Assert.Equal([RevisionArtifactRef.PromotionRecord("promotion:ESP32-DEVKITC:2026-06-02")], timelineEvent.ArtifactRefs);
    }

    [Fact]
    public void FabricationExportEventRecordsExportArtifact()
    {
        RevisionTimelineEvent timelineEvent = RevisionTimelineEvent.FabricationExport(
            RevisionTimelineEventId.From("evt-fab-001"),
            occurredAt: new DateTimeOffset(2026, 6, 2, 9, 30, 0, TimeSpan.Zero),
            actor: "fabrication",
            summary: "Exported Gerber package",
            changedObjectRefs: [RevisionObjectRef.Board("main-board")],
            artifactRefs: [RevisionArtifactRef.FabricationPackage("outputs/main-board-gerbers.zip")]);

        Assert.Equal(RevisionTimelineArea.FabricationExport, timelineEvent.Area);
        Assert.Equal([RevisionObjectRef.Board("main-board")], timelineEvent.ChangedObjectRefs);
        Assert.Equal([RevisionArtifactRef.FabricationPackage("outputs/main-board-gerbers.zip")], timelineEvent.ArtifactRefs);
    }

    [Fact]
    public void OrderingReviewEventRecordsOrderReviewArtifact()
    {
        RevisionTimelineEvent timelineEvent = RevisionTimelineEvent.OrderingReview(
            RevisionTimelineEventId.From("evt-order-001"),
            occurredAt: new DateTimeOffset(2026, 6, 2, 9, 35, 0, TimeSpan.Zero),
            actor: "buyer",
            summary: "Reviewed marketplace order quantities",
            changedObjectRefs: [RevisionObjectRef.Order("PO-2026-00042")],
            artifactRefs: [RevisionArtifactRef.OrderReview("order-review:PO-2026-00042")]);

        Assert.Equal(RevisionTimelineArea.OrderingReview, timelineEvent.Area);
        Assert.Equal([RevisionObjectRef.Order("PO-2026-00042")], timelineEvent.ChangedObjectRefs);
        Assert.Equal([RevisionArtifactRef.OrderReview("order-review:PO-2026-00042")], timelineEvent.ArtifactRefs);
    }

    [Fact]
    public void EventStoreAppendsAndListsEventsInDeterministicOrder()
    {
        RevisionTimelineEvent later = RevisionTimelineEvent.Save(
            RevisionTimelineEventId.From("evt-save-002"),
            occurredAt: new DateTimeOffset(2026, 6, 2, 10, 0, 0, TimeSpan.Zero),
            actor: "maintainer",
            summary: "Saved board routing",
            changedObjectRefs: [RevisionObjectRef.Board("main-board")],
            artifactRefs: [RevisionArtifactRef.Document("dragoncad.json")]);
        RevisionTimelineEvent sameTimeSecond = RevisionTimelineEvent.Import(
            RevisionTimelineEventId.From("evt-import-002"),
            occurredAt: new DateTimeOffset(2026, 6, 2, 9, 45, 0, TimeSpan.Zero),
            actor: "vendor-sync",
            summary: "Imported capacitors",
            changedObjectRefs: [RevisionObjectRef.LibraryComponent("C0603C104K5RACTU")],
            artifactRefs: [RevisionArtifactRef.ImportSource("vendor:capacitors")]);
        RevisionTimelineEvent sameTimeFirst = RevisionTimelineEvent.Promotion(
            RevisionTimelineEventId.From("evt-promotion-002"),
            occurredAt: new DateTimeOffset(2026, 6, 2, 9, 45, 0, TimeSpan.Zero),
            actor: "reviewer",
            summary: "Promoted resistor",
            changedObjectRefs: [RevisionObjectRef.LibraryComponent("RC0603FR-0710KL")],
            artifactRefs: [RevisionArtifactRef.PromotionRecord("promotion:resistor")]);

        RevisionTimelineStore store = new();
        store.Append(later);
        store.Append(sameTimeSecond);
        store.Append(sameTimeFirst);

        IReadOnlyList<RevisionTimelineEvent> events = store.List();

        Assert.Equal(
            [
                RevisionTimelineEventId.From("evt-import-002"),
                RevisionTimelineEventId.From("evt-promotion-002"),
                RevisionTimelineEventId.From("evt-save-002"),
            ],
            events.Select(timelineEvent => timelineEvent.Id).ToArray());
    }
}
