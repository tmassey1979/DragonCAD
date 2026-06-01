namespace DragonCAD.Sourcing.BomOrdering;

public sealed record BomOrderPlan(
    int BuildQuantity,
    Money TotalCost,
    IReadOnlyList<BomVendorOrder> VendorOrders,
    IReadOnlyList<BomOrderDiagnostic> Diagnostics)
{
    public bool IsComplete => Diagnostics.Count == 0;
}
