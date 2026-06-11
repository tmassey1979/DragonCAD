namespace DragonCAD.Sourcing.Credentials;

public sealed record ProviderCredentialValue(
    string ProviderName,
    string KeyName,
    ProviderCredentialKind Kind,
    string SecretValue);
