namespace DragonCAD.Sourcing.BomPlanning;

public sealed record BomPlanningPurchaseLine(
    string GroupKey,
    string VendorName,
    string VendorPartNumber,
    string ManufacturerPartNumber,
    bool IsPreferredVendor,
    bool IsSubstitution,
    int RequiredQuantity,
    int PurchaseQuantity,
    Money UnitPrice,
    Money ExtendedCost,
    int Stock,
    int LeadTimeDays,
    BomPartLifecycle Lifecycle);
