using System.Globalization;
using System.Text;

namespace DragonCAD.App.Marketplace.Cart.Export;

public sealed class MarketplaceBomExportPreviewViewModel
{
    private MarketplaceBomExportPreviewViewModel(
        IReadOnlyList<MarketplaceBomExportPreviewRow> rows,
        IReadOnlyList<MarketplaceBomExportDiagnostic> diagnostics,
        string totalSummary)
    {
        Rows = rows;
        Diagnostics = diagnostics;
        TotalSummary = totalSummary;
    }

    public string Header => "Vendor,MPN,Manufacturer,Component,Quantity,Unit Price,Subtotal,Canonical Id";

    public IReadOnlyList<MarketplaceBomExportPreviewRow> Rows { get; }

    public IReadOnlyList<MarketplaceBomExportDiagnostic> Diagnostics { get; }

    public string TotalSummary { get; }

    public IReadOnlyList<string> CsvLines => [Header, .. Rows.Select(row => row.CsvLine)];

    public static MarketplaceBomExportPreviewViewModel FromCart(MarketplaceCartViewModel cart)
    {
        ArgumentNullException.ThrowIfNull(cart);

        MarketplaceBomExportPreviewRow[] rows = cart.Lines
            .Select(MarketplaceBomExportPreviewRow.FromCartLine)
            .ToArray();

        MarketplaceBomExportDiagnostic[] diagnostics = cart.Diagnostics
            .Select(MarketplaceBomExportDiagnostic.FromCartDiagnostic)
            .ToArray();

        return new MarketplaceBomExportPreviewViewModel(rows, diagnostics, FormatCurrency(cart.TotalUsd));
    }

    internal static string FormatCurrency(decimal value) =>
        value.ToString("$0.00##", CultureInfo.InvariantCulture);
}

public sealed record MarketplaceBomExportPreviewRow(
    string Vendor,
    string ManufacturerPartNumber,
    string Manufacturer,
    string Component,
    string Quantity,
    string UnitPrice,
    string Subtotal,
    string CanonicalComponentId)
{
    public string Provider => Vendor;

    public string CsvLine => string.Join(
        ',',
        Escape(Vendor),
        Escape(ManufacturerPartNumber),
        Escape(Manufacturer),
        Escape(Component),
        Quantity,
        UnitPrice,
        Subtotal,
        Escape(CanonicalComponentId));

    public static MarketplaceBomExportPreviewRow FromCartLine(MarketplaceCartLine line)
    {
        ArgumentNullException.ThrowIfNull(line);

        return new MarketplaceBomExportPreviewRow(
            Vendor: line.Provider,
            ManufacturerPartNumber: line.ManufacturerPartNumber,
            Manufacturer: line.Manufacturer,
            Component: line.DisplayName,
            Quantity: line.Quantity.ToString(CultureInfo.InvariantCulture),
            UnitPrice: MarketplaceBomExportPreviewViewModel.FormatCurrency(line.UnitPriceUsd),
            Subtotal: MarketplaceBomExportPreviewViewModel.FormatCurrency(line.SubtotalUsd),
            CanonicalComponentId: line.CanonicalComponentId);
    }

    private static string Escape(string value)
    {
        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n') && !value.Contains('\r'))
        {
            return value;
        }

        StringBuilder builder = new(value.Length + 2);
        builder.Append('"');
        foreach (char character in value)
        {
            if (character == '"')
            {
                builder.Append('"');
            }

            builder.Append(character);
        }

        builder.Append('"');
        return builder.ToString();
    }
}

public sealed record MarketplaceBomExportDiagnostic(
    string Code,
    string Message,
    string Vendor,
    string ManufacturerPartNumber)
{
    public static MarketplaceBomExportDiagnostic FromCartDiagnostic(MarketplaceCartDiagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);

        return new MarketplaceBomExportDiagnostic(
            diagnostic.Code,
            diagnostic.Message,
            diagnostic.Provider,
            diagnostic.ManufacturerPartNumber);
    }
}
