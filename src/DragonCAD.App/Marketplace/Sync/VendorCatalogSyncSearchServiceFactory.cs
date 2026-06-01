using DragonCAD.Sourcing.Catalog.DigiKey;
using DragonCAD.Sourcing.Catalog.Mouser;
using DragonCAD.Sourcing.Catalog.Sync;

namespace DragonCAD.App.Marketplace.Sync;

public static class VendorCatalogSyncSearchServiceFactory
{
    public static IVendorCatalogSyncSearchService CreateFromEnvironment()
    {
        var digiKeyOAuthOptions = DigiKeyOAuthClientOptions.FromEnvironment();
        var mouserOptions = MouserSearchClientOptions.FromEnvironment();

        var providers = new IVendorCatalogSearchProvider[]
        {
            new DigiKeyCatalogSearchProvider(
                new DigiKeyOAuthTokenCache(new DigiKeyOAuthClient(new HttpClient(), digiKeyOAuthOptions)),
                new HttpClient(),
                digiKeyOAuthOptions),
            new MouserCatalogSearchProvider(
                new MouserSearchClient(new HttpClient(), mouserOptions)),
        };

        return new VendorCatalogSyncSearchService(new VendorCatalogSyncRunner(providers));
    }
}
