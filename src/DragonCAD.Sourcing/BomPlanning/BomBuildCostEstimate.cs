namespace DragonCAD.Sourcing.BomPlanning;

public sealed record BomBuildCostEstimate(
    DateTimeOffset EstimateAt,
    string CurrencyCode,
    IReadOnlyList<BomBuildCostEstimateScenario> Scenarios,
    IReadOnlyList<BomBuildCostUnavailableLine> UnavailableLines,
    IReadOnlyList<BomBuildCostDiagnostic> Diagnostics)
{
    public bool IsComplete => UnavailableLines.Count == 0;
}

public sealed record BomBuildCostEstimateScenario(
    int BuildQuantity,
    Money ExtendedBuildTotal,
    string FormattedExtendedBuildTotal,
    IReadOnlyList<BomBuildCostEstimateLine> Lines);

public sealed record BomBuildCostEstimateLine(
    int BuildQuantity,
    string GroupKey,
    string VendorName,
    string VendorPartNumber,
    string ManufacturerPartNumber,
    bool IsPreferredVendor,
    bool IsAlternate,
    int RequiredQuantity,
    int PurchaseQuantity,
    Money UnitPrice,
    Money LineTotal,
    string FormattedLineTotal,
    int AvailableStock,
    int LeadTimeDays,
    BomPartLifecycle Lifecycle,
    string? PreferredVendorNote,
    IReadOnlyList<BomBuildCostDiagnostic> Diagnostics);

public sealed record BomBuildCostUnavailableLine(
    int BuildQuantity,
    string GroupKey,
    int RequiredQuantity,
    int AvailableQuantity,
    string Message);

public sealed record BomBuildCostDiagnostic(
    string Code,
    int BuildQuantity,
    string GroupKey,
    string Message);
