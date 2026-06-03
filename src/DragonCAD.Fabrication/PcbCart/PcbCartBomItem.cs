namespace DragonCAD.Fabrication.PcbCart;

public sealed record PcbCartBomItem
{
    private PcbCartBomItem(string designator, int quantity, string? manufacturerPartNumber)
    {
        Designator = designator;
        Quantity = quantity;
        ManufacturerPartNumber = manufacturerPartNumber;
    }

    public string Designator { get; }

    public int Quantity { get; }

    public string? ManufacturerPartNumber { get; }

    public static PcbCartBomItem Create(string designator, int quantity, string? manufacturerPartNumber)
    {
        if (string.IsNullOrWhiteSpace(designator))
        {
            throw new ArgumentException("Designator must not be empty.", nameof(designator));
        }

        if (quantity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Quantity must be at least 1.");
        }

        return new PcbCartBomItem(
            designator.Trim(),
            quantity,
            string.IsNullOrWhiteSpace(manufacturerPartNumber) ? null : manufacturerPartNumber.Trim());
    }
}
