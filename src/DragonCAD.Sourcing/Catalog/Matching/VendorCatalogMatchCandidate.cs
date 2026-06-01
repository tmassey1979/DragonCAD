namespace DragonCAD.Sourcing.Catalog.Matching;

public sealed record VendorCatalogMatchCandidate(
    string ManufacturerPartNumber,
    string Manufacturer,
    string Package,
    Uri? DatasheetUrl);
