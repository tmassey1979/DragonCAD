namespace DragonCAD.App.Marketplace.Filters;

public static class MarketplaceFilterPresetApplicator
{
    public static IReadOnlyList<MarketplaceComponentRow> Apply(
        IEnumerable<MarketplaceComponentRow> rows,
        MarketplaceFilterPreset preset)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(preset);

        IEnumerable<MarketplaceComponentRow> filteredRows = rows;

        if (!IsAll(preset.Provider))
        {
            filteredRows = filteredRows.Where(row => string.Equals(row.Provider, preset.Provider, StringComparison.Ordinal));
        }

        if (!IsAll(preset.Category))
        {
            filteredRows = filteredRows.Where(row => string.Equals(row.Category, preset.Category, StringComparison.Ordinal));
        }

        string searchText = preset.SearchText.Trim();
        if (searchText.Length > 0)
        {
            filteredRows = filteredRows.Where(row => row.Matches(searchText));
        }

        if (preset.InStockOnly)
        {
            filteredRows = filteredRows.Where(row => row.StockQuantity > 0);
        }

        if (preset.RequiresDatasheet)
        {
            filteredRows = filteredRows.Where(row => row.HasDatasheet);
        }

        return filteredRows.ToArray();
    }

    private static bool IsAll(string value) =>
        string.Equals(value, "All", StringComparison.Ordinal);
}
