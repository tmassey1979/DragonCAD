namespace DragonCAD.Sourcing.Catalog.Sync;

public static class VendorCatalogSyncRunPlanner
{
    private static readonly IReadOnlyList<ProviderProfile> DefaultProviders =
    [
        new(
            ProviderId: "digikey",
            DisplayName: "Digi-Key",
            RequiredCredentialKeys: ["client_id", "client_secret"],
            Capabilities: CatalogProviderCapabilities.Api,
            RateLimitNotes: "Digi-Key Product Information API: respect account throttles and retry-after headers.",
            RequiresManualFeed: false),
        new(
            ProviderId: "mouser",
            DisplayName: "Mouser",
            RequiredCredentialKeys: ["api_key"],
            Capabilities: CatalogProviderCapabilities.Api,
            RateLimitNotes: "Mouser Search API: respect account throttles and response metadata.",
            RequiresManualFeed: false),
        new(
            ProviderId: "jameco",
            DisplayName: "Jameco",
            RequiredCredentialKeys: [],
            Capabilities: CatalogProviderCapabilities.Manual | CatalogProviderCapabilities.Feed,
            RateLimitNotes: "Jameco manual catalog feed: refresh through approved offline imports.",
            RequiresManualFeed: true),
        new(
            ProviderId: "sparkfun",
            DisplayName: "SparkFun",
            RequiredCredentialKeys: [],
            Capabilities: CatalogProviderCapabilities.Feed,
            RateLimitNotes: "SparkFun repository-derived catalog metadata: review cached open-hardware sync state.",
            RequiresManualFeed: false),
        new(
            ProviderId: "adafruit",
            DisplayName: "Adafruit",
            RequiredCredentialKeys: [],
            Capabilities: CatalogProviderCapabilities.Api,
            RateLimitNotes: "Adafruit public product API: plan against cached public product metadata.",
            RequiresManualFeed: false),
    ];

    public static VendorCatalogSyncRunPlan Plan(VendorCatalogSyncRunRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var searchTerms = NormalizeSearchTerms(request.RequestedSearchTerms);
        var providers = DefaultProviders
            .Select(provider => PlanProvider(provider, request, searchTerms))
            .ToArray();

        return new VendorCatalogSyncRunPlan(
            request.PlannedAtUtc,
            request.FreshnessWindow,
            providers);
    }

    private static VendorCatalogSyncProviderRunPlan PlanProvider(
        ProviderProfile provider,
        VendorCatalogSyncRunRequest request,
        IReadOnlyList<string> searchTerms)
    {
        var blockers = new List<VendorCatalogSyncPlanDiagnostic>();
        var warnings = new List<VendorCatalogSyncPlanDiagnostic>();
        VendorCatalogSyncCredentialReadiness readiness = CredentialReadiness(provider, request, blockers);

        AddManualFeedBlocker(provider, blockers);
        AddUnsupportedCapabilityBlocker(provider, request.RequiredCapabilities, blockers);
        AddCacheFreshnessWarning(provider, request, warnings);

        return new VendorCatalogSyncProviderRunPlan(
            provider.ProviderId,
            provider.DisplayName,
            readiness,
            provider.RateLimitNotes,
            request.FreshnessWindow,
            searchTerms,
            provider.Capabilities,
            blockers,
            warnings);
    }

    private static VendorCatalogSyncCredentialReadiness CredentialReadiness(
        ProviderProfile provider,
        VendorCatalogSyncRunRequest request,
        List<VendorCatalogSyncPlanDiagnostic> blockers)
    {
        if (provider.RequiredCredentialKeys.Count == 0)
        {
            return VendorCatalogSyncCredentialReadiness.NotRequired;
        }

        request.CredentialKeysByProviderId.TryGetValue(provider.ProviderId, out IReadOnlySet<string>? availableKeys);
        var missingKeys = provider.RequiredCredentialKeys
            .Where(key => availableKeys is null || !availableKeys.Contains(key))
            .ToArray();

        if (missingKeys.Length == 0)
        {
            return VendorCatalogSyncCredentialReadiness.Ready;
        }

        blockers.Add(new VendorCatalogSyncPlanDiagnostic(
            VendorCatalogSyncDiagnosticCodes.MissingCredential,
            $"{provider.DisplayName} catalog sync is missing required credential key(s): {string.Join(", ", missingKeys)}."));
        return VendorCatalogSyncCredentialReadiness.MissingRequiredCredential;
    }

    private static void AddManualFeedBlocker(
        ProviderProfile provider,
        List<VendorCatalogSyncPlanDiagnostic> blockers)
    {
        if (!provider.RequiresManualFeed)
        {
            return;
        }

        blockers.Add(new VendorCatalogSyncPlanDiagnostic(
            VendorCatalogSyncDiagnosticCodes.ManualFeedRequired,
            $"{provider.DisplayName} catalog sync requires an approved manual catalog feed before this run can execute."));
    }

    private static void AddUnsupportedCapabilityBlocker(
        ProviderProfile provider,
        CatalogProviderCapabilities requiredCapabilities,
        List<VendorCatalogSyncPlanDiagnostic> blockers)
    {
        if (requiredCapabilities == CatalogProviderCapabilities.None)
        {
            return;
        }

        CatalogProviderCapabilities unsupported = requiredCapabilities & ~provider.Capabilities;
        if (unsupported == CatalogProviderCapabilities.None)
        {
            return;
        }

        blockers.Add(new VendorCatalogSyncPlanDiagnostic(
            VendorCatalogSyncDiagnosticCodes.UnsupportedCapability,
            $"{provider.DisplayName} catalog sync does not support required capability: {unsupported}."));
    }

    private static void AddCacheFreshnessWarning(
        ProviderProfile provider,
        VendorCatalogSyncRunRequest request,
        List<VendorCatalogSyncPlanDiagnostic> warnings)
    {
        if (!request.CacheRetrievedAtUtcByProviderId.TryGetValue(provider.ProviderId, out DateTimeOffset retrievedAtUtc))
        {
            return;
        }

        TimeSpan cacheAge = request.PlannedAtUtc - retrievedAtUtc;
        if (cacheAge <= request.FreshnessWindow)
        {
            return;
        }

        warnings.Add(new VendorCatalogSyncPlanDiagnostic(
            VendorCatalogSyncDiagnosticCodes.StaleCache,
            $"{provider.DisplayName} cached catalog data is {Math.Floor(cacheAge.TotalHours):N0} hours old, older than the {request.FreshnessWindow.TotalHours:N0} hour freshness window."));
    }

    private static IReadOnlyList<string> NormalizeSearchTerms(IReadOnlyList<string> searchTerms)
    {
        ArgumentNullException.ThrowIfNull(searchTerms);

        return searchTerms
            .Select(term => term?.Trim() ?? string.Empty)
            .Where(term => term.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private sealed record ProviderProfile(
        string ProviderId,
        string DisplayName,
        IReadOnlyList<string> RequiredCredentialKeys,
        CatalogProviderCapabilities Capabilities,
        string RateLimitNotes,
        bool RequiresManualFeed);
}
