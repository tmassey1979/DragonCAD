using System.Text.Json;

namespace DragonCAD.App.Marketplace.Sync.InUse;

public sealed class InUseVendorCatalogFreshnessPolicyStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly string path;

    public InUseVendorCatalogFreshnessPolicyStore(string path)
    {
        this.path = string.IsNullOrWhiteSpace(path)
            ? throw new ArgumentException("Policy path is required.", nameof(path))
            : path;
    }

    public InUseVendorCatalogFreshnessPolicy Load()
    {
        if (!File.Exists(path))
        {
            return InUseVendorCatalogFreshnessPolicy.Default;
        }

        using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        InUseVendorCatalogFreshnessPolicyDto? dto = JsonSerializer.Deserialize<InUseVendorCatalogFreshnessPolicyDto>(stream, Options);
        if (dto is null)
        {
            return InUseVendorCatalogFreshnessPolicy.Default;
        }

        return new InUseVendorCatalogFreshnessPolicy(
            TimeSpan.FromHours(dto.DefaultFreshnessHours),
            dto.ProviderFreshnessHours.ToDictionary(pair => pair.Key, pair => TimeSpan.FromHours(pair.Value), StringComparer.OrdinalIgnoreCase));
    }

    public void Save(InUseVendorCatalogFreshnessPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        InUseVendorCatalogFreshnessPolicyDto dto = new(
            policy.DefaultFreshnessWindow.TotalHours,
            policy.ProviderFreshnessWindows
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .ToDictionary(pair => pair.Key, pair => pair.Value.TotalHours, StringComparer.Ordinal));
        string json = JsonSerializer.Serialize(dto, Options);
        File.WriteAllText(path, json);
    }

    private sealed record InUseVendorCatalogFreshnessPolicyDto(
        double DefaultFreshnessHours,
        IReadOnlyDictionary<string, double> ProviderFreshnessHours);
}
