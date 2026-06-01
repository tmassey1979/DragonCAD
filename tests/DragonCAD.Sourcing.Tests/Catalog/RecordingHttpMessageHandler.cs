using System.Net;

namespace DragonCAD.Sourcing.Tests.Catalog;

public sealed class RecordingHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> responses;

    public RecordingHttpMessageHandler(params HttpResponseMessage[] responses)
    {
        this.responses = new Queue<HttpResponseMessage>(responses);
    }

    public List<HttpRequestMessage> Requests { get; } = [];

    public List<string> RequestBodies { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Requests.Add(request);

        if (request.Content is not null)
        {
            RequestBodies.Add(await request.Content.ReadAsStringAsync(cancellationToken));
        }

        return responses.Count > 0
            ? responses.Dequeue()
            : new HttpResponseMessage(HttpStatusCode.OK);
    }
}
