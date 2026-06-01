using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace DragonCAD.App.Marketplace;

public sealed class MarketplaceBrowserViewModel : INotifyPropertyChanged
{
    private static readonly string[] MajorProviderFilterOptions =
    [
        "All",
        "SparkFun",
        "Adafruit",
        "Digi-Key",
        "Mouser",
        "Jameco"
    ];

    private readonly IReadOnlyList<MarketplaceComponentRow> allRows;
    private readonly IReadOnlyList<string> categoryFilterOptions;
    private string searchText = "";
    private string selectedProviderFilter = "All";
    private string selectedCategoryFilter = "All";
    private MarketplaceComponentRow? selectedComponent;

    private MarketplaceBrowserViewModel(IReadOnlyList<MarketplaceComponentRow> rows)
    {
        allRows = rows;
        categoryFilterOptions = BuildCategoryFilterOptions(rows);
        Components = new ObservableCollection<MarketplaceComponentRow>(rows);
        selectedComponent = Components.FirstOrDefault();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<MarketplaceComponentRow> Components { get; }

    public IReadOnlyList<string> ProviderFilterOptions => MajorProviderFilterOptions;

    public IReadOnlyList<string> CategoryFilterOptions => categoryFilterOptions;

    public int VisibleComponentCount => Components.Count;

    public int TotalComponentCount => allRows.Count;

    public string SearchSummary
    {
        get
        {
            if (VisibleComponentCount == 0)
            {
                return TotalComponentCount == 0
                    ? "No marketplace components available"
                    : "No components match the current filters";
            }

            return $"Showing {VisibleComponentCount.ToString("N0", CultureInfo.CurrentCulture)} of {TotalComponentCount.ToString("N0", CultureInfo.CurrentCulture)} components";
        }
    }

    public string EmptyStateMessage
    {
        get
        {
            if (VisibleComponentCount > 0)
            {
                return "";
            }

            return TotalComponentCount == 0
                ? "No marketplace components are available yet."
                : "No marketplace components match your search and filters.";
        }
    }

    public string SearchText
    {
        get => searchText;
        set
        {
            string nextValue = value ?? "";
            if (searchText == nextValue)
            {
                return;
            }

            searchText = nextValue;
            ApplyFilters();
            OnPropertyChanged();
        }
    }

    public string SelectedProviderFilter
    {
        get => selectedProviderFilter;
        set
        {
            string nextValue = string.IsNullOrWhiteSpace(value) ? "All" : value;
            if (selectedProviderFilter == nextValue)
            {
                return;
            }

            selectedProviderFilter = nextValue;
            ApplyFilters();
            OnPropertyChanged();
        }
    }

    public string SelectedCategoryFilter
    {
        get => selectedCategoryFilter;
        set
        {
            string nextValue = string.IsNullOrWhiteSpace(value) ? "All" : value;
            if (selectedCategoryFilter == nextValue)
            {
                return;
            }

            selectedCategoryFilter = nextValue;
            ApplyFilters();
            OnPropertyChanged();
        }
    }

    public MarketplaceComponentRow? SelectedComponent
    {
        get => selectedComponent;
        set
        {
            if (selectedComponent == value)
            {
                return;
            }

            selectedComponent = value;
            OnPropertyChanged();
        }
    }

    public static MarketplaceBrowserViewModel FromRows(IEnumerable<MarketplaceComponentRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        MarketplaceComponentRow[] materializedRows = rows.ToArray();

        return new MarketplaceBrowserViewModel(materializedRows);
    }

    private void ApplyFilters()
    {
        IEnumerable<MarketplaceComponentRow> rows = allRows;

        if (selectedProviderFilter != "All")
        {
            rows = rows.Where(row => string.Equals(row.Provider, selectedProviderFilter, StringComparison.Ordinal));
        }

        if (selectedCategoryFilter != "All")
        {
            rows = rows.Where(row => string.Equals(row.Category, selectedCategoryFilter, StringComparison.Ordinal));
        }

        string trimmedSearchText = searchText.Trim();
        if (trimmedSearchText.Length > 0)
        {
            rows = rows.Where(row => row.Matches(trimmedSearchText));
        }

        Components.Clear();
        foreach (MarketplaceComponentRow row in rows)
        {
            Components.Add(row);
        }

        SelectedComponent = Components.FirstOrDefault();
        OnSearchStateChanged();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private void OnSearchStateChanged()
    {
        OnPropertyChanged(nameof(VisibleComponentCount));
        OnPropertyChanged(nameof(TotalComponentCount));
        OnPropertyChanged(nameof(SearchSummary));
        OnPropertyChanged(nameof(EmptyStateMessage));
    }

    private static IReadOnlyList<string> BuildCategoryFilterOptions(IEnumerable<MarketplaceComponentRow> rows) =>
        new[] { "All" }
            .Concat(rows.Select(row => row.Category).Where(category => category.Length > 0).Distinct(StringComparer.Ordinal).OrderBy(category => category, StringComparer.Ordinal))
            .ToArray();
}

public sealed record MarketplaceComponentRow(
    string Provider,
    string Category,
    string DisplayName,
    string Manufacturer,
    string ManufacturerPartNumber,
    string CanonicalComponentId,
    string DuplicateOfComponentId,
    string DatasheetUrl,
    int StockQuantity,
    decimal? MinimumUnitPriceUsd)
{
    public bool IsCanonical => string.IsNullOrWhiteSpace(DuplicateOfComponentId);

    public string CanonicalBadge => IsCanonical ? "Canonical" : $"Duplicate of {DuplicateOfComponentId}";

    public bool HasDatasheet => !string.IsNullOrWhiteSpace(DatasheetUrl);

    public string DatasheetStatus => HasDatasheet ? "Datasheet ready" : "Datasheet needed";

    public string StockPriceSummary
    {
        get
        {
            if (StockQuantity <= 0 && MinimumUnitPriceUsd is null)
            {
                return "Out of stock / price unavailable";
            }

            if (StockQuantity <= 0)
            {
                return $"Out of stock from {FormatPrice(MinimumUnitPriceUsd.GetValueOrDefault())}";
            }

            if (MinimumUnitPriceUsd is null)
            {
                return $"{StockQuantity.ToString("N0", CultureInfo.CurrentCulture)} in stock / price unavailable";
            }

            return $"{StockQuantity.ToString("N0", CultureInfo.CurrentCulture)} in stock from {FormatPrice(MinimumUnitPriceUsd.Value)}";
        }
    }

    public bool CanAddToBom => StockQuantity > 0 && MinimumUnitPriceUsd is not null;

    public string BomActionLabel => CanAddToBom ? "Add to BOM" : "Unavailable for BOM";

    public bool Matches(string searchText) =>
        Contains(Provider, searchText) ||
        Contains(Category, searchText) ||
        Contains(DisplayName, searchText) ||
        Contains(Manufacturer, searchText) ||
        Contains(ManufacturerPartNumber, searchText) ||
        Contains(CanonicalComponentId, searchText) ||
        Contains(DuplicateOfComponentId, searchText);

    private static bool Contains(string value, string searchText) =>
        value.Contains(searchText, StringComparison.OrdinalIgnoreCase);

    private static string FormatPrice(decimal price) =>
        price.ToString("$0.00##", CultureInfo.InvariantCulture);
}
