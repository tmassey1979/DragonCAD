using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace DragonCAD.App.Marketplace.Cart;

public sealed class MarketplaceCartViewModel : INotifyPropertyChanged
{
    private readonly Dictionary<string, MarketplaceCartLine> linesById = new(StringComparer.Ordinal);

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<MarketplaceCartLine> Lines { get; } = [];

    public ObservableCollection<MarketplaceCartVendorGroup> VendorGroups { get; } = [];

    public ObservableCollection<MarketplaceCartDiagnostic> Diagnostics { get; } = [];

    public decimal TotalUsd => Lines.Sum(line => line.SubtotalUsd);

    public string TotalSummary => TotalUsd.ToString("$0.00##", CultureInfo.InvariantCulture);

    public void AddItem(MarketplaceComponentRow row, int quantity = 1)
    {
        ArgumentNullException.ThrowIfNull(row);

        if (!CanAdd(row, quantity, out string reason))
        {
            Diagnostics.Add(new MarketplaceCartDiagnostic(
                Code: "Unavailable",
                Message: $"{row.DisplayName} cannot be added to the BOM cart: {reason}.",
                Provider: row.Provider,
                ManufacturerPartNumber: row.ManufacturerPartNumber));
            return;
        }

        string lineId = BuildLineId(row);
        if (linesById.TryGetValue(lineId, out MarketplaceCartLine? existingLine))
        {
            existingLine.Quantity += quantity;
        }
        else
        {
            linesById.Add(lineId, MarketplaceCartLine.FromRow(lineId, row, quantity));
        }

        RefreshRows();
    }

    public void UpdateQuantity(string lineId, int quantity)
    {
        if (!linesById.TryGetValue(lineId, out MarketplaceCartLine? line))
        {
            return;
        }

        if (quantity <= 0)
        {
            linesById.Remove(lineId);
            RefreshRows();
            return;
        }

        if (quantity > line.AvailableQuantity)
        {
            Diagnostics.Add(new MarketplaceCartDiagnostic(
                Code: "InsufficientStock",
                Message: $"{line.DisplayName} only has {line.AvailableQuantity.ToString("N0", CultureInfo.CurrentCulture)} available.",
                Provider: line.Provider,
                ManufacturerPartNumber: line.ManufacturerPartNumber));
            return;
        }

        line.Quantity = quantity;
        RefreshRows();
    }

    public void RemoveLine(string lineId)
    {
        if (linesById.Remove(lineId))
        {
            RefreshRows();
        }
    }

    private void RefreshRows()
    {
        MarketplaceCartLine[] sortedLines = linesById.Values
            .OrderBy(line => line.Provider, StringComparer.Ordinal)
            .ThenBy(line => line.DisplayName, StringComparer.Ordinal)
            .ThenBy(line => line.ManufacturerPartNumber, StringComparer.Ordinal)
            .ToArray();

        Lines.Clear();
        foreach (MarketplaceCartLine line in sortedLines)
        {
            Lines.Add(line);
        }

        VendorGroups.Clear();
        foreach (IGrouping<string, MarketplaceCartLine> providerGroup in sortedLines.GroupBy(line => line.Provider).OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            VendorGroups.Add(new MarketplaceCartVendorGroup(providerGroup.Key, providerGroup.ToArray()));
        }

        OnPropertyChanged(nameof(TotalUsd));
        OnPropertyChanged(nameof(TotalSummary));
    }

    private static bool CanAdd(MarketplaceComponentRow row, int quantity, out string reason)
    {
        if (quantity <= 0)
        {
            reason = "quantity must be greater than zero";
            return false;
        }

        if (row.StockQuantity <= 0)
        {
            reason = "it is out of stock";
            return false;
        }

        if (row.MinimumUnitPriceUsd is null)
        {
            reason = "price is unavailable";
            return false;
        }

        if (quantity > row.StockQuantity)
        {
            reason = "requested quantity exceeds available stock";
            return false;
        }

        reason = "";
        return true;
    }

    private static string BuildLineId(MarketplaceComponentRow row) =>
        string.Join("|", row.Provider, row.ManufacturerPartNumber, row.CanonicalComponentId);

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class MarketplaceCartLine
{
    private int quantity;

    private MarketplaceCartLine(
        string lineId,
        string provider,
        string displayName,
        string manufacturer,
        string manufacturerPartNumber,
        string canonicalComponentId,
        int availableQuantity,
        decimal unitPriceUsd,
        int quantity)
    {
        LineId = lineId;
        Provider = provider;
        DisplayName = displayName;
        Manufacturer = manufacturer;
        ManufacturerPartNumber = manufacturerPartNumber;
        CanonicalComponentId = canonicalComponentId;
        AvailableQuantity = availableQuantity;
        UnitPriceUsd = unitPriceUsd;
        this.quantity = quantity;
    }

    public string LineId { get; }

    public string Provider { get; }

    public string DisplayName { get; }

    public string Manufacturer { get; }

    public string ManufacturerPartNumber { get; }

    public string CanonicalComponentId { get; }

    public int AvailableQuantity { get; }

    public decimal UnitPriceUsd { get; }

    public int Quantity
    {
        get => quantity;
        set => quantity = value;
    }

    public decimal SubtotalUsd => UnitPriceUsd * Quantity;

    public string UnitPriceSummary => UnitPriceUsd.ToString("$0.00##", CultureInfo.InvariantCulture);

    public string SubtotalSummary => SubtotalUsd.ToString("$0.00##", CultureInfo.InvariantCulture);

    public static MarketplaceCartLine FromRow(string lineId, MarketplaceComponentRow row, int quantity) =>
        new(
            lineId,
            row.Provider,
            row.DisplayName,
            row.Manufacturer,
            row.ManufacturerPartNumber,
            row.CanonicalComponentId,
            row.StockQuantity,
            row.MinimumUnitPriceUsd.GetValueOrDefault(),
            quantity);
}

public sealed record MarketplaceCartVendorGroup(string Provider, IReadOnlyList<MarketplaceCartLine> Lines)
{
    public decimal SubtotalUsd => Lines.Sum(line => line.SubtotalUsd);

    public string SubtotalSummary => SubtotalUsd.ToString("$0.00##", CultureInfo.InvariantCulture);
}

public sealed record MarketplaceCartDiagnostic(
    string Code,
    string Message,
    string Provider,
    string ManufacturerPartNumber);
