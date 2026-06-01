using DragonCAD.Sourcing.Credentials;

namespace DragonCAD.Sourcing.Catalog.DigiKey;

public sealed record DigiKeyOAuthClientOptions(
    string ClientId,
    string ClientSecret,
    Uri? TokenEndpoint = null)
{
    public Uri EffectiveTokenEndpoint => TokenEndpoint ?? new Uri("https://api.digikey.com/v1/oauth2/token");

    public static DigiKeyOAuthClientOptions FromEnvironment(Func<string, string?>? readEnvironment = null)
    {
        readEnvironment ??= DragonCadCredentialEnvironment.Get;

        return new DigiKeyOAuthClientOptions(
            readEnvironment("DRAGONCAD_DIGIKEY_CLIENT_ID") ?? string.Empty,
            readEnvironment("DRAGONCAD_DIGIKEY_CLIENT_SECRET") ?? string.Empty);
    }
}
