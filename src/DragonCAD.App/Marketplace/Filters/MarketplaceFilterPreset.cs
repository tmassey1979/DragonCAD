using System.Collections.ObjectModel;

namespace DragonCAD.App.Marketplace.Filters;

public sealed record MarketplaceFilterPreset(
    string Name,
    string Provider,
    string Category,
    string SearchText,
    bool RequiresDatasheet,
    bool InStockOnly)
{
    public static MarketplaceFilterPreset All { get; } = new(
        Name: "All",
        Provider: "All",
        Category: "All",
        SearchText: "",
        RequiresDatasheet: false,
        InStockOnly: false);
}

public sealed class MarketplaceSavedFilterPresetStore
{
    private readonly List<MarketplaceFilterPreset> presets = [];
    private readonly ObservableCollection<MarketplaceFilterPreset> orderedPresets = [];

    public IReadOnlyList<MarketplaceFilterPreset> Presets => orderedPresets;

    public MarketplaceFilterPreset Save(
        string name,
        string provider,
        string category,
        string searchText,
        bool requiresDatasheet,
        bool inStockOnly)
    {
        MarketplaceFilterPreset preset = new(
            Name: NormalizeRequired(name, nameof(name)),
            Provider: NormalizeFilter(provider),
            Category: NormalizeFilter(category),
            SearchText: (searchText ?? "").Trim(),
            RequiresDatasheet: requiresDatasheet,
            InStockOnly: inStockOnly);

        int existingIndex = presets.FindIndex(existing => string.Equals(existing.Name, preset.Name, StringComparison.OrdinalIgnoreCase));
        if (existingIndex >= 0)
        {
            presets[existingIndex] = preset;
        }
        else
        {
            presets.Add(preset);
        }

        RefreshOrderedPresets();
        return preset;
    }

    private void RefreshOrderedPresets()
    {
        orderedPresets.Clear();
        foreach (MarketplaceFilterPreset preset in presets.OrderBy(preset => preset.Name, StringComparer.OrdinalIgnoreCase).ThenBy(preset => preset.Name, StringComparer.Ordinal))
        {
            orderedPresets.Add(preset);
        }
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        string normalized = (value ?? "").Trim();
        return normalized.Length == 0 ? throw new ArgumentException("Preset name is required.", parameterName) : normalized;
    }

    private static string NormalizeFilter(string value)
    {
        string normalized = (value ?? "").Trim();
        return normalized.Length == 0 ? "All" : normalized;
    }
}
