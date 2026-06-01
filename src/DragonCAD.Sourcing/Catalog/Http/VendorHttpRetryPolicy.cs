using System.Net;

namespace DragonCAD.Sourcing.Catalog.Http;

public sealed class VendorHttpRetryPolicy
{
    private readonly IVendorHttpDelay delay;
    private readonly int maxRetries;
    private readonly TimeSpan baseDelay;

    public VendorHttpRetryPolicy(
        int maxRetries = 2,
        TimeSpan? baseDelay = null,
        IVendorHttpDelay? delay = null)
    {
        if (maxRetries < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRetries), "Retry count cannot be negative.");
        }

        this.maxRetries = maxRetries;
        this.baseDelay = baseDelay ?? TimeSpan.FromMilliseconds(250);
        this.delay = delay ?? SystemVendorHttpDelay.Instance;
    }

    public async Task<HttpResponseMessage> SendAsync(
        HttpClient httpClient,
        Func<HttpRequestMessage> createRequest,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(createRequest);

        for (var attempt = 0; ; attempt++)
        {
            HttpRequestMessage request = createRequest();
            HttpResponseMessage response = await httpClient
                .SendAsync(request, cancellationToken)
                .ConfigureAwait(false);

            if (!ShouldRetry(response.StatusCode) || attempt >= maxRetries)
            {
                return response;
            }

            TimeSpan retryDelay = ReadRetryAfter(response) ?? TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, attempt));
            response.Dispose();
            await delay.DelayAsync(retryDelay, cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool ShouldRetry(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.TooManyRequests
            or HttpStatusCode.RequestTimeout
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;

    private static TimeSpan? ReadRetryAfter(HttpResponseMessage response)
    {
        if (response.Headers.RetryAfter?.Delta is { } delta)
        {
            return delta;
        }

        if (response.Headers.RetryAfter?.Date is { } date)
        {
            TimeSpan delay = date - DateTimeOffset.UtcNow;
            return delay > TimeSpan.Zero ? delay : TimeSpan.Zero;
        }

        return null;
    }
}
