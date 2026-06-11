namespace DragonCAD.Sourcing.Credentials;

public sealed record ProviderCredentialSecret(
    string ProviderName,
    string KeyName,
    ProviderCredentialKind Kind,
    string SecretValue,
    ProviderCredentialStorageLocation StorageLocation,
    string? StorageReferenceName,
    DateTimeOffset? LastValidatedAt);
