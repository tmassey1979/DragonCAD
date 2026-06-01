namespace DragonCAD.Sourcing.Catalog.Sync;

public sealed class VendorCatalogSyncRunner
{
    private readonly IReadOnlyDictionary<string, IVendorCatalogSearchProvider> providers;

    public VendorCatalogSyncRunner(IEnumerable<IVendorCatalogSearchProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);

        this.providers = providers.ToDictionary(
            provider => provider.ProviderName,
            StringComparer.OrdinalIgnoreCase);
    }

    public async Task<VendorCatalogSyncRunResult> SearchProviderAsync(
        string providerName,
        string query,
        int limit,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);

        var normalizedQuery = query?.Trim() ?? string.Empty;
        if (normalizedQuery.Length == 0)
        {
            return Blocked(
                providerName,
                normalizedQuery,
                VendorCatalogSyncDiagnosticCodes.MissingQuery,
                "A catalog sync search query is required.");
        }

        if (!providers.TryGetValue(providerName, out var provider))
        {
            return Blocked(
                providerName,
                normalizedQuery,
                VendorCatalogSyncDiagnosticCodes.ProviderUnavailable,
                $"{providerName} does not have an executable catalog sync provider configured.");
        }

        CatalogImportResult result = await provider
            .SearchAsync(normalizedQuery, Math.Clamp(limit, 1, 50), cancellationToken)
            .ConfigureAwait(false);

        return new VendorCatalogSyncRunResult(
            provider.ProviderName,
            normalizedQuery,
            VendorCatalogSyncRunStatus.Completed,
            result.Listings,
            result.Diagnostics);
    }

    private static VendorCatalogSyncRunResult Blocked(
        string providerName,
        string query,
        string code,
        string message) =>
        new(
            providerName,
            query,
            VendorCatalogSyncRunStatus.Blocked,
            [],
            [new CatalogImportDiagnostic(CatalogDiagnosticSeverity.Error, code, message, providerName, null)]);
}
