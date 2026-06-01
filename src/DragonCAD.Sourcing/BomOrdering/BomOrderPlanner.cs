namespace DragonCAD.Sourcing.BomOrdering;

public static class BomOrderPlanner
{
    public static BomOrderPlan Plan(
        IEnumerable<BomOrderLine> bomLines,
        IEnumerable<BomOrderVendorOffer> offers,
        int buildQuantity)
    {
        ArgumentNullException.ThrowIfNull(bomLines);
        ArgumentNullException.ThrowIfNull(offers);

        if (buildQuantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(buildQuantity), buildQuantity, "Build quantity must be greater than zero.");
        }

        var offersByPart = offers
            .GroupBy(offer => Normalize(offer.ManufacturerPartNumber))
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.OrdinalIgnoreCase);

        var purchaseLines = new List<(string VendorName, BomOrderPurchaseLine Line)>();
        var diagnostics = new List<BomOrderDiagnostic>();
        Money? total = null;

        foreach (var bomLine in bomLines.OrderBy(line => line.LineId, StringComparer.OrdinalIgnoreCase))
        {
            var requiredQuantity = checked(bomLine.QuantityPerAssembly * buildQuantity);
            var remainingQuantity = requiredQuantity;

            if (offersByPart.TryGetValue(Normalize(bomLine.ManufacturerPartNumber), out var matchingOffers))
            {
                foreach (var allocation in Allocate(bomLine, matchingOffers, requiredQuantity))
                {
                    remainingQuantity -= allocation.Line.RequiredQuantity;
                    purchaseLines.Add(allocation);
                    total = Add(total, allocation.Line.ExtendedCost);

                    if (remainingQuantity == 0)
                    {
                        break;
                    }
                }
            }

            if (remainingQuantity > 0)
            {
                diagnostics.Add(BomOrderDiagnostic.Unavailable(bomLine, remainingQuantity));
            }
        }

        var vendorOrders = purchaseLines
            .GroupBy(line => line.VendorName, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var orderedLines = group
                    .Select(item => item.Line)
                    .OrderBy(line => line.ManufacturerPartNumber, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(line => line.VendorPartNumber, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                return new BomVendorOrder(group.Key, Sum(orderedLines.Select(line => line.ExtendedCost)), orderedLines);
            })
            .ToArray();

        return new BomOrderPlan(
            buildQuantity,
            total ?? Money.Usd(0),
            vendorOrders,
            diagnostics
                .OrderBy(diagnostic => diagnostic.BomLineId, StringComparer.OrdinalIgnoreCase)
                .ToArray());
    }

    private static IEnumerable<(string VendorName, BomOrderPurchaseLine Line)> Allocate(
        BomOrderLine bomLine,
        IEnumerable<BomOrderVendorOffer> offers,
        int requiredQuantity)
    {
        var remainingQuantity = requiredQuantity;
        var candidates = offers
            .Select(offer => TryCreateCandidate(offer, remainingQuantity))
            .Where(candidate => candidate is not null)
            .Select(candidate => candidate!)
            .OrderBy(candidate => candidate.UnitPrice.Amount)
            .ThenBy(candidate => candidate.UnitPrice.CurrencyCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.PurchaseQuantity)
            .ThenBy(candidate => candidate.Offer.VendorName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.Offer.VendorPartNumber, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var candidate in candidates)
        {
            if (remainingQuantity == 0)
            {
                yield break;
            }

            var allocatedQuantity = Math.Min(candidate.RequiredQuantity, remainingQuantity);
            var purchaseQuantity = RoundPurchaseQuantity(
                allocatedQuantity,
                candidate.Offer.MinimumOrderQuantity,
                candidate.Offer.OrderMultiple);

            if (purchaseQuantity > candidate.Offer.QuantityAvailable)
            {
                continue;
            }

            var priceBreak = candidate.Offer.PriceLadder.FindBestBreakFor(purchaseQuantity);
            var extendedCost = new Money(
                priceBreak.UnitPrice.Amount * purchaseQuantity,
                priceBreak.UnitPrice.CurrencyCode);

            remainingQuantity -= allocatedQuantity;
            yield return (
                candidate.Offer.VendorName,
                new BomOrderPurchaseLine(
                    bomLine.LineId,
                    bomLine.ManufacturerPartNumber,
                    candidate.Offer.VendorPartNumber,
                    allocatedQuantity,
                    purchaseQuantity,
                    priceBreak.UnitPrice,
                    extendedCost));
        }
    }

    private static BomOrderCandidate? TryCreateCandidate(BomOrderVendorOffer offer, int remainingQuantity)
    {
        var allocatableQuantity = Math.Min(remainingQuantity, offer.QuantityAvailable);
        while (allocatableQuantity > 0)
        {
            var purchaseQuantity = RoundPurchaseQuantity(
                allocatableQuantity,
                offer.MinimumOrderQuantity,
                offer.OrderMultiple);

            if (purchaseQuantity <= offer.QuantityAvailable)
            {
                var priceBreak = offer.PriceLadder.FindBestBreakFor(purchaseQuantity);
                return new BomOrderCandidate(offer, allocatableQuantity, purchaseQuantity, priceBreak.UnitPrice);
            }

            allocatableQuantity--;
        }

        return null;
    }

    private static int RoundPurchaseQuantity(int requiredQuantity, int minimumOrderQuantity, int orderMultiple)
    {
        var purchaseQuantity = Math.Max(requiredQuantity, minimumOrderQuantity);
        var remainder = purchaseQuantity % orderMultiple;
        return remainder == 0
            ? purchaseQuantity
            : checked(purchaseQuantity + orderMultiple - remainder);
    }

    private static Money Sum(IEnumerable<Money> amounts)
    {
        Money? total = null;
        foreach (var amount in amounts)
        {
            total = Add(total, amount);
        }

        return total ?? Money.Usd(0);
    }

    private static Money Add(Money? total, Money addend)
    {
        if (total is null)
        {
            return addend;
        }

        if (!string.Equals(total.Value.CurrencyCode, addend.CurrencyCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("BOM order plan cannot total mixed currencies.");
        }

        return new Money(total.Value.Amount + addend.Amount, total.Value.CurrencyCode);
    }

    private static string Normalize(string partNumber)
    {
        return partNumber.Trim().ToUpperInvariant();
    }

    private sealed record BomOrderCandidate(
        BomOrderVendorOffer Offer,
        int RequiredQuantity,
        int PurchaseQuantity,
        Money UnitPrice);
}
