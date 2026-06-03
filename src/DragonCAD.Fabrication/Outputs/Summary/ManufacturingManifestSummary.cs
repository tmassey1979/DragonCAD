namespace DragonCAD.Fabrication.Outputs.Summary;

public sealed record ManufacturingManifestSummary(
    IReadOnlyList<ManufacturingManifestRoleSummary> RoleSummaries,
    IReadOnlyList<ManufacturingManifestSummaryRole> MissingRequiredRoles,
    IReadOnlyList<ManufacturingManifestReviewWarning> ReviewWarnings,
    int TotalFileCount);
