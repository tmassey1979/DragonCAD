namespace DragonCAD.Sourcing.Marketplace;

[Flags]
public enum MarketplaceCatalogCapabilities
{
    None = 0,
    Search = 1,
    ProductDetails = 2,
    Pricing = 4,
    Stock = 8,
    Lifecycle = 16,
    DatasheetLinks = 32,
    ImageLinks = 64,
}
