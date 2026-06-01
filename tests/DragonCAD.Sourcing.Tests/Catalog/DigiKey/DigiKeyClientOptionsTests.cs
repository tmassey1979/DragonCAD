using DragonCAD.Sourcing.Catalog.DigiKey;

namespace DragonCAD.Sourcing.Tests.Catalog.DigiKey;

public sealed class DigiKeyClientOptionsTests
{
    [Fact]
    public void OAuthOptionsCanBeLoadedFromDragonCadEnvironmentNames()
    {
        var options = DigiKeyOAuthClientOptions.FromEnvironment(
            name => name switch
            {
                "DRAGONCAD_DIGIKEY_CLIENT_ID" => "client-id-123",
                "DRAGONCAD_DIGIKEY_CLIENT_SECRET" => "client-secret-abc",
                _ => null,
            });

        Assert.Equal("client-id-123", options.ClientId);
        Assert.Equal("client-secret-abc", options.ClientSecret);
    }

    [Fact]
    public void ProductSearchOptionsCanBeCreatedFromOAuthToken()
    {
        var token = new DigiKeyOAuthToken("access-token-123", "Bearer", 3600);

        var options = DigiKeyProductSearchClientOptions.FromOAuthToken("client-id-123", token);

        Assert.Equal("client-id-123", options.ClientId);
        Assert.Equal("access-token-123", options.AccessToken);
    }
}
