using DragonCAD.App.Marketplace;
using DragonCAD.App.Marketplace.Cart;
using DragonCAD.App.Marketplace.Cart.Commands;

namespace DragonCAD.App.Tests.Marketplace.Cart.Commands;

public sealed class MarketplaceCartCommandServiceTests
{
    [Fact]
    public void AddCreatesLineAndReportsUiFacingSummaries()
    {
        MarketplaceCartViewModel cart = new();
        MarketplaceComponentRow row = Row("Digi-Key", "NE555 Timer", "NE555P", stockQuantity: 10, price: 0.42m);
        MarketplaceCartCommandService service = new(cart);

        MarketplaceCartCommandResult result = service.Add(row);

        Assert.True(result.Succeeded);
        MarketplaceCartLine line = Assert.Single(cart.Lines);
        Assert.Equal("NE555 Timer", line.DisplayName);
        Assert.Equal(1, line.Quantity);
        Assert.Equal("$0.42", result.TotalSummary);
        Assert.Equal("NE555 Timer added to BOM cart.", result.StatusMessage);
        Assert.Equal("Added NE555 Timer to BOM cart.", result.ResultSummary);
        Assert.Equal("Review Digi-Key BOM line NE555P.", result.ActionSummary);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void IncrementQuantityRaisesLineQuantityAndRefreshesTotalSummary()
    {
        MarketplaceCartViewModel cart = CartWith(Row("Digi-Key", "NE555 Timer", "NE555P", stockQuantity: 10, price: 0.42m), quantity: 2);
        MarketplaceCartCommandService service = new(cart);

        MarketplaceCartCommandResult result = service.Increment(cart.Lines[0].LineId);

        Assert.True(result.Succeeded);
        Assert.Equal(3, cart.Lines[0].Quantity);
        Assert.Equal("$1.26", result.TotalSummary);
        Assert.Equal("NE555 Timer quantity set to 3.", result.StatusMessage);
        Assert.Equal("Increased NE555 Timer quantity to 3.", result.ResultSummary);
        Assert.Equal("Review Digi-Key BOM line NE555P.", result.ActionSummary);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void DecrementQuantityToZeroRemovesLine()
    {
        MarketplaceCartViewModel cart = CartWith(Row("Adafruit", "Feather ESP32", "3405", stockQuantity: 5, price: 19.95m), quantity: 1);
        MarketplaceCartCommandService service = new(cart);

        MarketplaceCartCommandResult result = service.Decrement(cart.Lines[0].LineId);

        Assert.True(result.Succeeded);
        Assert.Empty(cart.Lines);
        Assert.Equal("$0.00", result.TotalSummary);
        Assert.Equal("Feather ESP32 removed from BOM cart.", result.StatusMessage);
        Assert.Equal("Removed Feather ESP32 from BOM cart.", result.ResultSummary);
        Assert.Equal("Review BOM cart before export or checkout.", result.ActionSummary);
    }

    [Fact]
    public void DecrementQuantityReportsUiFacingSummaries()
    {
        MarketplaceCartViewModel cart = CartWith(Row("Mouser", "LM7805", "LM7805CT", stockQuantity: 20, price: 0.81m), quantity: 3);
        MarketplaceCartCommandService service = new(cart);

        MarketplaceCartCommandResult result = service.Decrement(cart.Lines[0].LineId);

        Assert.True(result.Succeeded);
        Assert.Equal(2, cart.Lines[0].Quantity);
        Assert.Equal("$1.62", result.TotalSummary);
        Assert.Equal("LM7805 quantity set to 2.", result.StatusMessage);
        Assert.Equal("Decreased LM7805 quantity to 2.", result.ResultSummary);
        Assert.Equal("Review Mouser BOM line LM7805CT.", result.ActionSummary);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void SetQuantityRejectsInvalidTextQuantity()
    {
        MarketplaceCartViewModel cart = CartWith(Row("Mouser", "LM7805", "LM7805CT", stockQuantity: 20, price: 0.81m), quantity: 2);
        MarketplaceCartCommandService service = new(cart);

        MarketplaceCartCommandResult result = service.SetQuantity(cart.Lines[0].LineId, "two");

        Assert.False(result.Succeeded);
        Assert.Equal(2, cart.Lines[0].Quantity);
        MarketplaceCartCommandDiagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("InvalidQuantity", diagnostic.Code);
        Assert.Equal("Quantity must be a whole number greater than or equal to zero.", diagnostic.Message);
        Assert.Equal("$1.62", result.TotalSummary);
    }

    [Fact]
    public void SetQuantityRejectsOverStockQuantity()
    {
        MarketplaceCartViewModel cart = CartWith(Row("SparkFun", "USB-C Breakout", "BOB-15100", stockQuantity: 3, price: 5.95m), quantity: 2);
        MarketplaceCartCommandService service = new(cart);

        MarketplaceCartCommandResult result = service.SetQuantity(cart.Lines[0].LineId, "4");

        Assert.False(result.Succeeded);
        Assert.Equal(2, cart.Lines[0].Quantity);
        MarketplaceCartCommandDiagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("InsufficientStock", diagnostic.Code);
        Assert.Equal("USB-C Breakout only has 3 available.", diagnostic.Message);
        Assert.Equal("$11.90", result.TotalSummary);
    }

    [Fact]
    public void RemoveDeletesLineAndReportsTotalSummary()
    {
        MarketplaceCartViewModel cart = new();
        cart.AddItem(Row("Digi-Key", "NE555 Timer", "NE555P", stockQuantity: 10, price: 0.42m), quantity: 5);
        cart.AddItem(Row("Mouser", "LM7805", "LM7805CT", stockQuantity: 20, price: 0.81m), quantity: 2);
        MarketplaceCartCommandService service = new(cart);

        MarketplaceCartCommandResult result = service.Remove(cart.Lines.Single(line => line.Provider == "Digi-Key").LineId);

        Assert.True(result.Succeeded);
        MarketplaceCartLine line = Assert.Single(cart.Lines);
        Assert.Equal("Mouser", line.Provider);
        Assert.Equal("$1.62", result.TotalSummary);
        Assert.Equal("NE555 Timer removed from BOM cart.", result.StatusMessage);
        Assert.Equal("Removed NE555 Timer from BOM cart.", result.ResultSummary);
        Assert.Equal("Review BOM cart before export or checkout.", result.ActionSummary);
    }

    [Fact]
    public void SetQuantityUpdatesTotalSummaryAfterCommand()
    {
        MarketplaceCartViewModel cart = CartWith(Row("Jameco", "10k Resistor", "CF14JT10K0", stockQuantity: 500, price: 0.02m), quantity: 10);
        MarketplaceCartCommandService service = new(cart);

        MarketplaceCartCommandResult result = service.SetQuantity(cart.Lines[0].LineId, "25");

        Assert.True(result.Succeeded);
        Assert.Equal(25, cart.Lines[0].Quantity);
        Assert.Equal("$0.50", result.TotalSummary);
        Assert.Equal("10k Resistor quantity set to 25.", result.StatusMessage);
    }

    private static MarketplaceCartViewModel CartWith(MarketplaceComponentRow row, int quantity)
    {
        MarketplaceCartViewModel cart = new();
        cart.AddItem(row, quantity);
        return cart;
    }

    private static MarketplaceComponentRow Row(
        string provider,
        string displayName,
        string manufacturerPartNumber,
        int stockQuantity,
        decimal price) =>
        new(
            Provider: provider,
            Category: "IC",
            DisplayName: displayName,
            Manufacturer: "Dragon Test",
            ManufacturerPartNumber: manufacturerPartNumber,
            CanonicalComponentId: $"dragon:{manufacturerPartNumber.ToLowerInvariant()}",
            DuplicateOfComponentId: "",
            DatasheetUrl: "",
            StockQuantity: stockQuantity,
            MinimumUnitPriceUsd: price);
}
