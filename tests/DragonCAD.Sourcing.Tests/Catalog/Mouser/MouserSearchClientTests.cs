using System.Net;
using System.Text;
using DragonCAD.Sourcing.Catalog.Mouser;

namespace DragonCAD.Sourcing.Tests.Catalog.Mouser;

public sealed class MouserSearchClientTests
{
    [Fact]
    public async Task SearchByPartNumberPostsJsonRequestWithApiKeyAndMapsListings()
    {
        using var handler = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                  "SearchResults": {
                    "NumberOfResult": 1,
                    "Parts": [
                      {
                        "MouserPartNumber": "595-LM7805CT",
                        "ManufacturerPartNumber": "LM7805CT/NOPB",
                        "Manufacturer": "Texas Instruments",
                        "Description": "Linear Voltage Regulators 5V",
                        "AvailabilityInStock": "1,234 In Stock",
                        "DataSheetUrl": "https://example.test/lm7805.pdf",
                        "ProductDetailUrl": "https://www.mouser.com/ProductDetail/595-LM7805CT",
                        "Category": "Power Management ICs",
                        "PriceBreaks": [
                          { "Quantity": 1, "Price": "$0.72", "Currency": "USD" },
                          { "Quantity": 10, "Price": "$0.51", "Currency": "USD" }
                        ]
                      }
                    ]
                  }
                }
                """,
                Encoding.UTF8,
                "application/json"),
        });
        using var httpClient = new HttpClient(handler);
        var client = new MouserSearchClient(
            httpClient,
            new MouserSearchClientOptions("mouser-key"));

        var result = await client.SearchByPartNumberAsync("LM7805", limit: 5, CancellationToken.None);

        Assert.Empty(result.Diagnostics);
        var listing = Assert.Single(result.Listings);
        Assert.Equal("Mouser", listing.ProviderName);
        Assert.Equal("595-LM7805CT", listing.VendorSku);
        Assert.Equal("LM7805CT/NOPB", listing.ManufacturerPartNumber);
        Assert.Equal("Texas Instruments", listing.Manufacturer);
        Assert.Equal(1234, listing.StockQuantity);
        Assert.Equal("Power Management ICs", listing.Fields["Category"]);
        Assert.Equal(Money.Usd(0.51m), listing.PriceLadder.FindBestBreakFor(25).UnitPrice);

        Assert.Equal(HttpMethod.Post, handler.Requests.Single().Method);
        Assert.Equal("https://api.mouser.com/api/v2/search/partnumber?apiKey=mouser-key", handler.Requests.Single().RequestUri?.ToString());
        Assert.Contains("\"mouserPartNumber\":\"LM7805\"", handler.RequestBodies.Single(), StringComparison.Ordinal);
        Assert.Contains("\"records\":5", handler.RequestBodies.Single(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task SearchByPartNumberReturnsDiagnosticWhenApiKeyIsMissing()
    {
        using var handler = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
        using var httpClient = new HttpClient(handler);
        var client = new MouserSearchClient(httpClient, new MouserSearchClientOptions(""));

        var result = await client.SearchByPartNumberAsync("LM7805", limit: 5, CancellationToken.None);

        Assert.Empty(result.Listings);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(MouserCatalogDiagnosticCodes.MissingCredentials, diagnostic.Code);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task SearchByPartNumberReportsHttpFailureWithoutLeakingApiKey()
    {
        using var handler = new RecordingHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = new StringContent("bad key", Encoding.UTF8, "text/plain"),
        });
        using var httpClient = new HttpClient(handler);
        var client = new MouserSearchClient(httpClient, new MouserSearchClientOptions("mouser-secret"));

        var result = await client.SearchByPartNumberAsync("LM7805", limit: 5, CancellationToken.None);

        Assert.Empty(result.Listings);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(MouserCatalogDiagnosticCodes.HttpFailure, diagnostic.Code);
        Assert.Contains("403", diagnostic.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("mouser-secret", diagnostic.Message, StringComparison.Ordinal);
    }
}
