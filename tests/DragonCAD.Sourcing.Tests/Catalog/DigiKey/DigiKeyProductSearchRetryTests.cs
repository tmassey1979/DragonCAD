using System.Net;
using System.Text;
using DragonCAD.Sourcing.Catalog.DigiKey;
using DragonCAD.Sourcing.Catalog.Http;

namespace DragonCAD.Sourcing.Tests.Catalog.DigiKey;

public sealed class DigiKeyProductSearchRetryTests
{
    [Fact]
    public async Task SearchByKeywordRetriesRateLimitResponseThenMapsSuccessfulListing()
    {
        using var handler = new RecordingHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.TooManyRequests)
            {
                Content = new StringContent("rate limited", Encoding.UTF8, "text/plain"),
            },
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "Products": [
                        {
                          "DigiKeyProductNumber": "296-12345-1-ND",
                          "ManufacturerProductNumber": "LM7805CT/NOPB",
                          "Description": { "ProductDescription": "5V linear regulator" },
                          "Manufacturer": { "Name": "Texas Instruments" },
                          "QuantityAvailable": 1234,
                          "StandardPricing": [
                            { "BreakQuantity": 1, "UnitPrice": 0.72 }
                          ]
                        }
                      ]
                    }
                    """,
                    Encoding.UTF8,
                    "application/json"),
            });
        var delay = new RecordingVendorHttpDelay();
        using var httpClient = new HttpClient(handler);
        var client = new DigiKeyProductSearchClient(
            httpClient,
            new DigiKeyProductSearchClientOptions("client-id-123", "token-abc"),
            new VendorHttpRetryPolicy(delay: delay));

        var result = await client.SearchByKeywordAsync("LM7805", limit: 5, CancellationToken.None);

        Assert.Empty(result.Diagnostics);
        Assert.Single(result.Listings);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Single(delay.Delays);
    }

    [Fact]
    public async Task SearchByKeywordDoesNotRetryUnauthorizedCredentialFailure()
    {
        using var handler = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("invalid token", Encoding.UTF8, "text/plain"),
        });
        var delay = new RecordingVendorHttpDelay();
        using var httpClient = new HttpClient(handler);
        var client = new DigiKeyProductSearchClient(
            httpClient,
            new DigiKeyProductSearchClientOptions("client-id-123", "secret-token"),
            new VendorHttpRetryPolicy(delay: delay));

        var result = await client.SearchByKeywordAsync("LM7805", limit: 5, CancellationToken.None);

        Assert.Empty(result.Listings);
        Assert.Equal(DigiKeyCatalogDiagnosticCodes.HttpFailure, Assert.Single(result.Diagnostics).Code);
        Assert.Single(handler.Requests);
        Assert.Empty(delay.Delays);
    }
}
