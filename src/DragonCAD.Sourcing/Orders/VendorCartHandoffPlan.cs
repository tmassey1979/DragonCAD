namespace DragonCAD.Sourcing.Orders;

public sealed record VendorCartHandoffPlan(
    DateTimeOffset CreatedAt,
    IReadOnlyList<VendorCartHandoffRecord> Records,
    IReadOnlyList<VendorCartDiagnostic> Diagnostics)
{
    public bool HasBlockingDiagnostics => Diagnostics.Count > 0;
}
