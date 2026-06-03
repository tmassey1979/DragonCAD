using System.Text.Json;
using DragonCAD.Sourcing.Marketplace;

namespace DragonCAD.Sourcing.Tests.Marketplace;

public sealed class MarketplaceProviderContractTests
{
    [Fact]
    public void CapabilityMetadataSerializesStableFlagsAndManufacturingArtifacts()
    {
        var capabilities = new MarketplaceProviderCapabilities(
            ProviderId: "digikey",
            DisplayName: "Digi-Key",
            Catalog: MarketplaceCatalogCapabilities.Search
                | MarketplaceCatalogCapabilities.ProductDetails
                | MarketplaceCatalogCapabilities.Pricing
                | MarketplaceCatalogCapabilities.Stock
                | MarketplaceCatalogCapabilities.Lifecycle
                | MarketplaceCatalogCapabilities.DatasheetLinks
                | MarketplaceCatalogCapabilities.ImageLinks,
            Manufacturing: MarketplaceManufacturingCapabilities.PrototypeBoardHandoff
                | MarketplaceManufacturingCapabilities.ProductionQuoteHandoff,
            Terms: MarketplaceProviderTerms.AllowsCatalogCache | MarketplaceProviderTerms.RequiresAttribution,
            RequiredArtifacts:
            [
                MarketplaceManufacturingArtifact.Gerbers,
                MarketplaceManufacturingArtifact.DrillFiles,
                MarketplaceManufacturingArtifact.BillOfMaterials,
            ]);

        var json = JsonSerializer.Serialize(capabilities);
        var restored = JsonSerializer.Deserialize<MarketplaceProviderCapabilities>(json);

        Assert.NotNull(restored);
        Assert.Equal(capabilities.ProviderId, restored.ProviderId);
        Assert.True(restored.Catalog.HasFlag(MarketplaceCatalogCapabilities.ProductDetails));
        Assert.True(restored.Manufacturing.HasFlag(MarketplaceManufacturingCapabilities.ProductionQuoteHandoff));
        Assert.Equal(
            [
                MarketplaceManufacturingArtifact.Gerbers,
                MarketplaceManufacturingArtifact.DrillFiles,
                MarketplaceManufacturingArtifact.BillOfMaterials,
            ],
            restored.RequiredArtifacts);
        Assert.True(restored.Terms.HasFlag(MarketplaceProviderTerms.RequiresAttribution));
    }

    [Fact]
    public void ProviderResponsesRequireSourceAndTimestampMetadata()
    {
        var retrievedAt = new DateTimeOffset(2026, 6, 3, 14, 30, 0, TimeSpan.Zero);
        var metadata = new MarketplaceResponseMetadata(
            SourceVendor: "Mouser",
            SourceId: "MOU-667-ERJ-3EKF1002V",
            SourceUrl: new Uri("https://www.mouser.com/ProductDetail/667-ERJ-3EKF1002V"),
            RetrievedAt: retrievedAt,
            Terms: MarketplaceProviderTerms.AllowsCatalogCache,
            CapabilityFlags: MarketplaceCatalogCapabilities.Search | MarketplaceCatalogCapabilities.Stock);
        var response = new MarketplaceCatalogSearchResponse(
            Metadata: metadata,
            Matches:
            [
                new MarketplaceCatalogSearchResult(
                    VendorSku: "667-ERJ-3EKF1002V",
                    ManufacturerPartNumber: "ERJ-3EKF1002V",
                    Manufacturer: "Panasonic",
                    Description: "10 kOhm resistor",
                    ProductDetail: new MarketplaceProductDetail(
                        Lifecycle: MarketplaceProductLifecycle.Active,
                        DatasheetUrl: new Uri("https://industrial.panasonic.com/ds/ERJ-3EKF1002V.pdf"),
                        ImageUrl: new Uri("https://www.mouser.com/images/panasonic/images/erj.jpg")),
                    Pricing: new MarketplacePricing([new MarketplacePriceBreak(Quantity: 1, UnitPrice: Money.Usd(0.10m))]),
                    Stock: new MarketplaceStock(OnHandQuantity: 1200, IsBackorderable: true)),
            ]);

        Assert.Equal("Mouser", response.Metadata.SourceVendor);
        Assert.Equal("MOU-667-ERJ-3EKF1002V", response.Metadata.SourceId);
        Assert.Equal(retrievedAt, response.Metadata.RetrievedAt);
        Assert.True(response.Metadata.HasCatalogCapability(MarketplaceCatalogCapabilities.Stock));
        Assert.Equal(MarketplaceProductLifecycle.Active, response.Matches[0].ProductDetail.Lifecycle);
        Assert.Equal(1200, response.Matches[0].Stock.OnHandQuantity);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void ProviderResponsesRejectMissingSourceVendor(string sourceVendor)
    {
        var error = Assert.Throws<ArgumentException>(() => new MarketplaceResponseMetadata(
            SourceVendor: sourceVendor,
            SourceId: "sku-1",
            SourceUrl: null,
            RetrievedAt: DateTimeOffset.UtcNow,
            Terms: MarketplaceProviderTerms.None,
            CapabilityFlags: MarketplaceCatalogCapabilities.Search));

        Assert.Equal("SourceVendor", error.ParamName);
    }

    [Fact]
    public void ManufacturingDiagnosticsDescribeUnsupportedCapabilitiesAndMissingArtifacts()
    {
        var request = new MarketplaceBoardHandoffRequest(
            ProjectId: "amp-controller",
            Revision: "A",
            RequestedCapability: MarketplaceManufacturingCapabilities.ProductionQuoteHandoff,
            Artifacts:
            [
                new MarketplaceManufacturingArtifactLink(
                    MarketplaceManufacturingArtifact.Gerbers,
                    new Uri("file:///workspace/amp-controller/gerbers.zip")),
            ]);
        var diagnostics = MarketplaceManufacturingContractValidator.Validate(
            request,
            new MarketplaceProviderCapabilities(
                ProviderId: "oshpark",
                DisplayName: "OSH Park",
                Catalog: MarketplaceCatalogCapabilities.None,
                Manufacturing: MarketplaceManufacturingCapabilities.PrototypeBoardHandoff,
                Terms: MarketplaceProviderTerms.RequiresAttribution,
                RequiredArtifacts:
                [
                    MarketplaceManufacturingArtifact.Gerbers,
                    MarketplaceManufacturingArtifact.DrillFiles,
                    MarketplaceManufacturingArtifact.BillOfMaterials,
                ]));

        Assert.Equal(
            [
                MarketplaceUnsupportedCapabilityKind.UnsupportedManufacturingCapability,
                MarketplaceUnsupportedCapabilityKind.MissingRequiredArtifact,
                MarketplaceUnsupportedCapabilityKind.MissingRequiredArtifact,
            ],
            diagnostics.Select(diagnostic => diagnostic.Kind));
        Assert.Contains(diagnostics, diagnostic => diagnostic.Message.Contains("production quote", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(diagnostics, diagnostic => diagnostic.Artifact == MarketplaceManufacturingArtifact.DrillFiles);
        Assert.Contains(diagnostics, diagnostic => diagnostic.Artifact == MarketplaceManufacturingArtifact.BillOfMaterials);
    }
}
