using DragonCAD.App.Marketplace.Status;

namespace DragonCAD.App.Tests.Marketplace.Status;

public sealed class MarketplaceIntegrationStatusDashboardViewModelTests
{
    [Fact]
    public void FromSectionsCreatesRowsInDeterministicWorkflowOrder()
    {
        MarketplaceIntegrationStatusDashboardViewModel dashboard = MarketplaceIntegrationStatusDashboardViewModel.FromSections(
            [
                Section(MarketplaceIntegrationSection.LiveSmoke, ready: 3),
                Section(MarketplaceIntegrationSection.BomRollup, ready: 8, warnings: 1),
                Section(MarketplaceIntegrationSection.ApiSync, ready: 4),
                Section(MarketplaceIntegrationSection.FabricationOrdering, ready: 2),
                Section(MarketplaceIntegrationSection.DedupReview, blocked: 2),
                Section(MarketplaceIntegrationSection.TrustedLibraryPromotion, ready: 5),
                Section(MarketplaceIntegrationSection.InUseSync, ready: 6)
            ]);

        Assert.Equal(
            [
                "API sync",
                "In-use sync",
                "BOM rollup",
                "Dedup review",
                "Trusted-library promotion",
                "Fabrication ordering",
                "Live smoke"
            ],
            dashboard.Rows.Select(row => row.SectionLabel));
    }

    [Fact]
    public void FromSectionsDerivesCountsSeverityLabelsAndOverallAction()
    {
        MarketplaceIntegrationStatusDashboardViewModel dashboard = MarketplaceIntegrationStatusDashboardViewModel.FromSections(
            [
                Section(MarketplaceIntegrationSection.ApiSync, ready: 4, warnings: 1),
                Section(MarketplaceIntegrationSection.InUseSync, ready: 6),
                Section(MarketplaceIntegrationSection.BomRollup, ready: 9, warnings: 2),
                Section(MarketplaceIntegrationSection.DedupReview, ready: 3, blocked: 2),
                Section(MarketplaceIntegrationSection.TrustedLibraryPromotion, ready: 5),
                Section(MarketplaceIntegrationSection.FabricationOrdering, ready: 1, blocked: 1),
                Section(MarketplaceIntegrationSection.LiveSmoke, ready: 2)
            ]);

        Assert.Equal(7, dashboard.SectionCount);
        Assert.Equal(5, dashboard.ReadySectionCount);
        Assert.Equal(2, dashboard.BlockedSectionCount);
        Assert.Equal(2, dashboard.WarningSectionCount);
        Assert.Equal(30, dashboard.ReadyItemCount);
        Assert.Equal(3, dashboard.WarningItemCount);
        Assert.Equal(3, dashboard.BlockedItemCount);
        Assert.Equal(MarketplaceIntegrationSeverity.Blocked, dashboard.OverallSeverity);
        Assert.Equal("Blocked", dashboard.OverallSeverityLabel);
        Assert.Equal("3 blocked, 3 warnings across 4 sections", dashboard.SummaryText);
        Assert.Equal("Resolve 3 blocked marketplace integration items", dashboard.NextActionText);

        MarketplaceIntegrationStatusRow dedup = Assert.Single(dashboard.Rows, row => row.SectionLabel == "Dedup review");
        Assert.Equal("Blocked", dedup.SeverityLabel);
        Assert.Equal("3 ready, 0 warnings, 2 blocked", dedup.CountSummary);
        Assert.Equal("Review duplicate component candidates", dedup.NextActionText);

        MarketplaceIntegrationStatusRow bom = Assert.Single(dashboard.Rows, row => row.SectionLabel == "BOM rollup");
        Assert.Equal("Attention", bom.SeverityLabel);
        Assert.Equal("9 ready, 2 warnings, 0 blocked", bom.CountSummary);
        Assert.Equal("Resolve BOM price and availability warnings", bom.NextActionText);
    }

    [Fact]
    public void FromSectionsReportsReadyDashboardWhenEverySectionIsClean()
    {
        MarketplaceIntegrationStatusDashboardViewModel dashboard = MarketplaceIntegrationStatusDashboardViewModel.FromSections(
            [
                Section(MarketplaceIntegrationSection.ApiSync, ready: 4),
                Section(MarketplaceIntegrationSection.InUseSync, ready: 6),
                Section(MarketplaceIntegrationSection.BomRollup, ready: 9),
                Section(MarketplaceIntegrationSection.DedupReview, ready: 3),
                Section(MarketplaceIntegrationSection.TrustedLibraryPromotion, ready: 5),
                Section(MarketplaceIntegrationSection.FabricationOrdering, ready: 1),
                Section(MarketplaceIntegrationSection.LiveSmoke, ready: 2)
            ]);

        Assert.Equal(MarketplaceIntegrationSeverity.Ready, dashboard.OverallSeverity);
        Assert.Equal("Ready", dashboard.OverallSeverityLabel);
        Assert.Equal("7 sections ready", dashboard.SummaryText);
        Assert.Equal("Marketplace integration is ready", dashboard.NextActionText);
        Assert.All(dashboard.Rows, row => Assert.Equal("Ready", row.SeverityLabel));
    }

    [Fact]
    public void FromSectionsReportsWarningOnlySummaryForCompactHeader()
    {
        MarketplaceIntegrationStatusDashboardViewModel dashboard = MarketplaceIntegrationStatusDashboardViewModel.FromSections(
            [
                Section(MarketplaceIntegrationSection.ApiSync, ready: 4, warnings: 1),
                Section(MarketplaceIntegrationSection.InUseSync, ready: 6),
                Section(MarketplaceIntegrationSection.BomRollup, ready: 9, warnings: 2),
                Section(MarketplaceIntegrationSection.DedupReview, ready: 3),
                Section(MarketplaceIntegrationSection.TrustedLibraryPromotion, ready: 5),
                Section(MarketplaceIntegrationSection.FabricationOrdering, ready: 1),
                Section(MarketplaceIntegrationSection.LiveSmoke, ready: 2)
            ]);

        Assert.Equal(MarketplaceIntegrationSeverity.Attention, dashboard.OverallSeverity);
        Assert.Equal("Attention", dashboard.OverallSeverityLabel);
        Assert.Equal("3 warnings across 2 sections", dashboard.SummaryText);
        Assert.Equal("Review 3 marketplace integration warnings", dashboard.NextActionText);
    }

    [Fact]
    public void FromSectionsUsesSingularCompactSummaryWhenOneSectionNeedsAttention()
    {
        MarketplaceIntegrationStatusDashboardViewModel dashboard = MarketplaceIntegrationStatusDashboardViewModel.FromSections(
            [
                Section(MarketplaceIntegrationSection.ApiSync, ready: 4, blocked: 1),
                Section(MarketplaceIntegrationSection.InUseSync, ready: 6),
                Section(MarketplaceIntegrationSection.BomRollup, ready: 9),
                Section(MarketplaceIntegrationSection.DedupReview, ready: 3),
                Section(MarketplaceIntegrationSection.TrustedLibraryPromotion, ready: 5),
                Section(MarketplaceIntegrationSection.FabricationOrdering, ready: 1),
                Section(MarketplaceIntegrationSection.LiveSmoke, ready: 2)
            ]);

        Assert.Equal(MarketplaceIntegrationSeverity.Blocked, dashboard.OverallSeverity);
        Assert.Equal("1 blocked across 1 section", dashboard.SummaryText);
    }

    [Fact]
    public void FromInputsDerivesSectionCountsFromMarketplaceChildInputs()
    {
        MarketplaceIntegrationStatusDashboardViewModel dashboard = MarketplaceIntegrationStatusDashboardFactory.FromInputs(
            new MarketplaceIntegrationStatusInputs(
                ApiSync: new MarketplaceApiSyncStatusInput(SyncedVendorCount: 3, WarningCount: 1, BlockedCount: 0),
                InUseSync: new MarketplaceInUseSyncStatusInput(SyncedQueueCount: 9, PendingQueueCount: 2, DueQueueCount: 1),
                BomRollup: new MarketplaceBomRollupStatusInput(CompleteLineCount: 11, DiagnosticCount: 4, IncompleteLineCount: 2),
                DedupReview: new MarketplaceDedupReviewStatusInput(ClearComponentCount: 8, PendingComponentCount: 3, WarningCount: 2),
                TrustedLibraryPromotion: new MarketplaceTrustedPromotionStatusInput(ReadyComponentCount: 5, WarningCount: 1, BlockedCount: 1),
                FabricationOrdering: new MarketplaceFabricationOrderingStatusInput(ReadyOrderCount: 6, WarningCount: 2, BlockedCount: 3),
                LiveSmoke: new MarketplaceLiveSmokeStatusInput(PassingCheckCount: 7, WarningCount: 1, BlockedCheckCount: 2)));

        Assert.Equal(7, dashboard.SectionCount);
        Assert.Equal(49, dashboard.ReadyItemCount);
        Assert.Equal(16, dashboard.WarningItemCount);
        Assert.Equal(9, dashboard.BlockedItemCount);
        Assert.Equal(MarketplaceIntegrationSeverity.Blocked, dashboard.OverallSeverity);
        Assert.Equal("Blocked", dashboard.OverallSeverityLabel);
        Assert.Equal("9 blocked, 16 warnings across 7 sections", dashboard.SummaryText);
        Assert.Equal("Resolve 9 blocked marketplace integration items", dashboard.NextActionText);

        AssertSection(dashboard, MarketplaceIntegrationSection.ApiSync, ready: 3, warnings: 1, blocked: 0);
        AssertSection(dashboard, MarketplaceIntegrationSection.InUseSync, ready: 9, warnings: 2, blocked: 1);
        AssertSection(dashboard, MarketplaceIntegrationSection.BomRollup, ready: 11, warnings: 4, blocked: 2);
        AssertSection(dashboard, MarketplaceIntegrationSection.DedupReview, ready: 8, warnings: 5, blocked: 0);
        AssertSection(dashboard, MarketplaceIntegrationSection.TrustedLibraryPromotion, ready: 5, warnings: 1, blocked: 1);
        AssertSection(dashboard, MarketplaceIntegrationSection.FabricationOrdering, ready: 6, warnings: 2, blocked: 3);
        AssertSection(dashboard, MarketplaceIntegrationSection.LiveSmoke, ready: 7, warnings: 1, blocked: 2);
    }

    private static MarketplaceIntegrationSectionStatus Section(
        MarketplaceIntegrationSection section,
        int ready = 0,
        int warnings = 0,
        int blocked = 0) =>
        new(section, ready, warnings, blocked);

    private static void AssertSection(
        MarketplaceIntegrationStatusDashboardViewModel dashboard,
        MarketplaceIntegrationSection section,
        int ready,
        int warnings,
        int blocked)
    {
        MarketplaceIntegrationStatusRow row = Assert.Single(dashboard.Rows, row => row.Section == section);
        Assert.Equal(ready, row.ReadyCount);
        Assert.Equal(warnings, row.WarningCount);
        Assert.Equal(blocked, row.BlockedCount);
    }
}
