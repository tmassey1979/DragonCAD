namespace DragonCAD.Sourcing;

public static class BomRunCostEstimator
{
    public static BomRunCostEstimate Estimate(
        IEnumerable<SourcingBomLine> bomLines,
        IEnumerable<VendorQuoteOffer> quoteOffers,
        int buildQuantity)
    {
        ArgumentNullException.ThrowIfNull(bomLines);
        ArgumentNullException.ThrowIfNull(quoteOffers);

        if (buildQuantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(buildQuantity),
                buildQuantity,
                "Build quantity must be greater than zero.");
        }

        var offersByPartNumber = quoteOffers
            .GroupBy(offer => NormalizePartNumber(offer.Quote.ManufacturerPartNumber))
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.OrdinalIgnoreCase);

        var estimateLines = new List<BomCostEstimateLine>();
        var diagnostics = new List<MissingQuoteDiagnostic>();
        Money? total = null;

        foreach (var bomLine in bomLines)
        {
            var requiredQuantity = checked(bomLine.QuantityPerAssembly * buildQuantity);
            var partNumber = NormalizePartNumber(bomLine.ManufacturerPartNumber);

            if (!offersByPartNumber.TryGetValue(partNumber, out var matchingOffers) || matchingOffers.Length == 0)
            {
                diagnostics.Add(MissingQuoteDiagnostic.NoVendorQuote(bomLine, requiredQuantity));
                continue;
            }

            var selectedQuote = VendorQuoteComparison.SortBestFirst(matchingOffers, requiredQuantity)[0];
            estimateLines.Add(new BomCostEstimateLine(bomLine, requiredQuantity, selectedQuote));
            total = Add(total, selectedQuote.ExtendedCost);
        }

        return new BomRunCostEstimate(
            buildQuantity,
            total ?? Money.Usd(0),
            estimateLines,
            diagnostics
                .OrderBy(diagnostic => diagnostic.BomLineId, StringComparer.OrdinalIgnoreCase)
                .ToArray());
    }

    private static Money Add(Money? total, Money addend)
    {
        if (total is null)
        {
            return addend;
        }

        if (!string.Equals(total.Value.CurrencyCode, addend.CurrencyCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("BOM cost estimate cannot total quotes with mixed currencies.");
        }

        return new Money(total.Value.Amount + addend.Amount, total.Value.CurrencyCode);
    }

    private static string NormalizePartNumber(string partNumber)
    {
        return partNumber.Trim().ToUpperInvariant();
    }
}
