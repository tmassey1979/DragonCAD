using System.Collections.ObjectModel;

namespace DragonCAD.Sourcing.Marketplace;

public sealed record MarketplaceCatalogSearchRequest(
    string Query,
    int? RequiredQuantity,
    MarketplaceCatalogCapabilities RequestedCapabilities);

public sealed record MarketplaceProductDetailRequest(
    string VendorSku,
    string? ManufacturerPartNumber);

public sealed record MarketplaceCatalogSearchResponse(
    MarketplaceResponseMetadata Metadata,
    IReadOnlyList<MarketplaceCatalogSearchResult> Matches)
{
    public IReadOnlyList<MarketplaceCatalogSearchResult> Matches { get; } =
        new ReadOnlyCollection<MarketplaceCatalogSearchResult>([.. Matches ?? []]);
}

public sealed record MarketplaceProductDetailResponse(
    MarketplaceResponseMetadata Metadata,
    MarketplaceCatalogSearchResult Product);

public sealed record MarketplaceCatalogSearchResult(
    string VendorSku,
    string ManufacturerPartNumber,
    string Manufacturer,
    string Description,
    MarketplaceProductDetail ProductDetail,
    MarketplacePricing Pricing,
    MarketplaceStock Stock);

public sealed record MarketplaceProductDetail(
    MarketplaceProductLifecycle Lifecycle,
    Uri? DatasheetUrl,
    Uri? ImageUrl);

public sealed record MarketplacePricing(IReadOnlyList<MarketplacePriceBreak> PriceBreaks)
{
    public IReadOnlyList<MarketplacePriceBreak> PriceBreaks { get; } =
        new ReadOnlyCollection<MarketplacePriceBreak>([.. PriceBreaks ?? []]);
}

public sealed record MarketplacePriceBreak(int Quantity, Money UnitPrice);

public sealed record MarketplaceStock(int? OnHandQuantity, bool IsBackorderable);
