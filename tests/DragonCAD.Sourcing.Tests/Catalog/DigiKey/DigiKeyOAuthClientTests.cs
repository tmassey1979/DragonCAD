using System.Net;
using System.Text;
using DragonCAD.Sourcing.Catalog.DigiKey;

namespace DragonCAD.Sourcing.Tests.Catalog.DigiKey;

public sealed class DigiKeyOAuthClientTests
{
    [Fact]
    public async Task RequestClientCredentialsTokenPostsFormWithoutLeakingSecret()
    {
        using var handler = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                  "access_token": "access-token-123",
                  "token_type": "Bearer",
                  "expires_in": 3600
                }
                """,
                Encoding.UTF8,
                "application/json"),
        });
        using var httpClient = new HttpClient(handler);
        var client = new DigiKeyOAuthClient(
            httpClient,
            new DigiKeyOAuthClientOptions("client-id-123", "client-secret-abc"));

        var result = await client.RequestClientCredentialsTokenAsync(CancellationToken.None);

        Assert.Empty(result.Diagnostics);
        Assert.NotNull(result.Token);
        Assert.Equal("access-token-123", result.Token.AccessToken);
        Assert.Equal("Bearer", result.Token.TokenType);
        Assert.Equal(3600, result.Token.ExpiresInSeconds);
        Assert.Equal("https://api.digikey.com/v1/oauth2/token", handler.Requests.Single().RequestUri?.ToString());
        Assert.Equal("application/x-www-form-urlencoded", handler.Requests.Single().Content?.Headers.ContentType?.MediaType);
        Assert.Contains("grant_type=client_credentials", handler.RequestBodies.Single(), StringComparison.Ordinal);
        Assert.Contains("client_id=client-id-123", handler.RequestBodies.Single(), StringComparison.Ordinal);
        Assert.Contains("client_secret=client-secret-abc", handler.RequestBodies.Single(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RequestClientCredentialsTokenReturnsDiagnosticWhenClientSecretIsMissing()
    {
        using var handler = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
        using var httpClient = new HttpClient(handler);
        var client = new DigiKeyOAuthClient(
            httpClient,
            new DigiKeyOAuthClientOptions("client-id-123", ""));

        var result = await client.RequestClientCredentialsTokenAsync(CancellationToken.None);

        Assert.Null(result.Token);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DigiKeyCatalogDiagnosticCodes.MissingCredentials, diagnostic.Code);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task RequestClientCredentialsTokenReportsHttpFailureWithoutLeakingSecret()
    {
        using var handler = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("invalid secret", Encoding.UTF8, "text/plain"),
        });
        using var httpClient = new HttpClient(handler);
        var client = new DigiKeyOAuthClient(
            httpClient,
            new DigiKeyOAuthClientOptions("client-id-123", "client-secret-abc"));

        var result = await client.RequestClientCredentialsTokenAsync(CancellationToken.None);

        Assert.Null(result.Token);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DigiKeyCatalogDiagnosticCodes.HttpFailure, diagnostic.Code);
        Assert.Contains("400", diagnostic.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("client-id-123", diagnostic.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("client-secret-abc", diagnostic.Message, StringComparison.Ordinal);
    }
}
