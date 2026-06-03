using DragonCAD.Sourcing.Catalog;
using DragonCAD.Sourcing.Catalog.Jameco;

namespace DragonCAD.Sourcing.Vendors.OpenHardware;

public static class JamecoManualCatalogIngestion
{
    private const string ProviderName = "Jameco";
    private const string Provenance = "manual-csv";

    private static readonly CatalogProviderCapabilities Capabilities =
        CatalogProviderCapabilities.Feed |
        CatalogProviderCapabilities.Manual |
        CatalogProviderCapabilities.ScrapeRestricted;

    public static CatalogImportBatch Parse(string csv, OpenHardwareSourceEntry source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (!string.Equals(source.ProviderName, ProviderName, StringComparison.OrdinalIgnoreCase) ||
            source.Mode != OpenHardwareSourceMode.ManualCsvFeed)
        {
            return new CatalogImportBatch(
                ProviderName,
                Capabilities,
                [],
                [UnsupportedSourceMode(source)]);
        }

        var batch = JamecoCsvManualFeedParser.Parse(csv, source.RetrievedAtUtc);
        return new CatalogImportBatch(
            batch.ProviderName,
            batch.SourceCapabilities,
            batch.Items.Select(item => AddProvenance(item, source)).ToArray(),
            batch.Diagnostics);
    }

    private static VendorCatalogItem AddProvenance(VendorCatalogItem item, OpenHardwareSourceEntry source)
    {
        var fields = new Dictionary<string, string>(item.Fields, StringComparer.Ordinal)
        {
            ["SourceId"] = source.SourceId,
            ["Provenance"] = Provenance,
            ["RetrievedAtUtc"] = source.RetrievedAtUtc.UtcDateTime.ToString("O"),
            ["RefreshAfterUtc"] = source.RefreshAfterUtc.UtcDateTime.ToString("O"),
        };

        if (!string.IsNullOrWhiteSpace(source.ManualFeedName))
        {
            fields["ManualFeedName"] = source.ManualFeedName;
        }

        return new VendorCatalogItem(
            providerName: item.ProviderName,
            vendorSku: item.VendorSku,
            manufacturerPartNumber: item.ManufacturerPartNumber,
            manufacturer: item.Manufacturer,
            description: item.Description,
            priceBreaks: item.PriceBreaks,
            stockQuantity: item.StockQuantity,
            datasheetUrl: item.DatasheetUrl,
            productUrl: item.ProductUrl,
            fields: fields,
            sourceCapabilities: item.SourceCapabilities);
    }

    private static CatalogImportDiagnostic UnsupportedSourceMode(OpenHardwareSourceEntry source)
    {
        return new CatalogImportDiagnostic(
            CatalogDiagnosticSeverity.Error,
            OpenHardwareSourceManifestDiagnosticCodes.UnsupportedSourceMode,
            $"Jameco catalog ingestion only supports curated manual CSV sources; source '{source.SourceId}' used '{source.Mode}'.",
            ProviderName);
    }
}
