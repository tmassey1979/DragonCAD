using System.Globalization;

namespace DragonCAD.App.Marketplace.Cart.Commands;

public sealed class MarketplaceCartCommandService
{
    private readonly MarketplaceCartViewModel cart;

    public MarketplaceCartCommandService(MarketplaceCartViewModel cart)
    {
        this.cart = cart;
    }

    public MarketplaceCartCommandResult Add(MarketplaceComponentRow row, int quantity = 1)
    {
        ArgumentNullException.ThrowIfNull(row);

        cart.AddItem(row, quantity);
        MarketplaceCartLine? line = FindLine(BuildLineId(row));
        if (line is null)
        {
            MarketplaceCartDiagnostic? diagnostic = cart.Diagnostics.LastOrDefault(
                item => string.Equals(item.Provider, row.Provider, StringComparison.Ordinal) &&
                    string.Equals(item.ManufacturerPartNumber, row.ManufacturerPartNumber, StringComparison.Ordinal));

            return Failure(
                $"{row.DisplayName} was not added to BOM cart.",
                diagnostic is null
                    ? new MarketplaceCartCommandDiagnostic(
                        Code: "Unavailable",
                        Message: $"{row.DisplayName} could not be added to the BOM cart.",
                        LineId: BuildLineId(row),
                        Provider: row.Provider,
                        ManufacturerPartNumber: row.ManufacturerPartNumber)
                    : new MarketplaceCartCommandDiagnostic(
                        Code: diagnostic.Code,
                        Message: diagnostic.Message,
                        LineId: BuildLineId(row),
                        Provider: diagnostic.Provider,
                        ManufacturerPartNumber: diagnostic.ManufacturerPartNumber));
        }

        return Success(
            $"{row.DisplayName} added to BOM cart.",
            $"Added {row.DisplayName} to BOM cart.",
            LineActionSummary(line));
    }

    public MarketplaceCartCommandResult Increment(string lineId)
    {
        MarketplaceCartLine? line = FindLine(lineId);
        if (line is null)
        {
            return NotFound(lineId);
        }

        return SetQuantity(
            lineId,
            (line.Quantity + 1).ToString(CultureInfo.InvariantCulture),
            "Increased");
    }

    public MarketplaceCartCommandResult Decrement(string lineId)
    {
        MarketplaceCartLine? line = FindLine(lineId);
        if (line is null)
        {
            return NotFound(lineId);
        }

        return SetQuantity(
            lineId,
            (line.Quantity - 1).ToString(CultureInfo.InvariantCulture),
            "Decreased");
    }

    public MarketplaceCartCommandResult SetQuantity(string lineId, string quantityText) =>
        SetQuantity(lineId, quantityText, "Set");

    private MarketplaceCartCommandResult SetQuantity(string lineId, string quantityText, string quantityVerb)
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
            return Success(
                $"{displayName} removed from BOM cart.",
                $"Removed {displayName} from BOM cart.",
                "Review BOM cart before export or checkout.");
        }

        cart.UpdateQuantity(lineId, quantity);
        MarketplaceCartLine updatedLine = FindLine(lineId) ?? line;
        string quantitySummary = quantity.ToString("N0", CultureInfo.InvariantCulture);
        string resultVerb = quantityVerb == "Set" ? "Set" : quantityVerb;
        return Success(
            $"{displayName} quantity set to {quantitySummary}.",
            $"{resultVerb} {displayName} quantity to {quantitySummary}.",
            LineActionSummary(updatedLine));
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
        return Success(
            $"{displayName} removed from BOM cart.",
            $"Removed {displayName} from BOM cart.",
            "Review BOM cart before export or checkout.");
    }

    private MarketplaceCartLine? FindLine(string lineId) =>
        cart.Lines.FirstOrDefault(line => string.Equals(line.LineId, lineId, StringComparison.Ordinal));

    private static string BuildLineId(MarketplaceComponentRow row) =>
        string.Join("|", row.Provider, row.ManufacturerPartNumber, row.CanonicalComponentId);

    private static string LineActionSummary(MarketplaceCartLine line) =>
        $"Review {line.Provider} BOM line {line.ManufacturerPartNumber}.";

    private MarketplaceCartCommandResult Success(
        string statusMessage,
        string resultSummary,
        string actionSummary) =>
        new(
            Succeeded: true,
            StatusMessage: statusMessage,
            TotalSummary: cart.TotalSummary,
            ResultSummary: resultSummary,
            ActionSummary: actionSummary,
            Diagnostics: []);

    private MarketplaceCartCommandResult Failure(string statusMessage, MarketplaceCartCommandDiagnostic diagnostic) =>
        new(
            Succeeded: false,
            StatusMessage: statusMessage,
            TotalSummary: cart.TotalSummary,
            ResultSummary: statusMessage,
            ActionSummary: "Resolve cart command diagnostics before retrying.",
            Diagnostics: [diagnostic]);

    private MarketplaceCartCommandResult NotFound(string lineId) =>
        new(
            Succeeded: false,
            StatusMessage: "Cart line was not found.",
            TotalSummary: cart.TotalSummary,
            ResultSummary: "Cart line was not found.",
            ActionSummary: "Refresh BOM cart and try again.",
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
    string ResultSummary,
    string ActionSummary,
    IReadOnlyList<MarketplaceCartCommandDiagnostic> Diagnostics);

public sealed record MarketplaceCartCommandDiagnostic(
    string Code,
    string Message,
    string LineId,
    string Provider,
    string ManufacturerPartNumber);
