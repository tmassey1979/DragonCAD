using DragonCAD.App.Marketplace;
using DragonCAD.App.Marketplace.Cart;
using DragonCAD.Sourcing;
using DragonCAD.Sourcing.Bom;
using DragonCAD.Sourcing.Catalog;

namespace DragonCAD.App.Marketplace.Bom;

public static class MarketplaceBomCostRollupFactory
{
    public static MarketplaceBomCostRollupViewModel FromCart(
        MarketplaceCartViewModel cart,
        IEnumerable<MarketplaceComponentRow> catalogRows)
    {
        ArgumentNullException.ThrowIfNull(cart);
        ArgumentNullException.ThrowIfNull(catalogRows);

        BomCostRollup rollup = BomCostRollupCalculator.RollUp(
            BuildComponentQuantities(cart.Lines),
            BuildListings(catalogRows));

        return MarketplaceBomCostRollupViewModel.FromRollup(rollup);
    }

    private static IReadOnlyList<BomComponentQuantity> BuildComponentQuantities(
        IEnumerable<MarketplaceCartLine> cartLines)
    {
        return cartLines
            .GroupBy(line => NormalizePartNumber(line.ManufacturerPartNumber), StringComparer.OrdinalIgnoreCase)
            .Select(group => new BomComponentQuantity(
                BuildReference(group, group.First().ManufacturerPartNumber),
                group.First().ManufacturerPartNumber,
                group.Sum(line => line.Quantity)))
            .OrderBy(component => component.Reference, StringComparer.OrdinalIgnoreCase)
            .ThenBy(component => component.ManufacturerPartNumber, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<NormalizedCatalogListing> BuildListings(
        IEnumerable<MarketplaceComponentRow> catalogRows)
    {
        return catalogRows
            .Where(row => row.MinimumUnitPriceUsd is not null)
            .OrderBy(row => row.Provider, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.ManufacturerPartNumber, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.CanonicalComponentId, StringComparer.OrdinalIgnoreCase)
            .Select(ToListing)
            .ToArray();
    }

    private static NormalizedCatalogListing ToListing(MarketplaceComponentRow row)
    {
        var fields = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Category"] = row.Category,
            ["CanonicalComponentId"] = row.CanonicalComponentId,
        };

        if (!string.IsNullOrWhiteSpace(row.DuplicateOfComponentId))
        {
            fields["DuplicateOfComponentId"] = row.DuplicateOfComponentId;
        }

        return new NormalizedCatalogListing(
            row.Provider,
            row.ManufacturerPartNumber,
            row.ManufacturerPartNumber,
            row.Manufacturer,
            row.DisplayName,
            PriceLadder.Normalize(
                [
                    new QuantityPriceBreak(1, Money.Usd(row.MinimumUnitPriceUsd.GetValueOrDefault()))
                ]),
            row.StockQuantity,
            TryCreateAbsoluteUri(row.DatasheetUrl),
            productUrl: null,
            fields,
            CatalogProviderCapabilities.Manual);
    }

    private static string BuildReference(IEnumerable<MarketplaceCartLine> lines, string fallback)
    {
        string reference = string.Join(
            ", ",
            lines
                .Select(line => line.CanonicalComponentId)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase));

        return reference.Length == 0 ? fallback : reference;
    }

    private static Uri? TryCreateAbsoluteUri(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) ? uri : null;

    private static string NormalizePartNumber(string manufacturerPartNumber) =>
        manufacturerPartNumber.Trim().ToUpperInvariant();
}
