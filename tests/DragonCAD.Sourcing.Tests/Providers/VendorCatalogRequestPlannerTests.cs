using DragonCAD.Sourcing.Providers;

namespace DragonCAD.Sourcing.Tests.Providers;

public sealed class VendorCatalogRequestPlannerTests
{
    [Fact]
    public void DefaultProfilesClassifyProviderAccessModes()
    {
        var profiles = VendorCatalogRequestPlanner.DefaultProfiles;

        Assert.Equal(VendorCatalogAccessMode.CredentialedApi, profiles["Digi-Key"].AccessMode);
        Assert.Equal(VendorCatalogAccessMode.CredentialedApi, profiles["Mouser"].AccessMode);
        Assert.Equal(VendorCatalogAccessMode.PublicProductApi, profiles["Adafruit"].AccessMode);
        Assert.Equal(VendorCatalogAccessMode.OpenHardwareRepositorySync, profiles["SparkFun"].AccessMode);
        Assert.Equal(VendorCatalogAccessMode.ManualCatalogFeedFallback, profiles["Jameco"].AccessMode);
    }

    [Fact]
    public void DigiKeyRequestPlanUsesCredentialReferencesWithoutLeakingSecretsInLogs()
    {
        var credentials = VendorCredentialBag.FromSecretValues(
            new Dictionary<string, string>
            {
                ["client_id"] = "real-client-id-should-not-log",
                ["client_secret"] = "real-client-secret-should-not-log",
            });

        var plan = VendorCatalogRequestPlanner.BuildQueryByManufacturerPartNumber(
            "Digi-Key",
            "LM7805CT/NOPB",
            credentials);

        Assert.Equal(HttpRequestMethod.Post, plan.Method);
        Assert.Equal("https://api.digikey.com/products/v4/search/keyword", plan.Endpoint.ToString());
        Assert.Equal("LM7805CT/NOPB", plan.Query["manufacturerPartNumber"]);
        Assert.Equal("<secret:client_id>", plan.Headers["X-DIGIKEY-Client-Id"]);
        Assert.Equal("<runtime-oauth-token>", plan.Headers["Authorization"]);
        Assert.DoesNotContain("real-client-id-should-not-log", plan.LogSafeSummary);
        Assert.DoesNotContain("real-client-secret-should-not-log", plan.LogSafeSummary);
        Assert.Contains("Digi-Key", plan.LogSafeSummary);
    }

    [Fact]
    public void ProfilesExposeRateLimitMetadataAndExpectedResponseFields()
    {
        var profiles = VendorCatalogRequestPlanner.DefaultProfiles;

        Assert.True(profiles["Digi-Key"].RateLimit.RequestsPerMinute > 0);
        Assert.True(profiles["Mouser"].RateLimit.RequestsPerMinute > 0);
        Assert.Contains("datasheet", profiles["Mouser"].ExpectedResponseFields, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("productUrl", profiles["Adafruit"].ExpectedResponseFields, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("datasheetUrl", profiles["SparkFun"].ExpectedResponseFields, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void MouserRequestPlanBuildsQueryByMpnWithApiKeyPlaceholder()
    {
        var credentials = VendorCredentialBag.FromSecretValues(
            new Dictionary<string, string>
            {
                ["api_key"] = "mouser-secret-key",
            });

        var plan = VendorCatalogRequestPlanner.BuildQueryByManufacturerPartNumber(
            "Mouser",
            "NE555P",
            credentials);

        Assert.Equal(HttpRequestMethod.Post, plan.Method);
        Assert.Equal("https://api.mouser.com/api/v2/search/partnumber", plan.Endpoint.ToString());
        Assert.Equal("NE555P", plan.Query["manufacturerPartNumber"]);
        Assert.Equal("<secret:api_key>", plan.Query["apiKey"]);
        Assert.DoesNotContain("mouser-secret-key", plan.LogSafeSummary);
    }

    [Fact]
    public void JamecoRequestPlanUsesManualFallbackWarningInsteadOfLiveApi()
    {
        var plan = VendorCatalogRequestPlanner.BuildQueryByManufacturerPartNumber(
            "Jameco",
            "7805",
            VendorCredentialBag.Empty);

        Assert.Equal(HttpRequestMethod.Get, plan.Method);
        Assert.Equal(VendorCatalogAccessMode.ManualCatalogFeedFallback, plan.AccessMode);
        Assert.Contains("manual catalog feed", plan.Warnings.Single(), StringComparison.OrdinalIgnoreCase);
        Assert.True(plan.RateLimit.RequiresManualRefresh);
    }
}
