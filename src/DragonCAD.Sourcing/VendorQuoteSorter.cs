namespace DragonCAD.Sourcing;

public static class VendorQuoteSorter
{
    public static IReadOnlyList<NormalizedVendorQuote> SortBestFirst(IEnumerable<NormalizedVendorQuote> quotes)
    {
        ArgumentNullException.ThrowIfNull(quotes);

        return quotes
            .OrderByDescending(quote => quote.IsInStock)
            .ThenBy(quote => quote.UnitPrice.Amount)
            .ThenBy(quote => quote.MinimumOrderQuantity)
            .ThenBy(quote => quote.VendorName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(quote => quote.VendorPartNumber, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
