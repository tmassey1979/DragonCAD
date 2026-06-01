using DragonCAD.Sourcing.Catalog;

namespace DragonCAD.Sourcing.Bom;

public static class BomCostRollupCalculator
{
    public static BomCostRollup RollUp(
        IEnumerable<BomComponentQuantity> components,
        IEnumerable<NormalizedCatalogListing> listings)
    {
        ArgumentNullException.ThrowIfNull(components);
        ArgumentNullException.ThrowIfNull(listings);

        var listingsByPartNumber = listings
            .GroupBy(listing => NormalizePartNumber(listing.ManufacturerPartNumber))
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.OrdinalIgnoreCase);

        var lines = new List<BomCostRollupLine>();
        var diagnostics = new List<BomCostRollupDiagnostic>();

        foreach (var component in components)
        {
            var partNumber = NormalizePartNumber(component.ManufacturerPartNumber);
            if (!listingsByPartNumber.TryGetValue(partNumber, out var matchingListings) || matchingListings.Length == 0)
            {
                lines.Add(new BomCostRollupLine(component, [], SelectedOffer: null));
                diagnostics.Add(MissingCatalogSource(component));
                continue;
            }

            var providerOffers = matchingListings
                .Select(listing => CreateOffer(component, listing))
                .GroupBy(offer => offer.ProviderName, StringComparer.OrdinalIgnoreCase)
                .Select(group => SortBestOffers(group).First())
                .OrderBy(offer => offer.ProviderName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(offer => offer.VendorSku, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            lines.Add(new BomCostRollupLine(component, providerOffers, SortBestOffers(providerOffers).First()));
        }

        var selectedOffers = lines
            .Select(line => line.SelectedOffer)
            .OfType<BomProviderOffer>()
            .ToArray();

        return new BomCostRollup(
            Sum(selectedOffers.Select(offer => offer.ExtendedCost)),
            lines,
            diagnostics
                .OrderBy(diagnostic => diagnostic.Reference, StringComparer.OrdinalIgnoreCase)
                .ThenBy(diagnostic => diagnostic.ManufacturerPartNumber, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            BuildProviderSummaries(selectedOffers));
    }

    private static BomProviderOffer CreateOffer(BomComponentQuantity component, NormalizedCatalogListing listing)
    {
        var priceBreak = listing.PriceLadder.FindBestBreakFor(component.Quantity);
        var extendedCost = new Money(priceBreak.UnitPrice.Amount * component.Quantity, priceBreak.UnitPrice.CurrencyCode);

        return new BomProviderOffer(
            listing.ProviderName,
            listing.VendorSku,
            listing.ManufacturerPartNumber,
            component.Quantity,
            priceBreak.Quantity,
            priceBreak.UnitPrice,
            extendedCost,
            listing.StockQuantity);
    }

    private static IReadOnlyList<BomProviderSummary> BuildProviderSummaries(IEnumerable<BomProviderOffer> selectedOffers)
    {
        return selectedOffers
            .GroupBy(offer => offer.ProviderName, StringComparer.OrdinalIgnoreCase)
            .Select(group => new BomProviderSummary(
                group.First().ProviderName,
                group.Count(),
                Sum(group.Select(offer => offer.ExtendedCost))))
            .OrderBy(summary => summary.ProviderName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<BomProviderOffer> SortBestOffers(IEnumerable<BomProviderOffer> offers)
    {
        return offers
            .OrderByDescending(offer => offer.IsFullyAvailable)
            .ThenBy(offer => offer.ExtendedCost.Amount)
            .ThenBy(offer => offer.UnitPrice.Amount)
            .ThenByDescending(offer => offer.StockQuantity ?? int.MaxValue)
            .ThenBy(offer => offer.ProviderName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(offer => offer.VendorSku, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static Money Sum(IEnumerable<Money> amounts)
    {
        Money? total = null;
        foreach (var amount in amounts)
        {
            if (total is null)
            {
                total = amount;
                continue;
            }

            if (!string.Equals(total.Value.CurrencyCode, amount.CurrencyCode, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("BOM cost rollup cannot total catalog listings with mixed currencies.");
            }

            total = new Money(total.Value.Amount + amount.Amount, total.Value.CurrencyCode);
        }

        return total ?? Money.Usd(0);
    }

    private static BomCostRollupDiagnostic MissingCatalogSource(BomComponentQuantity component)
    {
        return new BomCostRollupDiagnostic(
            BomCostRollupDiagnosticCode.MissingCatalogSource,
            component.Reference,
            component.ManufacturerPartNumber,
            component.Quantity,
            $"No normalized catalog listing found for {component.ManufacturerPartNumber}.");
    }

    private static string NormalizePartNumber(string partNumber)
    {
        return partNumber.Trim().ToUpperInvariant();
    }
}
