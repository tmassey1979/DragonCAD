using System.Net;
using System.Text;
using DragonCAD.Sourcing.Catalog.DigiKey;

namespace DragonCAD.Sourcing.Tests.Catalog.DigiKey;

public sealed class DigiKeyProductSearchClientTests
{
    [Fact]
    public async Task SearchByKeywordPostsV4RequestWithOAuthHeadersAndMapsListings()
    {
        using var handler = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
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
                      "DatasheetUrl": "https://example.test/lm7805.pdf",
                      "ProductUrl": "https://www.digikey.com/en/products/detail/texas-instruments/LM7805CT-NOPB/12345",
                      "QuantityAvailable": 1234,
                      "StandardPricing": [
                        { "BreakQuantity": 1, "UnitPrice": 0.72 },
                        { "BreakQuantity": 10, "UnitPrice": 0.51 }
                      ],
                      "ProductVariations": [
                        {
                          "DigiKeyProductNumber": "296-12345-1-ND",
                          "PackageType": { "Name": "Tube" },
                          "MinimumOrderQuantity": 1
                        }
                      ]
                    }
                  ]
                }
                """,
                Encoding.UTF8,
                "application/json"),
        });
        using var httpClient = new HttpClient(handler);
        var client = new DigiKeyProductSearchClient(
            httpClient,
            new DigiKeyProductSearchClientOptions("client-id-123", "token-abc"));

        var result = await client.SearchByKeywordAsync("LM7805", limit: 5, CancellationToken.None);

        Assert.Empty(result.Diagnostics);
        var listing = Assert.Single(result.Listings);
        Assert.Equal("Digi-Key", listing.ProviderName);
        Assert.Equal("296-12345-1-ND", listing.VendorSku);
        Assert.Equal("LM7805CT/NOPB", listing.ManufacturerPartNumber);
        Assert.Equal("Texas Instruments", listing.Manufacturer);
        Assert.Equal(1234, listing.StockQuantity);
        Assert.Equal("Tube", listing.Fields["PackageType"]);
        Assert.Equal(Money.Usd(0.51m), listing.PriceLadder.FindBestBreakFor(25).UnitPrice);

        Assert.Equal(HttpMethod.Post, handler.Requests.Single().Method);
        Assert.Equal("https://api.digikey.com/products/v4/search/keyword", handler.Requests.Single().RequestUri?.ToString());
        Assert.Equal("client-id-123", handler.Requests.Single().Headers.GetValues("X-DIGIKEY-Client-Id").Single());
        Assert.Equal("Bearer token-abc", handler.Requests.Single().Headers.Authorization?.ToString());
        Assert.Contains("\"Keywords\":\"LM7805\"", handler.RequestBodies.Single(), StringComparison.Ordinal);
        Assert.Contains("\"Limit\":5", handler.RequestBodies.Single(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task SearchByKeywordReturnsDiagnosticWhenCredentialsAreMissing()
    {
        using var handler = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
        using var httpClient = new HttpClient(handler);
        var client = new DigiKeyProductSearchClient(
            httpClient,
            new DigiKeyProductSearchClientOptions("", ""));

        var result = await client.SearchByKeywordAsync("LM7805", limit: 5, CancellationToken.None);

        Assert.Empty(result.Listings);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DigiKeyCatalogDiagnosticCodes.MissingCredentials, diagnostic.Code);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task SearchByKeywordReportsHttpFailureWithoutLeakingCredentials()
    {
        using var handler = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("invalid token", Encoding.UTF8, "text/plain"),
        });
        using var httpClient = new HttpClient(handler);
        var client = new DigiKeyProductSearchClient(
            httpClient,
            new DigiKeyProductSearchClientOptions("client-id-123", "secret-token"));

        var result = await client.SearchByKeywordAsync("LM7805", limit: 5, CancellationToken.None);

        Assert.Empty(result.Listings);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DigiKeyCatalogDiagnosticCodes.HttpFailure, diagnostic.Code);
        Assert.Contains("401", diagnostic.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("secret-token", diagnostic.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("client-id-123", diagnostic.Message, StringComparison.Ordinal);
    }
}
