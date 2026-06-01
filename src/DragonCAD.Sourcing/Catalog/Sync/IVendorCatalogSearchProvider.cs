namespace DragonCAD.Sourcing.Catalog.Sync;

public interface IVendorCatalogSearchProvider
{
    string ProviderName { get; }

    Task<CatalogImportResult> SearchAsync(
        string query,
        int limit,
        CancellationToken cancellationToken);
}
