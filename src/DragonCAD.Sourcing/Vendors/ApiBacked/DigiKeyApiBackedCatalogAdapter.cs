using System.Globalization;
using System.Text.Json;
using DragonCAD.Sourcing.Catalog;
using DragonCAD.Sourcing.Catalog.DigiKey;
using DragonCAD.Sourcing.Marketplace;
using DragonCAD.Sourcing.Providers;

namespace DragonCAD.Sourcing.Vendors.ApiBacked;

public sealed class DigiKeyApiBackedCatalogAdapter : IApiBackedCatalogAdapter
{
    private const string ProviderName = "Digi-Key";

    public ApiBackedProviderDiagnostics Diagnostics { get; } = new(
        ProviderName,
        new VendorRateLimitMetadata(60, null, false, "Respect Digi-Key Product Information API account throttles and retry-after headers."),
        ["client_id", "access_token"],
        MarketplaceProviderTerms.AllowsCatalogCache | MarketplaceProviderTerms.RequiresAttribution | MarketplaceProviderTerms.RequiresSourceUrl,
        "Digi-Key catalog records require source attribution and product URL provenance.");

    public ApiBackedCatalogIngestionResult MapSearchFixture(string json)
    {
        return MapFixture(json, root => EnumerateSearchProducts(root));
    }

    public ApiBackedCatalogIngestionResult MapDetailFixture(string json)
    {
        return MapFixture(json, root => EnumerateDetailProducts(root));
    }

    private static ApiBackedCatalogIngestionResult MapFixture(
        string json,
        Func<JsonElement, IEnumerable<JsonElement>> enumerateProducts)
    {
        ArgumentNullException.ThrowIfNull(json);

        try
        {
            using var document = JsonDocument.Parse(json);
            var diagnostics = new List<CatalogImportDiagnostic>();
            var records = new List<VendorCatalogItem>();

            foreach (var product in enumerateProducts(document.RootElement))
            {
                if (TryMapProduct(product, diagnostics, out var record))
                {
                    records.Add(record);
                }
            }

            return new ApiBackedCatalogIngestionResult(records, diagnostics);
        }
        catch (JsonException exception)
        {
            return DiagnosticResult(DigiKeyCatalogDiagnosticCodes.InvalidJson, $"Digi-Key fixture is malformed JSON: {exception.Message}");
        }
    }

    private static IEnumerable<JsonElement> EnumerateSearchProducts(JsonElement root)
    {
        if (ApiBackedJson.TryGetArray(root, "Products", out var products))
        {
            foreach (var product in products.EnumerateArray())
            {
                if (product.ValueKind == JsonValueKind.Object)
                {
                    yield return product;
                }
            }
        }
    }

    private static IEnumerable<JsonElement> EnumerateDetailProducts(JsonElement root)
    {
        if (ApiBackedJson.TryGetProperty(root, "Product", out var product) && product.ValueKind == JsonValueKind.Object)
        {
            yield return product;
            yield break;
        }

        if (root.ValueKind == JsonValueKind.Object)
        {
            yield return root;
        }
    }

    private static bool TryMapProduct(
        JsonElement product,
        ICollection<CatalogImportDiagnostic> diagnostics,
        out VendorCatalogItem record)
    {
        var vendorSku = ApiBackedJson.GetText(product, "DigiKeyProductNumber");
        var manufacturerPartNumber = ApiBackedJson.GetText(product, "ManufacturerProductNumber");
        var manufacturer = ApiBackedJson.GetNestedText(product, "Manufacturer", "Name");
        var priceBreaks = ReadPriceBreaks(product);

        if (string.IsNullOrWhiteSpace(vendorSku) ||
            string.IsNullOrWhiteSpace(manufacturerPartNumber) ||
            string.IsNullOrWhiteSpace(manufacturer) ||
            priceBreaks.Count == 0)
        {
            diagnostics.Add(new CatalogImportDiagnostic(
                CatalogDiagnosticSeverity.Warning,
                DigiKeyCatalogDiagnosticCodes.UnusableProduct,
                "Digi-Key fixture product was skipped because it did not include a SKU, manufacturer part number, manufacturer, or standard pricing.",
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
            ApiBackedJson.GetNestedText(product, "Description", "ProductDescription") ?? string.Empty,
            priceBreaks,
            ApiBackedJson.GetInt(product, "QuantityAvailable"),
            ApiBackedJson.GetUri(product, "DatasheetUrl"),
            ApiBackedJson.GetUri(product, "ProductUrl"),
            ReadFields(product),
            CatalogProviderCapabilities.Api);
        return true;
    }

    private static IReadOnlyList<QuantityPriceBreak> ReadPriceBreaks(JsonElement product)
    {
        if (!ApiBackedJson.TryGetArray(product, "StandardPricing", out var pricing))
        {
            return [];
        }

        var priceBreaks = new List<QuantityPriceBreak>();
        foreach (var price in pricing.EnumerateArray())
        {
            var quantity = ApiBackedJson.GetInt(price, "BreakQuantity");
            var unitPrice = ApiBackedJson.GetDecimal(price, "UnitPrice");
            if (quantity is > 0 && unitPrice is >= 0)
            {
                priceBreaks.Add(new QuantityPriceBreak(quantity.Value, Money.Usd(unitPrice.Value)));
            }
        }

        return priceBreaks;
    }

    private static IReadOnlyDictionary<string, string> ReadFields(JsonElement product)
    {
        var fields = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["SourceApi"] = "Digi-Key Product Information V4",
        };

        if (ApiBackedJson.TryGetArray(product, "ProductVariations", out var variations))
        {
            var firstVariation = variations.EnumerateArray().FirstOrDefault();
            AddIfPresent(fields, "PackageType", ApiBackedJson.GetNestedText(firstVariation, "PackageType", "Name"));
            AddIfPresent(fields, "MinimumOrderQuantity", ApiBackedJson.GetInt(firstVariation, "MinimumOrderQuantity")?.ToString(CultureInfo.InvariantCulture));
        }

        AddIfPresent(fields, "ProductStatus", ApiBackedJson.GetNestedText(product, "ProductStatus", "Status"));
        AddIfPresent(fields, "Category", ApiBackedJson.GetNestedText(product, "Category", "Name"));
        AddIfPresent(fields, "Series", ApiBackedJson.GetNestedText(product, "Series", "Name"));

        return fields;
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
