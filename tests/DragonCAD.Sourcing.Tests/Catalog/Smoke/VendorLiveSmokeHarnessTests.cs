using DragonCAD.Sourcing.Catalog;
using DragonCAD.Sourcing.Catalog.Smoke;
using DragonCAD.Sourcing.Catalog.Sync;

namespace DragonCAD.Sourcing.Tests.Catalog.Smoke;

public sealed class VendorLiveSmokeHarnessTests
{
    [Fact]
    public void IsEnabledReturnsFalseWhenGateIsMissing()
    {
        var enabled = VendorLiveSmokeHarness.IsEnabled(_ => null);

        Assert.False(enabled);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("true")]
    [InlineData("TRUE")]
    public void IsEnabledReturnsTrueWhenGateIsExplicitlyEnabled(string value)
    {
        var enabled = VendorLiveSmokeHarness.IsEnabled(name =>
            name == VendorLiveSmokeHarness.GateEnvironmentVariable ? value : null);

        Assert.True(enabled);
    }

    [Fact]
    public async Task RunDigiKeyKeywordSearchAsyncDoesNotCreateHttpClientsWhenGateIsDisabled()
    {
        var harness = new VendorLiveSmokeHarness(
            _ => null,
            _ => throw new InvalidOperationException("HTTP client should not be created when live smoke is disabled."));

        var result = await harness.RunDigiKeyKeywordSearchAsync("LM7805", limit: 1, CancellationToken.None);

        Assert.Equal(VendorLiveSmokeRunStatus.Disabled, result.Status);
        Assert.Equal("Digi-Key", result.ProviderName);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public async Task RunMouserKeywordSearchAsyncDoesNotCreateHttpClientsWhenGateIsDisabled()
    {
        var harness = new VendorLiveSmokeHarness(
            _ => null,
            _ => throw new InvalidOperationException("HTTP client should not be created when live smoke is disabled."));

        var result = await harness.RunMouserKeywordSearchAsync("LM7805", limit: 1, CancellationToken.None);

        Assert.Equal(VendorLiveSmokeRunStatus.Disabled, result.Status);
        Assert.Equal("Mouser", result.ProviderName);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void PlanProviderChecksIsDisabledByDefault()
    {
        var harness = new VendorLiveSmokeHarness(
            _ => null,
            _ => throw new InvalidOperationException("HTTP client should not be created when planning."));

        var plan = harness.PlanProviderChecks("LM7805", limit: 2);

        Assert.Equal(VendorLiveSmokeMode.Disabled, plan.Mode);
        Assert.All(plan.ProviderChecks, check => Assert.Equal(VendorLiveSmokeProviderStatus.Disabled, check.Status));
        Assert.All(plan.ProviderChecks, check => Assert.Equal("Set DRAGONCAD_VENDOR_LIVE_SMOKE to true to enable provider smoke planning.", check.Diagnostics.Single()));
    }

    [Fact]
    public void DryRunPlansProviderChecksWithoutCreatingHttpClients()
    {
        var harness = new VendorLiveSmokeHarness(
            name => name switch
            {
                VendorLiveSmokeHarness.GateEnvironmentVariable => "true",
                VendorLiveSmokeHarness.ModeEnvironmentVariable => "dry-run",
                "DRAGONCAD_DIGIKEY_CLIENT_ID" => "configured-client",
                "DRAGONCAD_DIGIKEY_CLIENT_SECRET" => "configured-secret",
                "DRAGONCAD_MOUSER_API_KEY" => "configured-key",
                _ => null,
            },
            _ => throw new InvalidOperationException("HTTP client should not be created for dry-run planning."));

        var plan = harness.PlanProviderChecks(" LM7805 ", limit: 3);

        Assert.Equal(VendorLiveSmokeMode.DryRun, plan.Mode);
        Assert.Collection(
            plan.ProviderChecks,
            check =>
            {
                Assert.Equal("Digi-Key", check.ProviderName);
                Assert.Equal(VendorLiveSmokeProviderStatus.Planned, check.Status);
                Assert.Equal("LM7805", check.Query);
                Assert.Equal(3, check.Limit);
                Assert.Contains("client_id", check.CredentialSummary);
                Assert.DoesNotContain("configured-client", check.CredentialSummary, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("configured-secret", check.CredentialSummary, StringComparison.OrdinalIgnoreCase);
            },
            check =>
            {
                Assert.Equal("Mouser", check.ProviderName);
                Assert.Equal(VendorLiveSmokeProviderStatus.Planned, check.Status);
                Assert.Equal("LM7805", check.Query);
                Assert.Equal(3, check.Limit);
                Assert.Contains("api_key", check.CredentialSummary);
                Assert.DoesNotContain("configured-key", check.CredentialSummary, StringComparison.OrdinalIgnoreCase);
            });
    }

    [Fact]
    public void LivePlanReportsMissingCredentialsWithoutCreatingHttpClients()
    {
        var harness = new VendorLiveSmokeHarness(
            name => name switch
            {
                VendorLiveSmokeHarness.GateEnvironmentVariable => "true",
                VendorLiveSmokeHarness.ModeEnvironmentVariable => "live",
                "DRAGONCAD_DIGIKEY_CLIENT_ID" => "configured-client",
                _ => null,
            },
            _ => throw new InvalidOperationException("HTTP client should not be created when credentials are missing."));

        var plan = harness.PlanProviderChecks("LM7805", limit: 1);

        Assert.Equal(VendorLiveSmokeMode.Live, plan.Mode);
        Assert.Collection(
            plan.ProviderChecks,
            check =>
            {
                Assert.Equal("Digi-Key", check.ProviderName);
                Assert.Equal(VendorLiveSmokeProviderStatus.MissingCredentials, check.Status);
                Assert.Contains("client_secret", check.Diagnostics.Single());
                Assert.DoesNotContain("DRAGONCAD_DIGIKEY_CLIENT_SECRET", check.Diagnostics.Single(), StringComparison.Ordinal);
            },
            check =>
            {
                Assert.Equal("Mouser", check.ProviderName);
                Assert.Equal(VendorLiveSmokeProviderStatus.MissingCredentials, check.Status);
                Assert.Contains("api_key", check.Diagnostics.Single());
                Assert.DoesNotContain("DRAGONCAD_MOUSER_API_KEY", check.Diagnostics.Single(), StringComparison.Ordinal);
            });
    }

    [Fact]
    public async Task RunProviderCheckAsyncReturnsDeterministicRedactedResult()
    {
        var harness = new VendorLiveSmokeHarness(
            name => name switch
            {
                VendorLiveSmokeHarness.GateEnvironmentVariable => "true",
                VendorLiveSmokeHarness.ModeEnvironmentVariable => "live",
                "DRAGONCAD_DIGIKEY_CLIENT_ID" => "arbitrary-client-123",
                "DRAGONCAD_DIGIKEY_CLIENT_SECRET" => "arbitrary-secret-456",
                "DRAGONCAD_MOUSER_API_KEY" => "arbitrary-key-789",
                _ => null,
            },
            _ => throw new InvalidOperationException("HTTP client should not be created by fake provider test."));
        var provider = new FakeSearchProvider(
            "Digi-Key",
            new CatalogImportResult(
                [],
                [new CatalogImportDiagnostic(CatalogDiagnosticSeverity.Error, "rate-limit", "Token arbitrary-secret-456 hit retry-after for DRAGONCAD_DIGIKEY_CLIENT_SECRET.", "Digi-Key")]));
        var plan = harness.PlanProviderChecks("LM7805", limit: 1);
        var check = plan.ProviderChecks.Single(item => item.ProviderName == "Digi-Key");

        Assert.Contains("arbitrary-secret-456", check.RedactionTerms);

        var result = await harness.RunProviderCheckAsync(
            check,
            provider,
            elapsedTime: _ => TimeSpan.FromMilliseconds(42),
            requestId: () => "req-fixed",
            CancellationToken.None);

        Assert.Equal("Digi-Key", result.ProviderName);
        Assert.Equal("req-fixed", result.RequestId);
        Assert.Equal(VendorLiveSmokeRunStatus.RateLimited, result.Status);
        Assert.Equal(TimeSpan.FromMilliseconds(42), result.Elapsed);
        Assert.Contains("Token [redacted] hit retry-after for [redacted].", result.SanitizedDiagnostics);
        Assert.DoesNotContain("arbitrary-secret-456", result.ToDeterministicReport(), StringComparison.Ordinal);
        Assert.DoesNotContain("DRAGONCAD_DIGIKEY_CLIENT_SECRET", result.ToDeterministicReport(), StringComparison.Ordinal);
    }

    private sealed class FakeSearchProvider : IVendorCatalogSearchProvider
    {
        private readonly CatalogImportResult result;

        public FakeSearchProvider(string providerName, CatalogImportResult result)
        {
            ProviderName = providerName;
            this.result = result;
        }

        public string ProviderName { get; }

        public Task<CatalogImportResult> SearchAsync(string query, int limit, CancellationToken cancellationToken) =>
            Task.FromResult(result);
    }
}
