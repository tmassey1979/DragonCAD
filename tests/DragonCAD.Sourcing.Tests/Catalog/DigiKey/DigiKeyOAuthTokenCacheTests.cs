using DragonCAD.Sourcing.Catalog;
using DragonCAD.Sourcing.Catalog.DigiKey;

namespace DragonCAD.Sourcing.Tests.Catalog.DigiKey;

public sealed class DigiKeyOAuthTokenCacheTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetTokenReusesCachedTokenBeforeExpirySkew()
    {
        var source = new FakeTokenSource(
            new DigiKeyOAuthTokenResult(new DigiKeyOAuthToken("token-1", "Bearer", 3600), []),
            new DigiKeyOAuthTokenResult(new DigiKeyOAuthToken("token-2", "Bearer", 3600), []));
        var clock = new MutableClock(Now);
        var cache = new DigiKeyOAuthTokenCache(source, clock, TimeSpan.FromMinutes(5));

        DigiKeyOAuthTokenResult first = await cache.GetTokenAsync(CancellationToken.None);
        clock.UtcNow = Now.AddMinutes(30);
        DigiKeyOAuthTokenResult second = await cache.GetTokenAsync(CancellationToken.None);

        Assert.Equal("token-1", first.Token?.AccessToken);
        Assert.Equal("token-1", second.Token?.AccessToken);
        Assert.Equal(1, source.CallCount);
    }

    [Fact]
    public async Task GetTokenRefreshesWhenTokenIsInsideExpirySkew()
    {
        var source = new FakeTokenSource(
            new DigiKeyOAuthTokenResult(new DigiKeyOAuthToken("token-1", "Bearer", 600), []),
            new DigiKeyOAuthTokenResult(new DigiKeyOAuthToken("token-2", "Bearer", 600), []));
        var clock = new MutableClock(Now);
        var cache = new DigiKeyOAuthTokenCache(source, clock, TimeSpan.FromMinutes(5));

        DigiKeyOAuthTokenResult first = await cache.GetTokenAsync(CancellationToken.None);
        clock.UtcNow = Now.AddMinutes(6);
        DigiKeyOAuthTokenResult second = await cache.GetTokenAsync(CancellationToken.None);

        Assert.Equal("token-1", first.Token?.AccessToken);
        Assert.Equal("token-2", second.Token?.AccessToken);
        Assert.Equal(2, source.CallCount);
    }

    [Fact]
    public async Task GetTokenDoesNotCacheFailedTokenResponse()
    {
        var source = new FakeTokenSource(
            new DigiKeyOAuthTokenResult(null, [new CatalogImportDiagnostic(CatalogDiagnosticSeverity.Error, "oauth.fail", "failed", "Digi-Key", null)]),
            new DigiKeyOAuthTokenResult(new DigiKeyOAuthToken("token-2", "Bearer", 600), []));
        var cache = new DigiKeyOAuthTokenCache(source, new MutableClock(Now), TimeSpan.FromMinutes(5));

        DigiKeyOAuthTokenResult first = await cache.GetTokenAsync(CancellationToken.None);
        DigiKeyOAuthTokenResult second = await cache.GetTokenAsync(CancellationToken.None);

        Assert.Null(first.Token);
        Assert.Equal("token-2", second.Token?.AccessToken);
        Assert.Equal(2, source.CallCount);
    }

    private sealed class FakeTokenSource : IDigiKeyOAuthTokenSource
    {
        private readonly Queue<DigiKeyOAuthTokenResult> results;

        public FakeTokenSource(params DigiKeyOAuthTokenResult[] results)
        {
            this.results = new Queue<DigiKeyOAuthTokenResult>(results);
        }

        public int CallCount { get; private set; }

        public Task<DigiKeyOAuthTokenResult> RequestClientCredentialsTokenAsync(CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(results.Dequeue());
        }
    }

    private sealed class MutableClock : IDigiKeyOAuthClock
    {
        public MutableClock(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; set; }
    }
}
