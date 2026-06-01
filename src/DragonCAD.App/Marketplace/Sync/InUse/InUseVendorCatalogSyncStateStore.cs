using System.Text.Json;

namespace DragonCAD.App.Marketplace.Sync.InUse;

public sealed class InUseVendorCatalogSyncStateStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly string path;

    public InUseVendorCatalogSyncStateStore(string path)
    {
        this.path = string.IsNullOrWhiteSpace(path)
            ? throw new ArgumentException("State path is required.", nameof(path))
            : path;
    }

    public IReadOnlyList<InUseVendorCatalogSyncState> Load()
    {
        if (!File.Exists(path))
        {
            return [];
        }

        using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using StreamReader reader = new(stream);
        string json = reader.ReadToEnd();
        InUseVendorCatalogSyncState[]? states = JsonSerializer.Deserialize<InUseVendorCatalogSyncState[]>(json, Options);
        return Sort(states ?? []);
    }

    public void Save(IEnumerable<InUseVendorCatalogSyncState> states)
    {
        ArgumentNullException.ThrowIfNull(states);

        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonSerializer.Serialize(Sort(states), Options);
        File.WriteAllText(path, json);
    }

    private static InUseVendorCatalogSyncState[] Sort(IEnumerable<InUseVendorCatalogSyncState> states) =>
        states
            .OrderBy(state => state.ComponentId, StringComparer.Ordinal)
            .ThenBy(state => state.ProviderName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(state => state.Query, StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
