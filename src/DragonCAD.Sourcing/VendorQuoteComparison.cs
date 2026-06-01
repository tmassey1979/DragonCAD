namespace DragonCAD.Sourcing;

public static class VendorQuoteComparison
{
    public static EvaluatedVendorQuote Evaluate(VendorQuoteOffer offer, int requestedBuildQuantity)
    {
        ArgumentNullException.ThrowIfNull(offer);

        if (requestedBuildQuantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(requestedBuildQuantity),
                requestedBuildQuantity,
                "Requested build quantity must be greater than zero.");
        }

        var purchaseQuantity = Math.Max(requestedBuildQuantity, offer.Quote.MinimumOrderQuantity);
        var priceBreak = offer.PriceLadder.FindBestBreakFor(purchaseQuantity);
        var extendedCost = new Money(priceBreak.UnitPrice.Amount * purchaseQuantity, priceBreak.UnitPrice.CurrencyCode);

        return new EvaluatedVendorQuote(
            offer.Quote,
            requestedBuildQuantity,
            purchaseQuantity,
            priceBreak.UnitPrice,
            extendedCost);
    }

    public static IReadOnlyList<EvaluatedVendorQuote> SortBestFirst(
        IEnumerable<VendorQuoteOffer> offers,
        int requestedBuildQuantity)
    {
        ArgumentNullException.ThrowIfNull(offers);

        return offers
            .Select(offer => Evaluate(offer, requestedBuildQuantity))
            .OrderByDescending(evaluated => evaluated.IsFullyAvailable)
            .ThenByDescending(evaluated => evaluated.Quote.IsInStock)
            .ThenBy(evaluated => evaluated.ExtendedCost.Amount)
            .ThenByDescending(evaluated => evaluated.Quote.QuantityAvailable)
            .ThenBy(evaluated => evaluated.Quote.VendorName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(evaluated => evaluated.Quote.VendorPartNumber, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
