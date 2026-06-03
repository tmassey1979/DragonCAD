using System.Globalization;
using System.Text.Json;
using DragonCAD.Sourcing.Catalog;
using DragonCAD.Sourcing.Catalog.Mouser;
using DragonCAD.Sourcing.Marketplace;
using DragonCAD.Sourcing.Providers;

namespace DragonCAD.Sourcing.Vendors.ApiBacked;

public sealed class MouserApiBackedCatalogAdapter : IApiBackedCatalogAdapter
{
    private const string ProviderName = "Mouser";

    public ApiBackedProviderDiagnostics Diagnostics { get; } = new(
        ProviderName,
        new VendorRateLimitMetadata(30, null, false, "Respect Mouser Search API account throttles and response metadata."),
        ["api_key"],
        MarketplaceProviderTerms.AllowsCatalogCache | MarketplaceProviderTerms.RequiresSourceUrl | MarketplaceProviderTerms.RequiresAttribution,
        "Mouser catalog records require product detail URL provenance and vendor attribution.");

    public ApiBackedCatalogIngestionResult MapSearchFixture(string json)
    {
        return MapFixture(json, root => EnumerateSearchParts(root));
    }

    public ApiBackedCatalogIngestionResult MapDetailFixture(string json)
    {
        return MapFixture(json, root => EnumerateDetailParts(root));
    }

    private static ApiBackedCatalogIngestionResult MapFixture(
        string json,
        Func<JsonElement, IEnumerable<JsonElement>> enumerateParts)
    {
        ArgumentNullException.ThrowIfNull(json);

        try
        {
            using var document = JsonDocument.Parse(json);
            var diagnostics = new List<CatalogImportDiagnostic>();
            var records = new List<VendorCatalogItem>();

            foreach (var part in enumerateParts(document.RootElement))
            {
                if (TryMapPart(part, diagnostics, out var record))
                {
                    records.Add(record);
                }
            }

            return new ApiBackedCatalogIngestionResult(records, diagnostics);
        }
        catch (JsonException exception)
        {
            return DiagnosticResult(MouserCatalogDiagnosticCodes.InvalidJson, $"Mouser fixture is malformed JSON: {exception.Message}");
        }
    }

    private static IEnumerable<JsonElement> EnumerateSearchParts(JsonElement root)
    {
        if (ApiBackedJson.TryGetProperty(root, "SearchResults", out var searchResults) &&
            ApiBackedJson.TryGetArray(searchResults, "Parts", out var parts))
        {
            foreach (var part in parts.EnumerateArray())
            {
                if (part.ValueKind == JsonValueKind.Object)
                {
                    yield return part;
                }
            }
        }
    }

    private static IEnumerable<JsonElement> EnumerateDetailParts(JsonElement root)
    {
        if (ApiBackedJson.TryGetProperty(root, "Part", out var part) && part.ValueKind == JsonValueKind.Object)
        {
            yield return part;
            yield break;
        }

        if (root.ValueKind == JsonValueKind.Object)
        {
            yield return root;
        }
    }

    private static bool TryMapPart(
        JsonElement part,
        ICollection<CatalogImportDiagnostic> diagnostics,
        out VendorCatalogItem record)
    {
        var vendorSku = ApiBackedJson.GetText(part, "MouserPartNumber");
        var manufacturerPartNumber = ApiBackedJson.GetText(part, "ManufacturerPartNumber");
        var manufacturer = ApiBackedJson.GetText(part, "Manufacturer");
        var priceBreaks = ReadPriceBreaks(part);

        if (string.IsNullOrWhiteSpace(vendorSku) ||
            string.IsNullOrWhiteSpace(manufacturerPartNumber) ||
            string.IsNullOrWhiteSpace(manufacturer) ||
            priceBreaks.Count == 0)
        {
            diagnostics.Add(new CatalogImportDiagnostic(
                CatalogDiagnosticSeverity.Warning,
                MouserCatalogDiagnosticCodes.UnusablePart,
                "Mouser fixture part was skipped because it did not include a SKU, manufacturer part number, manufacturer, or pricing.",
                ProviderName,
                vendorSku));
            record = null!;
            return false;
        }

        record = new VendorCatalogItem(
            ProviderName,
            vendorSku,
            manufacturerPartNumber,
            manufacturer,
            ApiBackedJson.GetText(part, "Description") ?? string.Empty,
            priceBreaks,
            ParseStockQuantity(ApiBackedJson.GetText(part, "AvailabilityInStock")),
            ApiBackedJson.GetUri(part, "DataSheetUrl"),
            ApiBackedJson.GetUri(part, "ProductDetailUrl"),
            ReadFields(part),
            CatalogProviderCapabilities.Api);
        return true;
    }

    private static IReadOnlyList<QuantityPriceBreak> ReadPriceBreaks(JsonElement part)
    {
        if (!ApiBackedJson.TryGetArray(part, "PriceBreaks", out var pricing))
        {
            return [];
        }

        var priceBreaks = new List<QuantityPriceBreak>();
        foreach (var price in pricing.EnumerateArray())
        {
            var quantity = ApiBackedJson.GetInt(price, "Quantity");
            var unitPrice = ParsePrice(ApiBackedJson.GetText(price, "Price"));
            var currency = ApiBackedJson.GetText(price, "Currency");
            if (quantity is > 0 && unitPrice is >= 0)
            {
                priceBreaks.Add(new QuantityPriceBreak(quantity.Value, new Money(unitPrice.Value, string.IsNullOrWhiteSpace(currency) ? "USD" : currency)));
            }
        }

        return priceBreaks;
    }

    private static IReadOnlyDictionary<string, string> ReadFields(JsonElement part)
    {
        var fields = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["SourceApi"] = "Mouser Search API",
        };

        AddIfPresent(fields, "Category", ApiBackedJson.GetText(part, "Category"));
        AddIfPresent(fields, "Packaging", ApiBackedJson.GetText(part, "Packaging"));
        AddIfPresent(fields, "LifecycleStatus", ApiBackedJson.GetText(part, "LifecycleStatus"));
        AddIfPresent(fields, "RohsStatus", ApiBackedJson.GetText(part, "ROHSStatus"));
        AddIfPresent(fields, "LeadTime", ApiBackedJson.GetText(part, "LeadTime"));
        AddIfPresent(fields, "MinimumOrderQuantity", ApiBackedJson.GetText(part, "Min"));
        AddIfPresent(fields, "Mult", ApiBackedJson.GetText(part, "Mult"));
        AddIfPresent(fields, "ImagePath", ApiBackedJson.GetText(part, "ImagePath"));

        return fields;
    }

    private static decimal? ParsePrice(string? value)
    {
        var normalized = (value ?? string.Empty).Replace("$", string.Empty, StringComparison.Ordinal).Trim();
        return decimal.TryParse(
            normalized,
            NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands,
            CultureInfo.InvariantCulture,
            out var price)
            ? price
            : null;
    }

    private static int? ParseStockQuantity(string? availability)
    {
        var digits = new string((availability ?? string.Empty).TakeWhile(character => char.IsDigit(character) || character == ',').ToArray());
        var normalized = digits.Replace(",", string.Empty, StringComparison.Ordinal);
        return int.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out var quantity)
            ? quantity
            : null;
    }

    private static void AddIfPresent(IDictionary<string, string> fields, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            fields[key] = value.Trim();
        }
    }

    private static ApiBackedCatalogIngestionResult DiagnosticResult(string code, string message)
    {
        return new ApiBackedCatalogIngestionResult(
            [],
            [new CatalogImportDiagnostic(CatalogDiagnosticSeverity.Error, code, message, ProviderName)]);
    }
}
