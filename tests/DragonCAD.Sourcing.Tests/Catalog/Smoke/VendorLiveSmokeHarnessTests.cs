using DragonCAD.Sourcing.Catalog.Smoke;

namespace DragonCAD.Sourcing.Tests.Catalog.Smoke;

public sealed class VendorLiveSmokeHarnessTests
{
    [Fact]
    public void IsEnabledReturnsFalseWhenGateIsMissing()
    {
        var enabled = VendorLiveSmokeHarness.IsEnabled(_ => null);

        Assert.False(enabled);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("true")]
    [InlineData("TRUE")]
    public void IsEnabledReturnsTrueWhenGateIsExplicitlyEnabled(string value)
    {
        var enabled = VendorLiveSmokeHarness.IsEnabled(name =>
            name == VendorLiveSmokeHarness.GateEnvironmentVariable ? value : null);

        Assert.True(enabled);
    }

    [Fact]
    public async Task RunDigiKeyKeywordSearchAsyncDoesNotCreateHttpClientsWhenGateIsDisabled()
    {
        var harness = new VendorLiveSmokeHarness(
            _ => null,
            _ => throw new InvalidOperationException("HTTP client should not be created when live smoke is disabled."));

        var result = await harness.RunDigiKeyKeywordSearchAsync("LM7805", limit: 1, CancellationToken.None);

        Assert.Equal(VendorLiveSmokeRunStatus.Disabled, result.Status);
        Assert.Equal("Digi-Key", result.ProviderName);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public async Task RunMouserKeywordSearchAsyncDoesNotCreateHttpClientsWhenGateIsDisabled()
    {
        var harness = new VendorLiveSmokeHarness(
            _ => null,
            _ => throw new InvalidOperationException("HTTP client should not be created when live smoke is disabled."));

        var result = await harness.RunMouserKeywordSearchAsync("LM7805", limit: 1, CancellationToken.None);

        Assert.Equal(VendorLiveSmokeRunStatus.Disabled, result.Status);
        Assert.Equal("Mouser", result.ProviderName);
        Assert.Empty(result.Diagnostics);
    }
}
