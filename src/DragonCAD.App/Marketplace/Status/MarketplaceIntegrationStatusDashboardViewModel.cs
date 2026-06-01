namespace DragonCAD.App.Marketplace.Status;

public sealed class MarketplaceIntegrationStatusDashboardViewModel
{
    private static readonly MarketplaceIntegrationSection[] SectionOrder =
    [
        MarketplaceIntegrationSection.ApiSync,
        MarketplaceIntegrationSection.InUseSync,
        MarketplaceIntegrationSection.BomRollup,
        MarketplaceIntegrationSection.DedupReview,
        MarketplaceIntegrationSection.TrustedLibraryPromotion,
        MarketplaceIntegrationSection.FabricationOrdering,
        MarketplaceIntegrationSection.LiveSmoke
    ];

    private MarketplaceIntegrationStatusDashboardViewModel(IReadOnlyList<MarketplaceIntegrationStatusRow> rows)
    {
        Rows = rows;
        ActionStripSummary = CreateActionStripSummary(rows);
    }

    public IReadOnlyList<MarketplaceIntegrationStatusRow> Rows { get; }

    public MarketplaceIntegrationActionStripSummary ActionStripSummary { get; }

    public int SectionCount => Rows.Count;

    public int ReadySectionCount => Rows.Count(row => row.BlockedCount == 0);

    public int WarningSectionCount => Rows.Count(row => row.WarningCount > 0);

    public int BlockedSectionCount => Rows.Count(row => row.BlockedCount > 0);

    public int ReadyItemCount => Rows.Sum(row => row.ReadyCount);

    public int WarningItemCount => Rows.Sum(row => row.WarningCount);

    public int BlockedItemCount => Rows.Sum(row => row.BlockedCount);

    public int AttentionSectionCount => Rows.Count(row => row.WarningCount > 0 || row.BlockedCount > 0);

    public MarketplaceIntegrationSeverity OverallSeverity
    {
        get
        {
            if (BlockedItemCount > 0)
            {
                return MarketplaceIntegrationSeverity.Blocked;
            }

            if (WarningItemCount > 0)
            {
                return MarketplaceIntegrationSeverity.Attention;
            }

            return MarketplaceIntegrationSeverity.Ready;
        }
    }

    public string OverallSeverityLabel =>
        OverallSeverity switch
        {
            MarketplaceIntegrationSeverity.Blocked => "Blocked",
            MarketplaceIntegrationSeverity.Attention => "Attention",
            _ => "Ready"
        };

    public string SummaryText
    {
        get
        {
            if (BlockedItemCount > 0)
            {
                string blockedSummary = $"{BlockedItemCount:N0} blocked";
                string warningSummary = WarningItemCount > 0
                    ? $", {WarningItemCount:N0} {Pluralize(WarningItemCount, "warning")}"
                    : string.Empty;

                return $"{blockedSummary}{warningSummary} across {AttentionSectionCount:N0} {Pluralize(AttentionSectionCount, "section")}";
            }

            if (WarningItemCount > 0)
            {
                return $"{WarningItemCount:N0} {Pluralize(WarningItemCount, "warning")} across {AttentionSectionCount:N0} {Pluralize(AttentionSectionCount, "section")}";
            }

            return $"{ReadySectionCount:N0} {Pluralize(ReadySectionCount, "section")} ready";
        }
    }

    public string NextActionText
    {
        get
        {
            if (BlockedItemCount > 0)
            {
                return $"Resolve {BlockedItemCount:N0} blocked marketplace integration {Pluralize(BlockedItemCount, "item")}";
            }

            if (WarningItemCount > 0)
            {
                return $"Review {WarningItemCount:N0} marketplace integration {Pluralize(WarningItemCount, "warning")}";
            }

            return "Marketplace integration is ready";
        }
    }

    public static MarketplaceIntegrationStatusDashboardViewModel FromSections(
        IEnumerable<MarketplaceIntegrationSectionStatus> sections)
    {
        ArgumentNullException.ThrowIfNull(sections);

        MarketplaceIntegrationStatusRow[] rows = sections
            .OrderBy(section => SectionSortIndex(section.Section))
            .Select(MarketplaceIntegrationStatusRow.FromSection)
            .ToArray();

        return new MarketplaceIntegrationStatusDashboardViewModel(rows);
    }

    private static int SectionSortIndex(MarketplaceIntegrationSection section)
    {
        int index = Array.IndexOf(SectionOrder, section);
        return index < 0 ? int.MaxValue : index;
    }

    private static MarketplaceIntegrationActionStripSummary CreateActionStripSummary(
        IReadOnlyList<MarketplaceIntegrationStatusRow> rows)
    {
        MarketplaceIntegrationStatusRow? issue = rows.FirstOrDefault(row => row.BlockedCount > 0);
        if (issue is not null)
        {
            return new MarketplaceIntegrationActionStripSummary(
                issue.SeverityLabel,
                $"{issue.SectionLabel}: {issue.BlockedCount:N0} blocked",
                issue.NextActionText);
        }

        issue = rows.FirstOrDefault(row => row.WarningCount > 0);
        if (issue is not null)
        {
            return new MarketplaceIntegrationActionStripSummary(
                issue.SeverityLabel,
                $"{issue.SectionLabel}: {issue.WarningCount:N0} {Pluralize(issue.WarningCount, "warning")}",
                issue.NextActionText);
        }

        return new MarketplaceIntegrationActionStripSummary(
            "Ready",
            "Marketplace integration: all sections ready",
            "Marketplace integration is ready");
    }

    internal static string Pluralize(int count, string singular) => count == 1 ? singular : $"{singular}s";
}

public sealed record MarketplaceIntegrationActionStripSummary(
    string SeverityLabel,
    string IssueText,
    string NextActionText);

public sealed record MarketplaceIntegrationSectionStatus(
    MarketplaceIntegrationSection Section,
    int ReadyCount,
    int WarningCount,
    int BlockedCount);

public enum MarketplaceIntegrationSection
{
    ApiSync,
    InUseSync,
    BomRollup,
    DedupReview,
    TrustedLibraryPromotion,
    FabricationOrdering,
    LiveSmoke
}

public enum MarketplaceIntegrationSeverity
{
    Ready,
    Attention,
    Blocked
}

public sealed record MarketplaceIntegrationStatusRow(
    MarketplaceIntegrationSection Section,
    string SectionLabel,
    int ReadyCount,
    int WarningCount,
    int BlockedCount,
    string SeverityLabel,
    string CountSummary,
    string NextActionText)
{
    public static MarketplaceIntegrationStatusRow FromSection(MarketplaceIntegrationSectionStatus section)
    {
        ArgumentNullException.ThrowIfNull(section);

        string severityLabel = FormatSeverity(section);
        return new MarketplaceIntegrationStatusRow(
            Section: section.Section,
            SectionLabel: FormatSectionLabel(section.Section),
            ReadyCount: section.ReadyCount,
            WarningCount: section.WarningCount,
            BlockedCount: section.BlockedCount,
            SeverityLabel: severityLabel,
            CountSummary: FormatCountSummary(section),
            NextActionText: FormatNextAction(section));
    }

    private static string FormatSeverity(MarketplaceIntegrationSectionStatus section)
    {
        if (section.BlockedCount > 0)
        {
            return "Blocked";
        }

        if (section.WarningCount > 0)
        {
            return "Attention";
        }

        return "Ready";
    }

    private static string FormatSectionLabel(MarketplaceIntegrationSection section) =>
        section switch
        {
            MarketplaceIntegrationSection.ApiSync => "API sync",
            MarketplaceIntegrationSection.InUseSync => "In-use sync",
            MarketplaceIntegrationSection.BomRollup => "BOM rollup",
            MarketplaceIntegrationSection.DedupReview => "Dedup review",
            MarketplaceIntegrationSection.TrustedLibraryPromotion => "Trusted-library promotion",
            MarketplaceIntegrationSection.FabricationOrdering => "Fabrication ordering",
            MarketplaceIntegrationSection.LiveSmoke => "Live smoke",
            _ => section.ToString()
        };

    private static string FormatCountSummary(MarketplaceIntegrationSectionStatus section) =>
        $"{section.ReadyCount:N0} ready, {section.WarningCount:N0} {MarketplaceIntegrationStatusDashboardViewModel.Pluralize(section.WarningCount, "warning")}, {section.BlockedCount:N0} blocked";

    private static string FormatNextAction(MarketplaceIntegrationSectionStatus section)
    {
        if (section.BlockedCount == 0 && section.WarningCount == 0)
        {
            return ReadyAction(section.Section);
        }

        return section.Section switch
        {
            MarketplaceIntegrationSection.ApiSync => section.BlockedCount > 0 ? "Configure vendor API sync" : "Review vendor API sync warnings",
            MarketplaceIntegrationSection.InUseSync => section.BlockedCount > 0 ? "Reconnect in-use catalog sync" : "Review in-use catalog freshness",
            MarketplaceIntegrationSection.BomRollup => "Resolve BOM price and availability warnings",
            MarketplaceIntegrationSection.DedupReview => "Review duplicate component candidates",
            MarketplaceIntegrationSection.TrustedLibraryPromotion => "Review trusted-library promotion queue",
            MarketplaceIntegrationSection.FabricationOrdering => "Resolve fabrication ordering blockers",
            MarketplaceIntegrationSection.LiveSmoke => section.BlockedCount > 0 ? "Rerun blocked live smoke checks" : "Review live smoke warnings",
            _ => "Review marketplace integration status"
        };
    }

    private static string ReadyAction(MarketplaceIntegrationSection section) =>
        section switch
        {
            MarketplaceIntegrationSection.ApiSync => "Vendor API sync is ready",
            MarketplaceIntegrationSection.InUseSync => "In-use catalog sync is ready",
            MarketplaceIntegrationSection.BomRollup => "BOM rollup is ready",
            MarketplaceIntegrationSection.DedupReview => "Dedup review is clear",
            MarketplaceIntegrationSection.TrustedLibraryPromotion => "Trusted-library promotion is ready",
            MarketplaceIntegrationSection.FabricationOrdering => "Fabrication ordering is ready",
            MarketplaceIntegrationSection.LiveSmoke => "Live smoke is passing",
            _ => "Marketplace integration is ready"
        };
}
