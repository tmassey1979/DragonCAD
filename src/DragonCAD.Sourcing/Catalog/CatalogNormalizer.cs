namespace DragonCAD.Sourcing.Catalog;

public static class CatalogNormalizer
{
    public static NormalizedCatalogListing Normalize(VendorCatalogItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return new NormalizedCatalogListing(
            providerName: item.ProviderName,
            vendorSku: item.VendorSku,
            manufacturerPartNumber: item.ManufacturerPartNumber,
            manufacturer: item.Manufacturer,
            description: item.Description,
            priceLadder: PriceLadder.Normalize(item.PriceBreaks),
            stockQuantity: item.StockQuantity,
            datasheetUrl: item.DatasheetUrl,
            productUrl: item.ProductUrl,
            fields: item.Fields,
            sourceCapabilities: item.SourceCapabilities);
    }

    public static CatalogImportResult Normalize(CatalogImportBatch batch)
    {
        ArgumentNullException.ThrowIfNull(batch);

        var diagnostics = new List<CatalogImportDiagnostic>(batch.Diagnostics);
        var listings = new List<NormalizedCatalogListing>(batch.Items.Count);

        AddCapabilityDiagnostics(batch.ProviderName, null, batch.SourceCapabilities, diagnostics);

        foreach (var item in batch.Items)
        {
            AddCapabilityDiagnostics(item.ProviderName, item.VendorSku, item.SourceCapabilities, diagnostics);

            if (string.IsNullOrWhiteSpace(item.ManufacturerPartNumber))
            {
                diagnostics.Add(new CatalogImportDiagnostic(
                    CatalogDiagnosticSeverity.Error,
                    CatalogDiagnosticCodes.MissingManufacturerPartNumber,
                    "Catalog item is missing a manufacturer part number and cannot be deduplicated.",
                    item.ProviderName,
                    item.VendorSku));
                continue;
            }

            listings.Add(Normalize(item));
        }

        return new CatalogImportResult(listings, diagnostics);
    }

    private static void AddCapabilityDiagnostics(
        string providerName,
        string? vendorSku,
        CatalogProviderCapabilities capabilities,
        ICollection<CatalogImportDiagnostic> diagnostics)
    {
        if (capabilities.HasFlag(CatalogProviderCapabilities.Manual))
        {
            diagnostics.Add(new CatalogImportDiagnostic(
                CatalogDiagnosticSeverity.Warning,
                CatalogDiagnosticCodes.ManualReviewRequired,
                "Provider catalog data may require manual review before it can be treated as a live ordering source.",
                providerName,
                vendorSku));
        }

        if (capabilities.HasFlag(CatalogProviderCapabilities.ScrapeRestricted))
        {
            diagnostics.Add(new CatalogImportDiagnostic(
                CatalogDiagnosticSeverity.Warning,
                CatalogDiagnosticCodes.ScrapeRestricted,
                "Provider catalog data is marked scrape-restricted; use an approved feed, API, or manual import path.",
                providerName,
                vendorSku));
        }
    }
}
