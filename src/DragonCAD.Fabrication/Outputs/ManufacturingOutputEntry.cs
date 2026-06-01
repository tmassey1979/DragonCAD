namespace DragonCAD.Fabrication.Outputs;

public sealed record ManufacturingOutputEntry(
    ManufacturingFileRole Role,
    ManufacturingRelativePath RelativePath,
    ManufacturingChecksum Checksum);
