using System.Globalization;
using System.Text.Json;
using DragonCAD.Sourcing.Catalog;
using DragonCAD.Sourcing.Catalog.Adafruit;
using DragonCAD.Sourcing.Marketplace;
using DragonCAD.Sourcing.Providers;

namespace DragonCAD.Sourcing.Vendors.ApiBacked;

public sealed class AdafruitApiBackedCatalogAdapter : IApiBackedCatalogAdapter
{
    private const string ProviderName = "Adafruit";

    public ApiBackedProviderDiagnostics Diagnostics { get; } = new(
        ProviderName,
        new VendorRateLimitMetadata(null, null, false, "Respect Adafruit IO and product API account limits for authenticated requests."),
        ["api_key"],
        MarketplaceProviderTerms.AllowsCatalogCache | MarketplaceProviderTerms.RequiresAttribution | MarketplaceProviderTerms.RequiresSourceId,
        "Adafruit catalog records require product ID provenance and vendor attribution.");

    public ApiBackedCatalogIngestionResult MapProductFixture(string json, DateTimeOffset retrievedAt)
    {
        ArgumentNullException.ThrowIfNull(json);

        try
        {
            using var document = JsonDocument.Parse(json);
            var diagnostics = new List<CatalogImportDiagnostic>();
            var records = new List<VendorCatalogItem>();

            foreach (var product in EnumerateProducts(document.RootElement))
            {
                if (TryMapProduct(product, retrievedAt, diagnostics, out var record))
                {
                    records.Add(record);
                }
            }

            return new ApiBackedCatalogIngestionResult(records, diagnostics);
        }
        catch (JsonException exception)
        {
            return new ApiBackedCatalogIngestionResult(
                [],
                [new CatalogImportDiagnostic(
                    CatalogDiagnosticSeverity.Error,
                    AdafruitCatalogDiagnosticCodes.MalformedJson,
                    $"Adafruit product API fixture is malformed JSON: {exception.Message}",
                    ProviderName)]);
        }
    }

    private static IEnumerable<JsonElement> EnumerateProducts(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var product in root.EnumerateArray())
            {
                if (product.ValueKind == JsonValueKind.Object)
                {
                    yield return product;
                }
            }

            yield break;
        }

        if (ApiBackedJson.TryGetArray(root, "products", out var products))
        {
            foreach (var product in products.EnumerateArray())
            {
                if (product.ValueKind == JsonValueKind.Object)
                {
                    yield return product;
                }
            }

            yield break;
        }

        if (root.ValueKind == JsonValueKind.Object)
        {
            yield return root;
        }
    }

    private static bool TryMapProduct(
        JsonElement product,
        DateTimeOffset retrievedAt,
        ICollection<CatalogImportDiagnostic> diagnostics,
        out VendorCatalogItem record)
    {
        var productId = ApiBackedJson.GetText(product, "id", "product_id");
        var title = ApiBackedJson.GetText(product, "title", "name");
        var manufacturerPartNumber = ApiBackedJson.GetText(product, "mpn", "manufacturer_part_number", "part_number");
        var price = ApiBackedJson.GetDecimal(product, "price", "current_price");

        if (productId is null || title is null || manufacturerPartNumber is null || price is null)
        {
            diagnostics.Add(new CatalogImportDiagnostic(
                CatalogDiagnosticSeverity.Error,
                AdafruitCatalogDiagnosticCodes.MissingRequiredField,
                "Adafruit product API fixture is missing id, title, manufacturer part number, or price.",
                ProviderName,
                productId is null ? null : BuildVendorSku(productId)));

            record = null!;
            return false;
        }

        var vendorSku = ApiBackedJson.GetText(product, "sku") ?? BuildVendorSku(productId);
        var productUrl = ApiBackedJson.GetUri(product, "url", "product_url", "permalink")
            ?? new Uri($"https://www.adafruit.com/product/{productId}", UriKind.Absolute);
        var learnUrl = ApiBackedJson.GetUri(product, "learn_url", "learn_guide_url");

        record = new VendorCatalogItem(
            ProviderName,
            vendorSku,
            manufacturerPartNumber,
            ApiBackedJson.GetText(product, "manufacturer", "brand") ?? ProviderName,
            title,
            [new QuantityPriceBreak(1, Money.Usd(price.Value))],
            ApiBackedJson.GetInt(product, "stock_quantity", "stock", "quantity_available"),
            ApiBackedJson.GetUri(product, "datasheet_url", "datasheet"),
            productUrl,
            BuildFields(product, productId, retrievedAt, learnUrl),
            CatalogProviderCapabilities.Api);
        return true;
    }

    private static Dictionary<string, string> BuildFields(
        JsonElement product,
        string productId,
        DateTimeOffset retrievedAt,
        Uri? learnUrl)
    {
        var fields = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["ProductId"] = productId,
            ["RetrievedAt"] = retrievedAt.ToString("O", CultureInfo.InvariantCulture),
            ["SourceApi"] = "Adafruit Product API",
        };

        AddField(fields, "LearnUrl", learnUrl?.ToString());
        AddField(fields, "Availability", ApiBackedJson.GetText(product, "availability", "status"));

        if (ApiBackedJson.TryGetProperty(product, "in_stock", out var inStock) &&
            inStock.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            fields["InStock"] = inStock.GetBoolean().ToString(CultureInfo.InvariantCulture).ToLowerInvariant();
        }

        return fields;
    }

    private static string BuildVendorSku(string productId)
    {
        return $"ID-{productId}";
    }

    private static void AddField(Dictionary<string, string> fields, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            fields[name] = value;
        }
    }
}
