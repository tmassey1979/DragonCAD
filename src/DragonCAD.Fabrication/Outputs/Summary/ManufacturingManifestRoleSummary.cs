namespace DragonCAD.Fabrication.Outputs.Summary;

public sealed record ManufacturingManifestRoleSummary(
    ManufacturingManifestSummaryRole Role,
    IReadOnlyList<string> RelativePaths,
    int FileCount,
    int ChecksumCount,
    bool HasChecksums);
