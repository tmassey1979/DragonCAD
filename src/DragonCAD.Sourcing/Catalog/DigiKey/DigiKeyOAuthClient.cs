using System.Net.Http.Headers;
using System.Text.Json;

namespace DragonCAD.Sourcing.Catalog.DigiKey;

public sealed class DigiKeyOAuthClient : IDigiKeyOAuthTokenSource
{
    private const string ProviderName = "Digi-Key";

    private readonly HttpClient httpClient;
    private readonly DigiKeyOAuthClientOptions options;

    public DigiKeyOAuthClient(HttpClient httpClient, DigiKeyOAuthClientOptions options)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<DigiKeyOAuthTokenResult> RequestClientCredentialsTokenAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.ClientId) || string.IsNullOrWhiteSpace(options.ClientSecret))
        {
            return DiagnosticResult(
                DigiKeyCatalogDiagnosticCodes.MissingCredentials,
                "Digi-Key OAuth credentials are missing. Configure client_id and client_secret before requesting a token.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, options.EffectiveTokenEndpoint)
        {
            Content = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", options.ClientId),
                new KeyValuePair<string, string>("client_secret", options.ClientSecret),
            ]),
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            return DiagnosticResult(
                DigiKeyCatalogDiagnosticCodes.HttpFailure,
                $"Digi-Key OAuth token request failed with HTTP {(int)response.StatusCode} {response.ReasonPhrase}.");
        }

        return ParseToken(body);
    }

    private static DigiKeyOAuthTokenResult ParseToken(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var accessToken = ReadString(root, "access_token");
            var tokenType = ReadString(root, "token_type");
            var expiresIn = ReadInt(root, "expires_in");

            if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(tokenType) || expiresIn is null)
            {
                return DiagnosticResult(
                    DigiKeyCatalogDiagnosticCodes.InvalidTokenResponse,
                    "Digi-Key OAuth token response did not include access_token, token_type, and expires_in.");
            }

            return new DigiKeyOAuthTokenResult(
                new DigiKeyOAuthToken(accessToken, tokenType, expiresIn.Value),
                []);
        }
        catch (JsonException exception)
        {
            return DiagnosticResult(
                DigiKeyCatalogDiagnosticCodes.InvalidJson,
                $"Digi-Key OAuth returned malformed JSON: {exception.Message}");
        }
    }

    private static string ReadString(JsonElement element, string propertyName)
    {
        return element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;
    }

    private static int? ReadInt(JsonElement element, string propertyName)
    {
        return element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(propertyName, out var property) &&
            property.TryGetInt32(out var value)
            ? value
            : null;
    }

    private static DigiKeyOAuthTokenResult DiagnosticResult(string code, string message)
    {
        return new DigiKeyOAuthTokenResult(
            null,
            [new CatalogImportDiagnostic(CatalogDiagnosticSeverity.Error, code, message, ProviderName, null)]);
    }
}
