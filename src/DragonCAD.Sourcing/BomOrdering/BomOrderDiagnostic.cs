namespace DragonCAD.Sourcing.BomOrdering;

public sealed record BomOrderDiagnostic(
    string BomLineId,
    string ManufacturerPartNumber,
    int UnfilledQuantity,
    string Message)
{
    public static BomOrderDiagnostic Unavailable(BomOrderLine line, int unfilledQuantity)
    {
        return new BomOrderDiagnostic(
            line.LineId,
            line.ManufacturerPartNumber,
            unfilledQuantity,
            $"Unable to source {unfilledQuantity} unit(s) of {line.ManufacturerPartNumber}.");
    }
}
