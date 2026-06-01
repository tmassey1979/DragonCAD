using System.Globalization;
using System.Text;
using System.Text.Json;
using DragonCAD.Sourcing.Catalog.Http;

namespace DragonCAD.Sourcing.Catalog.Mouser;

public sealed class MouserSearchClient
{
    private const string ProviderName = "Mouser";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient httpClient;
    private readonly MouserSearchClientOptions options;
    private readonly VendorHttpRetryPolicy retryPolicy;

    public MouserSearchClient(
        HttpClient httpClient,
        MouserSearchClientOptions options,
        VendorHttpRetryPolicy? retryPolicy = null)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.retryPolicy = retryPolicy ?? new VendorHttpRetryPolicy();
    }

    public Task<CatalogImportResult> SearchByPartNumberAsync(
        string partNumber,
        int limit,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(partNumber);
        return SearchAsync(options.EffectivePartNumberEndpoint, "mouserPartNumber", partNumber.Trim(), limit, cancellationToken);
    }

    public Task<CatalogImportResult> SearchByKeywordAsync(
        string keyword,
        int limit,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyword);
        return SearchAsync(options.EffectiveKeywordEndpoint, "searchByKeywordRequest", keyword.Trim(), limit, cancellationToken);
    }

    private async Task<CatalogImportResult> SearchAsync(
        Uri endpoint,
        string searchFieldName,
        string searchText,
        int limit,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            return DiagnosticResult(
                MouserCatalogDiagnosticCodes.MissingCredentials,
                "Mouser API key is missing. Configure api_key before searching.");
        }

        using var response = await retryPolicy
            .SendAsync(httpClient, () => BuildRequest(endpoint, searchFieldName, searchText, limit), cancellationToken)
            .ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            return DiagnosticResult(
                MouserCatalogDiagnosticCodes.HttpFailure,
                $"Mouser search failed with HTTP {(int)response.StatusCode} {response.ReasonPhrase}.");
        }

        return Parse(body);
    }

    private HttpRequestMessage BuildRequest(Uri endpoint, string searchFieldName, string searchText, int limit)
    {
        var requestUri = AppendApiKey(endpoint);
        var body = JsonSerializer.Serialize(
            new Dictionary<string, object?>
            {
                [searchFieldName] = searchText,
                ["records"] = Math.Clamp(limit, 1, 50),
                ["startingRecord"] = 0,
                ["searchOptions"] = "None",
                ["searchWithYourSignUpLanguage"] = "false",
            },
            JsonOptions);

        return new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
    }

    private Uri AppendApiKey(Uri endpoint)
    {
        var separator = string.IsNullOrEmpty(endpoint.Query) ? "?" : "&";
        return new Uri($"{endpoint}{separator}apiKey={Uri.EscapeDataString(options.ApiKey)}");
    }

    private static CatalogImportResult Parse(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var diagnostics = new List<CatalogImportDiagnostic>();
            var listings = new List<NormalizedCatalogListing>();

            if (!TryGetParts(document.RootElement, out var parts))
            {
                return new CatalogImportResult([], diagnostics);
            }

            foreach (var part in parts.EnumerateArray())
            {
                if (TryMapPart(part, diagnostics, out var listing))
                {
                    listings.Add(listing);
                }
            }

            return new CatalogImportResult(listings, diagnostics);
        }
        catch (JsonException exception)
        {
            return DiagnosticResult(
                MouserCatalogDiagnosticCodes.InvalidJson,
                $"Mouser returned malformed JSON: {exception.Message}");
        }
    }

    private static bool TryMapPart(
        JsonElement part,
        ICollection<CatalogImportDiagnostic> diagnostics,
        out NormalizedCatalogListing listing)
    {
        var vendorSku = ReadString(part, "MouserPartNumber");
        var manufacturerPartNumber = ReadString(part, "ManufacturerPartNumber");
        var manufacturer = ReadString(part, "Manufacturer");
        var priceBreaks = ReadPriceBreaks(part);

        if (string.IsNullOrWhiteSpace(vendorSku) ||
            string.IsNullOrWhiteSpace(manufacturerPartNumber) ||
            string.IsNullOrWhiteSpace(manufacturer) ||
            priceBreaks.Count == 0)
        {
            diagnostics.Add(new CatalogImportDiagnostic(
                CatalogDiagnosticSeverity.Warning,
                MouserCatalogDiagnosticCodes.UnusablePart,
                "Mouser part was skipped because it did not include a SKU, manufacturer part number, manufacturer, or pricing.",
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
            ReadString(part, "Description"),
            PriceLadder.Normalize(priceBreaks),
            ParseStockQuantity(ReadString(part, "AvailabilityInStock")),
            ReadUri(part, "DataSheetUrl"),
            ReadUri(part, "ProductDetailUrl"),
            ReadFields(part),
            CatalogProviderCapabilities.Api);
        return true;
    }

    private static IReadOnlyList<QuantityPriceBreak> ReadPriceBreaks(JsonElement part)
    {
        if (!TryGetArray(part, "PriceBreaks", out var pricing))
        {
            return [];
        }

        var priceBreaks = new List<QuantityPriceBreak>();
        foreach (var price in pricing.EnumerateArray())
        {
            var quantity = ReadInt(price, "Quantity");
            var unitPrice = ParsePrice(ReadString(price, "Price"));
            var currency = ReadString(price, "Currency");
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

        AddIfPresent(fields, "Category", ReadString(part, "Category"));
        AddIfPresent(fields, "Packaging", ReadString(part, "Packaging"));
        AddIfPresent(fields, "LifecycleStatus", ReadString(part, "LifecycleStatus"));
        AddIfPresent(fields, "RohsStatus", ReadString(part, "ROHSStatus"));
        AddIfPresent(fields, "LeadTime", ReadString(part, "LeadTime"));
        AddIfPresent(fields, "MinimumOrderQuantity", ReadString(part, "Min"));
        AddIfPresent(fields, "Mult", ReadString(part, "Mult"));
        AddIfPresent(fields, "ImagePath", ReadString(part, "ImagePath"));

        return fields;
    }

    private static bool TryGetParts(JsonElement root, out JsonElement parts)
    {
        if (root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty("SearchResults", out var searchResults) &&
            TryGetArray(searchResults, "Parts", out parts))
        {
            return true;
        }

        parts = default;
        return false;
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

    private static string ReadString(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out var property))
        {
            return string.Empty;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString() ?? string.Empty,
            JsonValueKind.Number => property.GetRawText(),
            _ => string.Empty,
        };
    }

    private static Uri? ReadUri(JsonElement element, string propertyName)
    {
        return Uri.TryCreate(ReadString(element, propertyName), UriKind.Absolute, out var uri)
            ? uri
            : null;
    }

    private static int? ReadInt(JsonElement element, string propertyName)
    {
        var raw = ReadString(element, propertyName);
        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    private static decimal? ParsePrice(string value)
    {
        var normalized = value.Replace("$", string.Empty, StringComparison.Ordinal).Trim();
        return decimal.TryParse(
            normalized,
            NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands,
            CultureInfo.InvariantCulture,
            out var price)
            ? price
            : null;
    }

    private static int? ParseStockQuantity(string availability)
    {
        var digits = new string(availability.TakeWhile(character => char.IsDigit(character) || character == ',').ToArray());
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

    private static CatalogImportResult DiagnosticResult(string code, string message)
    {
        return new CatalogImportResult(
            [],
            [new CatalogImportDiagnostic(CatalogDiagnosticSeverity.Error, code, message, ProviderName, null)]);
    }
}
