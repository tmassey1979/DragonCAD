using DragonCAD.App.Marketplace.Compliance;
using DragonCAD.Sourcing.Compliance;
using DragonCAD.Sourcing.Marketplace;

namespace DragonCAD.App.Tests.Marketplace.Compliance;

public sealed class ProviderTermsReviewViewModelTests
{
    [Fact]
    public void ApiBackedProviderSurfacesCapabilitiesTermsAndCredentialBlocker()
    {
        ProviderTermsReviewViewModel viewModel = ProviderTermsReviewViewModel.FromState(
            State(
                capabilities: Capabilities(
                    "mouser",
                    "Mouser",
                    catalog: MarketplaceCatalogCapabilities.Search |
                             MarketplaceCatalogCapabilities.ProductDetails |
                             MarketplaceCatalogCapabilities.Pricing |
                             MarketplaceCatalogCapabilities.Stock,
                    terms: MarketplaceProviderTerms.AllowsCatalogCache |
                           MarketplaceProviderTerms.AllowsPriceCache),
                compliance: Metadata(
                    "mouser",
                    sourceModes: [MarketplaceSourceMode.Api],
                    cacheLimit: new ProviderCacheLimit(
                        ProviderCacheLimitMode.TimeToLive,
                        maxAge: TimeSpan.FromHours(12),
                        maxEntries: 500,
                        note: "Keep API responses fresh."),
                    blockedAutomationModes: [MarketplaceAutomationMode.Scraping]),
                hasCredentials: false,
                catalogDataStale: false));

        Assert.Equal("Mouser", viewModel.ProviderName);
        Assert.Equal(["Search", "Product details", "Pricing", "Stock"], viewModel.CatalogCapabilityLabels);
        Assert.Empty(viewModel.ManufacturingCapabilityLabels);
        Assert.Equal(["API"], viewModel.SourceModeLabels);
        Assert.Equal("Attribution not required", viewModel.AttributionSummary);
        Assert.Equal("Time to live: 12 hours, 500 entries. Keep API responses fresh.", viewModel.CacheLimitSummary);
        Assert.Equal(["Scraping"], viewModel.BlockedAutomationModeLabels);
        Assert.Equal(["Catalog cache", "Price cache"], viewModel.ProviderTermLabels);
        Assert.False(viewModel.PrimaryAction.CanExecute);
        Assert.Equal("Credentials", viewModel.PrimaryAction.BlockerReason);
        Assert.Equal("Add Mouser credentials before API-backed catalog review can run.", viewModel.PrimaryAction.DisabledExplanation);
    }

    [Fact]
    public void ManualFeedProviderSurfacesAttributionAndStaleDataBlocker()
    {
        ProviderTermsReviewViewModel viewModel = ProviderTermsReviewViewModel.FromState(
            State(
                capabilities: Capabilities(
                    "jameco",
                    "Jameco",
                    catalog: MarketplaceCatalogCapabilities.Search,
                    terms: MarketplaceProviderTerms.RequiresAttribution |
                           MarketplaceProviderTerms.RequiresSourceUrl),
                compliance: Metadata(
                    "jameco",
                    sourceModes: [MarketplaceSourceMode.ManualDownload],
                    attribution: new AttributionRequirement(
                        isRequired: true,
                        notice: "Show feed copyright notice in imported catalog rows."),
                    redistribution: RedistributionPolicy.AllowedWithAttribution,
                    cacheLimit: ProviderCacheLimit.Unlimited,
                    blockedAutomationModes: [MarketplaceAutomationMode.Api, MarketplaceAutomationMode.Scraping]),
                hasCredentials: true,
                catalogDataStale: true));

        Assert.Equal(["Search"], viewModel.CatalogCapabilityLabels);
        Assert.Equal(["Manual download"], viewModel.SourceModeLabels);
        Assert.Equal("Required: Show feed copyright notice in imported catalog rows.", viewModel.AttributionSummary);
        Assert.Equal("Unlimited", viewModel.CacheLimitSummary);
        Assert.Equal(["API", "Scraping"], viewModel.BlockedAutomationModeLabels);
        Assert.Equal(["Requires attribution", "Requires source URL"], viewModel.ProviderTermLabels);
        Assert.Equal("Allowed with attribution", viewModel.RedistributionSummary);
        Assert.False(viewModel.PrimaryAction.CanExecute);
        Assert.Equal("Stale data", viewModel.PrimaryAction.BlockerReason);
        Assert.Equal("Refresh Jameco catalog data before reviewing manual-feed terms.", viewModel.PrimaryAction.DisabledExplanation);
    }

    [Fact]
    public void ScrapeRestrictedProviderExplainsProviderTermsBlocker()
    {
        ProviderTermsReviewViewModel viewModel = ProviderTermsReviewViewModel.FromState(
            State(
                capabilities: Capabilities(
                    "digikey",
                    "Digi-Key",
                    catalog: MarketplaceCatalogCapabilities.Search |
                             MarketplaceCatalogCapabilities.ProductDetails |
                             MarketplaceCatalogCapabilities.Pricing,
                    terms: MarketplaceProviderTerms.RequiresSourceId),
                compliance: Metadata(
                    "digikey",
                    sourceModes: [MarketplaceSourceMode.Api],
                    cacheLimit: ProviderCacheLimit.NoPersistentCache,
                    blockedAutomationModes: [MarketplaceAutomationMode.Scraping, MarketplaceAutomationMode.BulkDownload]),
                hasCredentials: true,
                catalogDataStale: false,
                requestedAutomationMode: MarketplaceAutomationMode.Scraping));

        Assert.Equal("No persistent cache", viewModel.CacheLimitSummary);
        Assert.Equal(["Scraping", "Bulk download"], viewModel.BlockedAutomationModeLabels);
        Assert.Equal(["Requires source ID"], viewModel.ProviderTermLabels);
        Assert.False(viewModel.PrimaryAction.CanExecute);
        Assert.Equal("Provider terms", viewModel.PrimaryAction.BlockerReason);
        Assert.Equal("Digi-Key terms block Scraping automation for this provider.", viewModel.PrimaryAction.DisabledExplanation);
    }

    [Fact]
    public void UploadQuoteHandoffProviderSurfacesManufacturingArtifactsAndMissingArtifactBlocker()
    {
        ProviderTermsReviewViewModel viewModel = ProviderTermsReviewViewModel.FromState(
            State(
                capabilities: Capabilities(
                    "oshpark",
                    "OSH Park",
                    catalog: MarketplaceCatalogCapabilities.None,
                    manufacturing: MarketplaceManufacturingCapabilities.PrototypeBoardHandoff |
                                   MarketplaceManufacturingCapabilities.ProductionQuoteHandoff,
                    terms: MarketplaceProviderTerms.None,
                    requiredArtifacts:
                    [
                        MarketplaceManufacturingArtifact.Gerbers,
                        MarketplaceManufacturingArtifact.DrillFiles,
                        MarketplaceManufacturingArtifact.BillOfMaterials
                    ]),
                compliance: Metadata(
                    "oshpark",
                    sourceModes: [MarketplaceSourceMode.CachedFixture],
                    cacheLimit: ProviderCacheLimit.Unlimited,
                    blockedAutomationModes: []),
                hasCredentials: true,
                catalogDataStale: false,
                availableArtifacts: [MarketplaceManufacturingArtifact.Gerbers]));

        Assert.Empty(viewModel.CatalogCapabilityLabels);
        Assert.Equal(["Prototype board handoff", "Production quote handoff"], viewModel.ManufacturingCapabilityLabels);
        Assert.Equal(["Gerbers", "Drill files", "Bill of materials"], viewModel.RequiredManufacturingArtifactLabels);
        Assert.Equal(["Drill files", "Bill of materials"], viewModel.MissingManufacturingArtifactLabels);
        Assert.Equal("No special provider terms", viewModel.ProviderTermsSummary);
        Assert.False(viewModel.PrimaryAction.CanExecute);
        Assert.Equal("Missing manufacturing artifacts", viewModel.PrimaryAction.BlockerReason);
        Assert.Equal("Export Drill files and Bill of materials before OSH Park upload or quote handoff.", viewModel.PrimaryAction.DisabledExplanation);
    }

    private static ProviderTermsReviewState State(
        MarketplaceProviderCapabilities capabilities,
        ProviderComplianceMetadata compliance,
        bool hasCredentials,
        bool catalogDataStale,
        IReadOnlyList<MarketplaceManufacturingArtifact>? availableArtifacts = null,
        MarketplaceAutomationMode requestedAutomationMode = MarketplaceAutomationMode.Api) =>
        new(
            capabilities,
            compliance,
            hasCredentials,
            catalogDataStale,
            availableArtifacts ?? [],
            requestedAutomationMode);

    private static MarketplaceProviderCapabilities Capabilities(
        string providerId,
        string displayName,
        MarketplaceCatalogCapabilities catalog,
        MarketplaceProviderTerms terms,
        MarketplaceManufacturingCapabilities manufacturing = MarketplaceManufacturingCapabilities.None,
        IReadOnlyList<MarketplaceManufacturingArtifact>? requiredArtifacts = null) =>
        new(providerId, displayName, catalog, manufacturing, terms, requiredArtifacts ?? []);

    private static ProviderComplianceMetadata Metadata(
        string providerId,
        IReadOnlyList<MarketplaceSourceMode> sourceModes,
        ProviderCacheLimit cacheLimit,
        IReadOnlyList<MarketplaceAutomationMode> blockedAutomationModes,
        AttributionRequirement? attribution = null,
        RedistributionPolicy redistribution = RedistributionPolicy.NotAllowed) =>
        new(
            providerId,
            sourceModes,
            attribution ?? AttributionRequirement.NotRequired,
            redistribution,
            cacheLimit,
            blockedAutomationModes);
}
