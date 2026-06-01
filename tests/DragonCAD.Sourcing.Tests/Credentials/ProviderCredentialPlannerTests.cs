using DragonCAD.Sourcing.Credentials;

namespace DragonCAD.Sourcing.Tests.Credentials;

public sealed class ProviderCredentialPlannerTests
{
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
        Assert.DoesNotContain("visible-secret-value", diagnostic.LogSafeMessage);
        Assert.DoesNotContain("visible-secret-value", plan.LogSafeSummary);
    }

    private static ProviderCredentialMetadata Configured(
        string providerName,
        string keyName,
        string secretValue) =>
        new(
            providerName,
            keyName,
            ProviderCredentialStorageLocation.OSCredentialVault,
            secretValue,
            ProviderCredentialState.Configured,
            LastValidatedAt: new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero));
}
