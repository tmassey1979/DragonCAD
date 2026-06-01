using DragonCAD.Fabrication.Outputs;

namespace DragonCAD.Fabrication.Ordering.Review;

public sealed record FabricationReviewWarning(
    string Code,
    string Message,
    ManufacturingFileRole? FileRole);
