using DragonCAD.Sourcing;

namespace DragonCAD.Sourcing.Tests;

public sealed class SourcingProviderCatalogTests
{
    [Fact]
    public void DefaultProvidersDescribeKnownVendorCapabilities()
    {
        var providers = SourcingProviderCatalog.DefaultProviders;

        Assert.Equal(
            ["Digi-Key", "Mouser", "Jameco", "SparkFun", "Adafruit"],
            providers.Select(provider => provider.Name));

        Assert.All(providers, provider => Assert.True(provider.SupportsDatasheetSearch));
        Assert.Equal(
            [true, true, true, true, true],
            providers.Select(provider => provider.SupportsStock));
        Assert.Equal(
            [true, true, true, false, false],
            providers.Select(provider => provider.SupportsPricing));
    }
}
