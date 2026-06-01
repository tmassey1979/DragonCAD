namespace DragonCAD.Sourcing;

public sealed record SourcingProviderDescriptor(
    string Name,
    bool SupportsPricing,
    bool SupportsStock,
    bool SupportsDatasheetSearch);
