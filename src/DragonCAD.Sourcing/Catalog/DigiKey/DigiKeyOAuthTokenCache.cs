namespace DragonCAD.Sourcing.Catalog.DigiKey;

public sealed class DigiKeyOAuthTokenCache : IDigiKeyOAuthTokenSource
{
    private readonly IDigiKeyOAuthTokenSource source;
    private readonly IDigiKeyOAuthClock clock;
    private readonly TimeSpan expirySkew;
    private DigiKeyOAuthToken? cachedToken;
    private DateTimeOffset cachedTokenExpiresAtUtc;

    public DigiKeyOAuthTokenCache(
        IDigiKeyOAuthTokenSource source,
        IDigiKeyOAuthClock? clock = null,
        TimeSpan? expirySkew = null)
    {
        this.source = source ?? throw new ArgumentNullException(nameof(source));
        this.clock = clock ?? SystemDigiKeyOAuthClock.Instance;
        this.expirySkew = expirySkew ?? TimeSpan.FromMinutes(5);
    }

    public async Task<DigiKeyOAuthTokenResult> RequestClientCredentialsTokenAsync(CancellationToken cancellationToken) =>
        await GetTokenAsync(cancellationToken).ConfigureAwait(false);

    public async Task<DigiKeyOAuthTokenResult> GetTokenAsync(CancellationToken cancellationToken)
    {
        if (cachedToken is not null && !IsExpiredOrNearExpiry())
        {
            return new DigiKeyOAuthTokenResult(cachedToken, []);
        }

        DigiKeyOAuthTokenResult result = await source
            .RequestClientCredentialsTokenAsync(cancellationToken)
            .ConfigureAwait(false);

        if (result.Token is not null)
        {
            cachedToken = result.Token;
            cachedTokenExpiresAtUtc = clock.UtcNow.AddSeconds(result.Token.ExpiresInSeconds);
        }

        return result;
    }

    private bool IsExpiredOrNearExpiry() =>
        clock.UtcNow >= cachedTokenExpiresAtUtc.Subtract(expirySkew);
}
