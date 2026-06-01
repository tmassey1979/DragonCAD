namespace DragonCAD.Sourcing;

public sealed record QuantityPriceBreak
{
    public QuantityPriceBreak(int quantity, Money unitPrice)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Quantity must be greater than zero.");
        }

        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public int Quantity { get; }

    public Money UnitPrice { get; }
}
