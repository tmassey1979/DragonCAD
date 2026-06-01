namespace DragonCAD.Sourcing.Catalog.DigiKey;

public interface IDigiKeyOAuthTokenSource
{
    Task<DigiKeyOAuthTokenResult> RequestClientCredentialsTokenAsync(CancellationToken cancellationToken);
}
