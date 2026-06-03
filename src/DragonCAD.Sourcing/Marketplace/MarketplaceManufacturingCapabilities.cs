namespace DragonCAD.Sourcing.Marketplace;

[Flags]
public enum MarketplaceManufacturingCapabilities
{
    None = 0,
    PrototypeBoardHandoff = 1,
    ProductionQuoteHandoff = 2,
}
