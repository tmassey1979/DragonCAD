namespace DragonCAD.Sourcing.BomOrdering;

public sealed record BomOrderPurchaseLine(
    string BomLineId,
    string ManufacturerPartNumber,
    string VendorPartNumber,
    int RequiredQuantity,
    int PurchaseQuantity,
    Money UnitPrice,
    Money ExtendedCost);
