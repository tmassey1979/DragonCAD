namespace DragonCAD.Sourcing.Credentials;

public sealed record ProviderCredentialMetadata(
    string ProviderName,
    string KeyName,
    ProviderCredentialStorageLocation StorageLocation,
    string? StorageReferenceName,
    ProviderCredentialState State,
    DateTimeOffset? LastValidatedAt)
{
    public string RedactedDisplay
    {
        get
        {
            var validationStatus = LastValidatedAt is null
                ? "not validated"
                : $"last validated {LastValidatedAt:O}";

            return $"{ProviderName} {KeyName}: {State} via {StorageLocation} (<redacted>, {validationStatus})";
        }
    }
}
