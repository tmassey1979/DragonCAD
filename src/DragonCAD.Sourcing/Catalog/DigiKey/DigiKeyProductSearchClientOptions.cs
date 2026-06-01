namespace DragonCAD.Sourcing.Catalog.DigiKey;

public sealed record DigiKeyProductSearchClientOptions(
    string ClientId,
    string AccessToken,
    string LocaleSite = "US",
    string LocaleLanguage = "en",
    string LocaleCurrency = "USD",
    Uri? Endpoint = null)
{
    public Uri EffectiveEndpoint => Endpoint ?? new Uri("https://api.digikey.com/products/v4/search/keyword");

    public static DigiKeyProductSearchClientOptions FromOAuthToken(
        string clientId,
        DigiKeyOAuthToken token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentNullException.ThrowIfNull(token);

        return new DigiKeyProductSearchClientOptions(clientId, token.AccessToken);
    }
}
