using DragonCAD.Sourcing.Compliance;
using DragonCAD.Sourcing.Providers;
using DragonCAD.Sourcing.Providers.Descriptors;

namespace DragonCAD.Sourcing.Tests.Providers.Descriptors;

public sealed class LocalSourcingProviderDescriptorCatalogTests
{
    [Fact]
    public void DefaultCatalogIncludesCompleteOfflineDescriptorsForKnownProviders()
    {
        var descriptors = LocalSourcingProviderDescriptorCatalog.DefaultDescriptors;

        Assert.Equal(["digikey", "mouser", "jameco", "sparkfun", "adafruit"], descriptors.Select(descriptor => descriptor.ProviderId));
        Assert.All(descriptors, AssertCompleteDescriptor);
        Assert.All(descriptors, descriptor => Assert.True(descriptor.IsOfflineDescriptor));
    }

    [Theory]
    [InlineData("digikey", "Digi-Key", VendorCatalogAccessMode.CredentialedApi, "client_id", "client_secret")]
    [InlineData("mouser", "Mouser", VendorCatalogAccessMode.CredentialedApi, "api_key")]
    [InlineData("jameco", "Jameco", VendorCatalogAccessMode.ManualCatalogFeedFallback)]
    [InlineData("sparkfun", "SparkFun", VendorCatalogAccessMode.OpenHardwareRepositorySync)]
    [InlineData("adafruit", "Adafruit", VendorCatalogAccessMode.PublicProductApi)]
    public void ProviderIdsAndCredentialRequirementsAreStable(
        string providerId,
        string displayName,
        VendorCatalogAccessMode accessMode,
        params string[] requiredCredentialKeys)
    {
        var descriptor = LocalSourcingProviderDescriptorCatalog.Get(providerId);

        Assert.Equal(providerId, descriptor.ProviderId);
        Assert.Equal(displayName, descriptor.DisplayName);
        Assert.Equal(accessMode, descriptor.AccessMode);
        Assert.Equal(displayName, descriptor.CredentialRequirement.ProviderName);
        Assert.Equal(requiredCredentialKeys, descriptor.CredentialRequirement.RequiredKeyNames);
    }

    [Fact]
    public void DescriptorsExposeComplianceCatalogDataAndAutomationBoundaries()
    {
        var digikey = LocalSourcingProviderDescriptorCatalog.Get("digikey");
        var sparkfun = LocalSourcingProviderDescriptorCatalog.Get("sparkfun");
        var jameco = LocalSourcingProviderDescriptorCatalog.Get("jameco");

        Assert.Contains(MarketplaceSourceMode.Api, digikey.SourceModes);
        Assert.Equal(ProviderCacheLimitMode.TimeToLive, digikey.CachePolicy.Mode);
        Assert.Contains(SourcingCatalogData.PriceBreaks, digikey.SupportedCatalogData);
        Assert.Contains(SourcingCatalogData.StockQuantity, digikey.SupportedCatalogData);
        Assert.Contains(MarketplaceAutomationMode.Scraping, digikey.BlockedAutomationModes);
        Assert.Contains(MarketplaceAutomationMode.BulkDownload, digikey.BlockedAutomationModes);

        Assert.Contains(MarketplaceSourceMode.RepositoryClone, sparkfun.SourceModes);
        Assert.Contains(SourcingCatalogData.OpenHardwareDesignFiles, sparkfun.SupportedCatalogData);
        Assert.Contains(MarketplaceAutomationMode.RepositoryMirror, sparkfun.BlockedAutomationModes);

        Assert.Contains(MarketplaceSourceMode.ManualDownload, jameco.SourceModes);
        Assert.Equal(ProviderCacheLimitMode.NoPersistentCache, jameco.CachePolicy.Mode);
        Assert.Contains(MarketplaceAutomationMode.Scraping, jameco.BlockedAutomationModes);
    }

    [Fact]
    public void DiagnosticsContainNoCredentialValuesOrSecretIdentifiers()
    {
        var diagnostics = LocalSourcingProviderDescriptorCatalog.DefaultDescriptors
            .SelectMany(descriptor => descriptor.Diagnostics)
            .ToArray();

        Assert.NotEmpty(diagnostics);
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Contains("secret", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Contains("token", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Contains("password", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Contains("api_key", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Contains("client_secret", StringComparison.OrdinalIgnoreCase));
    }

    private static void AssertCompleteDescriptor(LocalSourcingProviderDescriptor descriptor)
    {
        Assert.False(string.IsNullOrWhiteSpace(descriptor.ProviderId));
        Assert.False(string.IsNullOrWhiteSpace(descriptor.DisplayName));
        Assert.NotEmpty(descriptor.SourceModes);
        Assert.NotEmpty(descriptor.SupportedCatalogData);
        Assert.NotEmpty(descriptor.BlockedAutomationModes);
        Assert.NotNull(descriptor.CredentialRequirement);
        Assert.NotNull(descriptor.CachePolicy);
        Assert.NotEmpty(descriptor.Diagnostics);
    }
}
