using DragonCAD.Sourcing.Credentials;

namespace DragonCAD.Sourcing.Catalog.Mouser;

public sealed record MouserSearchClientOptions(
    string ApiKey,
    Uri? PartNumberEndpoint = null,
    Uri? KeywordEndpoint = null)
{
    public Uri EffectivePartNumberEndpoint => PartNumberEndpoint ?? new Uri("https://api.mouser.com/api/v2/search/partnumber");

    public Uri EffectiveKeywordEndpoint => KeywordEndpoint ?? new Uri("https://api.mouser.com/api/v2/search/keyword");

    public static MouserSearchClientOptions FromEnvironment(Func<string, string?>? readEnvironment = null)
    {
        readEnvironment ??= DragonCadCredentialEnvironment.Get;

        return new MouserSearchClientOptions(
            readEnvironment("DRAGONCAD_MOUSER_API_KEY") ?? string.Empty);
    }
}
