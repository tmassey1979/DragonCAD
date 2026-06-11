namespace DragonCAD.Sourcing.Credentials;

public sealed class InMemoryProviderCredentialStore : IProviderCredentialStore
{
    private readonly Dictionary<ProviderCredentialStoreKey, StoredCredential> credentials = new();

    public ValueTask SetAsync(
        ProviderCredentialSecret credential,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(credential);
        cancellationToken.ThrowIfCancellationRequested();

        var key = CreateKey(credential.ProviderName, credential.KeyName);
        credentials[key] = new StoredCredential(
            credential.ProviderName,
            credential.KeyName,
            credential.Kind,
            credential.SecretValue,
            credential.StorageLocation,
            credential.StorageReferenceName,
            credential.LastValidatedAt);

        return ValueTask.CompletedTask;
    }

    public ValueTask<ProviderCredentialValue?> GetAsync(
        string providerName,
        string keyName,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = CreateKey(providerName, keyName);

        if (!credentials.TryGetValue(key, out var credential))
        {
            return ValueTask.FromResult<ProviderCredentialValue?>(null);
        }

        return ValueTask.FromResult<ProviderCredentialValue?>(
            new ProviderCredentialValue(
                credential.ProviderName,
                credential.KeyName,
                credential.Kind,
                credential.SecretValue));
    }

    public ValueTask<bool> DeleteAsync(
        string providerName,
        string keyName,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = CreateKey(providerName, keyName);

        return ValueTask.FromResult(credentials.Remove(key));
    }

    public ValueTask<ProviderCredentialMetadata> GetStatusAsync(
        string providerName,
        string keyName,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = CreateKey(providerName, keyName);

        if (!credentials.TryGetValue(key, out var credential))
        {
            return ValueTask.FromResult(
                new ProviderCredentialMetadata(
                    providerName,
                    keyName,
                    ProviderCredentialKind.Unknown,
                    ProviderCredentialStorageLocation.ManualSessionOnly,
                    StorageReferenceName: null,
                    ProviderCredentialState.Missing,
                    LastValidatedAt: null));
        }

        return ValueTask.FromResult(ToMetadata(credential));
    }

    public ValueTask<IReadOnlyList<ProviderCredentialMetadata>> ListAsync(
        string providerName,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);

        var matches = credentials.Values
            .Where(credential => credential.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(credential => credential.KeyName, StringComparer.OrdinalIgnoreCase)
            .Select(ToMetadata)
            .ToArray();

        return ValueTask.FromResult<IReadOnlyList<ProviderCredentialMetadata>>(matches);
    }

    private static ProviderCredentialStoreKey CreateKey(string providerName, string keyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(keyName);

        return new ProviderCredentialStoreKey(providerName, keyName);
    }

    private static ProviderCredentialMetadata ToMetadata(StoredCredential credential) =>
        new(
            credential.ProviderName,
            credential.KeyName,
            credential.Kind,
            credential.StorageLocation,
            credential.StorageReferenceName,
            ProviderCredentialState.Configured,
            credential.LastValidatedAt);

    private readonly struct ProviderCredentialStoreKey : IEquatable<ProviderCredentialStoreKey>
    {
        public ProviderCredentialStoreKey(string providerName, string keyName)
        {
            ProviderName = providerName;
            KeyName = keyName;
        }

        public string ProviderName { get; }

        public string KeyName { get; }

        public bool Equals(ProviderCredentialStoreKey other) =>
            ProviderName.Equals(other.ProviderName, StringComparison.OrdinalIgnoreCase) &&
            KeyName.Equals(other.KeyName, StringComparison.OrdinalIgnoreCase);

        public override bool Equals(object? obj) =>
            obj is ProviderCredentialStoreKey other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(ProviderName),
                StringComparer.OrdinalIgnoreCase.GetHashCode(KeyName));
    }

    private sealed record StoredCredential(
        string ProviderName,
        string KeyName,
        ProviderCredentialKind Kind,
        string SecretValue,
        ProviderCredentialStorageLocation StorageLocation,
        string? StorageReferenceName,
        DateTimeOffset? LastValidatedAt);
}
