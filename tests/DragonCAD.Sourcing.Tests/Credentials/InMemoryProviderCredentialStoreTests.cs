using System.Text.Json;
using DragonCAD.Sourcing.Credentials;

namespace DragonCAD.Sourcing.Tests.Credentials;

public sealed class InMemoryProviderCredentialStoreTests
{
    private static readonly DateTimeOffset ValidatedAt = new(2026, 6, 10, 13, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task SetGetStatusAndDeleteAreScopedByProviderAndKey()
    {
        var store = new InMemoryProviderCredentialStore();

        await store.SetAsync(
            new ProviderCredentialSecret(
                "Digi-Key",
                "client_secret",
                ProviderCredentialKind.ClientSecret,
                "test-only-digikey-client-secret",
                ProviderCredentialStorageLocation.ManualSessionOnly,
                "digikey-session-reference",
                ValidatedAt),
            CancellationToken.None);
        await store.SetAsync(
            new ProviderCredentialSecret(
                "Mouser",
                "client_secret",
                ProviderCredentialKind.ApiKey,
                "test-only-mouser-api-key",
                ProviderCredentialStorageLocation.ManualSessionOnly,
                "mouser-session-reference",
                ValidatedAt),
            CancellationToken.None);

        var digikeyValue = await store.GetAsync("Digi-Key", "client_secret", CancellationToken.None);
        var mouserValue = await store.GetAsync("Mouser", "client_secret", CancellationToken.None);
        var digikeyStatus = await store.GetStatusAsync("Digi-Key", "client_secret", CancellationToken.None);

        Assert.NotNull(digikeyValue);
        Assert.NotNull(mouserValue);
        Assert.Equal("test-only-digikey-client-secret", digikeyValue.SecretValue);
        Assert.Equal("test-only-mouser-api-key", mouserValue.SecretValue);
        Assert.Equal(ProviderCredentialKind.ClientSecret, digikeyStatus.Kind);
        Assert.Equal(ProviderCredentialState.Configured, digikeyStatus.State);
        Assert.DoesNotContain("test-only-digikey-client-secret", digikeyStatus.RedactedDisplay, StringComparison.Ordinal);

        Assert.True(await store.DeleteAsync("Digi-Key", "client_secret", CancellationToken.None));
        Assert.Null(await store.GetAsync("Digi-Key", "client_secret", CancellationToken.None));
        Assert.NotNull(await store.GetAsync("Mouser", "client_secret", CancellationToken.None));
        Assert.False(await store.DeleteAsync("Digi-Key", "client_secret", CancellationToken.None));
    }

    [Fact]
    public async Task MissingStatusReturnsRedactedDiagnosticMetadata()
    {
        var store = new InMemoryProviderCredentialStore();

        var status = await store.GetStatusAsync("Digi-Key", "client_secret", CancellationToken.None);

        Assert.Equal("Digi-Key", status.ProviderName);
        Assert.Equal("client_secret", status.KeyName);
        Assert.Equal(ProviderCredentialKind.Unknown, status.Kind);
        Assert.Equal(ProviderCredentialState.Missing, status.State);
        Assert.Contains("<redacted>", status.RedactedDisplay, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PlannerProjectRecordNeverSerializesStoredSecretOrReference()
    {
        var store = new InMemoryProviderCredentialStore();
        await store.SetAsync(
            new ProviderCredentialSecret(
                "Mouser",
                "api_key",
                ProviderCredentialKind.ApiKey,
                "test-only-mouser-secret-value",
                ProviderCredentialStorageLocation.OSCredentialVault,
                "mouser-vault-reference",
                ValidatedAt),
            CancellationToken.None);

        var plan = ProviderCredentialPlanner.Plan(
            ProviderCredentialRequirement.KnownProviders["Mouser"],
            await store.ListAsync("Mouser", CancellationToken.None));
        var json = JsonSerializer.Serialize(plan.ToProjectRecord());

        Assert.Contains("Mouser", json, StringComparison.Ordinal);
        Assert.Contains("api_key", json, StringComparison.Ordinal);
        Assert.Contains("ApiKey", json, StringComparison.Ordinal);
        Assert.DoesNotContain("test-only-mouser-secret-value", json, StringComparison.Ordinal);
        Assert.DoesNotContain("mouser-vault-reference", json, StringComparison.Ordinal);
    }
}
