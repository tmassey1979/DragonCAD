using DragonCAD.Fabrication.Outputs;

namespace DragonCAD.Fabrication.Packages;

public sealed record ManufacturingPackageArtifact(
    ManufacturingPackageArtifactKind Kind,
    ManufacturingRelativePath RelativePath,
    ManufacturingChecksum Checksum,
    long Length);
