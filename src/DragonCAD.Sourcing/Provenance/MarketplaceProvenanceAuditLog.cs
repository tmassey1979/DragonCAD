using System.Collections.ObjectModel;
using DragonCAD.Core.Components.Identity;

namespace DragonCAD.Sourcing.Provenance;

public static class MarketplaceProvenanceAuditLog
{
    public static MarketplaceProvenanceAuditQueryResult Query(
        IEnumerable<MarketplaceProvenanceAuditRecord> records,
        MarketplaceProvenanceAuditQuery query)
    {
        ArgumentNullException.ThrowIfNull(records);
        ArgumentNullException.ThrowIfNull(query);

        List<MarketplaceProvenanceAuditRecord> filteredRecords = records
            .Where(record => Matches(record, query))
            .OrderBy(record => record.EventTime)
            .ThenBy(record => record.EventId, StringComparer.Ordinal)
            .ToList();

        return new MarketplaceProvenanceAuditQueryResult(
            filteredRecords,
            new MarketplaceProvenanceAuditSummary(
                CountByDecisionType(filteredRecords),
                CountByProvider(filteredRecords)));
    }

    private static bool Matches(MarketplaceProvenanceAuditRecord record, MarketplaceProvenanceAuditQuery query)
    {
        if (query.ComponentId is { } componentId && record.ComponentId != componentId)
        {
            return false;
        }

        if (query.SourceProvider is { } sourceProvider
            && !string.Equals(record.SourceProvider, sourceProvider, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (query.DecisionType is { } decisionType && record.DecisionType != decisionType)
        {
            return false;
        }

        if (query.Reviewer is { } reviewer
            && !string.Equals(record.Reviewer, reviewer, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (query.FromEventTime is { } fromEventTime && record.EventTime < fromEventTime)
        {
            return false;
        }

        if (query.ToEventTime is { } toEventTime && record.EventTime > toEventTime)
        {
            return false;
        }

        return true;
    }

    private static IReadOnlyDictionary<MarketplaceProvenanceDecisionType, int> CountByDecisionType(
        IEnumerable<MarketplaceProvenanceAuditRecord> records)
    {
        Dictionary<MarketplaceProvenanceDecisionType, int> counts = records
            .GroupBy(record => record.DecisionType)
            .OrderBy(group => group.Key)
            .ToDictionary(group => group.Key, group => group.Count());

        return new ReadOnlyDictionary<MarketplaceProvenanceDecisionType, int>(counts);
    }

    private static IReadOnlyDictionary<string, int> CountByProvider(IEnumerable<MarketplaceProvenanceAuditRecord> records)
    {
        Dictionary<string, int> counts = records
            .GroupBy(record => record.SourceProvider, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

        return new ReadOnlyDictionary<string, int>(counts);
    }
}

public sealed record MarketplaceProvenanceAuditQuery(
    ComponentId? ComponentId = null,
    string? SourceProvider = null,
    MarketplaceProvenanceDecisionType? DecisionType = null,
    string? Reviewer = null,
    DateTimeOffset? FromEventTime = null,
    DateTimeOffset? ToEventTime = null);

public sealed record MarketplaceProvenanceAuditQueryResult(
    IReadOnlyList<MarketplaceProvenanceAuditRecord> Records,
    MarketplaceProvenanceAuditSummary Summary);

public sealed record MarketplaceProvenanceAuditSummary(
    IReadOnlyDictionary<MarketplaceProvenanceDecisionType, int> DecisionTypeCounts,
    IReadOnlyDictionary<string, int> ProviderCounts);

public sealed record MarketplaceProvenanceAuditRecord(
    string EventId,
    ComponentId ComponentId,
    string SourceProvider,
    MarketplaceProvenanceDecisionType DecisionType,
    string Reviewer,
    DateTimeOffset EventTime);

public enum MarketplaceProvenanceDecisionType
{
    Accepted,
    Rejected,
    Ignored,
}
