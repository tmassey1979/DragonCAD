using System.Collections.ObjectModel;

namespace DragonCAD.Sourcing.Catalog;

public sealed record VendorCatalogItem
{
    public VendorCatalogItem(
        string providerName,
        string vendorSku,
        string manufacturerPartNumber,
        string manufacturer,
        string description,
        IReadOnlyList<QuantityPriceBreak> priceBreaks,
        int? stockQuantity,
        Uri? datasheetUrl,
        Uri? productUrl,
        IReadOnlyDictionary<string, string>? fields = null,
        CatalogProviderCapabilities sourceCapabilities = CatalogProviderCapabilities.Api)
    {
        ProviderName = providerName;
        VendorSku = vendorSku;
        ManufacturerPartNumber = manufacturerPartNumber;
        Manufacturer = manufacturer;
        Description = description;
        PriceBreaks = priceBreaks ?? throw new ArgumentNullException(nameof(priceBreaks));
        StockQuantity = stockQuantity;
        DatasheetUrl = datasheetUrl;
        ProductUrl = productUrl;
        Fields = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(fields ?? new Dictionary<string, string>(), StringComparer.Ordinal));
        SourceCapabilities = sourceCapabilities;
    }

    public string ProviderName { get; }

    public string VendorSku { get; }

    public string ManufacturerPartNumber { get; }

    public string Manufacturer { get; }

    public string Description { get; }

    public IReadOnlyList<QuantityPriceBreak> PriceBreaks { get; }

    public int? StockQuantity { get; }

    public Uri? DatasheetUrl { get; }

    public Uri? ProductUrl { get; }

    public IReadOnlyDictionary<string, string> Fields { get; }

    public CatalogProviderCapabilities SourceCapabilities { get; }
}
