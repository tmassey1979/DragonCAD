namespace DragonCAD.Fabrication.Outputs.Summary;

public sealed record ManufacturingManifestReviewWarning(
    string Code,
    ManufacturingManifestSummaryRole Role,
    string Message,
    string? RelativePath = null);
