namespace DragonCAD.Sourcing.BomOrdering;

public sealed record BomVendorOrder(
    string VendorName,
    Money TotalCost,
    IReadOnlyList<BomOrderPurchaseLine> Lines);
