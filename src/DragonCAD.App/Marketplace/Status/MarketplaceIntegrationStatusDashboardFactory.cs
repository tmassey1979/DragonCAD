namespace DragonCAD.App.Marketplace.Status;

public static class MarketplaceIntegrationStatusDashboardFactory
{
    public static MarketplaceIntegrationStatusDashboardViewModel FromInputs(MarketplaceIntegrationStatusInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);

        return MarketplaceIntegrationStatusDashboardViewModel.FromSections(
            [
                new MarketplaceIntegrationSectionStatus(
                    MarketplaceIntegrationSection.ApiSync,
                    inputs.ApiSync.SyncedVendorCount,
                    inputs.ApiSync.WarningCount,
                    inputs.ApiSync.BlockedCount),
                new MarketplaceIntegrationSectionStatus(
                    MarketplaceIntegrationSection.InUseSync,
                    inputs.InUseSync.SyncedQueueCount,
                    inputs.InUseSync.PendingQueueCount,
                    inputs.InUseSync.DueQueueCount),
                new MarketplaceIntegrationSectionStatus(
                    MarketplaceIntegrationSection.BomRollup,
                    inputs.BomRollup.CompleteLineCount,
                    inputs.BomRollup.DiagnosticCount,
                    inputs.BomRollup.IncompleteLineCount),
                new MarketplaceIntegrationSectionStatus(
                    MarketplaceIntegrationSection.DedupReview,
                    inputs.DedupReview.ClearComponentCount,
                    inputs.DedupReview.PendingComponentCount + inputs.DedupReview.WarningCount,
                    0),
                new MarketplaceIntegrationSectionStatus(
                    MarketplaceIntegrationSection.TrustedLibraryPromotion,
                    inputs.TrustedLibraryPromotion.ReadyComponentCount,
                    inputs.TrustedLibraryPromotion.WarningCount,
                    inputs.TrustedLibraryPromotion.BlockedCount),
                new MarketplaceIntegrationSectionStatus(
                    MarketplaceIntegrationSection.FabricationOrdering,
                    inputs.FabricationOrdering.ReadyOrderCount,
                    inputs.FabricationOrdering.WarningCount,
                    inputs.FabricationOrdering.BlockedCount),
                new MarketplaceIntegrationSectionStatus(
                    MarketplaceIntegrationSection.LiveSmoke,
                    inputs.LiveSmoke.PassingCheckCount,
                    inputs.LiveSmoke.WarningCount,
                    inputs.LiveSmoke.BlockedCheckCount)
            ]);
    }
}

public sealed record MarketplaceIntegrationStatusInputs(
    MarketplaceApiSyncStatusInput ApiSync,
    MarketplaceInUseSyncStatusInput InUseSync,
    MarketplaceBomRollupStatusInput BomRollup,
    MarketplaceDedupReviewStatusInput DedupReview,
    MarketplaceTrustedPromotionStatusInput TrustedLibraryPromotion,
    MarketplaceFabricationOrderingStatusInput FabricationOrdering,
    MarketplaceLiveSmokeStatusInput LiveSmoke);

public sealed record MarketplaceApiSyncStatusInput(
    int SyncedVendorCount,
    int WarningCount,
    int BlockedCount);

public sealed record MarketplaceInUseSyncStatusInput(
    int SyncedQueueCount,
    int PendingQueueCount,
    int DueQueueCount);

public sealed record MarketplaceBomRollupStatusInput(
    int CompleteLineCount,
    int DiagnosticCount,
    int IncompleteLineCount);

public sealed record MarketplaceDedupReviewStatusInput(
    int ClearComponentCount,
    int PendingComponentCount,
    int WarningCount);

public sealed record MarketplaceTrustedPromotionStatusInput(
    int ReadyComponentCount,
    int WarningCount,
    int BlockedCount);

public sealed record MarketplaceFabricationOrderingStatusInput(
    int ReadyOrderCount,
    int WarningCount,
    int BlockedCount);

public sealed record MarketplaceLiveSmokeStatusInput(
    int PassingCheckCount,
    int WarningCount,
    int BlockedCheckCount);
