using System.Globalization;
using System.Text;
using DragonCAD.Sourcing;

namespace DragonCAD.Sourcing.Catalog.Jameco;

public static class JamecoCsvManualFeedParser
{
    private const string ProviderName = "Jameco";

    private static readonly CatalogProviderCapabilities Capabilities =
        CatalogProviderCapabilities.Feed |
        CatalogProviderCapabilities.Manual |
        CatalogProviderCapabilities.ScrapeRestricted;

    public static CatalogImportBatch Parse(string csv, DateTimeOffset retrievedAt)
    {
        ArgumentNullException.ThrowIfNull(csv);

        var records = ReadRecords(csv);
        if (records.Count == 0)
        {
            return new CatalogImportBatch(ProviderName, Capabilities, [], []);
        }

        var header = BuildHeaderIndex(records[0]);
        var items = new List<VendorCatalogItem>();
        var diagnostics = new List<CatalogImportDiagnostic>();
        var seenSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 1; index < records.Count; index++)
        {
            var row = records[index];
            if (IsBlankRow(row))
            {
                continue;
            }

            var rowNumber = index + 1;
            var sku = ReadValue(row, header, "jamecosku", "sku", "productid", "jamecoproductid");
            var title = ReadValue(row, header, "title", "description", "productdescription");

            if (string.IsNullOrWhiteSpace(sku))
            {
                diagnostics.Add(Error(JamecoCatalogDiagnosticCodes.MissingSku, rowNumber, "Jameco SKU or product id is required."));
                continue;
            }

            sku = NormalizeText(sku);

            if (string.IsNullOrWhiteSpace(title))
            {
                diagnostics.Add(Error(JamecoCatalogDiagnosticCodes.MissingTitle, rowNumber, "Title or description is required.", sku));
                continue;
            }

            if (!seenSkus.Add(sku))
            {
                diagnostics.Add(Error(JamecoCatalogDiagnosticCodes.DuplicateSku, rowNumber, "Duplicate Jameco SKU was skipped.", sku));
                continue;
            }

            var stockText = ReadValue(row, header, "stock", "stockquantity", "quantityavailable", "availablequantity");
            if (!TryParseStock(stockText, out var stockQuantity))
            {
                diagnostics.Add(Error(JamecoCatalogDiagnosticCodes.InvalidStockQuantity, rowNumber, "Stock quantity must be a non-negative whole number.", sku));
                continue;
            }

            var priceText = ReadValue(row, header, "unitprice", "price", "unitcost");
            if (!TryParseUnitPrice(priceText, out var unitPrice))
            {
                diagnostics.Add(Error(JamecoCatalogDiagnosticCodes.InvalidUnitPrice, rowNumber, "Unit price must be a non-negative decimal amount.", sku));
                continue;
            }

            var manufacturerPartNumber = ReadValue(row, header, "manufacturerpartnumber", "mpn", "mfrpartnumber");
            var manufacturer = ReadValue(row, header, "manufacturer", "mfr");
            var productUrl = ReadUri(ReadValue(row, header, "producturl", "url", "productlink"));
            var datasheetUrl = ReadUri(ReadValue(row, header, "datasheeturl", "datasheet", "datasheetlink"));

            items.Add(new VendorCatalogItem(
                providerName: ProviderName,
                vendorSku: sku,
                manufacturerPartNumber: NormalizeText(manufacturerPartNumber),
                manufacturer: string.IsNullOrWhiteSpace(manufacturer) ? ProviderName : NormalizeText(manufacturer),
                description: NormalizeText(title),
                priceBreaks: [new QuantityPriceBreak(1, Money.Usd(unitPrice))],
                stockQuantity: stockQuantity,
                datasheetUrl: datasheetUrl,
                productUrl: productUrl,
                fields: BuildFields(sku, retrievedAt, priceText),
                sourceCapabilities: Capabilities));
        }

        return new CatalogImportBatch(ProviderName, Capabilities, items, diagnostics);
    }

    private static CatalogImportDiagnostic Error(string code, int rowNumber, string message, string? vendorSku = null)
    {
        return new CatalogImportDiagnostic(
            CatalogDiagnosticSeverity.Error,
            code,
            $"Row {rowNumber}: {message}",
            ProviderName,
            vendorSku);
    }

    private static Dictionary<string, string> BuildFields(string sku, DateTimeOffset retrievedAt, string unitPrice)
    {
        var fields = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["JamecoProductId"] = sku,
            ["RetrievedAtUtc"] = retrievedAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
        };

        var normalizedUnitPrice = NormalizeText(unitPrice);
        if (normalizedUnitPrice.Length > 0)
        {
            fields["UnitPrice"] = normalizedUnitPrice;
        }

        return fields;
    }

    private static Dictionary<string, int> BuildHeaderIndex(IReadOnlyList<string> header)
    {
        var index = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < header.Count; i++)
        {
            var normalized = NormalizeHeader(header[i]);
            if (normalized.Length > 0 && !index.ContainsKey(normalized))
            {
                index[normalized] = i;
            }
        }

        return index;
    }

    private static string ReadValue(IReadOnlyList<string> row, IReadOnlyDictionary<string, int> header, params string[] names)
    {
        foreach (var name in names)
        {
            if (header.TryGetValue(name, out var index) && index < row.Count)
            {
                return row[index].Trim();
            }
        }

        return string.Empty;
    }

    private static bool TryParseStock(string value, out int stockQuantity)
    {
        var normalized = NormalizeText(value);
        return int.TryParse(normalized, NumberStyles.None, CultureInfo.InvariantCulture, out stockQuantity) &&
            stockQuantity >= 0;
    }

    private static bool TryParseUnitPrice(string value, out decimal unitPrice)
    {
        var normalized = NormalizeText(value);
        return decimal.TryParse(
                normalized,
                NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands | NumberStyles.AllowCurrencySymbol,
                CultureInfo.InvariantCulture,
                out unitPrice) &&
            unitPrice >= 0;
    }

    private static Uri? ReadUri(string value)
    {
        var normalized = NormalizeText(value);
        return Uri.TryCreate(normalized, UriKind.Absolute, out var uri) ? uri : null;
    }

    private static string NormalizeHeader(string value)
    {
        return new string(value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());
    }

    private static string NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : string.Join(' ', value.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
    }

    private static bool IsBlankRow(IEnumerable<string> row)
    {
        return row.All(string.IsNullOrWhiteSpace);
    }

    private static List<List<string>> ReadRecords(string csv)
    {
        var records = new List<List<string>>();
        var record = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < csv.Length; i++)
        {
            var current = csv[i];
            if (current == '"')
            {
                if (inQuotes && i + 1 < csv.Length && csv[i + 1] == '"')
                {
                    field.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (current == ',' && !inQuotes)
            {
                record.Add(field.ToString());
                field.Clear();
                continue;
            }

            if ((current == '\r' || current == '\n') && !inQuotes)
            {
                if (current == '\r' && i + 1 < csv.Length && csv[i + 1] == '\n')
                {
                    i++;
                }

                record.Add(field.ToString());
                field.Clear();
                AddRecord(records, record);
                record = [];
                continue;
            }

            field.Append(current);
        }

        record.Add(field.ToString());
        AddRecord(records, record);

        return records;
    }

    private static void AddRecord(ICollection<List<string>> records, List<string> record)
    {
        if (record.Count > 1 || !string.IsNullOrWhiteSpace(record[0]))
        {
            records.Add(record);
        }
    }
}
