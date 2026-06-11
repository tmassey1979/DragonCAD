using System.Text.Json;
using DragonCAD.Sourcing.Credentials;

namespace DragonCAD.Sourcing.Tests.Credentials;

public sealed class ProviderCredentialPlannerTests
{
    [Fact]
    public void KnownProvidersIncludeVendorAndFabricationCredentialBoundaries()
    {
        Assert.Equal(
            ["Digi-Key", "Mouser", "Adafruit", "SparkFun", "Jameco", "OSH Park", "PCBCart"],
            ProviderCredentialRequirement.KnownProviders.Keys);
    }

    [Fact]
    public void DigiKeyRequiresClientIdAndClientSecret()
    {
        var plan = ProviderCredentialPlanner.Plan(
            ProviderCredentialRequirement.KnownProviders["Digi-Key"],
            [
                Configured("Digi-Key", "client_id", "dk-client-secret"),
                Configured("Digi-Key", "client_secret", "dk-client-secret-value"),
            ]);

        Assert.True(plan.IsReady);
        Assert.Equal(["client_id", "client_secret"], plan.Credentials.Select(credential => credential.KeyName));
        Assert.All(plan.Credentials, credential => Assert.Equal(ProviderCredentialState.Configured, credential.State));
        Assert.DoesNotContain("dk-client-secret", plan.LogSafeSummary);
        Assert.DoesNotContain("dk-client-secret-value", plan.LogSafeSummary);
    }

    [Fact]
    public void MouserRequiresApiKey()
    {
        var plan = ProviderCredentialPlanner.Plan(
            ProviderCredentialRequirement.KnownProviders["Mouser"],
            [Configured("Mouser", "api_key", "mouser-real-api-key")]);

        Assert.True(plan.IsReady);
        var credential = Assert.Single(plan.Credentials);
        Assert.Equal("api_key", credential.KeyName);
        Assert.DoesNotContain("mouser-real-api-key", credential.RedactedDisplay);
        Assert.Contains("api_key", credential.RedactedDisplay);
    }

    [Theory]
    [InlineData("Adafruit")]
    [InlineData("SparkFun")]
    [InlineData("Jameco")]
    public void PublicProvidersNeedNoSecret(string providerName)
    {
        var plan = ProviderCredentialPlanner.Plan(
            ProviderCredentialRequirement.KnownProviders[providerName],
            []);

        Assert.True(plan.IsReady);
        Assert.Empty(plan.Credentials);
        Assert.Empty(plan.Diagnostics);
        Assert.Contains("no credentials required", plan.LogSafeSummary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MissingAndPartialCredentialsReportRedactedDiagnostics()
    {
        var plan = ProviderCredentialPlanner.Plan(
            ProviderCredentialRequirement.KnownProviders["Digi-Key"],
            [Configured("Digi-Key", "client_id", "visible-secret-value")]);

        Assert.False(plan.IsReady);
        Assert.Equal(["client_secret"], plan.MissingRequiredKeys);
        var diagnostic = Assert.Single(plan.Diagnostics);
        Assert.Equal("Digi-Key", diagnostic.ProviderName);
        Assert.Equal("client_secret", diagnostic.KeyName);
        Assert.Equal(ProviderCredentialDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("Configure", diagnostic.LogSafeMessage, StringComparison.Ordinal);
        Assert.Contains("outside the project file", diagnostic.LogSafeMessage, StringComparison.Ordinal);
        Assert.DoesNotContain("visible-secret-value", diagnostic.LogSafeMessage);
        Assert.DoesNotContain("visible-secret-value", plan.LogSafeSummary);
    }

    [Fact]
    public void ProjectRecordSerializesProviderChoicesWithoutCredentialReferences()
    {
        var plan = ProviderCredentialPlanner.Plan(
            ProviderCredentialRequirement.KnownProviders["Digi-Key"],
            [
                Configured("Digi-Key", "client_id", "DRAGONCAD_DIGIKEY_CLIENT_ID"),
                Configured("Digi-Key", "client_secret", "real-client-secret-value"),
            ]);

        var json = JsonSerializer.Serialize(plan.ToProjectRecord());

        Assert.Contains("Digi-Key", json, StringComparison.Ordinal);
        Assert.Contains("client_id", json, StringComparison.Ordinal);
        Assert.Contains("client_secret", json, StringComparison.Ordinal);
        Assert.Contains("OSCredentialVault", json, StringComparison.Ordinal);
        Assert.Contains("Configured", json, StringComparison.Ordinal);
        Assert.DoesNotContain("DRAGONCAD_DIGIKEY_CLIENT_ID", json, StringComparison.Ordinal);
        Assert.DoesNotContain("real-client-secret-value", json, StringComparison.Ordinal);
        Assert.DoesNotContain("StorageReferenceName", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StoreBoundaryFeedsPlannerWithoutExposingCredentialValues()
    {
        IProviderCredentialStore store = new InMemoryProviderCredentialStore();
        await store.SetAsync(
            new ProviderCredentialSecret(
                "Mouser",
                "api_key",
                ProviderCredentialKind.ApiKey,
                "test-only-mouser-secret",
                ProviderCredentialStorageLocation.OSCredentialVault,
                "mouser-secret-storage-reference",
                new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero)),
            CancellationToken.None);

        var plan = ProviderCredentialPlanner.Plan(
            ProviderCredentialRequirement.KnownProviders["Mouser"],
            await store.ListAsync("Mouser", CancellationToken.None));

        Assert.True(plan.IsReady);
        Assert.DoesNotContain("test-only-mouser-secret", plan.LogSafeSummary, StringComparison.Ordinal);
        Assert.DoesNotContain("mouser-secret-storage-reference", plan.LogSafeSummary, StringComparison.Ordinal);
        Assert.DoesNotContain("test-only-mouser-secret", JsonSerializer.Serialize(plan.ToProjectRecord()), StringComparison.Ordinal);
        Assert.DoesNotContain("mouser-secret-storage-reference", JsonSerializer.Serialize(plan.ToProjectRecord()), StringComparison.Ordinal);
    }

    private static ProviderCredentialMetadata Configured(
        string providerName,
        string keyName,
        string secretValue) =>
        new(
            providerName,
            keyName,
            keyName switch
            {
                "api_key" => ProviderCredentialKind.ApiKey,
                "client_id" => ProviderCredentialKind.ClientId,
                "client_secret" => ProviderCredentialKind.ClientSecret,
                _ => ProviderCredentialKind.Unknown,
            },
            ProviderCredentialStorageLocation.OSCredentialVault,
            secretValue,
            ProviderCredentialState.Configured,
            LastValidatedAt: new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero));
}
