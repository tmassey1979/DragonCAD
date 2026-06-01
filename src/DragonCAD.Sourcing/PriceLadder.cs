namespace DragonCAD.Sourcing;

public sealed record PriceLadder
{
    private PriceLadder(IReadOnlyList<QuantityPriceBreak> breaks)
    {
        Breaks = breaks;
    }

    public IReadOnlyList<QuantityPriceBreak> Breaks { get; }

    public static PriceLadder Normalize(IEnumerable<QuantityPriceBreak> priceBreaks)
    {
        ArgumentNullException.ThrowIfNull(priceBreaks);

        var normalizedBreaks = priceBreaks
            .GroupBy(priceBreak => priceBreak.Quantity)
            .Select(group => group
                .OrderBy(priceBreak => priceBreak.UnitPrice.Amount)
                .ThenBy(priceBreak => priceBreak.UnitPrice.CurrencyCode, StringComparer.OrdinalIgnoreCase)
                .First())
            .OrderBy(priceBreak => priceBreak.Quantity)
            .ThenBy(priceBreak => priceBreak.UnitPrice.Amount)
            .ThenBy(priceBreak => priceBreak.UnitPrice.CurrencyCode, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalizedBreaks.Length == 0)
        {
            throw new ArgumentException("At least one price break is required.", nameof(priceBreaks));
        }

        return new PriceLadder(normalizedBreaks);
    }

    public QuantityPriceBreak FindBestBreakFor(int requestedQuantity)
    {
        if (requestedQuantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(requestedQuantity), requestedQuantity, "Requested quantity must be greater than zero.");
        }

        var selectedBreak = Breaks[0];
        foreach (var priceBreak in Breaks)
        {
            if (priceBreak.Quantity > requestedQuantity)
            {
                return selectedBreak;
            }

            selectedBreak = priceBreak;
        }

        return selectedBreak;
    }
}
