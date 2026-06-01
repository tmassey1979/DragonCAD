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
        Assert.Equal("Blocked", dashboard.OverallSeverityLabel);
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

        Assert.Equal("Ready", dashboard.OverallSeverityLabel);
        Assert.Equal("Marketplace integration is ready", dashboard.NextActionText);
        Assert.All(dashboard.Rows, row => Assert.Equal("Ready", row.SeverityLabel));
    }

    private static MarketplaceIntegrationSectionStatus Section(
        MarketplaceIntegrationSection section,
        int ready = 0,
        int warnings = 0,
        int blocked = 0) =>
        new(section, ready, warnings, blocked);
}
