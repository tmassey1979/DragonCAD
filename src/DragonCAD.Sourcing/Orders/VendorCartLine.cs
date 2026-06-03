namespace DragonCAD.Sourcing.Orders;

public sealed record VendorCartLine
{
    public VendorCartLine(string bomLineId, string manufacturerPartNumber, int quantity, string sourceOfferId)
    {
        if (string.IsNullOrWhiteSpace(bomLineId))
        {
            throw new ArgumentException("BOM line id is required.", nameof(bomLineId));
        }

        if (string.IsNullOrWhiteSpace(manufacturerPartNumber))
        {
            throw new ArgumentException("Manufacturer part number is required.", nameof(manufacturerPartNumber));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Quantity must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(sourceOfferId))
        {
            throw new ArgumentException("Source offer id is required.", nameof(sourceOfferId));
        }

        BomLineId = bomLineId.Trim();
        ManufacturerPartNumber = manufacturerPartNumber.Trim();
        Quantity = quantity;
        SourceOfferId = sourceOfferId.Trim();
    }

    public string BomLineId { get; }

    public string ManufacturerPartNumber { get; }

    public int Quantity { get; }

    public string SourceOfferId { get; }
}
