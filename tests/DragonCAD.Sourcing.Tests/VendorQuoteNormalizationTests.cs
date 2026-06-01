using DragonCAD.Sourcing;

namespace DragonCAD.Sourcing.Tests;

public sealed class VendorQuoteNormalizationTests
{
    [Fact]
    public void MoneyRoundsToCurrencyMinorUnits()
    {
        var money = Money.Usd(0.125m);

        Assert.Equal("USD", money.CurrencyCode);
        Assert.Equal(0.13m, money.Amount);
    }

    [Fact]
    public void NormalizedQuotesSortInStockBeforePrice()
    {
        var quotes = new[]
        {
            Quote("Jameco", 250, 0.18m),
            Quote("Mouser", 0, 0.09m),
            Quote("Digi-Key", 50, 0.10m),
        };

        var sorted = VendorQuoteSorter.SortBestFirst(quotes).ToArray();

        Assert.Equal(["Digi-Key", "Jameco", "Mouser"], sorted.Select(quote => quote.VendorName));
    }

    [Theory]
    [InlineData(25, null, VendorAvailability.InStock)]
    [InlineData(0, 14, VendorAvailability.Backorder)]
    [InlineData(0, null, VendorAvailability.Unavailable)]
    public void NormalizedQuotesDeriveAvailabilityFromQuantityAndLeadTime(
        int quantityAvailable,
        int? leadTimeDays,
        VendorAvailability expectedAvailability)
    {
        var quote = new NormalizedVendorQuote(
            VendorName: "Digi-Key",
            VendorPartNumber: "296-LM7805CT-ND",
            ManufacturerPartNumber: "LM7805CT",
            UnitPrice: Money.Usd(0.42m),
            QuantityAvailable: quantityAvailable,
            MinimumOrderQuantity: 1,
            LeadTimeDays: leadTimeDays);

        Assert.Equal(expectedAvailability, quote.Availability);
    }

    [Fact]
    public void NormalizedQuotesUseDeterministicTieBreakers()
    {
        var quotes = new[]
        {
            Quote("mouser", 10, 0.10m, minimumOrderQuantity: 5),
            Quote("Digi-Key", 10, 0.10m, minimumOrderQuantity: 1),
            Quote("Adafruit", 10, 0.10m, minimumOrderQuantity: 1),
        };

        var sorted = VendorQuoteSorter.SortBestFirst(quotes).ToArray();

        Assert.Equal(["Adafruit", "Digi-Key", "mouser"], sorted.Select(quote => quote.VendorName));
    }

    private static NormalizedVendorQuote Quote(
        string vendorName,
        int quantityAvailable,
        decimal unitPrice,
        int minimumOrderQuantity = 1)
    {
        return new NormalizedVendorQuote(
            VendorName: vendorName,
            VendorPartNumber: $"{vendorName}-LM7805",
            ManufacturerPartNumber: "LM7805CT",
            UnitPrice: Money.Usd(unitPrice),
            QuantityAvailable: quantityAvailable,
            MinimumOrderQuantity: minimumOrderQuantity,
            LeadTimeDays: quantityAvailable > 0 ? 0 : 21);
    }
}
