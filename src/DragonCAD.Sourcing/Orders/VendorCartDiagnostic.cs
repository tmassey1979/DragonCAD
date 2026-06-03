namespace DragonCAD.Sourcing.Orders;

public sealed record VendorCartDiagnostic(
    VendorCartDiagnosticCode Code,
    string BomLineId,
    string SourceOfferId,
    string Message);
