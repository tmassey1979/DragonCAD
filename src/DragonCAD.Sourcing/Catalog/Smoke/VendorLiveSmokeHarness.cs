using DragonCAD.Sourcing.Catalog.DigiKey;
using DragonCAD.Sourcing.Catalog.Mouser;

namespace DragonCAD.Sourcing.Catalog.Smoke;

public sealed class VendorLiveSmokeHarness
{
    public const string GateEnvironmentVariable = "DRAGONCAD_VENDOR_LIVE_SMOKE";

    private const string DigiKeyProviderName = "Digi-Key";
    private const string MouserProviderName = "Mouser";

    private readonly Func<string, string?> readEnvironment;
    private readonly Func<string, HttpClient> createHttpClient;

    public VendorLiveSmokeHarness(
        Func<string, string?> readEnvironment,
        Func<string, HttpClient> createHttpClient)
    {
        this.readEnvironment = readEnvironment ?? throw new ArgumentNullException(nameof(readEnvironment));
        this.createHttpClient = createHttpClient ?? throw new ArgumentNullException(nameof(createHttpClient));
    }

    public static VendorLiveSmokeHarness CreateDefault()
    {
        return new VendorLiveSmokeHarness(
            Environment.GetEnvironmentVariable,
            _ => new HttpClient());
    }

    public static bool IsEnabled(Func<string, string?>? readEnvironment = null)
    {
        readEnvironment ??= Environment.GetEnvironmentVariable;

        var value = readEnvironment(GateEnvironmentVariable);
        return string.Equals(value, "1", StringComparison.Ordinal) ||
            string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<VendorLiveSmokeRunResult> RunDigiKeyKeywordSearchAsync(
        string keyword,
        int limit,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyword);

        if (!IsEnabled(readEnvironment))
        {
            return VendorLiveSmokeRunResult.Disabled(DigiKeyProviderName);
        }

        using var tokenHttpClient = createHttpClient(DigiKeyProviderName);
        var tokenClient = new DigiKeyOAuthClient(
            tokenHttpClient,
            DigiKeyOAuthClientOptions.FromEnvironment(readEnvironment));
        var tokenResult = await tokenClient.RequestClientCredentialsTokenAsync(cancellationToken).ConfigureAwait(false);
        if (tokenResult.Token is null)
        {
            return VendorLiveSmokeRunResult.Failed(DigiKeyProviderName, tokenResult.Diagnostics);
        }

        using var searchHttpClient = createHttpClient(DigiKeyProviderName);
        var searchClient = new DigiKeyProductSearchClient(
            searchHttpClient,
            DigiKeyProductSearchClientOptions.FromOAuthToken(
                readEnvironment("DRAGONCAD_DIGIKEY_CLIENT_ID") ?? string.Empty,
                tokenResult.Token));
        var searchResult = await searchClient.SearchByKeywordAsync(keyword, limit, cancellationToken).ConfigureAwait(false);

        return VendorLiveSmokeRunResult.FromCatalogResult(DigiKeyProviderName, searchResult);
    }

    public async Task<VendorLiveSmokeRunResult> RunMouserKeywordSearchAsync(
        string keyword,
        int limit,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyword);

        if (!IsEnabled(readEnvironment))
        {
            return VendorLiveSmokeRunResult.Disabled(MouserProviderName);
        }

        using var httpClient = createHttpClient(MouserProviderName);
        var client = new MouserSearchClient(
            httpClient,
            MouserSearchClientOptions.FromEnvironment(readEnvironment));
        var result = await client.SearchByKeywordAsync(keyword, limit, cancellationToken).ConfigureAwait(false);

        return VendorLiveSmokeRunResult.FromCatalogResult(MouserProviderName, result);
    }
}
