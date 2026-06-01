namespace DragonCAD.Sourcing;

public sealed record MissingQuoteDiagnostic(
    string BomLineId,
    string ManufacturerPartNumber,
    int RequiredQuantity,
    string Message)
{
    public static MissingQuoteDiagnostic NoVendorQuote(SourcingBomLine bomLine, int requiredQuantity)
    {
        return new MissingQuoteDiagnostic(
            bomLine.LineId,
            bomLine.ManufacturerPartNumber,
            requiredQuantity,
            $"No vendor quote was available for {bomLine.ManufacturerPartNumber}.");
    }
}
