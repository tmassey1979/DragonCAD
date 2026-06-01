using System.Net;
using System.Text;
using DragonCAD.Sourcing.Catalog.Http;
using DragonCAD.Sourcing.Catalog.Mouser;

namespace DragonCAD.Sourcing.Tests.Catalog.Mouser;

public sealed class MouserSearchRetryTests
{
    [Fact]
    public async Task SearchByPartNumberRetriesServiceUnavailableThenMapsSuccessfulListing()
    {
        using var handler = new RecordingHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("try later", Encoding.UTF8, "text/plain"),
            },
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "SearchResults": {
                        "Parts": [
                          {
                            "MouserPartNumber": "595-NE555P",
                            "ManufacturerPartNumber": "NE555P",
                            "Manufacturer": "Texas Instruments",
                            "Description": "Timer",
                            "AvailabilityInStock": "100 In Stock",
                            "PriceBreaks": [
                              { "Quantity": 1, "Price": "$0.44", "Currency": "USD" }
                            ]
                          }
                        ]
                      }
                    }
                    """,
                    Encoding.UTF8,
                    "application/json"),
            });
        var delay = new RecordingVendorHttpDelay();
        using var httpClient = new HttpClient(handler);
        var client = new MouserSearchClient(
            httpClient,
            new MouserSearchClientOptions("mouser-key"),
            new VendorHttpRetryPolicy(delay: delay));

        var result = await client.SearchByPartNumberAsync("NE555", limit: 5, CancellationToken.None);

        Assert.Empty(result.Diagnostics);
        Assert.Single(result.Listings);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Single(delay.Delays);
    }
}
