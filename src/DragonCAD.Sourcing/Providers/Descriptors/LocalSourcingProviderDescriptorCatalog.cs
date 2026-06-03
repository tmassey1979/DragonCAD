using DragonCAD.Sourcing.Compliance;
using DragonCAD.Sourcing.Credentials;
using DragonCAD.Sourcing.Providers;

namespace DragonCAD.Sourcing.Providers.Descriptors;

public static class LocalSourcingProviderDescriptorCatalog
{
    public static IReadOnlyList<LocalSourcingProviderDescriptor> DefaultDescriptors { get; } =
    [
        new(
            providerId: "digikey",
            displayName: "Digi-Key",
            sourceModes: [MarketplaceSourceMode.Api, MarketplaceSourceMode.CachedFixture],
            accessMode: VendorCatalogAccessMode.CredentialedApi,
            credentialRequirement: CredentialRequirement("Digi-Key"),
            cachePolicy: TimeToLiveCache("Cache catalog API response metadata for a bounded offline freshness window."),
            supportedCatalogData:
            [
                SourcingCatalogData.ManufacturerPartNumber,
                SourcingCatalogData.ProductUrl,
                SourcingCatalogData.DatasheetUrl,
                SourcingCatalogData.PriceBreaks,
                SourcingCatalogData.StockQuantity,
            ],
            blockedAutomationModes: [MarketplaceAutomationMode.Scraping, MarketplaceAutomationMode.BulkDownload],
            diagnostics: ["Credentialed API descriptor only; runtime credentials are resolved outside diagnostics."]),
        new(
            providerId: "mouser",
            displayName: "Mouser",
            sourceModes: [MarketplaceSourceMode.Api, MarketplaceSourceMode.CachedFixture],
            accessMode: VendorCatalogAccessMode.CredentialedApi,
            credentialRequirement: CredentialRequirement("Mouser"),
            cachePolicy: TimeToLiveCache("Cache catalog API response metadata for a bounded offline freshness window."),
            supportedCatalogData:
            [
                SourcingCatalogData.ManufacturerPartNumber,
                SourcingCatalogData.ProductUrl,
                SourcingCatalogData.DatasheetUrl,
                SourcingCatalogData.PriceBreaks,
                SourcingCatalogData.StockQuantity,
            ],
            blockedAutomationModes: [MarketplaceAutomationMode.Scraping, MarketplaceAutomationMode.BulkDownload],
            diagnostics: ["Credentialed API descriptor only; runtime credentials are resolved outside diagnostics."]),
        new(
            providerId: "jameco",
            displayName: "Jameco",
            sourceModes: [MarketplaceSourceMode.ManualDownload, MarketplaceSourceMode.CachedFixture],
            accessMode: VendorCatalogAccessMode.ManualCatalogFeedFallback,
            credentialRequirement: CredentialRequirement("Jameco"),
            cachePolicy: ProviderCacheLimit.NoPersistentCache,
            supportedCatalogData:
            [
                SourcingCatalogData.ManufacturerPartNumber,
                SourcingCatalogData.ProductUrl,
                SourcingCatalogData.DatasheetUrl,
                SourcingCatalogData.PriceBreaks,
                SourcingCatalogData.StockQuantity,
            ],
            blockedAutomationModes: [MarketplaceAutomationMode.Scraping, MarketplaceAutomationMode.BulkDownload],
            diagnostics: ["Manual catalog feed descriptor; live scraping remains blocked."]),
        new(
            providerId: "sparkfun",
            displayName: "SparkFun",
            sourceModes: [MarketplaceSourceMode.RepositoryClone, MarketplaceSourceMode.CachedFixture],
            accessMode: VendorCatalogAccessMode.OpenHardwareRepositorySync,
            credentialRequirement: CredentialRequirement("SparkFun"),
            cachePolicy: TimeToLiveCache("Cache repository-derived catalog metadata for offline review."),
            supportedCatalogData:
            [
                SourcingCatalogData.ManufacturerPartNumber,
                SourcingCatalogData.ProductUrl,
                SourcingCatalogData.DatasheetUrl,
                SourcingCatalogData.StockQuantity,
                SourcingCatalogData.RepositoryUrl,
                SourcingCatalogData.OpenHardwareDesignFiles,
            ],
            blockedAutomationModes: [MarketplaceAutomationMode.Scraping, MarketplaceAutomationMode.BulkDownload, MarketplaceAutomationMode.RepositoryMirror],
            diagnostics: ["Repository sync descriptor; use cached open-hardware metadata for offline planning."]),
        new(
            providerId: "adafruit",
            displayName: "Adafruit",
            sourceModes: [MarketplaceSourceMode.Api, MarketplaceSourceMode.CachedFixture],
            accessMode: VendorCatalogAccessMode.PublicProductApi,
            credentialRequirement: CredentialRequirement("Adafruit"),
            cachePolicy: TimeToLiveCache("Cache public product metadata for offline review."),
            supportedCatalogData:
            [
                SourcingCatalogData.ManufacturerPartNumber,
                SourcingCatalogData.ProductUrl,
                SourcingCatalogData.DatasheetUrl,
                SourcingCatalogData.StockQuantity,
            ],
            blockedAutomationModes: [MarketplaceAutomationMode.Scraping, MarketplaceAutomationMode.BulkDownload],
            diagnostics: ["Public product API descriptor; no credential values are captured."]),
    ];

    public static LocalSourcingProviderDescriptor Get(string providerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);

        return DefaultDescriptors.Single(
            descriptor => descriptor.ProviderId.Equals(providerId.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static ProviderCredentialRequirement CredentialRequirement(string providerName)
    {
        return ProviderCredentialRequirement.KnownProviders[providerName];
    }

    private static ProviderCacheLimit TimeToLiveCache(string note)
    {
        return new ProviderCacheLimit(
            ProviderCacheLimitMode.TimeToLive,
            maxAge: TimeSpan.FromHours(24),
            maxEntries: 10_000,
            note: note);
    }
}
