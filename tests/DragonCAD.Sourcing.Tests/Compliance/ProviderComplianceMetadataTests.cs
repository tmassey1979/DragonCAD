using DragonCAD.Sourcing.Compliance;

namespace DragonCAD.Sourcing.Tests.Compliance;

public sealed class ProviderComplianceMetadataTests
{
    [Fact]
    public void AttributionRequiredSourceExposesSourceModeAndAttributionRequirement()
    {
        var metadata = new ProviderComplianceMetadata(
            providerId: "sparkfun",
            allowedSourceModes: [MarketplaceSourceMode.RepositoryClone, MarketplaceSourceMode.Api],
            attribution: new AttributionRequirement(
                isRequired: true,
                notice: "Attribution required by upstream catalog terms."),
            redistribution: RedistributionPolicy.AllowedWithAttribution,
            cacheLimit: ProviderCacheLimit.Unlimited,
            blockedAutomationModes: [MarketplaceAutomationMode.None]);

        Assert.True(metadata.AllowsSourceMode(MarketplaceSourceMode.RepositoryClone));
        Assert.True(metadata.Attribution.IsRequired);
        Assert.Equal(RedistributionPolicy.AllowedWithAttribution, metadata.Redistribution);
        Assert.False(metadata.BlocksAutomationMode(MarketplaceAutomationMode.Api));
    }

    [Fact]
    public void NoRedistributionSourceKeepsRedistributionRestrictionVisible()
    {
        var metadata = new ProviderComplianceMetadata(
            providerId: "datasheet-vendor",
            allowedSourceModes: [MarketplaceSourceMode.Api],
            attribution: AttributionRequirement.NotRequired,
            redistribution: RedistributionPolicy.NotAllowed,
            cacheLimit: ProviderCacheLimit.Unlimited,
            blockedAutomationModes: []);

        Assert.Equal(RedistributionPolicy.NotAllowed, metadata.Redistribution);
        Assert.True(metadata.ProhibitsRedistribution);
    }

    [Fact]
    public void ApiCacheLimitedSourceExposesCacheLimit()
    {
        var cacheLimit = new ProviderCacheLimit(
            mode: ProviderCacheLimitMode.TimeToLive,
            maxAge: TimeSpan.FromHours(12),
            maxEntries: 500,
            note: "Cache API responses for a bounded freshness window.");
        var metadata = new ProviderComplianceMetadata(
            providerId: "mouser",
            allowedSourceModes: [MarketplaceSourceMode.Api],
            attribution: AttributionRequirement.NotRequired,
            redistribution: RedistributionPolicy.NotAllowed,
            cacheLimit: cacheLimit,
            blockedAutomationModes: [MarketplaceAutomationMode.Scraping]);

        Assert.Equal(ProviderCacheLimitMode.TimeToLive, metadata.CacheLimit.Mode);
        Assert.Equal(TimeSpan.FromHours(12), metadata.CacheLimit.MaxAge);
        Assert.Equal(500, metadata.CacheLimit.MaxEntries);
    }

    [Fact]
    public void BlockedScrapingSourceRejectsScrapingAutomationMode()
    {
        var metadata = new ProviderComplianceMetadata(
            providerId: "digikey",
            allowedSourceModes: [MarketplaceSourceMode.Api],
            attribution: AttributionRequirement.NotRequired,
            redistribution: RedistributionPolicy.NotAllowed,
            cacheLimit: ProviderCacheLimit.NoPersistentCache,
            blockedAutomationModes: [MarketplaceAutomationMode.Scraping, MarketplaceAutomationMode.BulkDownload]);

        Assert.True(metadata.BlocksAutomationMode(MarketplaceAutomationMode.Scraping));
        Assert.True(metadata.BlocksAutomationMode(MarketplaceAutomationMode.BulkDownload));
        Assert.False(metadata.BlocksAutomationMode(MarketplaceAutomationMode.Api));
    }
}
