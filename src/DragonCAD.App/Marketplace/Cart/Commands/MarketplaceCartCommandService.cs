using System.Globalization;

namespace DragonCAD.App.Marketplace.Cart.Commands;

public sealed class MarketplaceCartCommandService
{
    private readonly MarketplaceCartViewModel cart;

    public MarketplaceCartCommandService(MarketplaceCartViewModel cart)
    {
        this.cart = cart;
    }

    public MarketplaceCartCommandResult Increment(string lineId)
    {
        MarketplaceCartLine? line = FindLine(lineId);
        if (line is null)
        {
            return NotFound(lineId);
        }

        return SetQuantity(lineId, (line.Quantity + 1).ToString(CultureInfo.InvariantCulture));
    }

    public MarketplaceCartCommandResult Decrement(string lineId)
    {
        MarketplaceCartLine? line = FindLine(lineId);
        if (line is null)
        {
            return NotFound(lineId);
        }

        return SetQuantity(lineId, (line.Quantity - 1).ToString(CultureInfo.InvariantCulture));
    }

    public MarketplaceCartCommandResult SetQuantity(string lineId, string quantityText)
    {
        MarketplaceCartLine? line = FindLine(lineId);
        if (line is null)
        {
            return NotFound(lineId);
        }

        if (!int.TryParse(quantityText, NumberStyles.None, CultureInfo.InvariantCulture, out int quantity) || quantity < 0)
        {
            return Failure(
                "Quantity was not changed.",
                new MarketplaceCartCommandDiagnostic(
                    Code: "InvalidQuantity",
                    Message: "Quantity must be a whole number greater than or equal to zero.",
                    LineId: lineId,
                    Provider: line.Provider,
                    ManufacturerPartNumber: line.ManufacturerPartNumber));
        }

        if (quantity > line.AvailableQuantity)
        {
            return Failure(
                "Quantity was not changed.",
                new MarketplaceCartCommandDiagnostic(
                    Code: "InsufficientStock",
                    Message: $"{line.DisplayName} only has {line.AvailableQuantity.ToString("N0", CultureInfo.InvariantCulture)} available.",
                    LineId: lineId,
                    Provider: line.Provider,
                    ManufacturerPartNumber: line.ManufacturerPartNumber));
        }

        string displayName = line.DisplayName;
        if (quantity == 0)
        {
            cart.RemoveLine(lineId);
            return Success($"{displayName} removed from BOM cart.");
        }

        cart.UpdateQuantity(lineId, quantity);
        return Success($"{displayName} quantity set to {quantity.ToString("N0", CultureInfo.InvariantCulture)}.");
    }

    public MarketplaceCartCommandResult Remove(string lineId)
    {
        MarketplaceCartLine? line = FindLine(lineId);
        if (line is null)
        {
            return NotFound(lineId);
        }

        string displayName = line.DisplayName;
        cart.RemoveLine(lineId);
        return Success($"{displayName} removed from BOM cart.");
    }

    private MarketplaceCartLine? FindLine(string lineId) =>
        cart.Lines.FirstOrDefault(line => string.Equals(line.LineId, lineId, StringComparison.Ordinal));

    private MarketplaceCartCommandResult Success(string statusMessage) =>
        new(
            Succeeded: true,
            StatusMessage: statusMessage,
            TotalSummary: cart.TotalSummary,
            Diagnostics: []);

    private MarketplaceCartCommandResult Failure(string statusMessage, MarketplaceCartCommandDiagnostic diagnostic) =>
        new(
            Succeeded: false,
            StatusMessage: statusMessage,
            TotalSummary: cart.TotalSummary,
            Diagnostics: [diagnostic]);

    private MarketplaceCartCommandResult NotFound(string lineId) =>
        new(
            Succeeded: false,
            StatusMessage: "Cart line was not found.",
            TotalSummary: cart.TotalSummary,
            Diagnostics:
            [
                new MarketplaceCartCommandDiagnostic(
                    Code: "LineNotFound",
                    Message: "Cart line was not found.",
                    LineId: lineId,
                    Provider: "",
                    ManufacturerPartNumber: "")
            ]);
}

public sealed record MarketplaceCartCommandResult(
    bool Succeeded,
    string StatusMessage,
    string TotalSummary,
    IReadOnlyList<MarketplaceCartCommandDiagnostic> Diagnostics);

public sealed record MarketplaceCartCommandDiagnostic(
    string Code,
    string Message,
    string LineId,
    string Provider,
    string ManufacturerPartNumber);
