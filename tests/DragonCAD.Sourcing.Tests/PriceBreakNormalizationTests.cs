using DragonCAD.Sourcing;

namespace DragonCAD.Sourcing.Tests;

public sealed class PriceBreakNormalizationTests
{
    [Fact]
    public void PriceLadderSortsBreaksByQuantityAndPrice()
    {
        var ladder = PriceLadder.Normalize(
        [
            new QuantityPriceBreak(100, Money.Usd(0.08m)),
            new QuantityPriceBreak(1, Money.Usd(0.14m)),
            new QuantityPriceBreak(10, Money.Usd(0.10m)),
        ]);

        Assert.Equal([1, 10, 100], ladder.Breaks.Select(priceBreak => priceBreak.Quantity));
    }

    [Fact]
    public void PriceLadderKeepsBestPriceWhenDuplicateQuantitiesAreProvided()
    {
        var ladder = PriceLadder.Normalize(
        [
            new QuantityPriceBreak(10, Money.Usd(0.11m)),
            new QuantityPriceBreak(10, Money.Usd(0.09m)),
            new QuantityPriceBreak(1, Money.Usd(0.15m)),
        ]);

        Assert.Equal(2, ladder.Breaks.Count);
        Assert.Equal(Money.Usd(0.09m), ladder.Breaks[1].UnitPrice);
    }

    [Theory]
    [InlineData(1, 0.14)]
    [InlineData(9, 0.14)]
    [InlineData(10, 0.10)]
    [InlineData(99, 0.10)]
    [InlineData(100, 0.08)]
    public void PriceLadderFindsBestPriceForRequestedQuantity(int requestedQuantity, decimal expectedUnitPrice)
    {
        var ladder = PriceLadder.Normalize(
        [
            new QuantityPriceBreak(1, Money.Usd(0.14m)),
            new QuantityPriceBreak(10, Money.Usd(0.10m)),
            new QuantityPriceBreak(100, Money.Usd(0.08m)),
        ]);

        var priceBreak = ladder.FindBestBreakFor(requestedQuantity);

        Assert.Equal(Money.Usd(expectedUnitPrice), priceBreak.UnitPrice);
    }

    [Fact]
    public void PriceLadderRejectsEmptyBreaks()
    {
        var exception = Assert.Throws<ArgumentException>(() => PriceLadder.Normalize([]));

        Assert.Contains("At least one price break is required.", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void QuantityBreakRejectsNonPositiveQuantities(int quantity)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new QuantityPriceBreak(quantity, Money.Usd(0.10m)));

        Assert.Contains("Quantity must be greater than zero.", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-4)]
    public void PriceLadderRejectsNonPositiveRequestedQuantities(int requestedQuantity)
    {
        var ladder = PriceLadder.Normalize([new QuantityPriceBreak(1, Money.Usd(0.14m))]);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => ladder.FindBestBreakFor(requestedQuantity));

        Assert.Contains("Requested quantity must be greater than zero.", exception.Message);
    }
}
