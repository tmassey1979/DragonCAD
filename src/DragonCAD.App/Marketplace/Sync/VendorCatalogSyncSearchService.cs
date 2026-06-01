using DragonCAD.Sourcing.Catalog.Sync;

namespace DragonCAD.App.Marketplace.Sync;

public sealed class VendorCatalogSyncSearchService : IVendorCatalogSyncSearchService
{
    private readonly VendorCatalogSyncRunner runner;

    public VendorCatalogSyncSearchService(VendorCatalogSyncRunner runner)
    {
        this.runner = runner ?? throw new ArgumentNullException(nameof(runner));
    }

    public Task<VendorCatalogSyncRunResult> SearchAsync(
        string providerName,
        string query,
        int limit,
        CancellationToken cancellationToken) =>
        runner.SearchProviderAsync(providerName, query, limit, cancellationToken);
}
