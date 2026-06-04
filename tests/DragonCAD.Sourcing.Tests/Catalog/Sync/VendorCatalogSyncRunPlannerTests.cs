using DragonCAD.Sourcing.Catalog;
using DragonCAD.Sourcing.Catalog.Sync;

namespace DragonCAD.Sourcing.Tests.Catalog.Sync;

public sealed class VendorCatalogSyncRunPlannerTests
{
    private static readonly DateTimeOffset PlannedAtUtc = new(2026, 6, 3, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void PlanMarksCredentialedApiProviderReadyWhenRequiredCredentialKeysArePresent()
    {
        var request = Request(
            credentialKeysByProviderId: new Dictionary<string, IReadOnlySet<string>>
            {
                ["digikey"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "client_id", "client_secret" },
                ["mouser"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "api_key" },
            });

        VendorCatalogSyncRunPlan plan = VendorCatalogSyncRunPlanner.Plan(request);

        VendorCatalogSyncProviderRunPlan digikey = Provider(plan, "digikey");
        Assert.False(digikey.IsBlocked);
        Assert.Equal(VendorCatalogSyncCredentialReadiness.Ready, digikey.CredentialReadiness);
        Assert.Equal(["LM7805", "NE555P"], digikey.RequestedSearchTerms);
        Assert.Contains("Digi-Key", digikey.RateLimitNotes);
        Assert.True(digikey.Capabilities.HasFlag(CatalogProviderCapabilities.Api));
        Assert.Empty(digikey.Blockers);
    }

    [Fact]
    public void PlanBlocksCredentialedApiProviderWhenRequiredCredentialKeyIsMissing()
    {
        var request = Request(
            credentialKeysByProviderId: new Dictionary<string, IReadOnlySet<string>>
            {
                ["digikey"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "client_id" },
            });

        VendorCatalogSyncRunPlan plan = VendorCatalogSyncRunPlanner.Plan(request);

        VendorCatalogSyncProviderRunPlan digikey = Provider(plan, "digikey");
        Assert.True(digikey.IsBlocked);
        Assert.Equal(VendorCatalogSyncCredentialReadiness.MissingRequiredCredential, digikey.CredentialReadiness);
        Assert.Contains(
            digikey.Blockers,
            blocker => blocker.Code == VendorCatalogSyncDiagnosticCodes.MissingCredential
                && blocker.Message.Contains("client_secret", StringComparison.Ordinal)
                && !blocker.Message.Contains("real-client-secret", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PlanBlocksManualFeedProviderWithNonSecretDiagnostic()
    {
        VendorCatalogSyncRunPlan plan = VendorCatalogSyncRunPlanner.Plan(Request());

        VendorCatalogSyncProviderRunPlan jameco = Provider(plan, "jameco");
        Assert.True(jameco.IsBlocked);
        Assert.Equal(VendorCatalogSyncCredentialReadiness.NotRequired, jameco.CredentialReadiness);
        Assert.True(jameco.Capabilities.HasFlag(CatalogProviderCapabilities.Manual));
        var blocker = Assert.Single(jameco.Blockers);
        Assert.Equal(VendorCatalogSyncDiagnosticCodes.ManualFeedRequired, blocker.Code);
        Assert.Contains("manual catalog feed", blocker.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", blocker.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PlanWarnsWhenProviderCacheIsOlderThanFreshnessWindow()
    {
        var request = Request(
            cacheRetrievedAtUtcByProviderId: new Dictionary<string, DateTimeOffset>
            {
                ["adafruit"] = PlannedAtUtc.AddHours(-25),
            });

        VendorCatalogSyncRunPlan plan = VendorCatalogSyncRunPlanner.Plan(request);

        VendorCatalogSyncProviderRunPlan adafruit = Provider(plan, "adafruit");
        Assert.False(adafruit.IsBlocked);
        Assert.Equal(TimeSpan.FromHours(24), adafruit.FreshnessWindow);
        Assert.Contains(
            adafruit.Warnings,
            warning => warning.Code == VendorCatalogSyncDiagnosticCodes.StaleCache
                && warning.Message.Contains("25", StringComparison.Ordinal));
    }

    [Fact]
    public void PlanBlocksProviderWhenRequiredCapabilityIsUnsupported()
    {
        VendorCatalogSyncRunPlan plan = VendorCatalogSyncRunPlanner.Plan(
            Request(requiredCapabilities: CatalogProviderCapabilities.Api | CatalogProviderCapabilities.Feed));

        VendorCatalogSyncProviderRunPlan adafruit = Provider(plan, "adafruit");
        Assert.True(adafruit.IsBlocked);
        Assert.Contains(
            adafruit.Blockers,
            blocker => blocker.Code == VendorCatalogSyncDiagnosticCodes.UnsupportedCapability
                && blocker.Message.Contains("Feed", StringComparison.Ordinal));
    }

    [Fact]
    public void PlanUsesDeterministicProviderOrdering()
    {
        VendorCatalogSyncRunPlan plan = VendorCatalogSyncRunPlanner.Plan(Request());

        Assert.Equal(
            ["digikey", "mouser", "jameco", "sparkfun", "adafruit"],
            plan.Providers.Select(provider => provider.ProviderId));
    }

    private static VendorCatalogSyncRunRequest Request(
        IReadOnlyDictionary<string, IReadOnlySet<string>>? credentialKeysByProviderId = null,
        IReadOnlyDictionary<string, DateTimeOffset>? cacheRetrievedAtUtcByProviderId = null,
        CatalogProviderCapabilities requiredCapabilities = CatalogProviderCapabilities.None) =>
        new(
            RequestedSearchTerms: [" LM7805 ", "NE555P", "LM7805", " "],
            PlannedAtUtc: PlannedAtUtc,
            FreshnessWindow: TimeSpan.FromHours(24),
            CredentialKeysByProviderId: credentialKeysByProviderId ?? new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase),
            CacheRetrievedAtUtcByProviderId: cacheRetrievedAtUtcByProviderId ?? new Dictionary<string, DateTimeOffset>(StringComparer.OrdinalIgnoreCase),
            RequiredCapabilities: requiredCapabilities);

    private static VendorCatalogSyncProviderRunPlan Provider(VendorCatalogSyncRunPlan plan, string providerId) =>
        plan.Providers.Single(provider => provider.ProviderId == providerId);
}
