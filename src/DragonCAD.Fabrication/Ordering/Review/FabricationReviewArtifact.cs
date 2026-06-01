using DragonCAD.Fabrication.Outputs;

namespace DragonCAD.Fabrication.Ordering.Review;

public sealed record FabricationReviewArtifact(
    ManufacturingFileRole Role,
    bool IsPresent,
    IReadOnlyList<ManufacturingRelativePath> RelativePaths);
