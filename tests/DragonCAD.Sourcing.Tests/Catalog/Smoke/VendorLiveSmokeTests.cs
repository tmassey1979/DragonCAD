using DragonCAD.Sourcing.Catalog.Smoke;

namespace DragonCAD.Sourcing.Tests.Catalog.Smoke;

public sealed class VendorLiveSmokeTests
{
    [Fact]
    public async Task DigiKeyKeywordSearchCanRunAgainstVendorWhenExplicitlyEnabled()
    {
        var harness = VendorLiveSmokeHarness.CreateDefault();

        var result = await harness.RunDigiKeyKeywordSearchAsync("LM7805", limit: 1, CancellationToken.None);

        AssertLiveSmokeSucceededOrWasDisabled(result);
    }

    [Fact]
    public async Task MouserKeywordSearchCanRunAgainstVendorWhenExplicitlyEnabled()
    {
        var harness = VendorLiveSmokeHarness.CreateDefault();

        var result = await harness.RunMouserKeywordSearchAsync("LM7805", limit: 1, CancellationToken.None);

        AssertLiveSmokeSucceededOrWasDisabled(result);
    }

    private static void AssertLiveSmokeSucceededOrWasDisabled(VendorLiveSmokeRunResult result)
    {
        if (result.Status == VendorLiveSmokeRunStatus.Disabled)
        {
            Assert.Empty(result.Diagnostics);
            return;
        }

        Assert.Equal(VendorLiveSmokeRunStatus.Succeeded, result.Status);
        Assert.True(result.ListingCount > 0, "Vendor live smoke should return at least one normalized listing.");
        Assert.DoesNotContain(result.Diagnostics, diagnostic =>
            diagnostic.Message.Contains("DRAGONCAD_", StringComparison.Ordinal) ||
            diagnostic.Message.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
            diagnostic.Message.Contains("api key", StringComparison.OrdinalIgnoreCase));
    }
}
