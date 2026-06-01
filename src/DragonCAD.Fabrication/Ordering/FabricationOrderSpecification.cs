namespace DragonCAD.Fabrication.Ordering;

public sealed record FabricationOrderSpecification
{
    private FabricationOrderSpecification(int quantity, int layerCount)
    {
        Quantity = quantity;
        LayerCount = layerCount;
    }

    public int Quantity { get; }

    public int LayerCount { get; }

    public static FabricationOrderSpecification Create(int quantity, int layerCount)
    {
        if (quantity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Quantity must be at least 1.");
        }

        if (layerCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(layerCount), layerCount, "Layer count must be at least 1.");
        }

        return new FabricationOrderSpecification(quantity, layerCount);
    }
}
