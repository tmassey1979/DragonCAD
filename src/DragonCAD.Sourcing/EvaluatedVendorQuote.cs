namespace DragonCAD.Sourcing;

public sealed record EvaluatedVendorQuote(
    NormalizedVendorQuote Quote,
    int RequestedBuildQuantity,
    int PurchaseQuantity,
    Money UnitPrice,
    Money ExtendedCost)
{
    public bool IsFullyAvailable => Quote.QuantityAvailable >= PurchaseQuantity;
}
