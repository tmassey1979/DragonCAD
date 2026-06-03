namespace DragonCAD.Sourcing.Credentials;

public interface IProviderCredentialStore
{
    ValueTask<IReadOnlyList<ProviderCredentialMetadata>> ListAsync(
        string providerName,
        CancellationToken cancellationToken);
}
