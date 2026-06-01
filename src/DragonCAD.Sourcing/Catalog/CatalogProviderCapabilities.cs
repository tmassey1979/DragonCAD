namespace DragonCAD.Sourcing.Catalog;

[Flags]
public enum CatalogProviderCapabilities
{
    None = 0,
    Api = 1,
    Feed = 2,
    Manual = 4,
    ScrapeRestricted = 8,
}
