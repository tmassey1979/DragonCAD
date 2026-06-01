namespace DragonCAD.Sourcing.BomOrdering;

public sealed record BomOrderLine
{
    public BomOrderLine(string lineId, string manufacturerPartNumber, int quantityPerAssembly)
    {
        if (string.IsNullOrWhiteSpace(lineId))
        {
            throw new ArgumentException("BOM line id is required.", nameof(lineId));
        }

        if (string.IsNullOrWhiteSpace(manufacturerPartNumber))
        {
            throw new ArgumentException("Manufacturer part number is required.", nameof(manufacturerPartNumber));
        }

        if (quantityPerAssembly <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantityPerAssembly),
                quantityPerAssembly,
                "Quantity per assembly must be greater than zero.");
        }

        LineId = lineId.Trim();
        ManufacturerPartNumber = manufacturerPartNumber.Trim();
        QuantityPerAssembly = quantityPerAssembly;
    }

    public string LineId { get; }

    public string ManufacturerPartNumber { get; }

    public int QuantityPerAssembly { get; }
}
