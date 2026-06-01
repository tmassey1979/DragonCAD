using System.Globalization;
using System.Text.Json;
using DragonCAD.Sourcing;
using DragonCAD.Sourcing.Catalog;

namespace DragonCAD.Sourcing.Catalog.Adafruit;

public static class AdafruitCatalogFixtureParser
{
    private const string ProviderName = "Adafruit";

    public static CatalogImportResult Parse(string json, DateTimeOffset retrievedAt)
    {
        ArgumentNullException.ThrowIfNull(json);

        try
        {
            using var document = JsonDocument.Parse(json);
            return ParseDocument(document.RootElement, retrievedAt);
        }
        catch (JsonException exception)
        {
            return new CatalogImportResult(
                Listings: [],
                Diagnostics:
                [
                    new CatalogImportDiagnostic(
                        CatalogDiagnosticSeverity.Error,
                        AdafruitCatalogDiagnosticCodes.MalformedJson,
                        $"Adafruit catalog fixture is malformed JSON: {exception.Message}",
                        ProviderName)
                ]);
        }
    }

    private static CatalogImportResult ParseDocument(JsonElement root, DateTimeOffset retrievedAt)
    {
        var diagnostics = new List<CatalogImportDiagnostic>();
        var listings = new List<NormalizedCatalogListing>();

        foreach (var product in EnumerateProducts(root))
        {
            var listing = TryParseListing(product, retrievedAt, diagnostics);
            if (listing is not null)
            {
                listings.Add(listing);
            }
        }

        return new CatalogImportResult(listings, diagnostics);
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

        if (root.ValueKind == JsonValueKind.Object
            && TryGetProperty(root, "products", out var products)
            && products.ValueKind == JsonValueKind.Array)
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

    private static NormalizedCatalogListing? TryParseListing(
        JsonElement product,
        DateTimeOffset retrievedAt,
        List<CatalogImportDiagnostic> diagnostics)
    {
        var productId = GetText(product, "id", "product_id");
        var title = GetText(product, "title", "name");
        var manufacturerPartNumber = GetText(product, "mpn", "manufacturer_part_number", "part_number");
        var price = GetDecimal(product, "price", "current_price");

        if (productId is null || title is null || manufacturerPartNumber is null || price is null)
        {
            diagnostics.Add(new CatalogImportDiagnostic(
                CatalogDiagnosticSeverity.Error,
                AdafruitCatalogDiagnosticCodes.MissingRequiredField,
                "Adafruit product fixture is missing id, title, manufacturer part number, or price.",
                ProviderName,
                productId is null ? null : BuildVendorSku(productId)));

            return null;
        }

        var vendorSku = GetText(product, "sku") ?? BuildVendorSku(productId);
        var productUrl = GetUri(product, "url", "product_url", "permalink")
            ?? new Uri($"https://www.adafruit.com/product/{productId}", UriKind.Absolute);
        var datasheetUrl = GetUri(product, "datasheet_url", "datasheet");
        var learnUrl = GetUri(product, "learn_url", "learn_guide_url");
        var stockQuantity = GetStockQuantity(product);
        var fields = BuildFields(product, productId, retrievedAt, learnUrl);

        return new NormalizedCatalogListing(
            ProviderName,
            vendorSku,
            manufacturerPartNumber,
            GetText(product, "manufacturer", "brand") ?? ProviderName,
            title,
            PriceLadder.Normalize([new QuantityPriceBreak(1, Money.Usd(price.Value))]),
            stockQuantity,
            datasheetUrl,
            productUrl,
            fields,
            CatalogProviderCapabilities.Feed);
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
        };

        AddField(fields, "LearnUrl", learnUrl?.ToString());
        AddField(fields, "Availability", GetText(product, "availability", "status"));

        if (TryGetProperty(product, "in_stock", out var inStock) && inStock.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            fields["InStock"] = inStock.GetBoolean().ToString(CultureInfo.InvariantCulture).ToLowerInvariant();
        }

        return fields;
    }

    private static int? GetStockQuantity(JsonElement product)
    {
        var stockQuantity = GetInt(product, "stock_quantity", "stock", "quantity_available");
        if (stockQuantity is not null)
        {
            return stockQuantity;
        }

        if (TryGetProperty(product, "in_stock", out var inStock)
            && inStock.ValueKind == JsonValueKind.False)
        {
            return null;
        }

        return null;
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

    private static Uri? GetUri(JsonElement product, params string[] names)
    {
        var value = GetText(product, names);
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) ? uri : null;
    }

    private static string? GetText(JsonElement product, params string[] names)
    {
        foreach (var name in names)
        {
            if (!TryGetProperty(product, name, out var property))
            {
                continue;
            }

            var value = property.ValueKind switch
            {
                JsonValueKind.String => property.GetString(),
                JsonValueKind.Number => property.GetRawText(),
                JsonValueKind.True => bool.TrueString,
                JsonValueKind.False => bool.FalseString,
                _ => null,
            };

            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static int? GetInt(JsonElement product, params string[] names)
    {
        foreach (var name in names)
        {
            if (!TryGetProperty(product, name, out var property))
            {
                continue;
            }

            if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var numericValue))
            {
                return numericValue;
            }

            if (property.ValueKind == JsonValueKind.String
                && int.TryParse(property.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var stringValue))
            {
                return stringValue;
            }
        }

        return null;
    }

    private static decimal? GetDecimal(JsonElement product, params string[] names)
    {
        foreach (var name in names)
        {
            if (!TryGetProperty(product, name, out var property))
            {
                continue;
            }

            if (property.ValueKind == JsonValueKind.Number && property.TryGetDecimal(out var numericValue))
            {
                return numericValue;
            }

            if (property.ValueKind == JsonValueKind.String
                && decimal.TryParse(property.GetString(), NumberStyles.Currency, CultureInfo.InvariantCulture, out var stringValue))
            {
                return stringValue;
            }
        }

        return null;
    }

    private static bool TryGetProperty(JsonElement product, string name, out JsonElement value)
    {
        foreach (var property in product.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
