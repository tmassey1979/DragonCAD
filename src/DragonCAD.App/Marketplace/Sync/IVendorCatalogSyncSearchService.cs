using DragonCAD.Sourcing.Catalog.Sync;

namespace DragonCAD.App.Marketplace.Sync;

public interface IVendorCatalogSyncSearchService
{
    Task<VendorCatalogSyncRunResult> SearchAsync(
        string providerName,
        string query,
        int limit,
        CancellationToken cancellationToken);
}
