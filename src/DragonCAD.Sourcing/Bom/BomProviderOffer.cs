namespace DragonCAD.Sourcing.Bom;

public sealed record BomProviderOffer(
    string ProviderName,
    string VendorSku,
    string ManufacturerPartNumber,
    int RequiredQuantity,
    int SelectedPriceBreakQuantity,
    Money UnitPrice,
    Money ExtendedCost,
    int? StockQuantity)
{
    public bool IsFullyAvailable => StockQuantity is null || StockQuantity >= RequiredQuantity;
}
