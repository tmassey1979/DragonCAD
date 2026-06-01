namespace DragonCAD.Sourcing.Bom;

public sealed record BomComponentQuantity
{
    public BomComponentQuantity(string reference, string manufacturerPartNumber, int quantity)
    {
        Reference = RequireText(reference, nameof(reference));
        ManufacturerPartNumber = RequireText(manufacturerPartNumber, nameof(manufacturerPartNumber));

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Quantity must be greater than zero.");
        }

        Quantity = quantity;
    }

    public string Reference { get; }

    public string ManufacturerPartNumber { get; }

    public int Quantity { get; }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return string.Join(' ', value.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
    }
}
