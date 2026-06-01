using System.Globalization;
using DragonCAD.Sourcing;
using DragonCAD.Sourcing.Bom;

namespace DragonCAD.App.Marketplace.Bom;

public sealed class MarketplaceBomCostRollupViewModel
{
    private MarketplaceBomCostRollupViewModel(
        IReadOnlyList<MarketplaceBomCostRollupRow> rows,
        IReadOnlyList<MarketplaceBomDiagnosticRow> diagnostics,
        IReadOnlyList<MarketplaceBomProviderSummaryRow> providerSummaries,
        string totalSummary,
        bool isComplete)
    {
        Rows = rows;
        Diagnostics = diagnostics;
        ProviderSummaries = providerSummaries;
        TotalSummary = totalSummary;
        IsComplete = isComplete;
    }

    public IReadOnlyList<MarketplaceBomCostRollupRow> Rows { get; }

    public IReadOnlyList<MarketplaceBomDiagnosticRow> Diagnostics { get; }

    public IReadOnlyList<MarketplaceBomProviderSummaryRow> ProviderSummaries { get; }

    public string TotalSummary { get; }

    public bool IsComplete { get; }

    public static MarketplaceBomCostRollupViewModel FromRollup(BomCostRollup rollup)
    {
        ArgumentNullException.ThrowIfNull(rollup);

        MarketplaceBomDiagnosticRow[] diagnostics = rollup.Diagnostics
            .Select(MarketplaceBomDiagnosticRow.FromDiagnostic)
            .ToArray();

        MarketplaceBomCostRollupRow[] rows = rollup.Lines
            .Select(line => MarketplaceBomCostRollupRow.FromLine(line, diagnostics))
            .ToArray();

        MarketplaceBomProviderSummaryRow[] providerSummaries = rollup.ProviderSummaries
            .Select(MarketplaceBomProviderSummaryRow.FromSummary)
            .ToArray();

        return new MarketplaceBomCostRollupViewModel(
            rows,
            diagnostics,
            providerSummaries,
            FormatTotalSummary(rollup.TotalEstimatedCost, rows.Length, diagnostics.Length),
            rollup.IsComplete);
    }

    private static string FormatTotalSummary(Money total, int componentCount, int diagnosticCount)
    {
        string summary = $"Total: {MarketplaceBomMoneyFormatter.FormatAmount(total)} across {componentCount:N0} {Pluralize(componentCount, "component")}";
        if (diagnosticCount > 0)
        {
            summary += $", {diagnosticCount:N0} {Pluralize(diagnosticCount, "diagnostic")}";
        }

        return summary;
    }

    private static string Pluralize(int count, string singular) => count == 1 ? singular : $"{singular}s";
}

public sealed record MarketplaceBomCostRollupRow(
    string ComponentId,
    string ComponentName,
    int Quantity,
    string SelectedProvider,
    string SelectedSku,
    string SelectedUnitCost,
    string SelectedExtendedCost,
    IReadOnlyList<MarketplaceBomAlternativeOfferRow> AlternativeOffers,
    IReadOnlyList<string> Diagnostics)
{
    public static MarketplaceBomCostRollupRow FromLine(
        BomCostRollupLine line,
        IReadOnlyList<MarketplaceBomDiagnosticRow> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(line);
        ArgumentNullException.ThrowIfNull(diagnostics);

        BomProviderOffer? selectedOffer = line.SelectedOffer;
        MarketplaceBomDiagnosticRow[] rowDiagnostics = diagnostics
            .Where(diagnostic => diagnostic.ComponentId == line.Component.Reference
                && diagnostic.ComponentName == line.Component.ManufacturerPartNumber)
            .ToArray();

        MarketplaceBomAlternativeOfferRow[] alternativeOffers = line.ProviderOffers
            .Where(offer => !IsSameOffer(offer, selectedOffer))
            .Select(MarketplaceBomAlternativeOfferRow.FromOffer)
            .ToArray();

        return new MarketplaceBomCostRollupRow(
            ComponentId: line.Component.Reference,
            ComponentName: line.Component.ManufacturerPartNumber,
            Quantity: line.Component.Quantity,
            SelectedProvider: selectedOffer?.ProviderName ?? "Unpriced",
            SelectedSku: selectedOffer?.VendorSku ?? string.Empty,
            SelectedUnitCost: selectedOffer is null ? "$0.00" : MarketplaceBomMoneyFormatter.FormatUnitCost(selectedOffer),
            SelectedExtendedCost: selectedOffer is null ? "$0.00" : MarketplaceBomMoneyFormatter.FormatAmount(selectedOffer.ExtendedCost),
            AlternativeOffers: alternativeOffers,
            Diagnostics: rowDiagnostics
                .Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")
                .ToArray());
    }

    private static bool IsSameOffer(BomProviderOffer offer, BomProviderOffer? selectedOffer) =>
        selectedOffer is not null
        && string.Equals(offer.ProviderName, selectedOffer.ProviderName, StringComparison.OrdinalIgnoreCase)
        && string.Equals(offer.VendorSku, selectedOffer.VendorSku, StringComparison.OrdinalIgnoreCase);
}

public sealed record MarketplaceBomAlternativeOfferRow(
    string Provider,
    string Sku,
    string UnitCost,
    string ExtendedCost,
    string Availability)
{
    public static MarketplaceBomAlternativeOfferRow FromOffer(BomProviderOffer offer)
    {
        ArgumentNullException.ThrowIfNull(offer);

        return new MarketplaceBomAlternativeOfferRow(
            offer.ProviderName,
            offer.VendorSku,
            MarketplaceBomMoneyFormatter.FormatUnitCost(offer),
            MarketplaceBomMoneyFormatter.FormatAmount(offer.ExtendedCost),
            FormatAvailability(offer.StockQuantity));
    }

    private static string FormatAvailability(int? stockQuantity) =>
        stockQuantity is null ? "Stock not reported" : $"{stockQuantity.Value:N0} in stock";
}

public sealed record MarketplaceBomDiagnosticRow(
    string Code,
    string ComponentId,
    string ComponentName,
    int Quantity,
    string Message)
{
    public static MarketplaceBomDiagnosticRow FromDiagnostic(BomCostRollupDiagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);

        return new MarketplaceBomDiagnosticRow(
            diagnostic.Code.ToString(),
            diagnostic.Reference,
            diagnostic.ManufacturerPartNumber,
            diagnostic.RequiredQuantity,
            diagnostic.Message);
    }
}

public sealed record MarketplaceBomProviderSummaryRow(
    string Provider,
    int SelectedLineCount,
    string TotalEstimatedCost,
    string Summary)
{
    public static MarketplaceBomProviderSummaryRow FromSummary(BomProviderSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);

        string lineLabel = summary.SelectedLineCount == 1 ? "line" : "lines";
        string cost = MarketplaceBomMoneyFormatter.FormatAmount(summary.TotalEstimatedCost);

        return new MarketplaceBomProviderSummaryRow(
            summary.ProviderName,
            summary.SelectedLineCount,
            cost,
            $"{summary.ProviderName}: {summary.SelectedLineCount:N0} {lineLabel}, {cost}");
    }
}

internal static class MarketplaceBomMoneyFormatter
{
    public static string FormatAmount(Money money)
    {
        if (string.Equals(money.CurrencyCode, "USD", StringComparison.OrdinalIgnoreCase))
        {
            return string.Create(CultureInfo.InvariantCulture, $"${money.Amount:0.00}");
        }

        return string.Create(CultureInfo.InvariantCulture, $"{money.CurrencyCode} {money.Amount:0.00}");
    }

    public static string FormatUnitCost(BomProviderOffer offer) =>
        $"{FormatAmount(offer.UnitPrice)} ea @ {offer.SelectedPriceBreakQuantity:N0}+";
}
