using DragonCAD.Sourcing.Catalog.DigiKey;
using DragonCAD.Sourcing.Catalog.Mouser;
using DragonCAD.Sourcing.Catalog.Sync;

namespace DragonCAD.Sourcing.Catalog.Smoke;

public sealed class VendorLiveSmokeHarness
{
    public const string GateEnvironmentVariable = "DRAGONCAD_VENDOR_LIVE_SMOKE";
    public const string ModeEnvironmentVariable = "DRAGONCAD_VENDOR_LIVE_SMOKE_MODE";

    private const string DigiKeyProviderName = "Digi-Key";
    private const string MouserProviderName = "Mouser";
    private const string EnabledMessage = "Set DRAGONCAD_VENDOR_LIVE_SMOKE to true to enable provider smoke planning.";

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

    public VendorLiveSmokePlan PlanProviderChecks(string query, int limit)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);

        var normalizedQuery = query.Trim();
        var mode = ResolveMode(readEnvironment);

        return new VendorLiveSmokePlan(
            mode,
            [
                PlanProviderCheck(
                    DigiKeyProviderName,
                    normalizedQuery,
                    limit,
                    mode,
                    [
                        new CredentialCheck("client_id", "DRAGONCAD_DIGIKEY_CLIENT_ID"),
                        new CredentialCheck("client_secret", "DRAGONCAD_DIGIKEY_CLIENT_SECRET"),
                    ]),
                PlanProviderCheck(
                    MouserProviderName,
                    normalizedQuery,
                    limit,
                    mode,
                    [new CredentialCheck("api_key", "DRAGONCAD_MOUSER_API_KEY")]),
            ]);
    }

    public async Task<VendorLiveSmokeRunResult> RunProviderCheckAsync(
        VendorLiveSmokeProviderCheck check,
        IVendorCatalogSearchProvider provider,
        Func<CatalogImportResult, TimeSpan> elapsedTime,
        Func<string> requestId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(check);
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(elapsedTime);
        ArgumentNullException.ThrowIfNull(requestId);

        if (check.Status != VendorLiveSmokeProviderStatus.Planned ||
            check.Mode is not (VendorLiveSmokeMode.Live or VendorLiveSmokeMode.Sandbox))
        {
            return new VendorLiveSmokeRunResult(
                check.ProviderName,
                MapStatus(check.Status),
                0,
                [],
                requestId(),
                TimeSpan.Zero,
                check.Diagnostics
                    .Select(diagnostic => VendorLiveSmokeReportSanitizer.Sanitize(diagnostic, check.RedactionTerms))
                    .ToArray());
        }

        var result = await provider
            .SearchAsync(check.Query, check.Limit, cancellationToken)
            .ConfigureAwait(false);

        return VendorLiveSmokeRunResult.FromCatalogResult(
            provider.ProviderName,
            result,
            requestId(),
            elapsedTime(result),
            check.RedactionTerms);
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

    private VendorLiveSmokeProviderCheck PlanProviderCheck(
        string providerName,
        string query,
        int limit,
        VendorLiveSmokeMode mode,
        IReadOnlyList<CredentialCheck> requiredCredentials)
    {
        if (mode == VendorLiveSmokeMode.Disabled)
        {
            return new VendorLiveSmokeProviderCheck(
                providerName,
                query,
                limit,
                mode,
                VendorLiveSmokeProviderStatus.Disabled,
                BuildCredentialSummary(requiredCredentials),
                [EnabledMessage],
                BuildRedactionTerms(requiredCredentials));
        }

        var missingCredentialKeys = requiredCredentials
            .Where(credential => string.IsNullOrWhiteSpace(readEnvironment(credential.EnvironmentName)))
            .Select(credential => credential.KeyName)
            .ToArray();

        if (missingCredentialKeys.Length > 0)
        {
            return new VendorLiveSmokeProviderCheck(
                providerName,
                query,
                limit,
                mode,
                VendorLiveSmokeProviderStatus.MissingCredentials,
                BuildCredentialSummary(requiredCredentials),
                [$"{providerName} smoke check is missing required credential key(s): {string.Join(", ", missingCredentialKeys)}."],
                BuildRedactionTerms(requiredCredentials));
        }

        return new VendorLiveSmokeProviderCheck(
            providerName,
            query,
            limit,
            mode,
            VendorLiveSmokeProviderStatus.Planned,
            BuildCredentialSummary(requiredCredentials),
            mode == VendorLiveSmokeMode.DryRun
                ? ["Dry-run only: request plan was produced without provider calls."]
                : [],
            BuildRedactionTerms(requiredCredentials));
    }

    private string BuildCredentialSummary(IReadOnlyList<CredentialCheck> requiredCredentials)
    {
        return string.Join(
            "; ",
            requiredCredentials.Select(credential =>
            {
                var state = string.IsNullOrWhiteSpace(readEnvironment(credential.EnvironmentName))
                    ? "missing"
                    : "configured";
                return $"{credential.KeyName}: {state}";
            }));
    }

    private IReadOnlyList<string> BuildRedactionTerms(IReadOnlyList<CredentialCheck> requiredCredentials)
    {
        return requiredCredentials
            .Select(credential => readEnvironment(credential.EnvironmentName))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToArray();
    }

    private static VendorLiveSmokeMode ResolveMode(Func<string, string?> readEnvironment)
    {
        if (!IsEnabled(readEnvironment))
        {
            return VendorLiveSmokeMode.Disabled;
        }

        var mode = readEnvironment(ModeEnvironmentVariable);
        return mode?.Trim().ToLowerInvariant() switch
        {
            "dry-run" or "dryrun" or "plan" => VendorLiveSmokeMode.DryRun,
            "sandbox" => VendorLiveSmokeMode.Sandbox,
            "live" => VendorLiveSmokeMode.Live,
            _ => VendorLiveSmokeMode.DryRun,
        };
    }

    private static VendorLiveSmokeRunStatus MapStatus(VendorLiveSmokeProviderStatus status) =>
        status switch
        {
            VendorLiveSmokeProviderStatus.Disabled => VendorLiveSmokeRunStatus.Disabled,
            VendorLiveSmokeProviderStatus.MissingCredentials => VendorLiveSmokeRunStatus.MissingCredentials,
            VendorLiveSmokeProviderStatus.RateLimited => VendorLiveSmokeRunStatus.RateLimited,
            VendorLiveSmokeProviderStatus.Succeeded => VendorLiveSmokeRunStatus.Succeeded,
            VendorLiveSmokeProviderStatus.Failed => VendorLiveSmokeRunStatus.Failed,
            _ => VendorLiveSmokeRunStatus.Failed,
        };

    private sealed record CredentialCheck(string KeyName, string EnvironmentName);
}
