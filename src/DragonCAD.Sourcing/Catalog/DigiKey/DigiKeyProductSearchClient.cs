using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DragonCAD.Sourcing.Catalog.Http;

namespace DragonCAD.Sourcing.Catalog.DigiKey;

public sealed class DigiKeyProductSearchClient
{
    private const string ProviderName = "Digi-Key";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient httpClient;
    private readonly DigiKeyProductSearchClientOptions options;
    private readonly VendorHttpRetryPolicy retryPolicy;

    public DigiKeyProductSearchClient(
        HttpClient httpClient,
        DigiKeyProductSearchClientOptions options,
        VendorHttpRetryPolicy? retryPolicy = null)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.retryPolicy = retryPolicy ?? new VendorHttpRetryPolicy();
    }

    public async Task<CatalogImportResult> SearchByKeywordAsync(
        string keyword,
        int limit,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyword);

        if (string.IsNullOrWhiteSpace(options.ClientId) || string.IsNullOrWhiteSpace(options.AccessToken))
        {
            return DiagnosticResult(
                DigiKeyCatalogDiagnosticCodes.MissingCredentials,
                "Digi-Key credentials are missing. Configure client_id and access_token before searching.");
        }

        using var response = await retryPolicy
            .SendAsync(httpClient, () => BuildRequest(keyword.Trim(), limit), cancellationToken)
            .ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            return DiagnosticResult(
                DigiKeyCatalogDiagnosticCodes.HttpFailure,
                $"Digi-Key search failed with HTTP {(int)response.StatusCode} {response.ReasonPhrase}.");
        }

        return Parse(body);
    }

    private HttpRequestMessage BuildRequest(string keyword, int limit)
    {
        var requestBody = JsonSerializer.Serialize(
            new Dictionary<string, object?>
            {
                ["Keywords"] = keyword,
                ["Limit"] = Math.Clamp(limit, 1, 50),
                ["Offset"] = 0,
            });

        var request = new HttpRequestMessage(HttpMethod.Post, options.EffectiveEndpoint)
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json"),
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.AccessToken);
        request.Headers.Add("X-DIGIKEY-Client-Id", options.ClientId);
        request.Headers.Add("X-DIGIKEY-Locale-Site", options.LocaleSite);
        request.Headers.Add("X-DIGIKEY-Locale-Language", options.LocaleLanguage);
        request.Headers.Add("X-DIGIKEY-Locale-Currency", options.LocaleCurrency);

        return request;
    }

    private static CatalogImportResult Parse(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var diagnostics = new List<CatalogImportDiagnostic>();
            var listings = new List<NormalizedCatalogListing>();

            if (!TryGetArray(document.RootElement, "Products", out var products))
            {
                return new CatalogImportResult([], diagnostics);
            }

            foreach (var product in products.EnumerateArray())
            {
                if (TryMapProduct(product, diagnostics, out var listing))
                {
                    listings.Add(listing);
                }
            }

            return new CatalogImportResult(listings, diagnostics);
        }
        catch (JsonException exception)
        {
            return DiagnosticResult(
                DigiKeyCatalogDiagnosticCodes.InvalidJson,
                $"Digi-Key returned malformed JSON: {exception.Message}");
        }
    }

    private static bool TryMapProduct(
        JsonElement product,
        ICollection<CatalogImportDiagnostic> diagnostics,
        out NormalizedCatalogListing listing)
    {
        var vendorSku = ReadString(product, "DigiKeyProductNumber");
        var manufacturerPartNumber = ReadString(product, "ManufacturerProductNumber");
        var manufacturer = ReadNestedString(product, "Manufacturer", "Name");
        var description = ReadNestedString(product, "Description", "ProductDescription");
        var priceBreaks = ReadPriceBreaks(product);

        if (string.IsNullOrWhiteSpace(vendorSku) ||
            string.IsNullOrWhiteSpace(manufacturerPartNumber) ||
            string.IsNullOrWhiteSpace(manufacturer) ||
            priceBreaks.Count == 0)
        {
            diagnostics.Add(new CatalogImportDiagnostic(
                CatalogDiagnosticSeverity.Warning,
                DigiKeyCatalogDiagnosticCodes.UnusableProduct,
                "Digi-Key product was skipped because it did not include a SKU, manufacturer part number, manufacturer, or standard pricing.",
                ProviderName,
                string.IsNullOrWhiteSpace(vendorSku) ? null : vendorSku));
            listing = null!;
            return false;
        }

        listing = new NormalizedCatalogListing(
            ProviderName,
            vendorSku,
            manufacturerPartNumber,
            manufacturer,
            description,
            PriceLadder.Normalize(priceBreaks),
            ReadInt(product, "QuantityAvailable"),
            ReadUri(product, "DatasheetUrl"),
            ReadUri(product, "ProductUrl"),
            ReadFields(product),
            CatalogProviderCapabilities.Api);
        return true;
    }

    private static IReadOnlyList<QuantityPriceBreak> ReadPriceBreaks(JsonElement product)
    {
        if (!TryGetArray(product, "StandardPricing", out var pricing))
        {
            return [];
        }

        var priceBreaks = new List<QuantityPriceBreak>();
        foreach (var price in pricing.EnumerateArray())
        {
            var quantity = ReadInt(price, "BreakQuantity");
            var unitPrice = ReadDecimal(price, "UnitPrice");
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

        if (TryGetArray(product, "ProductVariations", out var variations))
        {
            var firstVariation = variations.EnumerateArray().FirstOrDefault();
            AddIfPresent(fields, "PackageType", ReadNestedString(firstVariation, "PackageType", "Name"));
            AddIfPresent(fields, "MinimumOrderQuantity", ReadInt(firstVariation, "MinimumOrderQuantity")?.ToString(CultureInfo.InvariantCulture));
        }

        AddIfPresent(fields, "ProductStatus", ReadNestedString(product, "ProductStatus", "Status"));
        AddIfPresent(fields, "Category", ReadNestedString(product, "Category", "Name"));
        AddIfPresent(fields, "Series", ReadNestedString(product, "Series", "Name"));

        return fields;
    }

    private static void AddIfPresent(IDictionary<string, string> fields, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            fields[key] = value.Trim();
        }
    }

    private static bool TryGetArray(JsonElement element, string propertyName, out JsonElement array)
    {
        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(propertyName, out array) &&
            array.ValueKind == JsonValueKind.Array)
        {
            return true;
        }

        array = default;
        return false;
    }

    private static string ReadNestedString(JsonElement element, string propertyName, string nestedPropertyName)
    {
        return element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(propertyName, out var nested) &&
            nested.ValueKind == JsonValueKind.Object
            ? ReadString(nested, nestedPropertyName)
            : string.Empty;
    }

    private static string ReadString(JsonElement element, string propertyName)
    {
        return element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;
    }

    private static Uri? ReadUri(JsonElement element, string propertyName)
    {
        return Uri.TryCreate(ReadString(element, propertyName), UriKind.Absolute, out var uri)
            ? uri
            : null;
    }

    private static int? ReadInt(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.Number when property.TryGetInt32(out var value) => value,
            JsonValueKind.String when int.TryParse(property.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) => value,
            _ => null,
        };
    }

    private static decimal? ReadDecimal(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.Number when property.TryGetDecimal(out var value) => value,
            JsonValueKind.String when decimal.TryParse(property.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var value) => value,
            _ => null,
        };
    }

    private static CatalogImportResult DiagnosticResult(string code, string message)
    {
        return new CatalogImportResult(
            [],
            [new CatalogImportDiagnostic(CatalogDiagnosticSeverity.Error, code, message, ProviderName, null)]);
    }
}
