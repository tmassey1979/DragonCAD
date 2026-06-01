using System.Collections.ObjectModel;

namespace DragonCAD.Sourcing.Catalog;

public sealed record NormalizedCatalogListing
{
    public NormalizedCatalogListing(
        string providerName,
        string vendorSku,
        string manufacturerPartNumber,
        string manufacturer,
        string description,
        PriceLadder priceLadder,
        int? stockQuantity,
        Uri? datasheetUrl,
        Uri? productUrl,
        IReadOnlyDictionary<string, string> fields,
        CatalogProviderCapabilities sourceCapabilities)
    {
        ProviderName = RequireText(providerName, nameof(providerName));
        VendorSku = RequireText(vendorSku, nameof(vendorSku));
        ManufacturerPartNumber = RequireText(manufacturerPartNumber, nameof(manufacturerPartNumber));
        Manufacturer = RequireText(manufacturer, nameof(manufacturer));
        Description = NormalizeText(description);
        PriceLadder = priceLadder ?? throw new ArgumentNullException(nameof(priceLadder));
        StockQuantity = stockQuantity;
        DatasheetUrl = datasheetUrl;
        ProductUrl = productUrl;
        Fields = new ReadOnlyDictionary<string, string>(NormalizeFields(fields));
        SourceCapabilities = sourceCapabilities;
    }

    public string ProviderName { get; }

    public string VendorSku { get; }

    public string ManufacturerPartNumber { get; }

    public string Manufacturer { get; }

    public string Description { get; }

    public PriceLadder PriceLadder { get; }

    public int? StockQuantity { get; }

    public Uri? DatasheetUrl { get; }

    public Uri? ProductUrl { get; }

    public IReadOnlyDictionary<string, string> Fields { get; }

    public CatalogProviderCapabilities SourceCapabilities { get; }

    private static string RequireText(string value, string parameterName)
    {
        var normalized = NormalizeText(value);
        if (normalized.Length == 0)
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return normalized;
    }

    private static string NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : string.Join(' ', value.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
    }

    private static Dictionary<string, string> NormalizeFields(IReadOnlyDictionary<string, string>? fields)
    {
        var normalized = new Dictionary<string, string>(StringComparer.Ordinal);
        if (fields is null)
        {
            return normalized;
        }

        foreach (var pair in fields)
        {
            var key = NormalizeText(pair.Key);
            var value = NormalizeText(pair.Value);
            if (key.Length > 0 && value.Length > 0)
            {
                normalized[key] = value;
            }
        }

        return normalized;
    }
}
