using DragonCAD.Sourcing.Catalog.Sync;

namespace DragonCAD.Sourcing.Catalog.Mouser;

public sealed class MouserCatalogSearchProvider : IVendorCatalogSearchProvider
{
    private readonly MouserSearchClient searchClient;

    public MouserCatalogSearchProvider(MouserSearchClient searchClient)
    {
        this.searchClient = searchClient ?? throw new ArgumentNullException(nameof(searchClient));
    }

    public string ProviderName => "Mouser";

    public Task<CatalogImportResult> SearchAsync(
        string query,
        int limit,
        CancellationToken cancellationToken) =>
        searchClient.SearchByPartNumberAsync(query, limit, cancellationToken);
}
