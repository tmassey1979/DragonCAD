namespace DragonCAD.Sourcing.Marketplace;

[Flags]
public enum MarketplaceProviderTerms
{
    None = 0,
    AllowsCatalogCache = 1,
    AllowsPriceCache = 2,
    AllowsStockCache = 4,
    RequiresAttribution = 8,
    RequiresSourceUrl = 16,
    RequiresSourceId = 32,
}
