namespace DragonCAD.Sourcing.Catalog.Matching;

public sealed record VendorCatalogMatchScore(
    VendorCatalogMatchQuality Quality,
    int Score,
    bool ManufacturerPartNumberMatches,
    bool ManufacturerMatches,
    bool PackageMatches,
    bool DatasheetUrlMatches);
