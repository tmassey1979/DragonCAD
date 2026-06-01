using DragonCAD.Sourcing.Catalog.Sync;

namespace DragonCAD.Sourcing.Catalog.DigiKey;

public sealed class DigiKeyCatalogSearchProvider : IVendorCatalogSearchProvider
{
    private readonly IDigiKeyOAuthTokenSource oauthClient;
    private readonly HttpClient productSearchHttpClient;
    private readonly DigiKeyOAuthClientOptions oauthOptions;

    public DigiKeyCatalogSearchProvider(
        IDigiKeyOAuthTokenSource oauthClient,
        HttpClient productSearchHttpClient,
        DigiKeyOAuthClientOptions oauthOptions)
    {
        this.oauthClient = oauthClient ?? throw new ArgumentNullException(nameof(oauthClient));
        this.productSearchHttpClient = productSearchHttpClient ?? throw new ArgumentNullException(nameof(productSearchHttpClient));
        this.oauthOptions = oauthOptions ?? throw new ArgumentNullException(nameof(oauthOptions));
    }

    public string ProviderName => "Digi-Key";

    public async Task<CatalogImportResult> SearchAsync(
        string query,
        int limit,
        CancellationToken cancellationToken)
    {
        DigiKeyOAuthTokenResult tokenResult = await oauthClient
            .RequestClientCredentialsTokenAsync(cancellationToken)
            .ConfigureAwait(false);

        if (tokenResult.Token is null)
        {
            return new CatalogImportResult([], tokenResult.Diagnostics);
        }

        var productClient = new DigiKeyProductSearchClient(
            productSearchHttpClient,
            DigiKeyProductSearchClientOptions.FromOAuthToken(oauthOptions.ClientId, tokenResult.Token));

        return await productClient
            .SearchByKeywordAsync(query, limit, cancellationToken)
            .ConfigureAwait(false);
    }
}
