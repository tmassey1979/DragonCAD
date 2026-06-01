namespace DragonCAD.Sourcing;

public static class SourcingProviderCatalog
{
    public static IReadOnlyList<SourcingProviderDescriptor> DefaultProviders { get; } =
    [
        new("Digi-Key", SupportsPricing: true, SupportsStock: true, SupportsDatasheetSearch: true),
        new("Mouser", SupportsPricing: true, SupportsStock: true, SupportsDatasheetSearch: true),
        new("Jameco", SupportsPricing: true, SupportsStock: true, SupportsDatasheetSearch: true),
        new("SparkFun", SupportsPricing: false, SupportsStock: true, SupportsDatasheetSearch: true),
        new("Adafruit", SupportsPricing: false, SupportsStock: true, SupportsDatasheetSearch: true),
    ];
}
