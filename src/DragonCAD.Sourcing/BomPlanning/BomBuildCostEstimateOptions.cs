namespace DragonCAD.Sourcing.BomPlanning;

public sealed record BomBuildCostEstimateOptions
{
    public BomBuildCostEstimateOptions(
        string currencyCode,
        string cultureName,
        DateTimeOffset estimateAt,
        TimeSpan maxQuoteAge)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            throw new ArgumentException("Currency code is required.", nameof(currencyCode));
        }

        if (string.IsNullOrWhiteSpace(cultureName))
        {
            throw new ArgumentException("Culture name is required.", nameof(cultureName));
        }

        if (maxQuoteAge <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(maxQuoteAge), maxQuoteAge, "Maximum quote age must be greater than zero.");
        }

        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        CultureName = cultureName.Trim();
        EstimateAt = estimateAt;
        MaxQuoteAge = maxQuoteAge;
    }

    public string CurrencyCode { get; }

    public string CultureName { get; }

    public DateTimeOffset EstimateAt { get; }

    public TimeSpan MaxQuoteAge { get; }
}
