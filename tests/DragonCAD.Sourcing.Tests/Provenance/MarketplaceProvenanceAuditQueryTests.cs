using DragonCAD.Core.Components.Identity;
using DragonCAD.Sourcing.Provenance;

namespace DragonCAD.Sourcing.Tests.Provenance;

public sealed class MarketplaceProvenanceAuditQueryTests
{
    [Fact]
    public void QueryFiltersByComponentId()
    {
        MarketplaceProvenanceAuditQueryResult result = MarketplaceProvenanceAuditLog.Query(
            SampleRecords(),
            new MarketplaceProvenanceAuditQuery(ComponentId: Component("core:timer:ne555p")));

        Assert.Equal(["evt-001", "evt-004", "evt-006"], result.Records.Select(record => record.EventId));
    }

    [Fact]
    public void QueryFiltersBySourceProvider()
    {
        MarketplaceProvenanceAuditQueryResult result = MarketplaceProvenanceAuditLog.Query(
            SampleRecords(),
            new MarketplaceProvenanceAuditQuery(SourceProvider: "Mouser"));

        Assert.Equal(["evt-001", "evt-005", "evt-006"], result.Records.Select(record => record.EventId));
    }

    [Fact]
    public void QueryFiltersByDecisionType()
    {
        MarketplaceProvenanceAuditQueryResult result = MarketplaceProvenanceAuditLog.Query(
            SampleRecords(),
            new MarketplaceProvenanceAuditQuery(DecisionType: MarketplaceProvenanceDecisionType.Rejected));

        Assert.Equal(["evt-002", "evt-006"], result.Records.Select(record => record.EventId));
    }

    [Fact]
    public void QueryFiltersByReviewer()
    {
        MarketplaceProvenanceAuditQueryResult result = MarketplaceProvenanceAuditLog.Query(
            SampleRecords(),
            new MarketplaceProvenanceAuditQuery(Reviewer: "casey"));

        Assert.Equal(["evt-005", "evt-002"], result.Records.Select(record => record.EventId));
    }

    [Fact]
    public void QueryFiltersByInclusiveDateRange()
    {
        MarketplaceProvenanceAuditQueryResult result = MarketplaceProvenanceAuditLog.Query(
            SampleRecords(),
            new MarketplaceProvenanceAuditQuery(
                FromEventTime: At(10, 30),
                ToEventTime: At(12, 0)));

        Assert.Equal(["evt-002", "evt-003", "evt-004"], result.Records.Select(record => record.EventId));
    }

    [Fact]
    public void QuerySummarizesCountsByDecisionTypeAndProvider()
    {
        MarketplaceProvenanceAuditQueryResult result = MarketplaceProvenanceAuditLog.Query(
            SampleRecords(),
            new MarketplaceProvenanceAuditQuery(ComponentId: Component("core:timer:ne555p")));

        Assert.Equal(2, result.Summary.DecisionTypeCounts[MarketplaceProvenanceDecisionType.Accepted]);
        Assert.Equal(1, result.Summary.DecisionTypeCounts[MarketplaceProvenanceDecisionType.Rejected]);
        Assert.Equal(2, result.Summary.ProviderCounts["Mouser"]);
        Assert.Equal(1, result.Summary.ProviderCounts["Digi-Key"]);
    }

    [Fact]
    public void QuerySortsDeterministicallyByEventTimeThenEventId()
    {
        MarketplaceProvenanceAuditQueryResult result = MarketplaceProvenanceAuditLog.Query(
            SampleRecords(),
            new MarketplaceProvenanceAuditQuery());

        Assert.Equal(
            ["evt-001", "evt-005", "evt-002", "evt-003", "evt-004", "evt-006"],
            result.Records.Select(record => record.EventId));
    }

    private static IReadOnlyList<MarketplaceProvenanceAuditRecord> SampleRecords() =>
    [
        new MarketplaceProvenanceAuditRecord(
            EventId: "evt-004",
            ComponentId: Component("core:timer:ne555p"),
            SourceProvider: "Digi-Key",
            DecisionType: MarketplaceProvenanceDecisionType.Accepted,
            Reviewer: "alex",
            EventTime: At(12, 0)),
        new MarketplaceProvenanceAuditRecord(
            EventId: "evt-002",
            ComponentId: Component("core:regulator:lm7805"),
            SourceProvider: "Jameco",
            DecisionType: MarketplaceProvenanceDecisionType.Rejected,
            Reviewer: "casey",
            EventTime: At(10, 30)),
        new MarketplaceProvenanceAuditRecord(
            EventId: "evt-006",
            ComponentId: Component("core:timer:ne555p"),
            SourceProvider: "Mouser",
            DecisionType: MarketplaceProvenanceDecisionType.Rejected,
            Reviewer: "alex",
            EventTime: At(13, 15)),
        new MarketplaceProvenanceAuditRecord(
            EventId: "evt-003",
            ComponentId: Component("core:connector:usb-c"),
            SourceProvider: "Digi-Key",
            DecisionType: MarketplaceProvenanceDecisionType.Ignored,
            Reviewer: "alex",
            EventTime: At(10, 30)),
        new MarketplaceProvenanceAuditRecord(
            EventId: "evt-005",
            ComponentId: Component("core:resistor:10k"),
            SourceProvider: "Mouser",
            DecisionType: MarketplaceProvenanceDecisionType.Accepted,
            Reviewer: "casey",
            EventTime: At(9, 0)),
        new MarketplaceProvenanceAuditRecord(
            EventId: "evt-001",
            ComponentId: Component("core:timer:ne555p"),
            SourceProvider: "Mouser",
            DecisionType: MarketplaceProvenanceDecisionType.Accepted,
            Reviewer: "alex",
            EventTime: At(9, 0)),
    ];

    private static ComponentId Component(string value) => new(value);

    private static DateTimeOffset At(int hour, int minute) =>
        new(2026, 6, 3, hour, minute, 0, TimeSpan.Zero);
}
