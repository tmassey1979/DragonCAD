namespace DragonCAD.Sourcing;

public readonly record struct Money
{
    public Money(decimal amount, string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            throw new ArgumentException("Currency code is required.", nameof(currencyCode));
        }

        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
    }

    public decimal Amount { get; }

    public string CurrencyCode { get; }

    public static Money Usd(decimal amount)
    {
        return new Money(amount, "USD");
    }
}
