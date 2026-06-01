using System.Globalization;
using DragonCAD.Sourcing.Catalog;
using DragonCAD.Sourcing.Catalog.Sync;

namespace DragonCAD.App.Marketplace.Sync.Results;

public sealed class VendorCatalogSyncResultViewModel
{
    private VendorCatalogSyncResultViewModel(
        string providerName,
        string query,
        IReadOnlyList<VendorCatalogSyncResultRow> resultRows,
        IReadOnlyList<VendorCatalogSyncDiagnosticRow> diagnostics)
    {
        ProviderName = providerName;
        Query = query;
        ResultRows = resultRows;
        Diagnostics = diagnostics;
    }

    public string ProviderName { get; }

    public string Query { get; }

    public IReadOnlyList<VendorCatalogSyncResultRow> ResultRows { get; }

    public IReadOnlyList<VendorCatalogSyncDiagnosticRow> Diagnostics { get; }

    public string Summary
    {
        get
        {
            int totalRows = ResultRows.Count + Diagnostics.Count;
            return $"{totalRows:N0} total rows: {ResultRows.Count:N0} {Pluralize(ResultRows.Count, "result")}, {Diagnostics.Count:N0} {Pluralize(Diagnostics.Count, "diagnostic")}";
        }
    }

    public static VendorCatalogSyncResultViewModel Empty { get; } = new(
        string.Empty,
        string.Empty,
        [],
        []);

    public static VendorCatalogSyncResultViewModel FromRunResult(VendorCatalogSyncRunResult runResult)
    {
        ArgumentNullException.ThrowIfNull(runResult);

        return new VendorCatalogSyncResultViewModel(
            runResult.ProviderName,
            runResult.Query,
            runResult.Listings.Select(VendorCatalogSyncResultRow.FromListing).ToArray(),
            runResult.Diagnostics.Select(VendorCatalogSyncDiagnosticRow.FromDiagnostic).ToArray());
    }

    private static string Pluralize(int count, string singular) => count == 1 ? singular : $"{singular}s";
}

public sealed record VendorCatalogSyncResultRow(
    string ProviderName,
    string VendorSku,
    string ManufacturerPartNumber,
    string Manufacturer,
    string Description,
    string StockPriceSummary,
    string PackageSummary,
    string DatasheetUrl,
    string ProductUrl)
{
    public bool HasDatasheet => !string.IsNullOrWhiteSpace(DatasheetUrl);

    public static VendorCatalogSyncResultRow FromListing(NormalizedCatalogListing listing)
    {
        ArgumentNullException.ThrowIfNull(listing);

        return new VendorCatalogSyncResultRow(
            listing.ProviderName,
            listing.VendorSku,
            listing.ManufacturerPartNumber,
            listing.Manufacturer,
            listing.Description,
            FormatStockPrice(listing),
            ReadPackageSummary(listing),
            listing.DatasheetUrl?.ToString() ?? string.Empty,
            listing.ProductUrl?.ToString() ?? string.Empty);
    }

    private static string FormatStockPrice(NormalizedCatalogListing listing)
    {
        string stock = listing.StockQuantity is null
            ? "stock unknown"
            : $"{listing.StockQuantity.Value.ToString("N0", CultureInfo.CurrentCulture)} in stock";
        string price = listing.PriceLadder.Breaks.Count == 0
            ? "price unavailable"
            : $"from {FormatPrice(listing.PriceLadder.Breaks[0].UnitPrice.Amount)}";

        return $"{stock} {price}";
    }

    private static string ReadPackageSummary(NormalizedCatalogListing listing)
    {
        if (listing.Fields.TryGetValue("PackageType", out var packageType))
        {
            return packageType;
        }

        if (listing.Fields.TryGetValue("Packaging", out var packaging))
        {
            return packaging;
        }

        return "Package unknown";
    }

    private static string FormatPrice(decimal price) =>
        price.ToString("$0.00##", CultureInfo.InvariantCulture);
}

public sealed record VendorCatalogSyncDiagnosticRow(
    string Severity,
    string Code,
    string Message,
    string VendorSku)
{
    public static VendorCatalogSyncDiagnosticRow FromDiagnostic(CatalogImportDiagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);

        return new VendorCatalogSyncDiagnosticRow(
            diagnostic.Severity.ToString(),
            diagnostic.Code,
            diagnostic.Message,
            diagnostic.VendorSku ?? string.Empty);
    }
}
