namespace DragonCAD.Sourcing.Credentials;

public interface IProviderCredentialStore
{
    ValueTask SetAsync(
        ProviderCredentialSecret credential,
        CancellationToken cancellationToken);

    ValueTask<ProviderCredentialValue?> GetAsync(
        string providerName,
        string keyName,
        CancellationToken cancellationToken);

    ValueTask<bool> DeleteAsync(
        string providerName,
        string keyName,
        CancellationToken cancellationToken);

    ValueTask<ProviderCredentialMetadata> GetStatusAsync(
        string providerName,
        string keyName,
        CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<ProviderCredentialMetadata>> ListAsync(
        string providerName,
        CancellationToken cancellationToken);
}
